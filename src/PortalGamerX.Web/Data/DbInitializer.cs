using PortalGamerX.Domain;
using PortalGamerX.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace PortalGamerX.Data;

public static class DbInitializer
{
    /// <summary>
    /// Inicialização segura: aplica migrations SOMENTE se Database:MigrateOnStartup=true,
    /// garante papéis, linha de configurações e (opcionalmente) o primeiro administrador.
    /// Nunca cria senha padrão: o bootstrap só ocorre com variáveis de ambiente explícitas e se não existir Admin.
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration cfg, IHostEnvironment env)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
        try
        {
            var db = services.GetRequiredService<AppDbContext>();

            if (!db.Database.GetMigrations().Any())
                logger.LogWarning("Nenhuma migration encontrada no projeto. Gere a migration inicial (scripts/new-migration.ps1) antes de executar.");

            if (cfg.GetValue<bool>("Database:MigrateOnStartup"))
            {
                logger.LogInformation("Aplicando migrations pendentes (Database:MigrateOnStartup=true).");
                await db.Database.MigrateAsync();
            }

            var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
            if (pending.Count > 0)
            {
                logger.LogError("Existem {Count} migrations pendentes. Aplique-as com 'dotnet PortalGamerX.dll --migrate'.", pending.Count);
                return;
            }

            await EnsureBaselineAsync(services, logger);
            await BootstrapAdminAsync(services, cfg, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha na inicialização do banco de dados. Verifique a conexão e as migrations.");
        }
    }

    public static async Task EnsureBaselineAsync(IServiceProvider services, ILogger logger)
    {
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        foreach (var role in new[] { Roles.Admin, Roles.Customer })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new ApplicationRole(role));
                if (!result.Succeeded)
                    logger.LogError("Falha ao criar papel {Role}: {Errors}", role, string.Join("; ", result.Errors.Select(e => e.Code)));
            }
        }

        var db = services.GetRequiredService<AppDbContext>();
        if (!await db.StoreSettings.AnyAsync())
        {
            db.StoreSettings.Add(new StoreSettings());
            await db.SaveChangesAsync();
        }
    }

    private static async Task BootstrapAdminAsync(IServiceProvider services, IConfiguration cfg, ILogger logger)
    {
        var email = cfg["Bootstrap:AdminEmail"];
        var password = cfg["Bootstrap:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var admins = await userManager.GetUsersInRoleAsync(Roles.Admin);
        if (admins.Count > 0) return; // já existe administrador: bootstrap não faz nada

        var result = await CreateAdminAsync(userManager, email.Trim(), password, cfg["Bootstrap:AdminName"] ?? "Administrador");
        if (result.Succeeded) logger.LogWarning("Administrador inicial criado ({Email}). REMOVA Bootstrap__AdminPassword das variáveis de ambiente.", email);
        else logger.LogError("Não foi possível criar o administrador inicial: {Errors}", string.Join("; ", result.Errors.Select(e => e.Code)));
    }

    public static async Task<IdentityResult> CreateAdminAsync(UserManager<ApplicationUser> userManager, string email, string password, string name)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = name,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        var created = await userManager.CreateAsync(user, password);
        if (!created.Succeeded) return created;
        return await userManager.AddToRoleAsync(user, Roles.Admin);
    }
}
