using PortalGamerX.Context;
using Microsoft.EntityFrameworkCore;
//using PortalGamerX.Repository;
//using PortalGamerX.Repository.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Serviços MVC
builder.Services.AddControllersWithViews();

// Autorização
builder.Services.AddAuthorization();

// Banco de Dados
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("default")));

// 💡 Injeção de dependência dos repositórios
//builder.Services.AddTransient<ILanchesRepository, LanchesRepository>();
//builder.Services.AddTransient<ICategoriaRepository, CategoriaRepository>();

var app = builder.Build();

// Middleware de exceções e segurança
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Pipeline de requisição HTTP
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Rotas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
