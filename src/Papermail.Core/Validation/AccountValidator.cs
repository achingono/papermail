using FluentValidation;
using Papermail.Core.Entities;

namespace Papermail.Core.Validation;
/// <summary>
/// Provides baseline validation rules for <see cref="Account"/> instances.
/// </summary>
public partial class AccountValidator : AbstractValidator<Account>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountValidator"/> class.
    /// </summary>
    public AccountValidator()
    {
        RuleFor(x => x.Sub).NotEmpty().WithName("Sub");
        RuleFor(x => x.EmailAddress).NotEmpty().EmailAddress().WithName("Email Address");
        RuleFor(x => x.ProviderId).NotEmpty().WithName("Provider");
    }
}