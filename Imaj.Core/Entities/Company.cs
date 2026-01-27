namespace Imaj.Core.Entities
{
    /// <summary>
    /// Şirket bilgilerini tutan entity.
    /// Birçok lookup tabloya referans veren ana tablo.
    /// </summary>
    public class Company : BaseEntity
    {
        // Id is inherited from BaseEntity
        public string Name { get; set; } = string.Empty;
        public byte MaxReserveDay { get; set; }
        public byte CalenderHourOffset { get; set; }
        public string JobReportName { get; set; } = string.Empty;
        public string InvoiceReportName { get; set; } = string.Empty;
        public string LabelReportName { get; set; } = string.Empty;
        public string MailServer { get; set; } = string.Empty;
        public string MailUser { get; set; } = string.Empty;
        public string MailPassword { get; set; } = string.Empty;
        public string MailAddress { get; set; } = string.Empty;
        public string MailPath { get; set; } = string.Empty;
        public string ReportPath { get; set; } = string.Empty;
        public string Footer { get; set; } = string.Empty;
        public bool Invisible { get; set; }
        public bool SelectFlag { get; set; }
        public short Stamp { get; set; }
        
        // Navigation properties
        public virtual ICollection<Function> Functions { get; set; } = new List<Function>();
        public virtual ICollection<TimeType> TimeTypes { get; set; } = new List<TimeType>();
        public virtual ICollection<WorkType> WorkTypes { get; set; } = new List<WorkType>();
        public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}
