using FluentValidation;

namespace Application.Orders.Queries;

public class GetSalesReportQueryValidator : AbstractValidator<GetSalesReportQuery>
{
    public GetSalesReportQueryValidator()
    {
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty()
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date must be greater than or equal to start date");
    }
}