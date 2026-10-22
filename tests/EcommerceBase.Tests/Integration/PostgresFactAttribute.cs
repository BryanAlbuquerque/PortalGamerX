namespace EcommerceBase.Tests.Integration;

/// <summary>
/// Testes de integração exigem um PostgreSQL DEDICADO a testes (o schema é recriado!).
/// Defina TEST_DATABASE_URL (ex.: Host=localhost;Database=ecommerce_test;Username=postgres;Password=...).
/// Sem a variável, os testes são ignorados.
/// </summary>
public sealed class PostgresFactAttribute : FactAttribute
{
    public static string? ConnectionString => Environment.GetEnvironmentVariable("TEST_DATABASE_URL");

    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            Skip = "TEST_DATABASE_URL não definida: teste de integração com PostgreSQL ignorado.";
    }
}
