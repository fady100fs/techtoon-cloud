using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Techtoon.Api.Extensions;
using Techtoon.Api.HealthChecks;
using Techtoon.Api.Middleware;
using Techtoon.Api.Options;
using Techtoon.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Bind options
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<JwtOptions>>().Value);

// Core services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

// Application services
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuthAppService, AuthAppService>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IItemAppService, ItemAppService>();
builder.Services.AddScoped<IPartyAppService, PartyAppService>();
builder.Services.AddScoped<IInvoiceAppService, InvoiceAppService>();
builder.Services.AddScoped<IPaymentAppService, PaymentAppService>();
builder.Services.AddScoped<IExpenseAppService, ExpenseAppService>();
builder.Services.AddScoped<ICashBoxAppService, CashBoxAppService>();

// JWT Authentication
builder.Services.AddTechtoonJwt(builder.Configuration);

// Authorization (Permission-based)
builder.Services.AddTechtoonAuthorization();

// CORS (specific origins from config)
builder.Services.AddTechtoonCors(builder.Configuration);

// Swagger with JWT support
builder.Services.AddTechtoonSwagger();

// Health checks (custom Neon PostgreSQL check)
var connStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
builder.Services.AddHealthChecks()
    .AddCheck<NeonDbHealthCheck>("neon-postgres", tags: new[] { "db", "neon" });

var app = builder.Build();

// Pipeline
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<IdempotencyMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseCors("TechtoonMobile");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health endpoints
app.MapHealthChecks("/health");

app.Run();