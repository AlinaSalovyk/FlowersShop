using Application.Common.Interfaces.Queries;
using Domain.Orders;
using MediatR;

namespace Application.Orders.Queries;

public record GetSalesReportQuery : IRequest<SalesReportDto>
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
}

public record SalesReportDto
{
    public decimal TotalRevenue { get; init; }
    public int TotalOrders { get; init; }
    public int TotalItemsSold { get; init; }
    public IReadOnlyList<TopFlowerDto> TopFlowers { get; init; } = [];
    public IReadOnlyList<DailySalesDto> DailySales { get; init; } = [];
}

public record TopFlowerDto
{
    public Guid FlowerId { get; init; }
    public string FlowerName { get; init; } = string.Empty;
    public int QuantitySold { get; init; }
    public decimal Revenue { get; init; }
}

public record DailySalesDto
{
    public DateTime Date { get; init; }
    public int OrdersCount { get; init; }
    public decimal Revenue { get; init; }
}

public class GetSalesReportQueryHandler(IOrderQueries orderQueries)
    : IRequestHandler<GetSalesReportQuery, SalesReportDto>
{
    public async Task<SalesReportDto> Handle(
        GetSalesReportQuery request,
        CancellationToken cancellationToken)
    {
        var orders = await orderQueries.GetByDateRangeAsync(
            request.StartDate,
            request.EndDate,
            cancellationToken);

        // Фільтруємо тільки завершені замовлення
        var completedOrders = orders
            .Where(o => o.Status == OrderStatus.Delivered)
            .ToList();

        var totalRevenue = completedOrders.Sum(o => o.TotalAmount);
        var totalOrders = completedOrders.Count;
        var totalItemsSold = completedOrders
            .SelectMany(o => o.Items ?? [])
            .Sum(i => i.Quantity);

        // Топ квітів
        var topFlowers = completedOrders
            .SelectMany(o => o.Items ?? [])
            .GroupBy(i => new { i.FlowerId, FlowerName = i.Flower?.Name ?? "Unknown" })
            .Select(g => new TopFlowerDto
            {
                FlowerId = g.Key.FlowerId.Value,
                FlowerName = g.Key.FlowerName,
                QuantitySold = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.Price * i.Quantity)
            })
            .OrderByDescending(f => f.Revenue)
            .Take(10)
            .ToList();

        // Продажі по днях
        var dailySales = completedOrders
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new DailySalesDto
            {
                Date = g.Key,
                OrdersCount = g.Count(),
                Revenue = g.Sum(o => o.TotalAmount)
            })
            .OrderBy(d => d.Date)
            .ToList();

        return new SalesReportDto
        {
            TotalRevenue = totalRevenue,
            TotalOrders = totalOrders,
            TotalItemsSold = totalItemsSold,
            TopFlowers = topFlowers,
            DailySales = dailySales
        };
    }
}