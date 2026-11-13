using System.Text;
using Microsoft.EntityFrameworkCore;
using PoderBelico.Dados;

namespace PoderBelico.Api.Catalogo;

public sealed class LinhaBusca
{
    public string Tipo { get; set; } = "";
    public string Slug { get; set; } = "";
}

public sealed record ResultadoBusca(
    string Termo, int Total,
    IReadOnlyList<EquipamentoResumo> Equipamentos, IReadOnlyList<EventoResumo> Eventos);

public static class BuscaEndpoints
{
    private const int LimiteLinhas = 100;

    public static void MapBusca(this RouteGroupBuilder api) => api.MapGet("/busca", Buscar);

    /// <summary>
    /// Monta a expressão do FTS5 a partir do texto digitado: cada palavra vira um termo entre aspas
    /// com busca por prefixo, e as palavras se combinam com E. Aspas, parênteses, "*" e operadores
    /// digitados pelo usuário são descartados, para o texto nunca virar sintaxe da consulta.
    /// </summary>
    public static string? MontarConsulta(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;

        var termos = new List<string>();
        var atual = new StringBuilder();
        foreach (var c in texto)
        {
            if (char.IsLetterOrDigit(c)) atual.Append(c);
            else if (atual.Length > 0)
            {
                termos.Add(atual.ToString());
                atual.Clear();
            }
        }
        if (atual.Length > 0) termos.Add(atual.ToString());

        return termos.Count == 0 ? null : string.Join(' ', termos.Select(t => $"\"{t}\"*"));
    }

    private static string Maiusculo<T>(T valor) where T : struct, Enum => valor.ToString().ToUpperInvariant();

    private static async Task<IResult> Buscar(PoderBelicoContext db, string? q, CancellationToken ct)
    {
        var consulta = MontarConsulta(q);
        if (consulta is null)
            return Results.BadRequest(new { erro = "Informe um termo de busca (letras ou números)." });

        var linhas = await db.Database
            .SqlQuery<LinhaBusca>($"""
                SELECT tipo AS Tipo, slug AS Slug
                FROM busca
                WHERE busca MATCH {consulta}
                ORDER BY rank
                LIMIT {LimiteLinhas}
                """)
            .ToListAsync(ct);

        var slugsEquipamento = linhas.Where(l => l.Tipo == "equipamento").Select(l => l.Slug).ToList();
        var slugsEvento = linhas.Where(l => l.Tipo == "evento").Select(l => l.Slug).ToList();

        var equipamentos = await EquipamentosAsync(db, slugsEquipamento, ct);
        var eventos = await EventosAsync(db, slugsEvento, ct);

        return Results.Ok(new ResultadoBusca(q!.Trim(), equipamentos.Count + eventos.Count, equipamentos, eventos));
    }

    /// <summary>Resumos na mesma ordem de relevância da busca; slug que não existe mais é ignorado.</summary>
    private static async Task<List<EquipamentoResumo>> EquipamentosAsync(
        PoderBelicoContext db, List<string> slugs, CancellationToken ct)
    {
        if (slugs.Count == 0) return [];

        var linhas = await db.Equipamentos.AsNoTracking()
            .Where(e => slugs.Contains(e.Slug))
            .Select(e => new
            {
                e.Slug, e.Nome, e.Variante,
                PaisCodigo = e.Pais!.CodigoIso, PaisNome = e.Pais!.Nome,
                ForcaCodigo = e.Forca!.Codigo,
                CategoriaCodigo = e.Categoria!.Codigo, CategoriaNome = e.Categoria!.Nome,
                e.AnoEntradaServico, e.Status, e.Confiabilidade, e.DescricaoCurta, e.ImagemUrl, e.ImagemCredito,
            })
            .ToListAsync(ct);

        var porSlug = linhas.ToDictionary(l => l.Slug);
        return slugs
            .Where(porSlug.ContainsKey)
            .Select(s => porSlug[s])
            .Select(l => new EquipamentoResumo(
                l.Slug, l.Nome, l.Variante,
                new PaisRef(l.PaisCodigo, l.PaisNome), Maiusculo(l.ForcaCodigo),
                new CategoriaRef(l.CategoriaCodigo, l.CategoriaNome),
                l.AnoEntradaServico, Maiusculo(l.Status), Maiusculo(l.Confiabilidade), l.DescricaoCurta,
                string.IsNullOrWhiteSpace(l.ImagemUrl) || string.IsNullOrWhiteSpace(l.ImagemCredito)
                    ? null
                    : new ImagemDto(l.ImagemUrl, l.ImagemCredito)))
            .ToList();
    }

    private static async Task<List<EventoResumo>> EventosAsync(
        PoderBelicoContext db, List<string> slugs, CancellationToken ct)
    {
        if (slugs.Count == 0) return [];

        var linhas = await db.Eventos.AsNoTracking()
            .Where(e => slugs.Contains(e.Slug))
            .Select(e => new
            {
                e.Slug, e.Nome, e.Tipo, e.DataInicio, e.DataFim, e.Local, e.Confiabilidade,
                PaiSlug = e.EventoPai != null ? e.EventoPai.Slug : null,
                PaiNome = e.EventoPai != null ? e.EventoPai.Nome : null,
                PaiTipo = e.EventoPai != null ? (TipoEvento?)e.EventoPai.Tipo : null,
            })
            .ToListAsync(ct);

        var porSlug = linhas.ToDictionary(l => l.Slug);
        return slugs
            .Where(porSlug.ContainsKey)
            .Select(s => porSlug[s])
            .Select(l => new EventoResumo(
                l.Slug, l.Nome, Maiusculo(l.Tipo), l.DataInicio, l.DataFim, l.Local, Maiusculo(l.Confiabilidade),
                l.PaiSlug is null ? null : new EventoRef(l.PaiSlug, l.PaiNome!, Maiusculo(l.PaiTipo!.Value))))
            .ToList();
    }
}
