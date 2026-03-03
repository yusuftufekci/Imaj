namespace Imaj.Service.DTOs
{
    public class ChangeCurrentUserPasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
