using System;

namespace Imaj.Service.DTOs
{
    /// <summary>
    /// İş log kaydı DTO'su.
    /// JobLog tablosundan gelen verileri taşır.
    /// </summary>
    public class JobLogDto
    {
        public decimal Id { get; set; }
        public DateTime LogDate { get; set; }
        
        // Hedef bilgisi (e-posta adresi vb.)
        public string Destination { get; set; } = string.Empty;
        
        // Kullanıcı Bilgileri
        public decimal UserId { get; set; }
        public string UserCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;

        // Hareket Bilgisi
        public decimal LogActionId { get; set; }
        public string ActionName { get; set; } = string.Empty;
    }
}

