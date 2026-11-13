using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PoderBelico.Dados;

namespace PoderBelico.Api.Tests;

/// <summary>SQLite em memória com o esquema criado a partir do modelo (inclui HasData e CHECKs).</summary>
public sealed class BancoEmMemoria : IDisposable
{
    private readonly SqliteConnection _conexao = new("DataSource=:memory:");

    public PoderBelicoContext Contexto { get; }

    public BancoEmMemoria()
    {
        _conexao.Open();
        Contexto = new PoderBelicoContext(
            new DbContextOptionsBuilder<PoderBelicoContext>().UseSqlite(_conexao).Options);
        Contexto.Database.EnsureCreated();
    }

    public static string CaminhoPaises =>
        Path.Combine(AppContext.BaseDirectory, "dados_teste", "paises.json");

    public void Dispose()
    {
        Contexto.Dispose();
        _conexao.Dispose();
    }
}
