using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PortalGamerX.Data;
using PortalGamerX.Models;

var builder = WebApplication.CreateBuilder(args);

#region Banco de Dados
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' não foi encontrada.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
#endregion

#region IDENTITY

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;

        // Configurações de senha
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 6;

        // Configurações de usuário
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();
#endregion

#region MVC / RAZOR

builder.Services.AddControllersWithViews();

builder.Services.AddRazorPages();
#endregion

// Build the app
var app = builder.Build();


#region PIPELINE

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

#endregion

#region ROUTES

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

#endregion

app.Run();