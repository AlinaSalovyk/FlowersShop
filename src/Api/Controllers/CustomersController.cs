using Api.Dtos;
using Api.Modules.Errors;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Customers.Commands;
using Domain.Customers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController(
    ISender sender,
    ICustomerQueries customerQueries,
    ICustomerRepository customerRepository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> GetAll(CancellationToken cancellationToken)
    {
        var customers = await customerQueries.GetAllAsync(cancellationToken);
        return customers.Select(CustomerDto.FromDomainModel).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(new CustomerId(id), cancellationToken);

        return customer.Match<ActionResult<CustomerDto>>(
            c => CustomerDto.FromDomainModel(c),
            () => NotFound());
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(
        [FromBody] CreateCustomerDto request,
        CancellationToken cancellationToken)
    {
        var command = new CreateCustomerCommand
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<CustomerDto>>(
            c => CreatedAtAction(nameof(GetById), new { id = c.Id.Value }, CustomerDto.FromDomainModel(c)),
            e => e.ToObjectResult());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateCustomerDto request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCustomerCommand
        {
            CustomerId = id,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<CustomerDto>>(
            c => CustomerDto.FromDomainModel(c),
            e => e.ToObjectResult());
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteCustomerCommand { CustomerId = id };
        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<CustomerDto>>(
            c => CustomerDto.FromDomainModel(c),
            e => e.ToObjectResult());
    }
}