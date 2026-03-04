namespace Imaj.Service.DTOs
{
    /// <summary>
    /// İş (Job) verisi için DTO.
    /// Liste ve detay görünümleri için kullanılır.
    /// </summary>
    public class JobDto
    {
        public decimal Id { get; set; }
        public int Reference { get; set; }
        public decimal FunctionId { get; set; }
        public string? FunctionName { get; set; }
        public decimal CustomerId { get; set; }
        public string? CustomerCode { get; set; }
        public string? CustomerName { get; set; }
        public string? Name { get; set; }
        public string? Contact { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal StateId { get; set; }
        public string? StatusName { get; set; }
        public bool IsEmailSent { get; set; }
        public bool IsEvaluated { get; set; }
        public decimal? InvoLineId { get; set; }
        public decimal WorkAmount { get; set; }
        public decimal ProductAmount { get; set; }
        public string? IntNotes { get; set; }
        public string? ExtNotes { get; set; }
        
        public List<JobWorkDto> JobWorks { get; set; } = new List<JobWorkDto>();
        public List<JobProdDto> JobProds { get; set; } = new List<JobProdDto>();
        public List<JobProdCatDto> JobProdCats { get; set; } = new List<JobProdCatDto>();
    }
}
