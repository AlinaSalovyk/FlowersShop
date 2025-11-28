using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using Application.Orders.Queries;
using Domain.Customers;
using Domain.Flowers;
using Domain.Orders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data.Categories;
using Tests.Data.Customers;
using Tests.Data.Flowers;
using Tests.Data.Orders;

namespace Api.Tests.Integration.Orders;

public class OrdersControllerTests : BaseIntegrationTest, IAsyncLifetime
{
    private readonly Customer _firstTestCustomer = CustomersData.FirstTestCustomer();
    private readonly Customer _secondTestCustomer = CustomersData.SecondTestCustomer();

    private readonly Flower _firstTestFlower = FlowersData.FirstTestFlower();
    private readonly Flower _secondTestFlower = FlowersData.SecondTestFlower();

    private readonly Order _firstTestOrder;

    private const string BaseRoute = "api/orders";

    private readonly string _detailRoute;
    private readonly string _statusRoute;
    private readonly string _customerOrdersRoute;

    public OrdersControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        var orderItems = new List<OrderItem>
        {
            OrdersData.FirstTestOrderItem(
                OrderId.New(),
                _firstTestFlower.Id,
                2,
                _firstTestFlower.Price)
        };

        _firstTestOrder = OrdersData.FirstTestOrder(_firstTestCustomer.Id, orderItems);

