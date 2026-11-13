using Microsoft.EntityFrameworkCore;
using PoderBelico.Dados;

namespace PoderBelico.Api;

/// <summary>
/// Na subida da API (se Banco:AplicarMigracoes = true) aplica as migrations e a semente de
/// referência. É um serviço hospedado, e não código solto no Program.cs, para não rodar
/// durante os comandos "dotnet ef".
/// </summary>
public class InicializacaoBanco(IServiceProvider servicos, IConfiguration configuracao) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        if (!configuracao.GetValue<bool>("Banco:AplicarMigracoes")) return;

        using var escopo = servicos.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<PoderBelicoContext>();

        await db.Database.MigrateAsync(ct);

        var caminhoPaises = Path.Combine(AppContext.BaseDirectory, "dados", "referencia", "paises.json");
        await SementeReferencia.AplicarAsync(db, caminhoPaises, ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
