using Microsoft.AspNetCore.Identity;
using PortalGamerX.Models.Entities;

namespace PortalGamerX.Data.Seed
{
    public static class SeedData
    {
        public static async Task InicializarAsync(
            IServiceProvider serviceProvider)
        {
            var roleManager =
                serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

            var userManager =
                serviceProvider.GetRequiredService<UserManager<Cliente>>();

            // Roles do sistema
            string[] roles =
            {
                "Admin",
                "Cliente"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var resultado = await roleManager.CreateAsync(
                        new IdentityRole<int>(role));

                    if (!resultado.Succeeded)
                    {
                        throw new Exception(
                            string.Join(
                                Environment.NewLine,
                                resultado.Errors.Select(
                                    e => e.Description)
                            )
                        );
                    }
                }
            }

            // Dados do administrador
            const string emailAdmin = "admin@portalgamerx.com";
            const string senhaAdmin = "AdminP@rtal72";

            var adminExistente =
                await userManager.FindByEmailAsync(emailAdmin);

            if (adminExistente == null)
            {
                var admin = new Cliente
                {
                    UserName = emailAdmin,
                    Email = emailAdmin,

                    Nome = "Administrador",
                    Sobrenome = "Portal GamerX",

                    EmailConfirmed = true,

                    AceitouTermosUso = true,
                    DataAceiteTermos = DateTime.Now
                };

                var resultado =
                    await userManager.CreateAsync(
                        admin,
                        senhaAdmin);

                if (!resultado.Succeeded)
                {
                    foreach (var erro in resultado.Errors)
                    {
                        Console.WriteLine(
                            $"ERRO AO CRIAR ADMIN: {erro.Description}");
                    }

                    throw new Exception(
                        string.Join(
                            Environment.NewLine,
                            resultado.Errors.Select(
                                e => e.Description)
                        )
                    );
                }

                var resultadoRole =
                    await userManager.AddToRoleAsync(
                        admin,
                        "Admin");

                if (!resultadoRole.Succeeded)
                {
                    throw new Exception(
                        string.Join(
                            Environment.NewLine,
                            resultadoRole.Errors.Select(
                                e => e.Description)
                        )
                    );
                }

                Console.WriteLine(
                    "Administrador criado com sucesso.");
            }
        }
    }
}