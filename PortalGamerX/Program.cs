using Microsoft.EntityFrameworkCore;
using PortalGamerX.Context;
using Microsoft.AspNetCore.Identity;
using PortalGamerX.Models.Entities;

var builder = WebApplication.CreateBuilder(args);

#region Services

// Serviços MVC
builder.Services.AddControllersWithViews();

// Banco de Dados
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("default")));

// Identity
builder.Services
    .AddIdentity<Cliente, IdentityRole<int>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Cliente/Conta/Login";
    options.AccessDeniedPath = "/Cliente/Conta/AcessoNegado";
});


// Autorização
builder.Services.AddAuthorization();

#endregion

var app = builder.Build();

// Middleware de exceções e segurança
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}


# region PIPELINE
// Pipeline de requisição HTTP
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

#endregion

#region ROTAS
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
#endregion

app.Run();
