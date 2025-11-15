using FluentValidation;

namespace Application.Flowers.Commands;

public class UpdateFlowerCommandValidator : AbstractValidator<UpdateFlowerCommand>
{
    public UpdateFlowerCommandValidator()
    {
        RuleFor(x => x.FlowerId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Categories).NotEmpty();
    }
}