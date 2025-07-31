using AutoMapper;
using server.Dtos.DanhMuc.Dm_HangHoaThiTruong;
using server.Models.DanhMuc;

namespace server.Mappings.DanhMuc
{
    public class Dm_HangHoaThiTruongProfile : Profile
    {
        public Dm_HangHoaThiTruongProfile()
        {
            // Mapping từ Model sang DTOs
            CreateMap<Dm_HangHoaThiTruong, Dm_HangHoaThiTruongDto>();
            CreateMap<Dm_HangHoaThiTruong, Dm_HangHoaThiTruongFlatDto>();
            CreateMap<Dm_HangHoaThiTruong, Dm_HangHoaThiTruongTreeDto>();
            
            // Mapping từ DTOs sang Model
            CreateMap<DmHangHoaThiTruongCreateDto, Dm_HangHoaThiTruong>();
            CreateMap<DmHangHoaThiTruongUpdateDto, Dm_HangHoaThiTruong>();
            
            // Mapping giữa các DTOs 
            CreateMap<Dm_HangHoaThiTruongDto, Dm_HangHoaThiTruongFlatDto>();
            CreateMap<Dm_HangHoaThiTruongFlatDto, Dm_HangHoaThiTruongTreeDto>();
        }
    }
}
