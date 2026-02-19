using System;

namespace Imaj.Service.DTOs.Security
{
    public class PermissionDecisionTraceDto
    {
        public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;
        public string Rule { get; set; } = string.Empty;
        public string Outcome { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
    }
}
