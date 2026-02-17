namespace Imaj.Core.Entities
{
    public class TransQry : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Descr { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
        public string Help { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string Keyword { get; set; } = string.Empty;
        public short Stamp { get; set; }
        public int? Reference { get; set; }
        public decimal? TransCatID { get; set; }
        public decimal? TransTypeID { get; set; }
        public byte? ParamCount1 { get; set; }
        public byte? ParamCount2 { get; set; }
        public decimal? MissingHelpLangID { get; set; }
        public decimal? MissingDescrLangID { get; set; }
        public decimal? MissingRemarkLangID { get; set; }
        public decimal? InvisibleID { get; set; }
        public decimal? DescrLangID { get; set; }
        public decimal? RemarkLangID { get; set; }
        public decimal? HelpLangID { get; set; }
    }
}
