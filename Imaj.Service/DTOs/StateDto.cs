namespace Imaj.Service.DTOs
{
    /// <summary>
    /// State (Durum) verisi için DTO
    /// </summary>
    public class StateDto
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }
}
