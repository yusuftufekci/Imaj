using System;

namespace Imaj.Service.DTOs
{
    public enum AbsenceWorkflowAction
    {
        Confirm = 1,
        UndoConfirm = 2,
        Utilize = 3,
        UndoUtilize = 4,
        Waste = 5,
        UndoWaste = 6,
        Drop = 7,
        Discard = 8,
        Evaluate = 9,
        UndoEvaluate = 10
    }

    public enum AbsenceScheduleAction
    {
        ChangeStartDate = 1,
        ChangeEndDate = 2,
        MoveStartDate = 3
    }

    public enum JobWorkflowAction
    {
        Complete = 1,
        UndoComplete = 2,
        Price = 3,
        UndoPrice = 4,
        Close = 5,
        UndoClose = 6,
        Discard = 7,
        UndoDiscard = 8,
        Evaluate = 9,
        UndoEvaluate = 10
    }

    public enum InvoiceWorkflowAction
    {
        Confirm = 1,
        UndoConfirm = 2,
        Issue = 3,
        Kill = 4,
        Discard = 5,
        Evaluate = 6,
        UndoEvaluate = 7
    }

    public class AbsenceLogDto
    {
        public decimal Id { get; set; }
        public DateTime LogDate { get; set; }
        public decimal UserId { get; set; }
        public string UserCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public decimal LogActionId { get; set; }
        public string ActionName { get; set; } = string.Empty;
    }
}
