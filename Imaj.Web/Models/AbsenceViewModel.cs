using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Imaj.Web.Models
{
    public class AbsenceFilterModel
    {
        public decimal? FunctionId { get; set; }
        public decimal? ReasonId { get; set; }

        [StringLength(32, ErrorMessage = "Ad en fazla 32 karakter olabilir.")]
        public string? Name { get; set; }

        [StringLength(32, ErrorMessage = "Ilgili alani en fazla 32 karakter olabilir.")]
        public string? Contact { get; set; }

        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }
        public DateTime? EndDateFrom { get; set; }
        public DateTime? EndDateTo { get; set; }
        public decimal? StateId { get; set; }
        public bool? Evaluated { get; set; }

        public List<decimal> ResourceIds { get; set; } = new();

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
    }

    public class AbsenceFunctionOptionViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class AbsenceReasonOptionViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class AbsenceStateOptionViewModel
    {
        public decimal Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class AbsenceResourceViewModel
    {
        public decimal ResourceId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal FunctionId { get; set; }
        public string FunctionName { get; set; } = string.Empty;
        public decimal ResoCatId { get; set; }
        public string ResoCatName { get; set; } = string.Empty;
        public bool Invisible { get; set; }
    }

    public class AbsenceIndexViewModel
    {
        public AbsenceFilterModel Filter { get; set; } = new();
        public List<AbsenceFunctionOptionViewModel> FunctionOptions { get; set; } = new();
        public List<AbsenceReasonOptionViewModel> ReasonOptions { get; set; } = new();
        public List<AbsenceStateOptionViewModel> StateOptions { get; set; } = new();

        public decimal? CreateFunctionId { get; set; }
        public DateTime? CreateStartDate { get; set; }
        public DateTime? CreateEndDate { get; set; }
    }

    public class AbsenceListItemViewModel
    {
        public decimal Id { get; set; }
        public decimal FunctionId { get; set; }
        public string FunctionName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal? ReasonId { get; set; }
        public string ReasonName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal StateId { get; set; }
        public string StateName { get; set; } = string.Empty;
        public bool Evaluated { get; set; }
    }

    public class AbsenceListViewModel
    {
        public List<AbsenceListItemViewModel> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
        public int TotalCount { get; set; }
        public AbsenceFilterModel Filter { get; set; } = new();
        public string ReturnUrl { get; set; } = "/Absence/List";
    }

    public abstract class AbsenceEditorViewModelBase
    {
        public decimal? Id { get; set; }

        [Range(1, double.MaxValue, ErrorMessage = "Fonksiyon secimi zorunludur.")]
        public decimal? FunctionId { get; set; }
        public string FunctionName { get; set; } = string.Empty;

        [Range(1, double.MaxValue, ErrorMessage = "Gerekce secimi zorunludur.")]
        public decimal? ReasonId { get; set; }
        public string ReasonName { get; set; } = string.Empty;

        [StringLength(32, ErrorMessage = "Ad en fazla 32 karakter olabilir.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(32, ErrorMessage = "Ilgili alani en fazla 32 karakter olabilir.")]
        public string Contact { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public decimal StateId { get; set; } = 10;
        public string StateName { get; set; } = "Acik";

        public bool Evaluated { get; set; }
        public string Notes { get; set; } = string.Empty;

        public string ReturnUrl { get; set; } = "/Absence";

        public List<AbsenceFunctionOptionViewModel> FunctionOptions { get; set; } = new();
        public List<AbsenceReasonOptionViewModel> ReasonOptions { get; set; } = new();
        public List<AbsenceResourceViewModel> Resources { get; set; } = new();
    }

    public class AbsenceDetailViewModel : AbsenceEditorViewModelBase
    {
        public int CurrentIndex { get; set; }
        public int TotalSelected { get; set; }
        public List<string> SelectedIds { get; set; } = new();
    }

    public class AbsenceCreateViewModel : AbsenceEditorViewModelBase
    {
    }

    public class AbsenceResourceLookupFilterModel
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public decimal? FunctionId { get; set; }
        public bool? IsInvalid { get; set; }
        public string? ExcludeIds { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
