using Api.Dtos;
using FluentValidation;

namespace Api.Modules.Validators;

public class UpdateFlowerDtoValidator : AbstractValidator<UpdateFlowerDto>
{
    public UpdateFlowerDtoValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Categories).NotEmpty();
    }
}