using FluentValidation;
using Imaj.Service.DTOs;

namespace Imaj.Service.Validators
{
    /// <summary>
    /// CustomerDto için FluentValidation kuralları.
    /// </summary>
    public class CustomerDtoValidator : AbstractValidator<CustomerDto>
    {
        public CustomerDtoValidator()
        {
            // Kod zorunlu ve max 8 karakter
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Kod alanı zorunludur.")
                .MaximumLength(8).WithMessage("Kod en fazla 8 karakter olabilir.");

            // Ad zorunlu ve max 32 karakter
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Ad alanı zorunludur.")
                .MaximumLength(32).WithMessage("Ad en fazla 32 karakter olabilir.");

            // Email format kontrolü (dolu ise)
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
                .When(x => !string.IsNullOrEmpty(x.Email))
                .MaximumLength(64).WithMessage("E-posta en fazla 64 karakter olabilir.");

            // Şehir max 32 karakter
            RuleFor(x => x.City)
                .MaximumLength(32).WithMessage("Şehir en fazla 32 karakter olabilir.");

            // Telefon max 32 karakter
            RuleFor(x => x.Phone)
                .MaximumLength(32).WithMessage("Telefon en fazla 32 karakter olabilir.");

            // Faks max 32 karakter
            RuleFor(x => x.Fax)
                .MaximumLength(32).WithMessage("Faks en fazla 32 karakter olabilir.");

            // Vergi Dairesi max 32 karakter
            RuleFor(x => x.TaxOffice)
                .MaximumLength(32).WithMessage("Vergi Dairesi en fazla 32 karakter olabilir.");

            // Vergi Numarası max 32 karakter
            RuleFor(x => x.TaxNumber)
                .MaximumLength(32).WithMessage("Vergi Numarası en fazla 32 karakter olabilir.");

            // Ülke max 32 karakter
            RuleFor(x => x.Country)
                .MaximumLength(32).WithMessage("Ülke en fazla 32 karakter olabilir.");

            // İlgili kişi max 32 karakter
            RuleFor(x => x.Contact)
                .MaximumLength(32).WithMessage("İlgili kişi en fazla 32 karakter olabilir.");

            // Fatura adı max 64 karakter
            RuleFor(x => x.InvoiceName)
                .MaximumLength(64).WithMessage("Fatura adı en fazla 64 karakter olabilir.");

            // Alan kodu max 32 karakter
            RuleFor(x => x.AreaCode)
                .MaximumLength(32).WithMessage("Alan kodu en fazla 32 karakter olabilir.");

            // Owner max 32 karakter
            RuleFor(x => x.Owner)
                .MaximumLength(32).WithMessage("Sahibi en fazla 32 karakter olabilir.");
        }
    }
}
