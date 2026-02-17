namespace Imaj.Core.Entities
{
    public class RoleQry : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string ExceptIDList { get; set; } = string.Empty;
        public short Stamp { get; set; }
        public decimal? InvisibleID { get; set; }
        public decimal? GlobalID { get; set; }
        public decimal? AllMenuID { get; set; }
        public decimal? AllMethReadID { get; set; }
        public decimal? AllMethWriteID { get; set; }
        public decimal? AllPropReadID { get; set; }
        public decimal? AllPropWriteID { get; set; }
        public decimal? ContAllIntfID { get; set; }
        public decimal? ContAllMethReadID { get; set; }
        public decimal? ContAllMethWriteID { get; set; }
        public decimal? ContAllPropReadID { get; set; }
        public decimal? ContAllPropWriteID { get; set; }
        public decimal? ContID { get; set; }
        public decimal? MenuID { get; set; }
    }
}
