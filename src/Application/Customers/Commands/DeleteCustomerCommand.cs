using Application.Common.Interfaces.Repositories;
using Application.Customers.Exceptions;
using Domain.Customers;
using LanguageExt;
using MediatR;

namespace Application.Customers.Commands;

public record DeleteCustomerCommand : IRequest<Either<CustomerException, Customer>>
{
    public required Guid CustomerId { get; init; }
}

public class DeleteCustomerCommandHandler(ICustomerRepository customerRepository)
    : IRequestHandler<DeleteCustomerCommand, Either<CustomerException, Customer>>
{
    public async Task<Either<CustomerException, Customer>> Handle(
        DeleteCustomerCommand request,
        CancellationToken cancellationToken)
    {
        var customerId = new CustomerId(request.CustomerId);
        var existingCustomer = await customerRepository.GetByIdAsync(customerId, cancellationToken);

        return await existingCustomer.MatchAsync(
            c => DeleteEntity(c, cancellationToken),
            () => Task.FromResult<Either<CustomerException, Customer>>(
                new CustomerNotFoundException(customerId)));
    }

    private async Task<Either<CustomerException, Customer>> DeleteEntity(
        Customer customer,
        CancellationToken cancellationToken)
    {
        try
        {
            return await customerRepository.DeleteAsync(customer, cancellationToken);
        }
        catch (Exception exception)
        {
            return new UnhandledCustomerException(customer.Id, exception);
        }
    }
}