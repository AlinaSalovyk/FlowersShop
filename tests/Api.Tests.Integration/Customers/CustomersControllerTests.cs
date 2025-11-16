using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using Domain.Customers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data.Customers;
using Xunit;

namespace Api.Tests.Integration.Customers;

public class CustomersControllerTests : BaseIntegrationTest, IAsyncLifetime
{
    private readonly Customer _firstTestCustomer = CustomersData.FirstTestCustomer();
    private readonly Customer _secondTestCustomer = CustomersData.SecondTestCustomer();
    // _thirdTestCustomer не використовується в тестах нижче, але я залишив його, якщо він потрібен для майбутнього
    private readonly Customer _thirdTestCustomer = CustomersData.ThirdTestCustomer();

    private const string BaseRoute = "api/customers";
    
    // 1. Поле для спільного маршруту
    private readonly string _detailRoute;

    public CustomersControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        // 2. Ініціалізація маршруту до першого клієнта
        _detailRoute = $"{BaseRoute}/{_firstTestCustomer.Id.Value}";
    }

    [Fact]
    public async Task ShouldGetAllCustomers()
    {
        // Act
        var response = await Client.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var customers = await response.ToResponseModel<List<CustomerDto>>();
        customers.Should().HaveCount(1);
        customers.First().Email.Should().Be(_firstTestCustomer.Email);
    }

    [Fact]
    public async Task ShouldGetCustomerById()
    {
        // Act
        // 3. Використовуємо _detailRoute
        var response = await Client.GetAsync(_detailRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var customerDto = await response.ToResponseModel<CustomerDto>();
        customerDto.Id.Should().Be(_firstTestCustomer.Id.Value);
        customerDto.FirstName.Should().Be(_firstTestCustomer.FirstName);
        customerDto.LastName.Should().Be(_firstTestCustomer.LastName);
        customerDto.Email.Should().Be(_firstTestCustomer.Email);
        customerDto.Phone.Should().Be(_firstTestCustomer.Phone);
        customerDto.Address.Should().Be(_firstTestCustomer.Address);
    }

    [Fact]
    public async Task ShouldReturnNotFoundWhenCustomerDoesNotExist()
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
    public async Task ShouldCreateCustomer()
    {
        // Arrange
        var request = new CreateCustomerDto(
            FirstName: _secondTestCustomer.FirstName,
            LastName: _secondTestCustomer.LastName,
            Email: _secondTestCustomer.Email,
            Phone: _secondTestCustomer.Phone,
            Address: _secondTestCustomer.Address);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var customerDto = await response.ToResponseModel<CustomerDto>();
        var customerId = new CustomerId(customerDto.Id);

        var dbCustomer = await Context.Customers.FirstAsync(x => x.Id.Equals(customerId));
        dbCustomer.FirstName.Should().Be(_secondTestCustomer.FirstName);
        dbCustomer.LastName.Should().Be(_secondTestCustomer.LastName);
        dbCustomer.Email.Should().Be(_secondTestCustomer.Email);
        dbCustomer.Phone.Should().Be(_secondTestCustomer.Phone);
        dbCustomer.Address.Should().Be(_secondTestCustomer.Address);
        dbCustomer.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        dbCustomer.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public async Task ShouldNotCreateCustomerBecauseEmailDuplication()
    {
        // Arrange
        var request = new CreateCustomerDto(
            FirstName: "Different",
            LastName: "Person",
            Email: _firstTestCustomer.Email, // Duplicate email
            Phone: "+380501111111",
            Address: "Different Address");

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ShouldNotCreateCustomerWithInvalidEmail()
    {
        var request = new CreateCustomerDto(
            FirstName: "Test",
            LastName: "User",
            Email: "invalid-email",
            Phone: "+380501234567",
            Address: "Test Address");

        var response = await Client.PostAsJsonAsync(BaseRoute, request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateCustomerWithEmptyFields()
    {
        var request = new CreateCustomerDto("", "", "", "", "");
        var response = await Client.PostAsJsonAsync(BaseRoute, request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateCustomerWithTooLongFields()
    {
        var request = new CreateCustomerDto(
            FirstName: new string('a', 101),
            LastName: new string('b', 101),
            Email: new string('c', 256) + "@test.com",
            Phone: new string('1', 21),
            Address: new string('d', 501));

        var response = await Client.PostAsJsonAsync(BaseRoute, request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldUpdateCustomer()
    {
        // Arrange
        var request = new UpdateCustomerDto(
            FirstName: "Updated FirstName",
            LastName: "Updated LastName",
            Email: "updated.email@example.com",
            Phone: "+380679999999",
            Address: "Updated Address, New City");

        // Act
        // 3. Використовуємо _detailRoute
        var response = await Client.PutAsJsonAsync(_detailRoute, request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var customerDto = await response.ToResponseModel<CustomerDto>();
        customerDto.FirstName.Should().Be("Updated FirstName");
        customerDto.LastName.Should().Be("Updated LastName");
        customerDto.Email.Should().Be("updated.email@example.com");
        customerDto.UpdatedAt.Should().NotBeNull();

        var dbCustomer = await Context.Customers.FirstAsync(x => x.Id.Equals(_firstTestCustomer.Id));
        dbCustomer.FirstName.Should().Be("Updated FirstName");
        dbCustomer.LastName.Should().Be("Updated LastName");
        dbCustomer.Email.Should().Be("updated.email@example.com");
        dbCustomer.Phone.Should().Be("+380679999999");
        dbCustomer.Address.Should().Be("Updated Address, New City");
        dbCustomer.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ShouldNotUpdateCustomerWithDuplicateEmail()
    {
        // Arrange
        await Context.Customers.AddAsync(_secondTestCustomer);
        await SaveChangesAsync();

        var request = new UpdateCustomerDto(
            FirstName: "Updated",
            LastName: "Name",
            Email: _secondTestCustomer.Email, // Trying to use existing email
            Phone: "+380501234567",
            Address: "Some Address");

        // Act
        // 3. Тут ми оновлюємо _firstTestCustomer, тому _detailRoute підходить
        var response = await Client.PutAsJsonAsync(_detailRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ShouldNotUpdateNonExistentCustomer()
    {
        var nonExistentId = Guid.NewGuid();
        var route = $"{BaseRoute}/{nonExistentId}";
        var request = new UpdateCustomerDto(
            FirstName: "Updated",
            LastName: "Name",
            Email: "email@test.com",
            Phone: "+380501234567",
            Address: "Address");

        var response = await Client.PutAsJsonAsync(route, request);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldDeleteCustomer()
    {
        // Act
        // 3. Використовуємо _detailRoute
        var response = await Client.DeleteAsync(_detailRoute);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var customerDto = await response.ToResponseModel<CustomerDto>();
        customerDto.Id.Should().Be(_firstTestCustomer.Id.Value);

        var dbCustomer = await Context.Customers.FirstOrDefaultAsync(x => x.Id.Equals(_firstTestCustomer.Id));
        dbCustomer.Should().BeNull();
    }

    [Fact]
    public async Task ShouldNotDeleteNonExistentCustomer()
    {
        var nonExistentId = Guid.NewGuid();
        var route = $"{BaseRoute}/{nonExistentId}";

        var response = await Client.DeleteAsync(route);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public async Task InitializeAsync()
    {
        await Context.Customers.AddAsync(_firstTestCustomer);
        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        Context.Customers.RemoveRange(Context.Customers);
        await SaveChangesAsync();
    }
}