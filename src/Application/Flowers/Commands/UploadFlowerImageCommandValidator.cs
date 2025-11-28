using FluentValidation;

namespace Application.Flowers.Commands;

public class UploadFlowerImagesCommandValidator : AbstractValidator<UploadFlowerImagesCommand>
{
    public UploadFlowerImagesCommandValidator()
    {
        RuleFor(x => x.FlowerId).NotEmpty();
        RuleFor(x => x.Images).NotEmpty();
        RuleFor(x => x.Images).NotEmpty().WithMessage("No files provided");
        RuleForEach(x => x.Images).ChildRules(image =>
        {
            image.RuleFor(x => x.OriginalName).NotEmpty();
            image.RuleFor(x => x.FileStream).NotNull();
        });
    }
}