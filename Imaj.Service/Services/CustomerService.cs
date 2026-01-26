using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Imaj.Core.Entities;
using Imaj.Core.Interfaces.Repositories;
using Imaj.Service.DTOs;
using Imaj.Service.Interfaces;
using Imaj.Service.Results;
using Microsoft.EntityFrameworkCore;

namespace Imaj.Service.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CustomerService(ICustomerRepository customerRepository, IUnitOfWork unitOfWork)
        {
            _customerRepository = customerRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult> AddAsync(CustomerDto customerDto)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Lock / Concurrency safety depends on Isolation Level, typically ReadCommitted or higher.
                // Since we rely on Max(ID) + 1, serialization is best but expensive.
                // However, transaction ensures atomicity. If 2 threads read same MaxID, one might fail on unique key constraint (if Id is PK).
                // Retry logic or Serialized transaction would be better for high concurrency.
                // For this app scope, atomic Commit is sufficient step up.

                var nextId = await _customerRepository.GetNextIdAsync();
                var customer = new Customer
                {
                    Id = nextId,
                    Code = customerDto.Code ?? string.Empty,
                    Name = customerDto.Name ?? string.Empty,
                    City = customerDto.City ?? string.Empty,
                    Phone = customerDto.Phone ?? string.Empty,
                    EMail = customerDto.Email ?? string.Empty,
                    Contact = customerDto.Contact ?? string.Empty,
                    TaxOffice = customerDto.TaxOffice ?? string.Empty,
                    TaxNumber = customerDto.TaxNumber ?? string.Empty,
                    Country = customerDto.Country ?? string.Empty,
                    Address = customerDto.Address ?? string.Empty,
                    Notes = customerDto.Notes ?? string.Empty,
                    Owner = customerDto.Owner ?? string.Empty,
                    SelectFlag = customerDto.SelectFlag,
                    InvoName = customerDto.InvoiceName ?? string.Empty,
                    Zip = customerDto.AreaCode ?? string.Empty,
                    Fax = customerDto.Fax ?? string.Empty,
                    Stamp = 0, // Default for short
                    CompanyID = 7 // Default static 7 per user request
                };

                await _customerRepository.AddAsync(customer);
                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                return ServiceResult.Success("Müşteri başarıyla eklendi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                
                // Generic error handler
                // In production, check for Duplicate Key exceptions (SqlException Number 2601/2627)
                return ServiceResult.Fail($"Müşteri eklenirken bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ServiceResult> UpdateAsync(CustomerDto dto)
        {
            try
            {
                var customer = await _customerRepository.GetByIdAsync(dto.Id);
                if (customer == null)
                {
                     // Try finding by code if ID is missing or 0? 
                     // But usually ID is passed.
                     return ServiceResult.Fail("Müşteri bulunamadı.");
                }

                // Update fields
                customer.Code = dto.Code ?? string.Empty;
                customer.Name = dto.Name ?? string.Empty;
                customer.City = dto.City ?? string.Empty;
                customer.Phone = dto.Phone ?? string.Empty;
                customer.EMail = dto.Email ?? string.Empty;
                customer.Contact = dto.Contact ?? string.Empty;
                customer.TaxOffice = dto.TaxOffice ?? string.Empty;
                customer.TaxNumber = dto.TaxNumber ?? string.Empty;
                customer.Country = dto.Country ?? string.Empty;
                customer.Address = dto.Address ?? string.Empty;
                customer.Notes = dto.Notes ?? string.Empty;
                customer.Owner = dto.Owner ?? string.Empty;
                customer.SelectFlag = dto.SelectFlag;
                customer.InvoName = dto.InvoiceName ?? string.Empty;
                customer.Zip = dto.AreaCode ?? string.Empty;
                customer.Fax = dto.Fax ?? string.Empty;
                
                // Do not update ID or CompanyID usually, unless required.

                _customerRepository.Update(customer); // Mark as modified
                await _unitOfWork.CommitAsync();

                return ServiceResult.Success("Müşteri başarıyla güncellendi.");
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail($"Müşteri güncellenirken bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<CustomerDto>>> GetAllAsync()
        {
            var customers = await _customerRepository.GetAllAsync();
            var dtos = customers.Select(MapToDto).ToList();
            return ServiceResult<List<CustomerDto>>.Success(dtos);
        }

        public async Task<ServiceResult<PagedResult<CustomerDto>>> GetByFilterAsync(CustomerFilterDto filter)
        {
            // Use Query() to delay execution (IQueryable) - Performance Optimization
            var query = _customerRepository.Query();

            if (!string.IsNullOrWhiteSpace(filter.Code))
                query = query.Where(c => c.Code != null && c.Code.Contains(filter.Code)); // EF Core translates Contains to SQL LIKE

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

            if (filter.IsInvalid.HasValue && filter.IsInvalid.Value)
            {
                 query = query.Where(c => c.Invisible == true);
            }
            else
            {
                 if (filter.IsInvalid == false) 
                    query = query.Where(c => c.Invisible == false);
            }

            // Total Count (Efficient SQL Count)
            var totalCount = await query.CountAsync(); 

            // Pagination (SQL OFFSET/FETCH)
            var items = await query
                .OrderByDescending(c => c.Id) // Ensure deterministic ordering
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(c => new CustomerDto 
                { 
                    // Projection to DTO (Only select columns needed? For now select all to be safe)
                    // If we want "Select *", we can just do ToListAsync then map. 
                    // But EF Core projection runs in SQL if we do new CustomerDto { ... } inside Select.
                    // However, MapToDto is a private method, so we can't use it in LINQ to Entities easily.
                    // We can either:
                    // 1. Fetch Entities then Map (simpler logic) -> Items are small page size only.
                    // 2. Project manually here.
                    // Option 1 is fine since we are paging (e.g. 10 items).
                   Id = c.Id,
                   Code = c.Code,
                   Name = c.Name,
                   City = c.City,
                   Phone = c.Phone,
                   Email = c.EMail,
                   Contact = c.Contact,
                   TaxOffice = c.TaxOffice,
                   TaxNumber = c.TaxNumber,
                   Country = c.Country,
                   Address = c.Address,
                   Notes = c.Notes,
                   Owner = c.Owner,
                   SelectFlag = c.SelectFlag,
                   InvoiceName = c.InvoName,
                   AreaCode = c.Zip,
                   Fax = c.Fax
                })
                .ToListAsync();

            var result = new PagedResult<CustomerDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            };
            
            return ServiceResult<PagedResult<CustomerDto>>.Success(result);
        }

        public async Task<ServiceResult<CustomerDto>> GetByIdAsync(decimal id)
        {
             var customer = await _customerRepository.GetByIdAsync(id);
             if (customer == null) return ServiceResult<CustomerDto>.Fail("Müşteri bulunamadı.");
             
             return ServiceResult<CustomerDto>.Success(MapToDto(customer));
        }

        public async Task<ServiceResult<CustomerDto>> GetByCodeAsync(string code)
        {
             var customer = await _customerRepository.SingleOrDefaultAsync(c => c.Code == code);
             if (customer == null) return ServiceResult<CustomerDto>.Fail("Müşteri bulunamadı.");
             
             return ServiceResult<CustomerDto>.Success(MapToDto(customer));
        }

        private CustomerDto MapToDto(Customer c)
        {
            return new CustomerDto
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                City = c.City,
                Phone = c.Phone,
                Email = c.EMail, // Note casing
                Contact = c.Contact,
                TaxOffice = c.TaxOffice,
                TaxNumber = c.TaxNumber,
                Country = c.Country,
                Address = c.Address,
                Notes = c.Notes,
                Owner = c.Owner,
                SelectFlag = c.SelectFlag,
                InvoiceName = c.InvoName,
                AreaCode = c.Zip,
                Fax = c.Fax
            };
        }
    }
}
