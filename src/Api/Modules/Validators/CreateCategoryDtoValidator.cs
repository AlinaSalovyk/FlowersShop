using Api.Dtos;
using FluentValidation;

namespace Api.Modules.Validators;

public class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
    }
}