using Application.Common.Interfaces.Repositories;
using Application.Customers.Exceptions;
using Domain.Customers;
using LanguageExt;
using MediatR;

namespace Application.Customers.Commands;

public record CreateCustomerCommand : IRequest<Either<CustomerException, Customer>>
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public required string Phone { get; init; }
    public required string Address { get; init; }
}

public class CreateCustomerCommandHandler(ICustomerRepository customerRepository)
    : IRequestHandler<CreateCustomerCommand, Either<CustomerException, Customer>>
{
    public async Task<Either<CustomerException, Customer>> Handle(
        CreateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        var existingCustomer = await customerRepository.GetByEmailAsync(request.Email, cancellationToken);

        return await existingCustomer.MatchAsync(
            c => new CustomerEmailAlreadyExistsException(request.Email),
            () => CreateEntity(request, cancellationToken));
    }

    private async Task<Either<CustomerException, Customer>> CreateEntity(
        CreateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var customer = await customerRepository.AddAsync(
                Customer.New(
                    CustomerId.New(),
                    request.FirstName,
                    request.LastName,
                    request.Email,
                    request.Phone,
                    request.Address),
                cancellationToken);

            return customer;
        }
        catch (Exception exception)
        {
            return new UnhandledCustomerException(CustomerId.Empty(), exception);
        }
    }
}