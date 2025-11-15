using Api.Modules;
using Application;
using Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.SetupServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Flowers Shop API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Flowers Shop API Documentation";
    });
}

await app.InitialiseDatabaseAsync();

app.UseCors();
app.MapControllers();

app.Run();

public partial class Program { }