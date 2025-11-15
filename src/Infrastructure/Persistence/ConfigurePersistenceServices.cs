using Application.Common.Interfaces;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Infrastructure.Persistence;

public static class ConfigurePersistenceServices
{
    public static void AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(configuration.GetConnectionString("DefaultConnection"));
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<ApplicationDbContext>(options => options
            .UseNpgsql(
                dataSource,
                builder => builder.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning)));

        services.AddScoped<ApplicationDbContextInitialiser>();
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddRepositories();
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<FlowerRepository>();
        services.AddScoped<IFlowerRepository>(provider => provider.GetRequiredService<FlowerRepository>());
        services.AddScoped<IFlowerQueries>(provider => provider.GetRequiredService<FlowerRepository>());

        services.AddScoped<CategoryRepository>();
        services.AddScoped<ICategoryRepository>(provider => provider.GetRequiredService<CategoryRepository>());
        services.AddScoped<ICategoryQueries>(provider => provider.GetRequiredService<CategoryRepository>());

        services.AddScoped<CategoryFlowerRepository>();
        services.AddScoped<ICategoryFlowerRepository>(provider => provider.GetRequiredService<CategoryFlowerRepository>());

        services.AddScoped<FlowerImageRepository>();
        services.AddScoped<IFlowerImageRepository>(provider => provider.GetRequiredService<FlowerImageRepository>());

        services.AddScoped<CustomerRepository>();
        services.AddScoped<ICustomerRepository>(provider => provider.GetRequiredService<CustomerRepository>());
        services.AddScoped<ICustomerQueries>(provider => provider.GetRequiredService<CustomerRepository>());

        services.AddScoped<OrderRepository>();
        services.AddScoped<IOrderRepository>(provider => provider.GetRequiredService<OrderRepository>());
        services.AddScoped<IOrderQueries>(provider => provider.GetRequiredService<OrderRepository>());
    }
}