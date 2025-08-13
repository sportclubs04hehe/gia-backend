namespace server.Models.Extends
{
    public class Dm_HangHoaThiTruongJoinedWithParent : Dm_HangHoaThiTruongJoined
    {
        public Guid? ParentId { get; set; }
        public int? Depth { get; set; }
    }
}