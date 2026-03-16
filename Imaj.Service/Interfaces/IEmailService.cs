using Imaj.Service.DTOs;
using Imaj.Service.Results;

namespace Imaj.Service.Interfaces
{
    public interface IEmailService
    {
        Task<ServiceResult> SendAsync(EmailMessageDto message, CancellationToken cancellationToken = default);
    }
}
