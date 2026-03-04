using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Imaj.Core.Entities;
using Imaj.Core.Interfaces.Repositories;
using Imaj.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Imaj.Data.Repositories
{
    public class Repository<T> : IRepository<T> where T : BaseEntity
    {
        private const int DefaultMaxRows = 500;
        private const int MaxAllowedRows = 5000;

        protected readonly ImajDbContext _context;
        private readonly DbSet<T> _dbSet;

        public Repository(ImajDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public async Task<IEnumerable<T>> GetAllAsync(int maxRows = DefaultMaxRows)
        {
            var boundedMaxRows = maxRows <= 0
                ? DefaultMaxRows
                : Math.Min(maxRows, MaxAllowedRows);

            return await _dbSet
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .Take(boundedMaxRows)
                .ToListAsync();
        }

        public async Task<T?> GetByIdAsync(decimal id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<T?> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
             return await _dbSet.SingleOrDefaultAsync(predicate);
        }

        public void Remove(T entity)
        {
            _dbSet.Remove(entity);
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public IQueryable<T> Query()
        {
            return _dbSet.AsQueryable();
        }
    }
}
