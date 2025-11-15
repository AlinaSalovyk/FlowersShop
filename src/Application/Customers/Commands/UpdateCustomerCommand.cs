using Application.Common.Interfaces.Repositories;
using Application.Customers.Exceptions;
using Domain.Customers;
using LanguageExt;
using MediatR;

namespace Application.Customers.Commands;

public record UpdateCustomerCommand : IRequest<Either<CustomerException, Customer>>
{
    public required Guid CustomerId { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public required string Phone { get; init; }
    public required string Address { get; init; }
}

public class UpdateCustomerCommandHandler(ICustomerRepository customerRepository)
    : IRequestHandler<UpdateCustomerCommand, Either<CustomerException, Customer>>
{
    public async Task<Either<CustomerException, Customer>> Handle(
        UpdateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        var customerId = new CustomerId(request.CustomerId);
        var existingCustomer = await customerRepository.GetByIdAsync(customerId, cancellationToken);

        return await existingCustomer.MatchAsync(
            c => UpdateEntity(c, request, cancellationToken),
            () => Task.FromResult<Either<CustomerException, Customer>>(
                new CustomerNotFoundException(customerId)));
    }

    private async Task<Either<CustomerException, Customer>> UpdateEntity(
        Customer customer,
        UpdateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Перевка чи імеле не зайнятий іншим користувачем
            if (customer.Email != request.Email)
            {
                var existingCustomerWithEmail = await customerRepository.GetByEmailAsync(request.Email, cancellationToken);
                if (existingCustomerWithEmail.IsSome)
                {
                    return new CustomerEmailAlreadyExistsException(request.Email);
                }
            }

            customer.UpdateDetails(
                request.FirstName,
                request.LastName,
                request.Email,
                request.Phone,
                request.Address);

            return await customerRepository.UpdateAsync(customer, cancellationToken);
        }
        catch (Exception exception)
        {
            return new UnhandledCustomerException(customer.Id, exception);
        }
    }
}