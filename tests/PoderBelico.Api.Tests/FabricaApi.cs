using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PoderBelico.Dados;

namespace PoderBelico.Api.Tests;

/// <summary>API de teste com SQLite em memória (uma conexão aberta e compartilhada).</summary>
public class FabricaApi : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _conexao = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _conexao.Open();
        builder.UseSetting("Banco:AplicarMigracoes", "false");

        builder.ConfigureServices(servicos =>
        {
            // Remove o registro original do contexto: as opções e as configurações
            // genéricas (DbContextOptions<>, IDbContextOptionsConfiguration<>) do AddDbContext.
            var antigos = servicos
                .Where(d => d.ServiceType.IsGenericType
                            && d.ServiceType.GenericTypeArguments.Contains(typeof(PoderBelicoContext)))
                .ToList();
            foreach (var descritor in antigos) servicos.Remove(descritor);

            servicos.AddDbContext<PoderBelicoContext>(o => o.UseSqlite(_conexao));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _conexao.Dispose();
    }
}