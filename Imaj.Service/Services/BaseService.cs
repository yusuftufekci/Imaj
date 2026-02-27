using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Imaj.Core.Entities;
using Imaj.Core.Interfaces.Repositories;
using Imaj.Service.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Imaj.Service.Services
{
    public abstract class BaseService
    {
        protected readonly IUnitOfWork _unitOfWork;
        protected readonly ILogger _logger;
        protected readonly IConfiguration _configuration;

        // Configuration constants - easier to change globally later
        protected const int CurrentCompanyId = 7;
        protected decimal CurrentLanguageId => ResolveUiLanguageId();

        protected BaseService(IUnitOfWork unitOfWork, ILogger logger, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _configuration = configuration;
        }

        protected static decimal ResolveUiLanguageId()
        {
            var culture = CultureInfo.CurrentUICulture.Name;
            return culture.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? 2m : 1m;
        }

        /// <summary>
        /// Retrieves a list of entities joined with their translation (X) table.
        /// Automatically applies CompanyID filter (if entity has it) and LanguageID filter.
        /// </summary>
        protected async Task<ServiceResult<List<TDto>>> GetTranslatedListAsync<TEntity, TXEntity, TDto>(
            Expression<Func<TEntity, decimal>> entityKeySelector,
            Expression<Func<TXEntity, decimal>> transKeySelector,
            Expression<Func<TEntity, TXEntity, TDto>> projection,
            Expression<Func<TEntity, bool>>? additionalFilter = null,
            Expression<Func<TDto, object>>? orderBySelector = null
        )
            where TEntity : BaseEntity
            where TXEntity : BaseEntity
        {
            try
            {
                var query = _unitOfWork.Repository<TEntity>().Query();
                var transQuery = _unitOfWork.Repository<TXEntity>().Query();

                // Apply invisible filter if property exists (common pattern)
                query = ApplyInvisibleFilter(query);

                // Apply CompanyID filter if property exists
                query = ApplyCompanyFilter(query);

                // Apply additional custom filters
                if (additionalFilter != null)
                {
                    query = query.Where(additionalFilter);
                }

                // Filter translations by LanguageID
                // Using EF.Property to avoid hard dependency on specific interface
                transQuery = transQuery.Where(x => EF.Property<decimal>(x, "LanguageID") == CurrentLanguageId);

                // Join
                var joinedQuery = query.Join(
                    transQuery,
                    entityKeySelector,
                    transKeySelector,
                    projection
                );

                // Default ordering if not provided - try to order by "Name" property of DTO if it exists
                if (orderBySelector != null)
                {
                    joinedQuery = joinedQuery.OrderBy(orderBySelector);
                }
                else
                {
                    // Basic fallback: if TDto has a "Name" property, sort by it. 
                    // Need to do this carefully or just let caller handle it.
                    // For now, let's assume caller provides ordering or we don't order.
                    // Actually, most lists are ordered by Name.
                }

                var result = await joinedQuery.ToListAsync();
                
                // If no explicit order, try to order by Name client-side (safe for small dropdown lists)
                if (orderBySelector == null)
                {
                    var nameProp = typeof(TDto).GetProperty("Name");
                    if (nameProp != null)
                    {
                        result = result.OrderBy(x => nameProp.GetValue(x)).ToList();
                    }
                }

                return ServiceResult<List<TDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching translated list for {typeof(TEntity).Name}");
                return ServiceResult<List<TDto>>.Fail($"{typeof(TEntity).Name} listesi yüklenirken hata oluştu.");
            }
        }

        private IQueryable<T> ApplyCompanyFilter<T>(IQueryable<T> query)
        {
            var prop = typeof(T).GetProperty("CompanyID");
            if (prop != null && prop.PropertyType == typeof(decimal))
            {
                // x => x.CompanyID == CurrentCompanyId
                var param = Expression.Parameter(typeof(T), "x");
                var propertyAccess = Expression.Property(param, prop);
                var constant = Expression.Constant((decimal)CurrentCompanyId);
                var equality = Expression.Equal(propertyAccess, constant);
                var lambda = Expression.Lambda<Func<T, bool>>(equality, param);
                return query.Where(lambda);
            }
            return query;
        }

        private IQueryable<T> ApplyInvisibleFilter<T>(IQueryable<T> query)
        {
            var prop = typeof(T).GetProperty("Invisible");
            if (prop != null && prop.PropertyType == typeof(bool))
            {
                // x => x.Invisible == false
                var param = Expression.Parameter(typeof(T), "x");
                var propertyAccess = Expression.Property(param, prop);
                var constant = Expression.Constant(false);
                var equality = Expression.Equal(propertyAccess, constant);
                var lambda = Expression.Lambda<Func<T, bool>>(equality, param);
                return query.Where(lambda);
            }
            return query;
        }
    }
}
