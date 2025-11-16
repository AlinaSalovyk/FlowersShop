// D:\Api\FlowersShop\src\Application\Customers\Commands\CreateCustomerCommandValidator.cs

using FluentValidation;

namespace Application.Customers.Commands;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{

    private const string PhoneRegex = @"^\+?(\d[\s-]?){8,}$";

    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);

        RuleFor(x => x.Phone)
            .NotEmpty()
            .MaximumLength(20)
            .Matches(PhoneRegex)
            .WithMessage("Phone number format is invalid.");
            
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
    }
}