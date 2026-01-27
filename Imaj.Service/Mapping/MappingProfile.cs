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
                .ForMember(dest => dest.EMail, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.InvoName, opt => opt.MapFrom(src => src.InvoiceName))
                .ForMember(dest => dest.Zip, opt => opt.MapFrom(src => src.AreaCode))
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
