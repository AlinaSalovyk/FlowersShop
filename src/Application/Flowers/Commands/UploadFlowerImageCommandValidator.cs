using FluentValidation;

namespace Application.Flowers.Commands;

public class UploadFlowerImageCommandValidator : AbstractValidator<UploadFlowerImageCommand>
{
    public UploadFlowerImageCommandValidator()
    {
        RuleFor(x => x.FlowerId).NotEmpty();
        RuleFor(x => x.OriginalName).NotEmpty();
        RuleFor(x => x.FileStream).NotNull();
    }
}