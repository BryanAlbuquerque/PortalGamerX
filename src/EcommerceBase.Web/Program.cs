using System.Globalization;
using EcommerceBase.Web.Data;
using EcommerceBase.Web.Infrastructure;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Render injeta PORT; localmente usa launchSettings/ASPNETCORE_URLS.
var port = Environment.GetEnvironmentVariable("PORT");
if (int.TryParse(port, out _)) builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

if (!builder.Environment.IsDevelopment())
    builder.Logging.AddJsonConsole(o => o.IncludeScopes = true);

builder.Services
    .AddEcommercePersistence(builder.Configuration)
    .AddEcommerceIdentity(builder.Configuration, builder.Environment)
    .AddEcommerceSecurity(builder.Environment)
    .AddEcommerceServices();

builder.Services.AddControllersWithViews(o =>
{
    o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    o.Conventions.Add(new AdminAreaAuthorizationConvention());
    o.ModelBinderProviders.Insert(0, new FlexibleDecimalModelBinderProvider());
});

var app = builder.Build();

// Comandos administrativos (--migrate, --create-admin): executam e encerram sem subir o servidor.
if (args.Contains("--migrate") || args.Contains("--create-admin") || args.Contains("--reset-password"))
    return await CliCommands.RunAsync(app, args);

await using (var scope = app.Services.CreateAsyncScope())
    await DbInitializer.InitializeAsync(scope.ServiceProvider, app.Configuration, app.Environment);

var pt = new CultureInfo("pt-BR");
var localization = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(pt),
    SupportedCultures = new List<CultureInfo> { pt },
    SupportedUICultures = new List<CultureInfo> { pt }
};
localization.RequestCultureProviders.Clear();
app.UseRequestLocalization(localization);

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/erro");
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/erro/{0}");

app.UseHttpsRedirection();

app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "DENY";
    h["Referrer-Policy"] = "strict-origin-when-cross-origin";
    h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    h["Content-Security-Policy"] =
        "default-src 'self'; img-src 'self' data: https:; style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "script-src 'self' https://cdn.jsdelivr.net; font-src 'self' https://cdn.jsdelivr.net data:; " +
        "frame-ancestors 'none'; base-uri 'self'; object-src 'none'";
    await next();
});

app.UseStaticFiles();
var storage = app.Services.GetRequiredService<IFileStorage>();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.GetFullPath(storage.RootPath)),
    RequestPath = LocalFileStorage.PublicPrefix,
    ServeUnknownFileTypes = false,
    OnPrepareResponse = ctx => ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=604800"
});

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/healthz");
app.MapControllerRoute(name: "areas", pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
return 0;

public partial class Program { }
