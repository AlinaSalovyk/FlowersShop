using Api.Dtos;
using Api.Modules.Errors;
using Application.Common.Interfaces.Queries;
using Application.Orders.Commands;
using Application.Orders.Queries;
using Domain.Customers;
using Domain.Orders;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(
    ISender sender,
    IOrderQueries orderQueries) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetAll(CancellationToken cancellationToken)
    {
        var orders = await orderQueries.GetAllAsync(cancellationToken);
        return orders.Select(OrderDto.FromDomainModel).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetById(
        [FromRoute] Guid id,
        [FromServices] Application.Common.Interfaces.Repositories.IOrderRepository orderRepository,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(new OrderId(id), cancellationToken);

        return order.Match<ActionResult<OrderDto>>(
            o => OrderDto.FromDomainModel(o),
            () => NotFound());
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetByCustomer(
        [FromRoute] Guid customerId,
        CancellationToken cancellationToken)
    {
        var orders = await orderQueries.GetByCustomerIdAsync(new CustomerId(customerId), cancellationToken);
        return orders.Select(OrderDto.FromDomainModel).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(
        [FromBody] CreateOrderDto request,
        CancellationToken cancellationToken)
    {
        var command = new CreateOrderCommand
        {
            CustomerId = request.CustomerId,
            Items = request.Items.Select(i => new Application.Orders.Commands.OrderItemDto
            {
                FlowerId = i.FlowerId,
                Quantity = i.Quantity
            }).ToList()
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<OrderDto>>(
            o => CreatedAtAction(nameof(GetById), new { id = o.Id.Value }, OrderDto.FromDomainModel(o)),
            e => e.ToObjectResult());
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<OrderDto>> UpdateStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateOrderStatusDto request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateOrderStatusCommand
        {
            OrderId = id,
            Status = Enum.Parse<OrderStatus>(request.Status, true)
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<OrderDto>>(
            o => OrderDto.FromDomainModel(o),
            e => e.ToObjectResult());
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<OrderDto>> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteOrderCommand { OrderId = id };
        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<OrderDto>>(
            o => OrderDto.FromDomainModel(o),
            e => e.ToObjectResult());
    }

    [HttpGet("reports/sales")]
    public async Task<ActionResult<SalesReportDto>> GetSalesReport(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        var query = new GetSalesReportQuery
        {
            StartDate = startDate,
            EndDate = endDate
        };

        var result = await sender.Send(query, cancellationToken);
        return result;
    }
}