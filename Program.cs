using System.Reflection;
using System.Runtime.CompilerServices;
using AdvancedNetExercise.Common.Middleware;
using AdvancedNetExercise.Features.Products.DTOs;
using AdvancedNetExercise.Features.Products;
using AdvancedNetExercise.Validators;
using AdvancedNetExercise.Common.Mapping; 
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http.Extensions; 
using Microsoft.AspNetCore.Http; 


[assembly: InternalsVisibleTo("AdvancedNetExercise.Tests")]


LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());


builder.Services.AddSingleton<ICacheService, MockCacheService>();
builder.Services.AddSingleton<IProductRepository, MockProductRepository>();

builder.Services.AddSingleton<ApplicationContext>(); 


builder.Services.AddScoped<CreateProductHandler>();


builder.Services.AddValidatorsFromAssemblyContaining<CreateProductProfileValidator>(ServiceLifetime.Scoped);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Product Management API", Version = "v1" });
});


var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Management API v1"));
}


app.UseCorrelationMiddleware(); 

app.UseHttpsRedirection();



app.MapPost("/products", async (
    CreateProductProfileRequest request, 
    CreateProductHandler handler, 
    CancellationToken cancellationToken) =>
{
    try
    {
        var result = await handler.Handle(request, cancellationToken);
        
        return Results.Created($"/products/{result.Id}", result);
    }
    catch (ValidationException ex)
    {
        
        return Results.ValidationProblem(ex.Errors
            .ToDictionary(e => e.PropertyName, e => new[] { e.ErrorMessage }));
    }
    catch (Exception)
    {
        
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
})
.WithName("CreateProduct")
.WithDescription("Creates a new product and applies advanced mapping/validation logic.")
.Produces<ProductProfileDto>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest)
.WithOpenApi(); 

app.Run();


public partial class Program { }