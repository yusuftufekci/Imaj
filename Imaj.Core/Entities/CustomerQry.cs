namespace Imaj.Core.Entities
{
    public class CustomerQry : BaseEntity
    {
        public decimal CompanyID { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Fax { get; set; } = string.Empty;
        public string EMail { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string TaxOffice { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
        public bool FixedInvisible { get; set; }
        public short Stamp { get; set; }
        public decimal? InvisibleID { get; set; }
        public decimal? JobStateID { get; set; }
    }
}
