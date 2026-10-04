using Microsoft.AspNetCore.Identity;
using PortalGamerX.Models;

namespace PortalGamerX.Data
{
    public class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager =
           serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var userManager =
           serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles =
            {
                "Admin",
                "Cliente"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(
                        new IdentityRole(role));
                }
            }

            string emailAdmin = "Portal@Admin.com";

            var adminExistente =
            await userManager.FindByEmailAsync(emailAdmin);

            if (adminExistente == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = emailAdmin,
                    Email = emailAdmin,
                    Nome = "Administrador do Sistema",
                    EmailConfirmed = true,
                };

                var resultado = await userManager.CreateAsync(
                    admin,
                    "AdminGamerX72!"
                );

                if (!resultado.Succeeded)
                {
                    foreach (var erro in resultado.Errors)
                    {
                        Console.WriteLine($"ERRO: {erro.Description}");
                    }

                    throw new Exception(
                        string.Join(
                            Environment.NewLine,
                            resultado.Errors.Select(e => e.Description)
                        )
                    );
                }

                var resultadoRole =
                await userManager.AddToRoleAsync(
                    admin,
                    "Admin"
                );

                if (!resultadoRole.Succeeded)
                {
                    throw new Exception(
                        string.Join(
                            Environment.NewLine,
                            resultadoRole.Errors.Select(e => e.Description)
                        )
                    );
                }
            }
        }
    }
}
