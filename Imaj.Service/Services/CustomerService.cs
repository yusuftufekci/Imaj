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
            try
            {
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

                return ServiceResult.Success("Müşteri başarıyla eklendi.");
            }
            catch (Exception ex)
            {
                // In a real app, log the error
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
            var customers = await _customerRepository.GetAllAsync();
            var query = customers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Code))
                query = query.Where(c => c.Code != null && c.Code.Contains(filter.Code, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(filter.Name))
                query = query.Where(c => c.Name != null && c.Name.Contains(filter.Name, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(filter.City))
                query = query.Where(c => c.City != null && c.City.Contains(filter.City, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(filter.AreaCode))
                 query = query.Where(c => c.Zip != null && c.Zip.Contains(filter.AreaCode, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(filter.Country))
                query = query.Where(c => c.Country != null && c.Country.Contains(filter.Country, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(filter.Owner))
                query = query.Where(c => c.Owner != null && c.Owner.Contains(filter.Owner, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(filter.RelatedPerson))
                query = query.Where(c => c.Contact != null && c.Contact.Contains(filter.RelatedPerson, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(filter.Phone))
                query = query.Where(c => c.Phone != null && c.Phone.Contains(filter.Phone, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(filter.Fax))
                query = query.Where(c => c.Fax != null && c.Fax.Contains(filter.Fax, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(filter.Email))
                query = query.Where(c => c.EMail != null && c.EMail.Contains(filter.Email, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(filter.TaxOffice))
                query = query.Where(c => c.TaxOffice != null && c.TaxOffice.Contains(filter.TaxOffice, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(filter.TaxNumber))
                query = query.Where(c => c.TaxNumber != null && c.TaxNumber.Contains(filter.TaxNumber, StringComparison.OrdinalIgnoreCase));

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

            var totalCount = query.Count(); 

            var pagedItems = query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(MapToDto)
                .ToList();

            var result = new PagedResult<CustomerDto>
            {
                Items = pagedItems,
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
