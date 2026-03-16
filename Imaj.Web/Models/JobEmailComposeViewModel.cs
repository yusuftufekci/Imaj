using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Imaj.Web.Models
{
    public class JobEmailComposeViewModel
    {
        public string? RecipientEmail { get; set; }
        public string ReturnUrl { get; set; } = "/Job/List";
        public List<string> SelectedIds { get; set; } = new();
        [ValidateNever]
        public List<JobEmailComposeItemViewModel> Jobs { get; set; } = new();
    }

    public class JobEmailComposeItemViewModel
    {
        public string? RecipientEmail { get; set; }
        public string? Function { get; set; }
        public string? Reference { get; set; }
        public string? Customer { get; set; }
        public string? Name { get; set; }
        public string? RelatedPerson { get; set; }
        public string? Status { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public string? EmailSent { get; set; }
        public string? Evaluated { get; set; }
        public string? WorkAmount { get; set; }
        public string? ProductAmount { get; set; }
    }
}
