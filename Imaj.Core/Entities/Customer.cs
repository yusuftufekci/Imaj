using System;

namespace Imaj.Core.Entities
{
    public class Customer : BaseEntity
    {
        // ID is inherited from BaseEntity
        // ID is inherited from BaseEntity
        public decimal CompanyID { get; set; } = 7; // Default to 7 as per user request (FK constraint) 
        // Wait, CompanyID is decimal(18,0) and Not Null.
        // But if it's a FK or just a code... 
        // Let's stick to strict if possible, but safe default is 0.
        
        // Actually, for strings, init to Empty.
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string InvoName { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Fax { get; set; } = string.Empty;
        public string EMail { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string TaxOffice { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
        public bool Invisible { get; set; }
        public bool SelectFlag { get; set; }
        // Stamp is smallint, Not Null.
        public short Stamp { get; set; }
    }
}
