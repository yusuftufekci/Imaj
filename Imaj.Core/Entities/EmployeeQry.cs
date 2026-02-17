namespace Imaj.Core.Entities
{
    public class EmployeeQry : BaseEntity
    {
        public decimal CompanyID { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool FixedInvisible { get; set; }
        public string ExceptIDList { get; set; } = string.Empty;
        public string IDList { get; set; } = string.Empty;
        public short Stamp { get; set; }
        public decimal? InvisibleID { get; set; }
        public decimal? FunctionID { get; set; }
    }
}
