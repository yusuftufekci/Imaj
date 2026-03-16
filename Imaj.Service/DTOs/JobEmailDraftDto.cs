namespace Imaj.Service.DTOs
{
    public class JobEmailDraftDto
    {
        public string RecipientEmail { get; set; } = string.Empty;
        public List<JobEmailItemDto> Items { get; set; } = new();
    }

    public class JobEmailItemDto
    {
        public decimal JobId { get; set; }
        public int Reference { get; set; }
        public string FunctionName { get; set; } = string.Empty;
        public decimal StateId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string RelatedPerson { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal WorkAmount { get; set; }
        public decimal ProductAmount { get; set; }
        public bool IsEmailSent { get; set; }
        public bool IsEvaluated { get; set; }
    }
}
