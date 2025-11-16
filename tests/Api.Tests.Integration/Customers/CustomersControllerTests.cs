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
    private readonly Customer _thirdTestCustomer = CustomersData.ThirdTestCustomer();

    private const string BaseRoute = "api/customers";
    private readonly string _detailRoute;

    public CustomersControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        _detailRoute = $"{BaseRoute}/{_firstTestCustomer.Id.Value}";
    }

    #region GET Tests

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
    public async Task ShouldGetAllCustomers_WhenMultipleCustomersExist()
    {
        // Arrange
        await Context.Customers.AddRangeAsync(_secondTestCustomer, _thirdTestCustomer);
        await SaveChangesAsync();

        // Act
        var response = await Client.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var customers = await response.ToResponseModel<List<CustomerDto>>();
        customers.Should().HaveCount(3);
        customers.Should().Contain(c => c.Email == _firstTestCustomer.Email);
        customers.Should().Contain(c => c.Email == _secondTestCustomer.Email);
        customers.Should().Contain(c => c.Email == _thirdTestCustomer.Email);
    }

    [Fact]
    public async Task ShouldGetAllCustomers_WhenNoCustomersExist()
    {
        // Arrange
        Context.Customers.Remove(_firstTestCustomer);
        await SaveChangesAsync();

        // Act
        var response = await Client.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var customers = await response.ToResponseModel<List<CustomerDto>>();
        customers.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldGetCustomerById()
    {
        // Act
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
        customerDto.CreatedAt.Should().BeCloseTo(_firstTestCustomer.CreatedAt, TimeSpan.FromSeconds(1));
        customerDto.UpdatedAt.Should().Be(_firstTestCustomer.UpdatedAt);
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
    

    #endregion

    #region POST (Create) Tests

    [Fact]
    public async Task ShouldCreateCustomer()
    {
        // Arrange
        var request = new CreateCustomerDto(
            _secondTestCustomer.FirstName,
            _secondTestCustomer.LastName,
            _secondTestCustomer.Email,
            _secondTestCustomer.Phone,
            _secondTestCustomer.Address);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var customerDto = await response.ToResponseModel<CustomerDto>();
        var customerId = new CustomerId(customerDto.Id);

        customerDto.FirstName.Should().Be(_secondTestCustomer.FirstName);
        customerDto.LastName.Should().Be(_secondTestCustomer.LastName);
        customerDto.Email.Should().Be(_secondTestCustomer.Email);
        customerDto.Phone.Should().Be(_secondTestCustomer.Phone);
        customerDto.Address.Should().Be(_secondTestCustomer.Address);
        customerDto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        customerDto.UpdatedAt.Should().BeNull();

        var dbCustomer = await Context.Customers.FirstAsync(x => x.Id.Equals(customerId));
        dbCustomer.FirstName.Should().Be(_secondTestCustomer.FirstName);
        dbCustomer.LastName.Should().Be(_secondTestCustomer.LastName);
        dbCustomer.Email.Should().Be(_secondTestCustomer.Email);
        dbCustomer.Phone.Should().Be(_secondTestCustomer.Phone);
        dbCustomer.Address.Should().Be(_secondTestCustomer.Address);
        dbCustomer.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        dbCustomer.UpdatedAt.Should().BeNull();

        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/api/customers/{customerDto.Id}");
    }

    [Fact]
    public async Task ShouldNotCreateCustomerBecauseEmailDuplication()
    {
        // Arrange
        var request = new CreateCustomerDto(
            "Different",
            "Person",
            _firstTestCustomer.Email,
            "+380501111111",
            "Different Address");

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var customersCount = await Context.Customers.CountAsync();
        customersCount.Should().Be(1);
    }

    [Fact]
    public async Task ShouldNotCreateCustomerWithInvalidEmail()
    {
        // Arrange
        var request = new CreateCustomerDto(
            "Test",
            "User",
            "invalid-email",
            "+380501234567",
            "Test Address");

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateCustomerWithInvalidEmailFormat_MissingAt()
    {
        // Arrange
        var request = new CreateCustomerDto(
            "Test",
            "User",
            "testexample.com",
            "+380501234567",
            "Test Address");

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateCustomerWithInvalidEmailFormat_MissingDomain()
    {
        // Arrange
        var request = new CreateCustomerDto(
            "Test",
            "User",
            "test@",
            "+380501234567",
            "Test Address");

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateCustomerWithEmptyFields()
    {
        // Arrange
        var request = new CreateCustomerDto("", "", "", "", "");

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateCustomerWithWhitespaceFields()
    {
        // Arrange
        var request = new CreateCustomerDto("   ", "   ", "   ", "   ", "   ");

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateCustomerWithNullFields()
    {
        // Arrange
        var request = new CreateCustomerDto(null!, null!, null!, null!, null!);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateCustomerWithTooLongFields()
    {
        // Arrange
        var request = new CreateCustomerDto(
            new string('a', 101),
            new string('b', 101),
            new string('c', 256) + "@test.com",
            new string('1', 21),
            new string('d', 501));

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateCustomerWithTooLongFirstName()
    {
        // Arrange
        var request = new CreateCustomerDto(
            new string('a', 101),
            _secondTestCustomer.LastName,
            _secondTestCustomer.Email,
            _secondTestCustomer.Phone,
            _secondTestCustomer.Address);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateCustomerWithTooLongLastName()
    {
        // Arrange
        var request = new CreateCustomerDto(
            _secondTestCustomer.FirstName,
            new string('b', 101),
            _secondTestCustomer.Email,
            _secondTestCustomer.Phone,
            _secondTestCustomer.Address);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateCustomerWithTooLongEmail()
    {
        // Arrange
        var request = new CreateCustomerDto(
            _secondTestCustomer.FirstName,
            _secondTestCustomer.LastName,
            new string('c', 250) + "@test.com",
            _secondTestCustomer.Phone,
            _secondTestCustomer.Address);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateCustomerWithTooLongPhone()
    {
        // Arrange
        var request = new CreateCustomerDto(
            _secondTestCustomer.FirstName,
            _secondTestCustomer.LastName,
            _secondTestCustomer.Email,
            new string('1', 21),
            _secondTestCustomer.Address);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateCustomerWithTooLongAddress()
    {
        // Arrange
        var request = new CreateCustomerDto(
            _secondTestCustomer.FirstName,
            _secondTestCustomer.LastName,
            _secondTestCustomer.Email,
            _secondTestCustomer.Phone,
            new string('d', 501));

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldCreateCustomerWithMaximumAllowedFieldLengths()
    {
        // Arrange
        var request = new CreateCustomerDto(
            new string('a', 100),
            new string('b', 100),
            new string('c', 245) + "@test.com",
            new string('1', 20),
            new string('d', 500));

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ShouldNotCreateCustomerWithInvalidPhoneFormat()
    {
        // Arrange
        var request = new CreateCustomerDto(
            _secondTestCustomer.FirstName,
            _secondTestCustomer.LastName,
            _secondTestCustomer.Email,
            "invalid-phone",
            _secondTestCustomer.Address);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region PUT (Update) Tests

    [Fact]
    public async Task ShouldUpdateCustomer()
    {
        // Arrange
        var request = new UpdateCustomerDto(
            _thirdTestCustomer.FirstName,
            _thirdTestCustomer.LastName,
            _thirdTestCustomer.Email,
            _thirdTestCustomer.Phone,
            _thirdTestCustomer.Address);

        // Act
        var response = await Client.PutAsJsonAsync(_detailRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var customerDto = await response.ToResponseModel<CustomerDto>();
        customerDto.FirstName.Should().Be(_thirdTestCustomer.FirstName);
        customerDto.LastName.Should().Be(_thirdTestCustomer.LastName);
        customerDto.Email.Should().Be(_thirdTestCustomer.Email);
        customerDto.Phone.Should().Be(_thirdTestCustomer.Phone);
        customerDto.Address.Should().Be(_thirdTestCustomer.Address);
        customerDto.UpdatedAt.Should().NotBeNull();
        customerDto.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var dbCustomer = await Context.Customers.FirstAsync(x => x.Id.Equals(_firstTestCustomer.Id));
        dbCustomer.FirstName.Should().Be(_thirdTestCustomer.FirstName);
        dbCustomer.LastName.Should().Be(_thirdTestCustomer.LastName);
        dbCustomer.Email.Should().Be(_thirdTestCustomer.Email);
        dbCustomer.Phone.Should().Be(_thirdTestCustomer.Phone);
        dbCustomer.Address.Should().Be(_thirdTestCustomer.Address);
        dbCustomer.UpdatedAt.Should().NotBeNull();
        dbCustomer.CreatedAt.Should().Be(_firstTestCustomer.CreatedAt);
        dbCustomer.CreatedAt.Should().BeCloseTo(_firstTestCustomer.CreatedAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task ShouldNotUpdateCustomerWithDuplicateEmail()
    {
        // Arrange
        await Context.Customers.AddAsync(_secondTestCustomer);
        await SaveChangesAsync();

        var request = new UpdateCustomerDto(
            "Updated",
            "Name",
            _secondTestCustomer.Email,
            "+380501234567",
            "Some Address");

        // Act
        var response = await Client.PutAsJsonAsync(_detailRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var dbCustomer = await Context.Customers.FirstAsync(x => x.Id.Equals(_firstTestCustomer.Id));
        dbCustomer.Email.Should().Be(_firstTestCustomer.Email);
    }

    [Fact]
    public async Task ShouldUpdateCustomerWithSameEmail()
    {
        // Arrange
        var request = new UpdateCustomerDto(
            _thirdTestCustomer.FirstName,
            _thirdTestCustomer.LastName,
            _firstTestCustomer.Email,
            _thirdTestCustomer.Phone,
            _thirdTestCustomer.Address);

        // Act
        var response = await Client.PutAsJsonAsync(_detailRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var customerDto = await response.ToResponseModel<CustomerDto>();
        customerDto.Email.Should().Be(_firstTestCustomer.Email);
    }

    [Fact]
    public async Task ShouldNotUpdateNonExistentCustomer()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var route = $"{BaseRoute}/{nonExistentId}";
        var request = new UpdateCustomerDto(
            _secondTestCustomer.FirstName,
            _secondTestCustomer.LastName,
            _secondTestCustomer.Email,
            _secondTestCustomer.Phone,
            _secondTestCustomer.Address);

        // Act
        var response = await Client.PutAsJsonAsync(route, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldNotUpdateCustomerWithEmptyFields()
    {
        // Arrange
        var request = new UpdateCustomerDto("", "", "", "", "");

        // Act
        var response = await Client.PutAsJsonAsync(_detailRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotUpdateCustomerWithWhitespaceFields()
    {
        // Arrange
        var request = new UpdateCustomerDto("   ", "   ", "   ", "   ", "   ");

        // Act
        var response = await Client.PutAsJsonAsync(_detailRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotUpdateCustomerWithNullFields()
    {
        // Arrange
        var request = new UpdateCustomerDto(null!, null!, null!, null!, null!);

        // Act
        var response = await Client.PutAsJsonAsync(_detailRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotUpdateCustomerWithInvalidEmail()
    {
        // Arrange
        var request = new UpdateCustomerDto(
            _secondTestCustomer.FirstName,
            _secondTestCustomer.LastName,
            "invalid-email",
            _secondTestCustomer.Phone,
            _secondTestCustomer.Address);

        // Act
        var response = await Client.PutAsJsonAsync(_detailRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotUpdateCustomerWithTooLongFields()
    {
        // Arrange
        var request = new UpdateCustomerDto(
            new string('a', 101),
            new string('b', 101),
            new string('c', 256) + "@test.com",
            new string('1', 21),
            new string('d', 501));

        // Act
        var response = await Client.PutAsJsonAsync(_detailRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldUpdateCustomerWithMaximumAllowedFieldLengths()
    {
        // Arrange
        var request = new UpdateCustomerDto(
            new string('a', 100),
            new string('b', 100),
            new string('c', 245) + "@test.com",
            new string('1', 20),
            new string('d', 500));

        // Act
        var response = await Client.PutAsJsonAsync(_detailRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ShouldNotUpdateCustomerWithInvalidPhoneFormat()
    {
        // Arrange
        var request = new UpdateCustomerDto(
            _secondTestCustomer.FirstName,
            _secondTestCustomer.LastName,
            _secondTestCustomer.Email,
            "invalid-phone",
            _secondTestCustomer.Address);

        // Act
        var response = await Client.PutAsJsonAsync(_detailRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region DELETE Tests

    [Fact]
    public async Task ShouldDeleteCustomer()
    {
        // Act
        var response = await Client.DeleteAsync(_detailRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var customerDto = await response.ToResponseModel<CustomerDto>();
        customerDto.Id.Should().Be(_firstTestCustomer.Id.Value);
        customerDto.FirstName.Should().Be(_firstTestCustomer.FirstName);
        customerDto.LastName.Should().Be(_firstTestCustomer.LastName);
        customerDto.Email.Should().Be(_firstTestCustomer.Email);

        var dbCustomer = await Context.Customers.FirstOrDefaultAsync(x => x.Id.Equals(_firstTestCustomer.Id));
        dbCustomer.Should().BeNull();
    }

    [Fact]
    public async Task ShouldNotDeleteNonExistentCustomer()
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
    public async Task ShouldNotDeleteCustomerTwice()
    {
        // Arrange
        await Client.DeleteAsync(_detailRoute);

        // Act
        var response = await Client.DeleteAsync(_detailRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Initialization and Cleanup

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

    #endregion
}