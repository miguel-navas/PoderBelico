using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PoderBelico.Dados.Sql;

namespace PoderBelico.Dados;

public record PaisSemente(string CodigoIso, string Nome, int? AnoInicio, int? AnoFim);

/// <summary>
/// Carrega os dados de referência que não entram na migration: a tabela de busca
/// (FTS5) e a lista de países. É idempotente: pode rodar a cada inicialização.
/// (Forças e categorias fixas entram na migration, via HasData.)
/// </summary>
public static class SementeReferencia
{
    private static readonly JsonSerializerOptions Opcoes = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static async Task AplicarAsync(
        PoderBelicoContext db, string caminhoPaises, CancellationToken ct = default)
    {
        await db.Database.ExecuteSqlRawAsync(BuscaFts.Criar, ct);

        var json = await File.ReadAllTextAsync(caminhoPaises, ct);
        var paises = JsonSerializer.Deserialize<List<PaisSemente>>(json, Opcoes) ?? [];

        var existentes = await db.Paises.ToDictionaryAsync(p => p.CodigoIso, ct);
        foreach (var semente in paises)
        {
            if (existentes.TryGetValue(semente.CodigoIso, out var pais))
            {
                pais.Nome = semente.Nome;
                pais.AnoInicio = semente.AnoInicio;
                pais.AnoFim = semente.AnoFim;
            }
            else
            {
                db.Paises.Add(new Pais
                {
                    CodigoIso = semente.CodigoIso,
                    Nome = semente.Nome,
                    AnoInicio = semente.AnoInicio,
                    AnoFim = semente.AnoFim,
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
