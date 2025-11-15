using Api.Dtos;
using Domain.Orders;
using FluentValidation;

namespace Api.Modules.Validators;

public class UpdateOrderStatusDtoValidator : AbstractValidator<UpdateOrderStatusDto>
{
    public UpdateOrderStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(BeValidStatus)
            .WithMessage("Invalid order status. Valid values are: Pending, Confirmed, Processing, Shipped, Delivered, Cancelled");
    }

    private bool BeValidStatus(string status)
    {
        return Enum.TryParse<OrderStatus>(status, true, out _);
    }
}