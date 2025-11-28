using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using Domain.Customers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data.Customers;

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
    public async Task ShouldReturnCorrectContentTypeForGetRequests()
    {
        // Act
        var response = await Client.GetAsync(BaseRoute);

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
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
    public async Task ShouldReturnLocationHeaderOnCreate()
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
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Match("*/api/customers/*");
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
    public async Task ShouldCreateCustomerWithSpecialCharactersInName()
    {
        // Arrange
        var request = new CreateCustomerDto(
            "Марія-Ольга",
            "O'Brien-Müller",
            "maria@example.com",
            "+380501234567",
            "вул. Хрещатик, буд. 10, кв. 5");

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ShouldCreateCustomerWithUnicodeCharacters()
    {
        // Arrange
        var request = new CreateCustomerDto(
            "李明",
            "Müller",
            "test@例え.jp",
            "+380501234567",
            "вулиця Шевченка, 123");

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
    
    [Fact]
    public async Task ShouldCreateCustomerWithDifferentPhoneFormats()
    {
        // Arrange
        var validPhoneFormats = new[]
        {
            "+380501234567",
            "+38 050 123 45 67",
            "+38-050-123-45-67",
            "0501234567",
            "+1-555-123-4567"
        };

        foreach (var phone in validPhoneFormats)
        {
            var request = new CreateCustomerDto(
                "Test",
                "User",
                $"test{Math.Abs(phone.GetHashCode())}@example.com", 
                phone,
                "Test Address");

            // Act
            var response = await Client.PostAsJsonAsync(BaseRoute, request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created, 
                $"Phone format '{phone}' should be valid");
        }
    }
    
    [Fact]
    public async Task ShouldCreateCustomerWithMinimumValidPhoneLength()
    {
        // Arrange 
        var request = new CreateCustomerDto(
            _secondTestCustomer.FirstName,
            _secondTestCustomer.LastName,
            "minphone@example.com",
            "+12345678", 
            _secondTestCustomer.Address);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
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
        var request = new CreateCustomerDto("Test", "User", "testexample.com", "+380501234567", "Addr");
        var response = await Client.PostAsJsonAsync(BaseRoute, request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateCustomerWithInvalidEmailFormat_MissingDomain()
    {
        var request = new CreateCustomerDto("Test", "User", "test@", "+380501234567", "Addr");
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
    public async Task ShouldNotCreateCustomerWithWhitespaceFields()
    {
        var request = new CreateCustomerDto("   ", "   ", "   ", "   ", "   ");
        var response = await Client.PostAsJsonAsync(BaseRoute, request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateCustomerWithNullFields()
    {
        var request = new CreateCustomerDto(null!, null!, null!, null!, null!);
        var response = await Client.PostAsJsonAsync(BaseRoute, request);
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
    public async Task ShouldHandleSpecialCharactersInSearchFields()
    {
        // Arrange 
        var request = new CreateCustomerDto(
            "Test' OR '1'='1",
            "User",
            "sqltest@example.com",
            "+380501234567",
            "Address' OR '1'='1");

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
    
    [Fact]
    public async Task ShouldEnforceUniqueEmailConstraintAtDatabaseLevel()
    {
        var duplicateCustomer = Customer.New(
            CustomerId.New(),
            "Another",
            "Person",
            _firstTestCustomer.Email, 
            "+380509999999",
            "Some Address");

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await Context.Customers.AddAsync(duplicateCustomer);
            await Context.SaveChangesAsync();
        });
        
        // чистка  context від помилкового запису, щоб не ламати DisposeAsync
        Context.Entry(duplicateCustomer).State = EntityState.Detached;
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
        dbCustomer.Address.Should().Be(_thirdTestCustomer.Address);
    }
    
    [Fact]
    public async Task ShouldUpdateCustomerEmailWithDifferentCase()
    {
        // Arrange 
        var request = new UpdateCustomerDto(
            _firstTestCustomer.FirstName,
            _firstTestCustomer.LastName,
            _firstTestCustomer.Email.ToUpper(), 
            _firstTestCustomer.Phone,
            _firstTestCustomer.Address);

        // Act
        var response = await Client.PutAsJsonAsync(_detailRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var customer = await response.ToResponseModel<CustomerDto>();
        customer.Email.Should().Be(_firstTestCustomer.Email.ToUpper()); 
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
        var request = new UpdateCustomerDto("", "", "", "", "");
        var response = await Client.PutAsJsonAsync(_detailRoute, request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotUpdateCustomerWithWhitespaceFields()
    {
        var request = new UpdateCustomerDto("   ", "   ", "   ", "   ", "   ");
        var response = await Client.PutAsJsonAsync(_detailRoute, request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotUpdateCustomerWithInvalidEmail()
    {
        var request = new UpdateCustomerDto(_secondTestCustomer.FirstName, _secondTestCustomer.LastName, "invalid-email", _secondTestCustomer.Phone, _secondTestCustomer.Address);
        var response = await Client.PutAsJsonAsync(_detailRoute, request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotUpdateCustomerWithTooLongFields()
    {
        var request = new UpdateCustomerDto(
            new string('a', 101),
            new string('b', 101),
            new string('c', 256) + "@test.com",
            new string('1', 21),
            new string('d', 501));

        var response = await Client.PutAsJsonAsync(_detailRoute, request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    [Fact]
    public async Task ShouldNotUpdateCustomerWithInvalidPhoneFormat()
    {
        var request = new UpdateCustomerDto(
            _secondTestCustomer.FirstName,
            _secondTestCustomer.LastName,
            _secondTestCustomer.Email,
            "invalid-phone",
            _secondTestCustomer.Address);
        var response = await Client.PutAsJsonAsync(_detailRoute, request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task ShouldHandleConcurrentUpdatesOfSameCustomer()
    {
        // Arrange
        var request1 = new UpdateCustomerDto("Name1", "Last1", "email1@example.com", "+380501111111", "Address1");
        var request2 = new UpdateCustomerDto("Name2", "Last2", "email2@example.com", "+380502222222", "Address2");

        // Act
        var tasks = new[]
        {
            Client.PutAsJsonAsync(_detailRoute, request1),
            Client.PutAsJsonAsync(_detailRoute, request2)
        };
        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));
        var finalCustomer = await Context.Customers.AsNoTracking().FirstAsync(x => x.Id.Equals(_firstTestCustomer.Id));
        finalCustomer.Should().NotBeNull();
        // Перевіряємо, що значення відповідає одному з запитів 
        new[] { "email1@example.com", "email2@example.com" }.Should().Contain(finalCustomer.Email);
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