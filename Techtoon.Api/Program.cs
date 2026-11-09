using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Techtoon.Api.Extensions;
using Techtoon.Api.HealthChecks;
using Techtoon.Api.Middleware;
using Techtoon.Api.Options;
using Techtoon.Api.Services;
using Techtoon.Data.Context;

var builder = WebApplication.CreateBuilder(args);

// H5: the app runs behind Fly's proxy. Trust exactly one hop of X-Forwarded-For/Proto
// so RemoteIpAddress (rate limiting, audit) sees the real client and not the proxy.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

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

// EF Core context for the controllers that use it. Configuring it here keeps the
// provider bound to the configured connection string (and out of OnConfiguring).
builder.Services.AddDbContext<TechtoonDbContext>(options => options.UseNpgsql(connStr));

var app = builder.Build();

// Pipeline
app.UseForwardedHeaders();
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("TechtoonMobile");
app.UseAuthentication();
app.UseAuthorization();

// H7: idempotency needs the authenticated user id, so it must run after authentication.
app.UseMiddleware<IdempotencyMiddleware>();

app.MapControllers();

// Health endpoints
app.MapHealthChecks("/health");

app.Run();