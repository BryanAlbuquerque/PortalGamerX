using System.Threading.RateLimiting;
using EcommerceBase.Web.Data;
using EcommerceBase.Web.Domain;
using EcommerceBase.Web.Domain.Entities;
using EcommerceBase.Web.Integrations.Email;
using EcommerceBase.Web.Integrations.Fiscal;
using EcommerceBase.Web.Integrations.Payments;
using EcommerceBase.Web.Integrations.Shipping;
using EcommerceBase.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EcommerceBase.Web.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddEcommercePersistence(this IServiceCollection services, IConfiguration cfg)
    {
        var cs = ConnectionStringResolver.Resolve(cfg);
        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(cs));

        // Chaves do Data Protection no banco: sobrevivem a redeploys (cookies, tokens e segredos cifrados continuam válidos).
        services.AddDataProtection()
            .SetApplicationName("EcommerceBase")
            .PersistKeysToDbContext<AppDbContext>();
        return services;
    }

    public static IServiceCollection AddEcommerceIdentity(this IServiceCollection services, IConfiguration cfg, IHostEnvironment env)
    {
        services.AddIdentity<ApplicationUser, ApplicationRole>(o =>
            {
                o.Password.RequiredLength = 8;
                o.Password.RequireDigit = true;
                o.Password.RequireLowercase = true;
                o.Password.RequireUppercase = true;
                o.Password.RequireNonAlphanumeric = false;
                o.User.RequireUniqueEmail = true;
                o.SignIn.RequireConfirmedEmail = cfg.GetValue("Identity:RequireConfirmedEmail", true);
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                o.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromHours(3));
        services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(1));

        services.ConfigureApplicationCookie(o =>
        {
            o.Cookie.Name = ".EcommerceBase.Auth";
            o.Cookie.HttpOnly = true;
            o.Cookie.SameSite = SameSiteMode.Lax;
            o.Cookie.SecurePolicy = env.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            o.LoginPath = "/conta/entrar";
            o.LogoutPath = "/conta/sair";
            o.AccessDeniedPath = "/conta/acesso-negado";
            o.ExpireTimeSpan = TimeSpan.FromDays(14);
            o.SlidingExpiration = true;
        });

        services.AddAuthorization(o => o.AddPolicy(Policies.AdminOnly, p => p.RequireRole(Roles.Admin)));
        return services;
    }

    public static IServiceCollection AddEcommerceSecurity(this IServiceCollection services, IHostEnvironment env)
    {
        services.AddAntiforgery(o =>
        {
            o.Cookie.Name = ".EcommerceBase.Antiforgery";
            o.Cookie.HttpOnly = true;
            o.Cookie.SameSite = SameSiteMode.Lax;
            o.Cookie.SecurePolicy = env.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        });

        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            o.AddPolicy("shipping", ctx => RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            o.AddPolicy("webhook", ctx => RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
        return services;
    }

    public static IServiceCollection AddEcommerceServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddMemoryCache();

        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddScoped<IPublicUrl, PublicUrlService>();

        services.AddScoped<IIntegrationSettings, IntegrationSettingsService>();
        services.AddScoped<IStoreSettingsService, StoreSettingsService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IStockService, StockService>();
        services.AddScoped<ICouponService, CouponService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IAddressService, AddressService>();
        services.AddScoped<ICheckoutService, CheckoutService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IShippingService, ShippingService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IFiscalService, FiscalService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IDashboardService, DashboardService>();

        // Integrações (contratos substituíveis)
        services.AddScoped<IPaymentProvider, MercadoPagoProvider>();
        services.AddScoped<IShippingProvider, MelhorEnvioProvider>();
        services.AddScoped<IShippingProvider, DevelopmentShippingProvider>();
        services.AddScoped<IEmailProvider, SmtpEmailProvider>();
        services.AddScoped<IFiscalProvider, NotConfiguredFiscalProvider>();

        services.AddHttpClient(MercadoPagoProvider.HttpClientName, c =>
        {
            c.BaseAddress = new Uri("https://api.mercadopago.com/");
            c.Timeout = TimeSpan.FromSeconds(20);
        });
        services.AddHttpClient(MelhorEnvioProvider.HttpClientName, c => c.Timeout = TimeSpan.FromSeconds(20));

        services.AddHostedService<EmailDispatcherService>();
        services.AddHostedService<OrderExpirationService>();

        services.AddHealthChecks().AddCheck<DbHealthCheck>("database");
        return services;
    }
}
