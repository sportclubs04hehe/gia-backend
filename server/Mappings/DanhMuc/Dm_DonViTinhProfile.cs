using AutoMapper;
using server.Dtos.DanhMuc.Dm_DonViTinh;
using server.Models.DanhMuc;

namespace server.Mappings.DanhMuc
{
    public class Dm_DonViTinhProfile : Profile
    {
        public Dm_DonViTinhProfile()
        {
            CreateMap<Dm_DonViTinh, Dm_DonViTinhDto>();
            CreateMap<DmDonViTinhCreateDto, Dm_DonViTinh>();
            CreateMap<DmDonViTinhUpdateDto, Dm_DonViTinh>();
        }
    }
}
