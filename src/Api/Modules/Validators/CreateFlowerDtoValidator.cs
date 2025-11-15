using Api.Dtos;
using FluentValidation;

namespace Api.Modules.Validators;

public class CreateFlowerDtoValidator : AbstractValidator<CreateFlowerDto>
{
    public CreateFlowerDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Categories).NotEmpty();
    }
}