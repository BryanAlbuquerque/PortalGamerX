using PortalGamerX.Domain;
using PortalGamerX.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace PortalGamerX.Data;

/// <summary>
/// Comandos administrativos (executados e encerrados, sem subir o servidor web):
///   dotnet PortalGamerX.dll --migrate
///   dotnet PortalGamerX.dll --create-admin        (ADMIN_EMAIL, ADMIN_PASSWORD, ADMIN_NAME)
///   dotnet PortalGamerX.dll --reset-password      (ADMIN_EMAIL, ADMIN_PASSWORD; redefine a senha de um usuário existente)
///   dotnet PortalGamerX.dll --create-admin --promote-existing   (promove usuário já cadastrado; exige apenas ADMIN_EMAIL)
/// </summary>
public static class CliCommands
{
    public static async Task<int> RunAsync(WebApplication app, string[] args)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("CLI");

        try
        {
            if (args.Contains("--migrate"))
            {
                var db = sp.GetRequiredService<AppDbContext>();
                if (!db.Database.GetMigrations().Any())
                {
                    Console.Error.WriteLine("Nenhuma migration encontrada. Gere-a antes (scripts/new-migration.ps1).");
                    return 2;
                }
                Console.WriteLine("Aplicando migrations...");
                await db.Database.MigrateAsync();
                await DbInitializer.EnsureBaselineAsync(sp, logger);
                Console.WriteLine("Migrations aplicadas.");
            }

            if (args.Contains("--reset-password"))
            {
                // Redefine a senha de um usuário existente (ADMIN_EMAIL + ADMIN_PASSWORD). Uso: perda de acesso ao painel.
                var rEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
                var rPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
                if (string.IsNullOrWhiteSpace(rEmail) || string.IsNullOrWhiteSpace(rPassword))
                {
                    Console.Error.WriteLine("Defina ADMIN_EMAIL e ADMIN_PASSWORD.");
                    return 2;
                }
                var rum = sp.GetRequiredService<UserManager<ApplicationUser>>();
                var target = await rum.FindByEmailAsync(rEmail.Trim());
                if (target == null) { Console.Error.WriteLine("Usuário não encontrado."); return 2; }
                var token = await rum.GeneratePasswordResetTokenAsync(target);
                var rr = await rum.ResetPasswordAsync(target, token, rPassword);
                if (!rr.Succeeded)
                {
                    Console.Error.WriteLine("Falha: " + string.Join("; ", rr.Errors.Select(e => e.Description)));
                    return 1;
                }
                await rum.ResetAccessFailedCountAsync(target);
                await rum.SetLockoutEndDateAsync(target, null);
                Console.WriteLine("Senha redefinida. Remova ADMIN_PASSWORD do ambiente.");
                return 0;
            }

            if (args.Contains("--create-admin"))
            {
                await DbInitializer.EnsureBaselineAsync(sp, logger);
                var email = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
                var password = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
                var name = Environment.GetEnvironmentVariable("ADMIN_NAME") ?? "Administrador";
                if (string.IsNullOrWhiteSpace(email))
                {
                    Console.Error.WriteLine("Defina a variável de ambiente ADMIN_EMAIL.");
                    return 2;
                }

                var um = sp.GetRequiredService<UserManager<ApplicationUser>>();
                var existing = await um.FindByEmailAsync(email.Trim());
                if (existing != null)
                {
                    if (!args.Contains("--promote-existing"))
                    {
                        Console.Error.WriteLine("Usuário já existe. Use --promote-existing para promovê-lo a Admin.");
                        return 2;
                    }
                    if (await um.IsInRoleAsync(existing, Roles.Admin)) { Console.WriteLine("Usuário já é Admin."); return 0; }
                    var r = await um.AddToRoleAsync(existing, Roles.Admin);
                    Console.WriteLine(r.Succeeded ? "Usuário promovido a Admin." : "Falha: " + string.Join("; ", r.Errors.Select(e => e.Description)));
                    return r.Succeeded ? 0 : 1;
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    Console.Error.WriteLine("Defina a variável de ambiente ADMIN_PASSWORD.");
                    return 2;
                }
                var result = await DbInitializer.CreateAdminAsync(um, email.Trim(), password, name);
                if (!result.Succeeded)
                {
                    Console.Error.WriteLine("Falha: " + string.Join("; ", result.Errors.Select(e => e.Description)));
                    return 1;
                }
                Console.WriteLine("Administrador criado. Remova ADMIN_PASSWORD do ambiente.");
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Erro: " + ex.Message);
            logger.LogError(ex, "Falha ao executar comando administrativo.");
            return 1;
        }
    }
}
