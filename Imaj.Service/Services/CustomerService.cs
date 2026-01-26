using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Imaj.Core.Entities;
using Imaj.Core.Guards;
using Imaj.Core.Interfaces.Repositories;
using Imaj.Service.DTOs;
using Imaj.Service.Interfaces;
using Imaj.Service.Options;
using Imaj.Service.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Imaj.Service.Services
{
    /// <summary>
    /// Müşteri işlemleri için business service.
    /// </summary>
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly CustomerSettings _settings;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(
            ICustomerRepository customerRepository, 
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IOptions<CustomerSettings> options,
            ILogger<CustomerService> logger)
        {
            _customerRepository = customerRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _settings = options.Value;
            _logger = logger;
        }

        public async Task<ServiceResult> AddAsync(CustomerDto customerDto)
        {
            // Guard clauses ile parametre doğrulama
            Guard.AgainstNull(customerDto, nameof(customerDto));
            Guard.AgainstNullOrEmpty(customerDto.Code, nameof(customerDto.Code));
            Guard.AgainstNullOrEmpty(customerDto.Name, nameof(customerDto.Name));

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Yeni müşteri ekleniyor: {CustomerCode}", customerDto.Code);

                var nextId = await _customerRepository.GetNextIdAsync();
                
                // AutoMapper ile DTO'dan Entity'ye dönüşüm
                var customer = _mapper.Map<Customer>(customerDto);
                customer.Id = nextId;
                customer.CompanyID = _settings.DefaultCompanyId; // Options'dan alınıyor
                customer.Stamp = 0;

                await _customerRepository.AddAsync(customer);
                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Müşteri başarıyla eklendi: {CustomerId}", nextId);
                return ServiceResult.Success("Müşteri başarıyla eklendi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Müşteri eklenirken hata oluştu: {CustomerCode}", customerDto.Code);
                return ServiceResult.Fail($"Müşteri eklenirken bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ServiceResult> UpdateAsync(CustomerDto dto)
        {
            Guard.AgainstNull(dto, nameof(dto));
            Guard.AgainstZeroOrNegative(dto.Id, nameof(dto.Id));

            try
            {
                _logger.LogInformation("Müşteri güncelleniyor: {CustomerId}", dto.Id);

                var customer = await _customerRepository.GetByIdAsync(dto.Id);
                if (customer == null)
                {
                    _logger.LogWarning("Müşteri bulunamadı: {CustomerId}", dto.Id);
                    return ServiceResult.Fail("Müşteri bulunamadı.");
                }

                // AutoMapper ile güncelleme (mevcut entity'yi günceller)
                _mapper.Map(dto, customer);

                _customerRepository.Update(customer);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Müşteri başarıyla güncellendi: {CustomerId}", dto.Id);
                return ServiceResult.Success("Müşteri başarıyla güncellendi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Müşteri güncellenirken hata oluştu: {CustomerId}", dto.Id);
                return ServiceResult.Fail($"Müşteri güncellenirken bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<CustomerDto>>> GetAllAsync()
        {
            var customers = await _customerRepository.GetAllAsync();
            var dtos = _mapper.Map<List<CustomerDto>>(customers);
            return ServiceResult<List<CustomerDto>>.Success(dtos);
        }

        public async Task<ServiceResult<PagedResult<CustomerDto>>> GetByFilterAsync(CustomerFilterDto filter)
        {
            Guard.AgainstNull(filter, nameof(filter));

            // IQueryable ile gecikmeli sorgu
            var query = _customerRepository.Query();

            // Dinamik filtreleme
            if (!string.IsNullOrWhiteSpace(filter.Code))
                query = query.Where(c => c.Code != null && c.Code.Contains(filter.Code));

            if (!string.IsNullOrWhiteSpace(filter.Name))
                query = query.Where(c => c.Name != null && c.Name.Contains(filter.Name));

            if (!string.IsNullOrWhiteSpace(filter.City))
                query = query.Where(c => c.City != null && c.City.Contains(filter.City));

            if (!string.IsNullOrWhiteSpace(filter.AreaCode))
                query = query.Where(c => c.Zip != null && c.Zip.Contains(filter.AreaCode));

            if (!string.IsNullOrWhiteSpace(filter.Country))
                query = query.Where(c => c.Country != null && c.Country.Contains(filter.Country));

            if (!string.IsNullOrWhiteSpace(filter.Owner))
                query = query.Where(c => c.Owner != null && c.Owner.Contains(filter.Owner));

            if (!string.IsNullOrWhiteSpace(filter.RelatedPerson))
                query = query.Where(c => c.Contact != null && c.Contact.Contains(filter.RelatedPerson));

            if (!string.IsNullOrWhiteSpace(filter.Phone))
                query = query.Where(c => c.Phone != null && c.Phone.Contains(filter.Phone));

            if (!string.IsNullOrWhiteSpace(filter.Fax))
                query = query.Where(c => c.Fax != null && c.Fax.Contains(filter.Fax));

            if (!string.IsNullOrWhiteSpace(filter.Email))
                query = query.Where(c => c.EMail != null && c.EMail.Contains(filter.Email));

            if (!string.IsNullOrWhiteSpace(filter.TaxOffice))
                query = query.Where(c => c.TaxOffice != null && c.TaxOffice.Contains(filter.TaxOffice));

            if (!string.IsNullOrWhiteSpace(filter.TaxNumber))
                query = query.Where(c => c.TaxNumber != null && c.TaxNumber.Contains(filter.TaxNumber));

            if (!string.IsNullOrWhiteSpace(filter.JobStatus))
            {
                if (filter.JobStatus.Equals("Active", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(c => c.SelectFlag == true);
                else if (filter.JobStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(c => c.SelectFlag == false);
            }

            if (filter.IsInvalid.HasValue)
            {
                query = query.Where(c => c.Invisible == filter.IsInvalid.Value);
            }

            // Toplam kayıt sayısı (SQL COUNT)
            var totalCount = await query.CountAsync();

            // Sayfalama ve sıralama
            var pageSize = filter.PageSize > 0 ? filter.PageSize : _settings.DefaultPageSize;
            var items = await query
                .OrderByDescending(c => c.Id)
                .Skip((filter.Page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // AutoMapper ile projection
            var dtos = _mapper.Map<List<CustomerDto>>(items);

            var result = new PagedResult<CustomerDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = pageSize
            };

            return ServiceResult<PagedResult<CustomerDto>>.Success(result);
        }

        public async Task<ServiceResult<CustomerDto>> GetByIdAsync(decimal id)
        {
            Guard.AgainstZeroOrNegative(id, nameof(id));

            var customer = await _customerRepository.GetByIdAsync(id);
            if (customer == null) 
                return ServiceResult<CustomerDto>.Fail("Müşteri bulunamadı.");

            var dto = _mapper.Map<CustomerDto>(customer);
            return ServiceResult<CustomerDto>.Success(dto);
        }

        public async Task<ServiceResult<CustomerDto>> GetByCodeAsync(string code)
        {
            Guard.AgainstNullOrEmpty(code, nameof(code));

            var customer = await _customerRepository.SingleOrDefaultAsync(c => c.Code == code);
            if (customer == null) 
                return ServiceResult<CustomerDto>.Fail("Müşteri bulunamadı.");

            var dto = _mapper.Map<CustomerDto>(customer);
            return ServiceResult<CustomerDto>.Success(dto);
        }
    }
}
