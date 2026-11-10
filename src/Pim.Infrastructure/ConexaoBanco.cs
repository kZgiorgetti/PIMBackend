using Npgsql;

namespace Pim.Infrastructure;

public static class ConexaoBanco
{
    /// <summary>
    /// Aceita a URI copiada do painel do Supabase (postgresql://usuario:senha@host:5432/postgres) ou uma connection string do Npgsql.
    /// SSL é obrigatório (Supabase). Use o Session pooler (porta 5432, usuário postgres.&lt;ref&gt;) em redes só IPv4.
    /// </summary>
    public static string Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new InvalidOperationException("Defina DB_CONNECTION (URI do Supabase) ou ConnectionStrings:Pim.");

        var v = valor.Trim();
        if (!v.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) && !v.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return v;

        var uri = new Uri(v);
        var partes = uri.UserInfo.Split(':', 2);
        var b = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.Trim('/') is { Length: > 0 } db ? db : "postgres",
            Username = Uri.UnescapeDataString(partes[0]),
            Password = partes.Length > 1 ? Uri.UnescapeDataString(partes[1]) : "",
            SslMode = SslMode.Require,
            TrustServerCertificate = true,
            Timezone = "UTC"
        };
        return b.ConnectionString;
    }
}
