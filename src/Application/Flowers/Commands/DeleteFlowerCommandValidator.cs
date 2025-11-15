using FluentValidation;

namespace Application.Flowers.Commands;

public class DeleteFlowerCommandValidator : AbstractValidator<DeleteFlowerCommand>
{
    public DeleteFlowerCommandValidator()
    {
        RuleFor(x => x.FlowerId).NotEmpty();
    }
}