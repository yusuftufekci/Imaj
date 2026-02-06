namespace Imaj.Core.Entities
{
    /// <summary>
    /// İş log kayıtlarını temsil eder.
    /// Veritabanındaki JobLog tablosuna karşılık gelir.
    /// </summary>
    public class JobLog : BaseEntity
    {
        public decimal JobID { get; set; }
        
        // Veritabanında ActionDT kolonu olarak geçiyor
        public DateTime ActionDT { get; set; }
        
        public decimal LogActionID { get; set; }
        public decimal UserID { get; set; }
        
        // Veritabanında Destination kolonu olarak geçiyor (e-posta adresi vb.)
        public string Destination { get; set; } = string.Empty;
        
        public short Stamp { get; set; }

        public virtual Job? Job { get; set; }
        public virtual LogAction? LogAction { get; set; }
        public virtual User? User { get; set; }
    }
}
