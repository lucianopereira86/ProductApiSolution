using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using FluentValidation;
using ProductApi.Products.Commands.UpdateProduct;

var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddSingleton<InMemoryProductRepository>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured.");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateLifetime = true,
            RequireExpirationTime = true,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

            RoleClaimType = "role",
            NameClaimType = "name",

            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Products.Read", policy => policy.RequireClaim("permission", Permissions.ProductsRead));
    options.AddPolicy("Products.Write", policy => policy.RequireClaim("permission", Permissions.ProductsWrite));
    options.AddPolicy("Products.Owner", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new ProductOwnerRequirement());
    });
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddScoped<IAuthorizationHandler, ProductOwnerHandler>();

builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddScoped<EfProductRepository>();
builder.Services.AddScoped<IProductRepository>(sp =>
{
    var inner = sp.GetRequiredService<CachingProductRepository>();
    var logger = sp.GetRequiredService<ILogger<LoggingProductRepository>>();
    return new LoggingProductRepository(inner, logger);
});
builder.Services.AddScoped(sp =>
{
    var inner = sp.GetRequiredService<EfProductRepository>();
    var logger = sp.GetRequiredService<ILogger<CachingProductRepository>>();
    return new CachingProductRepository(inner, logger);
});
builder.Services.AddScoped<ProductService>();

builder.Services.AddScoped<LegacyBankSdk>();
builder.Services.AddScoped<IPaymentGateway, LegacyBankAdapter>();
builder.Services.AddScoped<CheckoutService>();

builder.Services.AddTransient<TransientService>();
builder.Services.AddScoped<ScopedService>();
builder.Services.AddSingleton<SingletonService>();
//builder.Services.AddSingleton<SingletonConsumer>();

builder.Services.AddScoped<ConsumerA>();
builder.Services.AddScoped<ConsumerB>();

builder.Services.AddScoped<DisposableService>();
builder.Services.AddScoped<DisposalConsumer>();

builder.Services.AddScoped<IPaymentStrategy, CreditCardPaymentStrategy>();
builder.Services.AddScoped<IPaymentStrategy, PixPaymentStrategy>();
builder.Services.AddScoped<IPaymentStrategy, BankSlipPaymentStrategy>();
builder.Services.AddScoped<PaymentStrategyResolver>();

builder.Services.AddScoped<ValidationHandler>();
builder.Services.AddScoped<FraudCheckHandler>();
builder.Services.AddScoped<PaymentExecutionHandler>();

builder.Services.AddScoped<IPaymentHandler>(sp =>
{
    var validation = sp.GetRequiredService<ValidationHandler>();
    var fraud = sp.GetRequiredService<FraudCheckHandler>();
    var execution = sp.GetRequiredService<PaymentExecutionHandler>();

    validation.SetNext(fraud).SetNext(execution);
    return validation;
});

builder.Services.AddScoped<IMediator, Mediator>();
builder.Services.AddScoped<IValidator<UpdateProductCommand>, UpdateProductCommandValidator>();

builder.Services.AddScoped<IRequestHandler<CreateProductCommand, int>, CreateProductHandler>();
builder.Services.AddScoped<IRequestHandler<UpdateProductCommand, bool>, UpdateProductHandler>();
builder.Services.AddScoped<IRequestHandler<GetProductByIdQuery, Product?>, GetProductByIdHandler>();

builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<RequestTimingMiddleware>();
app.UseExceptionHandler();
//app.UseMiddleware<ClientHeaderMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// app.MapPost("/products", async (ProductService service, CancellationToken cancellationToken) =>
// {
//     var result = await service.CreateAsync(1, "product 1", 10, 1, cancellationToken);
//     return Results.Ok(result);
// });

app.MapGet("/products/{id:int}", async (int id, ProductService service, CancellationToken cancellationToken) =>
{
    var result = await service.GetByIdAsync(id, cancellationToken);
    if (result == null)
        return Results.NotFound();
    return Results.Ok(result);
});

app.MapGet(
    "/lifetimes",
    (
        ConsumerA a,
        ConsumerB b) =>
    {
        // retornar IDs dos serviços
        return Results.Ok(new
        {
            ConsumerA = a,
            ConsumerB = b
        });
    });

app.MapGet("/disposal", (DisposalConsumer disposalConsumer) =>
{
    return Results.Ok(disposalConsumer.ServiceId);
});

