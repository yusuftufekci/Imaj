using System;
using System.Collections.Generic;
using Imaj.Service.DTOs;

namespace Imaj.Web.Models
{
    public class AbsenceWorkflowActionRequest
    {
        public decimal Id { get; set; }
        public AbsenceWorkflowAction Action { get; set; }
        public List<string> SelectedIds { get; set; } = new();
        public int CurrentIndex { get; set; }
        public string? ReturnUrl { get; set; }
    }

    public class AbsenceScheduleActionRequest
    {
        public decimal Id { get; set; }
        public DateTime NewDate { get; set; }
        public AbsenceScheduleAction Action { get; set; }
        public List<string> SelectedIds { get; set; } = new();
        public int CurrentIndex { get; set; }
        public string? ReturnUrl { get; set; }
    }

    public class JobWorkflowActionRequest
    {
        public int Reference { get; set; }
        public JobWorkflowAction Action { get; set; }
        public List<string> SelectedIds { get; set; } = new();
        public int CurrentIndex { get; set; }
        public string? ReturnUrl { get; set; }
    }

    public class InvoiceWorkflowActionRequest
    {
        public int Reference { get; set; }
        public InvoiceWorkflowAction Action { get; set; }
        public DateTime? IssueDate { get; set; }
        public string SourceView { get; set; } = "Detail";
        public List<string> SelectedReferences { get; set; } = new();
        public int CurrentIndex { get; set; }
        public string? ReturnUrl { get; set; }
    }
}