        _detailRoute = $"{BaseRoute}/{_firstTestOrder.Id.Value}";
        _statusRoute = $"{BaseRoute}/{_firstTestOrder.Id.Value}/status";
        _customerOrdersRoute = $"{BaseRoute}/customer/{_firstTestCustomer.Id.Value}";
    }

    #region GET Tests

    [Fact]
    public async Task ShouldGetAllOrders()
    {
        // Act
        var response = await Client.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.ToResponseModel<List<OrderDto>>();
        orders.Should().HaveCount(1);
        orders.First().CustomerId.Should().Be(_firstTestCustomer.Id.Value);
        orders.First().TotalAmount.Should().Be(_firstTestFlower.Price * 2);
        orders.First().Status.Should().Be(OrderStatus.Pending.ToString());
    }

    [Fact]
    public async Task ShouldGetEmptyListWhenNoOrders()
    {
        // Arrange
        await ClearDatabaseAsync();

        // Act
        var response = await Client.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.ToResponseModel<List<OrderDto>>();
        orders.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldGetOrderById()
    {
        // Act
        var response = await Client.GetAsync(_detailRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orderDto = await response.ToResponseModel<OrderDto>();
        orderDto.Id.Should().Be(_firstTestOrder.Id.Value);
        orderDto.CustomerId.Should().Be(_firstTestCustomer.Id.Value);
        orderDto.Status.Should().Be(OrderStatus.Pending.ToString());
        orderDto.Items.Should().HaveCount(1);
        orderDto.Items.First().Quantity.Should().Be(2);
        orderDto.Items.First().FlowerId.Should().Be(_firstTestFlower.Id.Value);
    }

    [Fact]
    public async Task ShouldReturnNotFoundWhenOrderDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var route = $"{BaseRoute}/{nonExistentId}";

        // Act
        var response = await Client.GetAsync(route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldGetOrdersByCustomer()
    {
        // Act
        var response = await Client.GetAsync(_customerOrdersRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.ToResponseModel<List<OrderDto>>();
        orders.Should().HaveCount(1);
        orders.First().CustomerId.Should().Be(_firstTestCustomer.Id.Value);
    }

    [Fact]
    public async Task ShouldGetEmptyListForCustomerWithoutOrders()
    {
        // Arrange
        var emptyCustomerRoute = $"{BaseRoute}/customer/{_secondTestCustomer.Id.Value}";

        // Act
        var response = await Client.GetAsync(emptyCustomerRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.ToResponseModel<List<OrderDto>>();
        orders.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldGetEmptyListForNonExistentCustomer()
    {
        // Arrange
        var nonExistentCustomerRoute = $"{BaseRoute}/customer/{Guid.NewGuid()}";

        // Act
        var response = await Client.GetAsync(nonExistentCustomerRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.ToResponseModel<List<OrderDto>>();
        orders.Should().BeEmpty();
    }

    #endregion

    #region POST (Create) Tests

    [Fact]
    public async Task ShouldCreateOrder()
    {
        // Arrange
        var request = new CreateOrderDto(
            CustomerId: _firstTestCustomer.Id.Value,
            Items:
            [
                new CreateOrderItemDto(
                    FlowerId: _secondTestFlower.Id.Value,
                    Quantity: 3)
            ]);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var orderDto = await response.ToResponseModel<OrderDto>();
        var orderId = new OrderId(orderDto.Id);

        var dbOrder = await Context.Orders
            .Include(o => o.Items)
            .AsNoTracking()
            .FirstAsync(x => x.Id.Equals(orderId));

        dbOrder.CustomerId.Should().Be(_firstTestCustomer.Id);
        dbOrder.Status.Should().Be(OrderStatus.Pending);
        dbOrder.Items.Should().HaveCount(1);
        dbOrder.Items!.First().Quantity.Should().Be(3);
        dbOrder.TotalAmount.Should().Be(_secondTestFlower.Price * 3);
        
        var dbFlower = await Context.Flowers.AsNoTracking().FirstAsync(f => f.Id.Equals(_secondTestFlower.Id));
        dbFlower.StockQuantity.Should().Be(_secondTestFlower.StockQuantity - 3);
    }

    [Fact]
    public async Task ShouldCreateOrderWithMultipleItems()
    {
        // Arrange
        var request = new CreateOrderDto(
            CustomerId: _firstTestCustomer.Id.Value,
            Items:
            [
                new CreateOrderItemDto(FlowerId: _firstTestFlower.Id.Value, Quantity: 2),
                new CreateOrderItemDto(FlowerId: _secondTestFlower.Id.Value, Quantity: 3)
            ]);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var orderDto = await response.ToResponseModel<OrderDto>();
        var orderId = new OrderId(orderDto.Id);

        var dbOrder = await Context.Orders
            .Include(o => o.Items)
            .AsNoTracking()
            .FirstAsync(x => x.Id.Equals(orderId));

        dbOrder.Items.Should().HaveCount(2);
        dbOrder.TotalAmount.Should().Be((_firstTestFlower.Price * 2) + (_secondTestFlower.Price * 3));
    }

    [Fact]
    public async Task ShouldNotCreateOrderWithNonExistentCustomer()
    {
        // Arrange
        var request = new CreateOrderDto(
            CustomerId: Guid.NewGuid(),
            Items:
            [
                new CreateOrderItemDto(
                    FlowerId: _firstTestFlower.Id.Value,
                    Quantity: 1)
            ]);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldNotCreateOrderWithNonExistentFlower()
    {
        // Arrange
        var request = new CreateOrderDto(
            CustomerId: _firstTestCustomer.Id.Value,
            Items:
            [
                new CreateOrderItemDto(
                    FlowerId: Guid.NewGuid(),
                    Quantity: 1)
            ]);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldNotCreateOrderWithEmptyItems()
    {
        // Arrange
        var request = new CreateOrderDto(
            CustomerId: _firstTestCustomer.Id.Value,
            Items: []);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateOrderWithInsufficientStock()
    {
        // Arrange
        var initialStock = _firstTestFlower.StockQuantity;
        var request = new CreateOrderDto(
            CustomerId: _firstTestCustomer.Id.Value,
            Items:
            [
                new CreateOrderItemDto(
                    FlowerId: _firstTestFlower.Id.Value,
                    Quantity: initialStock + 10)
            ]);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var dbFlower = await Context.Flowers.AsNoTracking().FirstAsync(f => f.Id.Equals(_firstTestFlower.Id));
        dbFlower.StockQuantity.Should().Be(initialStock);
    }

    [Fact]
    public async Task ShouldNotCreateOrderWithInvalidQuantity()
    {
        // Arrange
        var request = new CreateOrderDto(
            CustomerId: _firstTestCustomer.Id.Value,
            Items:
            [
                new CreateOrderItemDto(
                    FlowerId: _firstTestFlower.Id.Value,
                    Quantity: 0)
            ]);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateOrderWithNegativeQuantity()
    {
        // Arrange
        var request = new CreateOrderDto(
            CustomerId: _firstTestCustomer.Id.Value,
            Items:
            [
                new CreateOrderItemDto(
                    FlowerId: _firstTestFlower.Id.Value,
                    Quantity: -5)
            ]);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateOrderWithEmptyCustomerId()
    {
        // Arrange
        var request = new CreateOrderDto(
            CustomerId: Guid.Empty,
            Items:
            [
                new CreateOrderItemDto(
                    FlowerId: _firstTestFlower.Id.Value,
                    Quantity: 1)
            ]);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateOrderWithEmptyFlowerId()
    {
        // Arrange
        var request = new CreateOrderDto(
            CustomerId: _firstTestCustomer.Id.Value,
            Items:
            [
                new CreateOrderItemDto(
                    FlowerId: Guid.Empty,
                    Quantity: 1)
            ]);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region PATCH (UpdateStatus) Tests

    [Fact]
    public async Task ShouldUpdateOrderStatus()
    {
        // Arrange
        var request = new UpdateOrderStatusDto(Status: OrderStatus.Confirmed.ToString());

        // Act
        var response = await Client.PatchAsync(_statusRoute, JsonContent.Create(request));

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var orderDto = await response.ToResponseModel<OrderDto>();
        orderDto.Status.Should().Be(OrderStatus.Confirmed.ToString());
        orderDto.UpdatedAt.Should().NotBeNull();

        var dbOrder = await Context.Orders.AsNoTracking().FirstAsync(x => x.Id.Equals(_firstTestOrder.Id));
        dbOrder.Status.Should().Be(OrderStatus.Confirmed);
        dbOrder.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ShouldUpdateOrderStatusToDelivered()
    {
        // Arrange
        var request = new UpdateOrderStatusDto(Status: OrderStatus.Delivered.ToString());

        // Act
        var response = await Client.PatchAsync(_statusRoute, JsonContent.Create(request));

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var orderDto = await response.ToResponseModel<OrderDto>();
        orderDto.Status.Should().Be(OrderStatus.Delivered.ToString());
    }

    [Fact]
    public async Task ShouldUpdateOrderStatusToCancelled()
    {
        // Arrange
        var request = new UpdateOrderStatusDto(Status: OrderStatus.Cancelled.ToString());

        // Act
        var response = await Client.PatchAsync(_statusRoute, JsonContent.Create(request));

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var orderDto = await response.ToResponseModel<OrderDto>();
        orderDto.Status.Should().Be(OrderStatus.Cancelled.ToString());
    }

    [Fact]
    public async Task ShouldNotUpdateOrderStatusWithInvalidStatus()
    {
        // Arrange
        var request = new UpdateOrderStatusDto(Status: "InvalidStatus");

        // Act
        var response = await Client.PatchAsync(_statusRoute, JsonContent.Create(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotUpdateStatusOfNonExistentOrder()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var route = $"{BaseRoute}/{nonExistentId}/status";
        var request = new UpdateOrderStatusDto(Status: OrderStatus.Confirmed.ToString());

        // Act
        var response = await Client.PatchAsync(route, JsonContent.Create(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldNotUpdateStatusWithEmptyStatus()
    {
        // Arrange
        var request = new UpdateOrderStatusDto(Status: "");

        // Act
        var response = await Client.PatchAsync(_statusRoute, JsonContent.Create(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region DELETE Tests

    [Fact]
    public async Task ShouldDeleteOrder()
    {
        // Act
        var response = await Client.DeleteAsync(_detailRoute);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var orderDto = await response.ToResponseModel<OrderDto>();
        orderDto.Id.Should().Be(_firstTestOrder.Id.Value);

        var dbOrder = await Context.Orders.FirstOrDefaultAsync(x => x.Id.Equals(_firstTestOrder.Id));
        dbOrder.Should().BeNull();
    }

    [Fact]
    public async Task ShouldDeleteOrderAndItsItems()
    {
        // Act
        var response = await Client.DeleteAsync(_detailRoute);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var dbOrderItems = await Context.Set<OrderItem>()
            .Where(x => x.OrderId.Equals(_firstTestOrder.Id))
            .ToListAsync();
        dbOrderItems.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldNotDeleteNonExistentOrder()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var route = $"{BaseRoute}/{nonExistentId}";

        // Act
        var response = await Client.DeleteAsync(route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldNotDeleteOrderWithEmptyId()
    {
        // Arrange
        var route = $"{BaseRoute}/{Guid.Empty}";

        // Act
        var response = await Client.DeleteAsync(route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Reports Tests

    [Fact]
    public async Task ShouldGetSalesReport()
    {
        // Arrange
        var order = await Context.Orders.FirstAsync(o => o.Id == _firstTestOrder.Id);

        Context.Entry(order).Property(x => x.Status).CurrentValue = OrderStatus.Delivered;
        Context.Entry(order).Property(x => x.CreatedAt).CurrentValue = DateTime.UtcNow.AddDays(-1); 
        await Context.SaveChangesAsync(); 
        Context.ChangeTracker.Clear();
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow.AddDays(1);
        var route = $"{BaseRoute}/reports/sales?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";

        // Act
        var response = await Client.GetAsync(route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var report = await response.ToResponseModel<SalesReportDto>();
        report.Should().NotBeNull();
        report.TotalOrders.Should().Be(1); 
        report.TotalRevenue.Should().BeGreaterThan(0);
        report.TotalItemsSold.Should().Be(2);
        report.TopFlowers.Should().NotBeEmpty();
        report.DailySales.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ShouldGetEmptySalesReportForDateRangeWithoutOrders()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddYears(-10);
        var endDate = DateTime.UtcNow.AddYears(-9);
        var route = $"{BaseRoute}/reports/sales?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";

        // Act
        var response = await Client.GetAsync(route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var report = await response.ToResponseModel<SalesReportDto>();
        report.TotalOrders.Should().Be(0);
        report.TotalRevenue.Should().Be(0);
        report.TotalItemsSold.Should().Be(0);
    }

    [Fact]
    public async Task ShouldNotIncludePendingOrdersInSalesReport()
    {
        // Arrange 
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow.AddDays(1);
        var route = $"{BaseRoute}/reports/sales?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";

        // Act
        var response = await Client.GetAsync(route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var report = await response.ToResponseModel<SalesReportDto>();
        report.TotalOrders.Should().Be(0);
    }

    [Fact]
    public async Task ShouldNotGetSalesReportWithInvalidDateRange()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(1);
        var endDate = DateTime.UtcNow.AddDays(-1);
        var route = $"{BaseRoute}/reports/sales?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";

        // Act
        var response = await Client.GetAsync(route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    public async Task InitializeAsync()
    {
        await ClearDatabaseAsync();

        var category = CategoriesData.FirstTestCategory();

        await Context.Categories.AddAsync(category);
        await Context.Customers.AddAsync(_firstTestCustomer);
        await Context.Customers.AddAsync(_secondTestCustomer);
        await Context.Flowers.AddAsync(_firstTestFlower);
        await Context.Flowers.AddAsync(_secondTestFlower);
        await Context.Orders.AddAsync(_firstTestOrder);
        await SaveChangesAsync();
        Context.ChangeTracker.Clear();
    }

    public async Task DisposeAsync()
    {
        await ClearDatabaseAsync();
    }

    private async Task ClearDatabaseAsync()
    {
        var orders = await Context.Orders.Include(o => o.Items).ToListAsync();
        Context.Orders.RemoveRange(orders);
        await SaveChangesAsync();

        Context.Flowers.RemoveRange(Context.Flowers);
        Context.Customers.RemoveRange(Context.Customers);
        Context.Categories.RemoveRange(Context.Categories);

        await SaveChangesAsync();
        Context.ChangeTracker.Clear();
    }
}