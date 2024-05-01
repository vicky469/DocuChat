using AutoMapper;
using DocumentAPI.Common.Extensions;
using DocumentAPI.DTO.SEC;
using DocumentAPI.Infrastructure.Entity;

namespace DocumentAPI.DTO.Mapper;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<CompanyEntity, CompanyDTO>()
            .ForMember(dest => dest.CompanyEnum,
                opt => opt.MapFrom(src => EnumEx.TryGetEnumFromDescription<SecCompanyEnum>(src.Title)))
            .ForMember(dest => dest.CIK_Str_Padded,
                opt => opt.MapFrom(src => src.CIK_Str.ToString().PadLeft(10, '0')))
            .ForMember(dest => dest.Ticker,
                opt => opt.MapFrom(src => src.Ticker))
            .ForMember(dest => dest.Title,
                opt => opt.MapFrom(src => src.Title))
            .ReverseMap();

    }
}