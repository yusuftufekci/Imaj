using FluentValidation;
using Imaj.Service.DTOs;

namespace Imaj.Service.Validators
{
    /// <summary>
    /// LoginDto için FluentValidation kuralları.
    /// </summary>
    public class LoginDtoValidator : AbstractValidator<LoginDto>
    {
        public LoginDtoValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Kullanıcı adı zorunludur.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Şifre zorunludur.");
        }
    }
}
