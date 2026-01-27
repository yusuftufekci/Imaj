using AutoMapper;
using Imaj.Core.Entities;
using Imaj.Service.DTOs;

namespace Imaj.Service.Mapping
{
    /// <summary>
    /// AutoMapper profili.
    /// Entity ve DTO arasındaki mapping tanımlarını içerir.
    /// </summary>
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Customer <-> CustomerDto mapping
            CreateMap<Customer, CustomerDto>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.EMail))
                .ForMember(dest => dest.InvoiceName, opt => opt.MapFrom(src => src.InvoName))
                .ForMember(dest => dest.AreaCode, opt => opt.MapFrom(src => src.Zip));

            CreateMap<CustomerDto, Customer>()
                .ForMember(dest => dest.EMail, opt => opt.MapFrom(src => src.Email ?? string.Empty))
                .ForMember(dest => dest.InvoName, opt => opt.MapFrom(src => src.InvoiceName ?? string.Empty))
                .ForMember(dest => dest.Zip, opt => opt.MapFrom(src => src.AreaCode ?? string.Empty))
                // Nullable DTO string'lerini boş string'e çevir (DB NOT NULL constraint)
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code ?? string.Empty))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name ?? string.Empty))
                .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.City ?? string.Empty))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone ?? string.Empty))
                .ForMember(dest => dest.Contact, opt => opt.MapFrom(src => src.Contact ?? string.Empty))
                .ForMember(dest => dest.TaxOffice, opt => opt.MapFrom(src => src.TaxOffice ?? string.Empty))
                .ForMember(dest => dest.TaxNumber, opt => opt.MapFrom(src => src.TaxNumber ?? string.Empty))
                .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.Country ?? string.Empty))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address ?? string.Empty))
                .ForMember(dest => dest.Fax, opt => opt.MapFrom(src => src.Fax ?? string.Empty))
                .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.Notes ?? string.Empty))
                .ForMember(dest => dest.Owner, opt => opt.MapFrom(src => src.Owner ?? string.Empty))
                // Entity'ye özgü fieldlar - DTO'dan gelmeyenler
                .ForMember(dest => dest.CompanyID, opt => opt.Ignore())
                .ForMember(dest => dest.Stamp, opt => opt.Ignore())
                .ForMember(dest => dest.Invisible, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore());

            // User <-> UserDto mapping
            CreateMap<User, UserDto>().ReverseMap();
        }
    }
}
