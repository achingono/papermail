using FluentValidation;
using Papermail.Core.Entities;

namespace Papermail.Core.Validation;
/// <summary>
/// Provides baseline validation rules for <see cref="Provider"/> instances.
/// </summary>
public partial class ProviderValidator : AbstractValidator<Provider>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderValidator"/> class.
    /// </summary>
    public ProviderValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithName("Name");
    }
}