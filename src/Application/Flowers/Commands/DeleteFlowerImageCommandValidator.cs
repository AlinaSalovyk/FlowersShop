using FluentValidation;

namespace Application.Flowers.Commands;

public class DeleteFlowerImageCommandValidator : AbstractValidator<DeleteFlowerImageCommand>
{
    public DeleteFlowerImageCommandValidator()
    {
        RuleFor(x => x.FlowerId).NotEmpty();
        RuleFor(x => x.ImageId).NotEmpty();
    }
}