app.MapGet("/payments", (PaymentMethod method, decimal amount, PaymentStrategyResolver resolver) =>
{
    var strategy = resolver.Resolve(method);
    var result = new PaymentProcessor(strategy).Process(amount);
    return Results.Ok(result);
});

app.MapPost("/payments/process", async (PaymentRequest request, IPaymentHandler handler, CancellationToken cancellationToken) =>
{
    await handler.HandleAsync(request, cancellationToken);
    return Results.Ok();
});

app.MapPost("/mediator/products", async (CreateProductRequest request, ICurrentUserService currentUserService, IMediator mediator, CancellationToken cancellationToken) =>
{
    var userId = currentUserService.UserId;

    if (string.IsNullOrWhiteSpace(userId))
        return Results.Unauthorized();

    var command = new CreateProductCommand(
        userId,
        request.Name,
        request.Price,
        request.Stock,
        request.Description
    );
    var productId = await mediator.SendAsync(command, cancellationToken);
    return Results.Created($"/mediator/products/{productId}", new { Id = productId });
})
.RequireAuthorization("Products.Write");

app.MapPut("/mediator/products/{id:int}", async (int id, UpdateProductRequest request, IMediator mediator, CancellationToken cancellationToken) =>
{
    if (id != request.Id)
        return Results.BadRequest("Route ID and body ID must match.");

    var command = new UpdateProductCommand(
        id,
        request.Name,
        request.Price,
        request.Stock,
        request.Description,
        request.RowVersion
    );
    var updated = await mediator.SendAsync(command, cancellationToken);

    return updated
        ? Results.NoContent()
        : Results.NotFound();
})
.RequireAuthorization("Products.Write");


app.MapGet("/mediator/products/{id:int}", async (int id, IMediator mediator, CancellationToken cancellationToken) =>
{
    var product = await mediator.SendAsync(new GetProductByIdQuery(id), cancellationToken);
    if (product is null)
        return Results.NotFound();
    var response = new ProductResponse(
        product.Id,
        product.Name,
        product.Price,
        product.Stock,
        product.Description,
        Convert.ToBase64String(product.RowVersion));

    return Results.Ok(response);
})
.RequireAuthorization("Products.Read");


app.MapPost("/checkout", async (CheckoutService service, CancellationToken cancellationToken) =>
{
    decimal amount = 10;
    await service.ProcessPaymentAsync(amount, cancellationToken);
    return Results.Ok();
});

if (app.Environment.IsDevelopment())
{
    app.MapGet("/test/error", () =>
    {
        throw new InvalidOperationException("Sensitive internal details");
    });

    app.MapPost("/auth/login", (LoginRequest request, ITokenService tokenService) =>
    {
        var users = new[]
        {
            new
            {
                UserId = "0121616a-9e31-40e6-9aed-d5213b164085",
                Username = "admin",
                Password = "Admin123!",
                Role = "Admin",
                Permissions = new [] {Permissions.ProductsRead, Permissions.ProductsWrite}
            },
            new
            {
                UserId = "5d49d441-5d99-429d-a644-22e40cd1d160",
                Username = "editor",
                Password = "Editor123!",
                Role = "Editor",
                Permissions = new [] {Permissions.ProductsRead, Permissions.ProductsWrite}
            },
            new
            {
                UserId = "2eb94b2d-f54e-455e-9046-57ef62ccf618",
                Username = "viewer",
                Password = "Viewer123!",
                Role = "Viewer",
                Permissions = new [] {Permissions.ProductsRead}
            }
        };
        string userId;
        string role = string.Empty;
        List<string> permissions = [];
        // admin
        if (request.Username.Equals(users[0].Username) && request.Password.Equals(users[0].Password))
        {
            role = users[0].Role;
            userId = users[0].UserId;
            permissions.AddRange(users[0].Permissions);
        }
        // editor
        else if (request.Username.Equals(users[1].Username) && request.Password.Equals(users[1].Password))
        {
            role = users[1].Role;
            userId = users[1].UserId;
            permissions.AddRange(users[1].Permissions);
        }
        // viewer
        else if (request.Username.Equals(users[2].Username) && request.Password.Equals(users[2].Password))
        {
            role = users[2].Role;
            userId = users[2].UserId;
            permissions.AddRange(users[2].Permissions);
        }
        else
        {
            return Results.Unauthorized();
        }

        var token = tokenService.GenerateToken(userId, request.Username, role, permissions);
        var loginResponse = new LoginResponse(token, "Bearer");

        return Results.Ok(loginResponse);
    })
    .AllowAnonymous();
}

app.Run();

public partial class Program()
{ 
}