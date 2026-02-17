using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Imaj.Core.Entities;
using Imaj.Core.Extensions;
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

                // Ürün Kategorilerini Ekle (CustProdCat)
                if (customerDto.ProductCategories != null && customerDto.ProductCategories.Any())
                {
                    var custProdCatRepo = _unitOfWork.Repository<CustProdCat>();
                    
                    // Manuel ID Üretimi (Max + 1)
                    var currentMaxId = await custProdCatRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0;

                    foreach (var cat in customerDto.ProductCategories)
                    {
                        currentMaxId++;
                        var custProdCat = new CustProdCat
                        {
                            Id = currentMaxId, // Manuel ID ataması
                            CustomerID = nextId,
                            ProdCatID = cat.Id,
                            DiscPercentage = (byte)cat.Discount,
                            SelectFlag = false,
                            Stamp = 1,
                            Deleted = 0
                        };
                        await custProdCatRepo.AddAsync(custProdCat);
                    }
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Müşteri başarıyla eklendi: {CustomerId}", nextId);
                return ServiceResult.Success("Müşteri başarıyla eklendi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                
                // Inner exception varsa onu da logla (asıl hata genellikle burada)
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                _logger.LogError(ex, "Müşteri eklenirken hata oluştu: {CustomerCode}. Detay: {ErrorDetail}", 
                    customerDto.Code, innerMessage);
                
                // Kullanıcıya teknik detay gösterme, genel mesaj döndür
                return ServiceResult.Fail("Müşteri eklenirken bir hata oluştu. Lütfen bilgileri kontrol edip tekrar deneyin.");
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
                    return ServiceResult.NotFound("Müşteri bulunamadı.");
                }

                // AutoMapper ile güncelleme (mevcut entity'yi günceller)
                _mapper.Map(dto, customer);

                _customerRepository.Update(customer);

                // Ürün Kategorilerini Güncelle (CustProdCat)
                // Strateji: Mevcut hesaplanmış kayıtları sil, yenilerini ekle (Full Replace)
                var custProdCatRepo = _unitOfWork.Repository<CustProdCat>();
                
                // Mevcut kayıtları bul (Navigation property olmadığı için Query ile)
                // Mevcut AKTİF kayıtları bul
                var existingCats = await custProdCatRepo.Query()
                    .Where(x => x.CustomerID == customer.Id && x.Deleted == 0)
                    .ToListAsync();
                
                // Soft Delete (Deleted = 1)
                foreach (var existing in existingCats)
                {
                    existing.Deleted = 1;
                    custProdCatRepo.Update(existing);
                }

                // Yenileri ekle
                if (dto.ProductCategories != null && dto.ProductCategories.Any())
                {
                    // Manuel ID Üretimi (Max + 1)
                    var currentMaxId = await custProdCatRepo.Query().MaxAsync(x => (decimal?)x.Id) ?? 0;

                    foreach (var cat in dto.ProductCategories)
                    {
                        currentMaxId++;
                        var custProdCat = new CustProdCat
                        {
                            Id = currentMaxId, // Manuel ID ataması
                            CustomerID = customer.Id,
                            ProdCatID = cat.Id,
                            DiscPercentage = (byte)cat.Discount,
                            SelectFlag = false,
                            Stamp = 1,
                            Deleted = 0
                        };
                        await custProdCatRepo.AddAsync(custProdCat);
                    }
                }


                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Müşteri başarıyla güncellendi: {CustomerId}", dto.Id);
                return ServiceResult.Success("Müşteri başarıyla güncellendi.");
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                _logger.LogError(ex, "Müşteri güncellenirken hata oluştu: {CustomerId}. Detay: {ErrorDetail}", 
                    dto.Id, innerMessage);
                return ServiceResult.Fail("Müşteri güncellenirken bir hata oluştu. Lütfen tekrar deneyin.");
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

            // IQueryable ile gecikmeli sorgu + WhereIfNotEmpty ile temiz filtreleme
            var query = _customerRepository.Query()
                .WhereIfNotEmpty(filter.Code, c => c.Code != null && c.Code.Contains(filter.Code!))
                .WhereIfNotEmpty(filter.Name, c => c.Name != null && c.Name.Contains(filter.Name!))
                .WhereIfNotEmpty(filter.City, c => c.City != null && c.City.Contains(filter.City!))
                .WhereIfNotEmpty(filter.AreaCode, c => c.Zip != null && c.Zip.Contains(filter.AreaCode!))
                .WhereIfNotEmpty(filter.Country, c => c.Country != null && c.Country.Contains(filter.Country!))
                .WhereIfNotEmpty(filter.Owner, c => c.Owner != null && c.Owner.Contains(filter.Owner!))
                .WhereIfNotEmpty(filter.RelatedPerson, c => c.Contact != null && c.Contact.Contains(filter.RelatedPerson!))
                .WhereIfNotEmpty(filter.Phone, c => c.Phone != null && c.Phone.Contains(filter.Phone!))
                .WhereIfNotEmpty(filter.Fax, c => c.Fax != null && c.Fax.Contains(filter.Fax!))
                .WhereIfNotEmpty(filter.Email, c => c.EMail != null && c.EMail.Contains(filter.Email!))
                .WhereIfNotEmpty(filter.TaxOffice, c => c.TaxOffice != null && c.TaxOffice.Contains(filter.TaxOffice!))
                .WhereIfNotEmpty(filter.TaxNumber, c => c.TaxNumber != null && c.TaxNumber.Contains(filter.TaxNumber!))
                .WhereIf(filter.JobStatus == "Active", c => c.SelectFlag == true)
                .WhereIf(filter.JobStatus == "Completed", c => c.SelectFlag == false)
                .WhereIfHasValue(filter.JobStateId, c => _unitOfWork.Repository<Job>().Query().Any(j => j.CustomerID == c.Id && j.StateID == filter.JobStateId!.Value))
                .WhereIfHasValue(filter.IsInvalid, c => c.Invisible == filter.IsInvalid!.Value);

            // Toplam kayıt sayısı (SQL COUNT)
            var totalCount = await query.CountAsync();

            // Sayfalama ve sıralama
            var pageSize = filter.PageSize > 0 ? filter.PageSize : _settings.DefaultPageSize;
            var items = await query
                .OrderByDescending(c => c.Id)
                .Paginate(filter.Page, pageSize)
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
                return ServiceResult<CustomerDto>.NotFound("Müşteri bulunamadı.");

            var dto = _mapper.Map<CustomerDto>(customer);

            // Ürün kategorilerini yükle
            await LoadProductCategoriesAsync(dto);

            return ServiceResult<CustomerDto>.Success(dto);
        }

        public async Task<ServiceResult<CustomerDto>> GetByCodeAsync(string code)
        {
            Guard.AgainstNullOrEmpty(code, nameof(code));

            var customer = await _customerRepository.SingleOrDefaultAsync(c => c.Code == code);
            if (customer == null) 
                return ServiceResult<CustomerDto>.NotFound("Müşteri bulunamadı.");

            var dto = _mapper.Map<CustomerDto>(customer);
            
            // Ürün kategorilerini yükle
            await LoadProductCategoriesAsync(dto);
            
            return ServiceResult<CustomerDto>.Success(dto);
        }

        /// <summary>
        /// Müşteriye ait ürün kategorilerini veritabanından yükler.
        /// CustProdCat ilişki tablosundan mevcut seçimleri getirir.
        /// </summary>
        private async Task LoadProductCategoriesAsync(CustomerDto dto)
        {
            var catsQuery = from cpc in _unitOfWork.Repository<CustProdCat>().Query()
                           join pc in _unitOfWork.Repository<ProdCat>().Query() on cpc.ProdCatID equals pc.Id
                           join xpc in _unitOfWork.Repository<XProdCat>().Query() on pc.Id equals xpc.ProdCatID
                           join tt in _unitOfWork.Repository<TaxType>().Query() on pc.TaxTypeID equals tt.Id
                           join xtt in _unitOfWork.Repository<XTaxType>().Query() on tt.Id equals xtt.TaxTypeID
                           where cpc.CustomerID == dto.Id && cpc.Deleted == 0
                                 && xpc.LanguageID == 1
                                 && xtt.LanguageID == 1
                           select new ProductCategoryDto
                           {
                               Id = pc.Id,
                               Name = xpc.Name,
                               TaxTypeId = pc.TaxTypeID,
                               TaxCode = tt.Code,
                               TaxName = xtt.Name,
                               Sequence = pc.Sequence,
                               Discount = cpc.DiscPercentage,
                               IsSelected = true
                           };

            dto.ProductCategories = await catsQuery.ToListAsync();
        }
    }
}
