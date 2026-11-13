using Microsoft.EntityFrameworkCore;
using PoderBelico.Dados;

namespace PoderBelico.Api.Catalogo;

public sealed record EventoRef(string Slug, string Nome, string Tipo);

public sealed record EventoResumo(
    string Slug, string Nome, string Tipo, DateOnly DataInicio, DateOnly? DataFim,
    string? Local, string Confiabilidade, EventoRef? EventoPai);

public sealed record NoEvento(
    string Slug, string Nome, string Tipo, DateOnly DataInicio, DateOnly? DataFim,
    IReadOnlyList<NoEvento> Subeventos);

public sealed record LadoDto(string? Lado, IReadOnlyList<PaisRef> Paises);

public sealed record EquipamentoNoEvento(
    string Slug, string Nome, CategoriaRef Categoria, string Papel, int? QuantidadeAprox, string? Observacao);

public sealed record ForcaGrupo(string Forca, IReadOnlyList<EquipamentoNoEvento> Equipamentos);

public sealed record PaisGrupo(PaisRef Pais, IReadOnlyList<ForcaGrupo> Forcas);

public sealed record EventoDetalhe(
    string Slug, string Nome, string Tipo, DateOnly DataInicio, DateOnly? DataFim,
    string? Local, string? Descricao, string? Resultado, string Confiabilidade,
    bool SomenteFontesProvisorias, EventoRef? EventoPai,
    IReadOnlyList<LadoDto> Beligerantes, IReadOnlyList<NoEvento> Subeventos,
    IReadOnlyList<PaisGrupo> Participacoes, IReadOnlyList<FonteDto> Fontes);

public static class EventosEndpoints
{
    private const int TamanhoPadrao = 24;
    private const int TamanhoMaximo = 100;
    private const int NiveisMaximos = 10;

    public static void MapEventos(this RouteGroupBuilder api)
    {
        api.MapGet("/eventos", Listar);
        api.MapGet("/eventos/{slug}", Detalhe);
        api.MapGet("/linha-do-tempo", LinhaDoTempo);
    }

    private static string Maiusculo<T>(T valor) where T : struct, Enum => valor.ToString().ToUpperInvariant();

    private static IResult Invalido(string mensagem) => Results.BadRequest(new { erro = mensagem });

    private static async Task<IResult> Listar(
        PoderBelicoContext db, string? tipo, string? pais, string? forca, int? de, int? ate,
        int? pagina, int? tamanho, CancellationToken ct)
    {
        var consulta = db.Eventos.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(forca))
        {
            if (!Enum.TryParse<CodigoForca>(forca.Trim(), true, out var f) || !Enum.IsDefined(f))
                return Invalido($"forca inválida: '{forca}' (use MARINHA, AEREA ou EXERCITO)");
            // Evento "da força" = tem ao menos um equipamento dessa força participando.
            consulta = consulta.Where(e => db.Participacoes.Any(p => p.EventoId == e.Id && p.Equipamento!.Forca!.Codigo == f));
        }

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            if (!Enum.TryParse<TipoEvento>(tipo.Trim(), true, out var t) || !Enum.IsDefined(t))
                return Invalido($"tipo inválido: '{tipo}' (use GUERRA, CAMPANHA ou BATALHA)");
            consulta = consulta.Where(e => e.Tipo == t);
        }

        if (!string.IsNullOrWhiteSpace(pais))
        {
            var iso = pais.Trim().ToUpperInvariant();
            consulta = consulta.Where(e => e.Paises.Any(p => p.Pais!.CodigoIso == iso));
        }

        // Período: o evento entra se o seu intervalo cruza o intervalo pedido (sem fim = em andamento).
        if (de is not null)
        {
            if (de < 1 || de > 9999) return Invalido($"ano inválido em 'de': {de}");
            var inicio = new DateOnly(de.Value, 1, 1);
            consulta = consulta.Where(e => e.DataFim == null || e.DataFim >= inicio);
        }
        if (ate is not null)
        {
            if (ate < 1 || ate > 9999) return Invalido($"ano inválido em 'ate': {ate}");
            var fim = new DateOnly(ate.Value, 12, 31);
            consulta = consulta.Where(e => e.DataInicio <= fim);
        }

        var paginaAtual = Math.Max(1, pagina ?? 1);
        var tamanhoPagina = Math.Clamp(tamanho ?? TamanhoPadrao, 1, TamanhoMaximo);

        var total = await consulta.CountAsync(ct);
        var linhas = await consulta
            .OrderBy(e => e.DataInicio).ThenBy(e => e.Nome).ThenBy(e => e.Slug)
            .Skip((paginaAtual - 1) * tamanhoPagina).Take(tamanhoPagina)
            .Select(e => new
            {
                e.Slug, e.Nome, e.Tipo, e.DataInicio, e.DataFim, e.Local, e.Confiabilidade,
                PaiSlug = e.EventoPai != null ? e.EventoPai.Slug : null,
                PaiNome = e.EventoPai != null ? e.EventoPai.Nome : null,
                PaiTipo = e.EventoPai != null ? (TipoEvento?)e.EventoPai.Tipo : null,
            })
            .ToListAsync(ct);

        var itens = linhas.Select(l => new EventoResumo(
            l.Slug, l.Nome, Maiusculo(l.Tipo), l.DataInicio, l.DataFim, l.Local, Maiusculo(l.Confiabilidade),
            l.PaiSlug is null ? null : new EventoRef(l.PaiSlug, l.PaiNome!, Maiusculo(l.PaiTipo!.Value)))).ToList();

        var totalPaginas = (int)Math.Ceiling(total / (double)tamanhoPagina);
        return Results.Ok(new ListaPaginada<EventoResumo>(itens, paginaAtual, tamanhoPagina, total, totalPaginas));
    }

    /// <summary>
    /// Eventos de 1914 em diante agrupados por guerra: cada raiz (evento sem pai) com a árvore de seus
    /// subeventos, em ordem de data de início. Com filtro, só ficam os eventos que casam e os seus ancestrais.
    /// </summary>
    private static async Task<IResult> LinhaDoTempo(PoderBelicoContext db, string? forca, string? pais, CancellationToken ct)
    {
        var consulta = db.Eventos.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(forca))
        {
            if (!Enum.TryParse<CodigoForca>(forca.Trim(), true, out var f) || !Enum.IsDefined(f))
                return Invalido($"forca inválida: '{forca}' (use MARINHA, AEREA ou EXERCITO)");
            consulta = consulta.Where(e => db.Participacoes.Any(p => p.EventoId == e.Id && p.Equipamento!.Forca!.Codigo == f));
        }
        if (!string.IsNullOrWhiteSpace(pais))
        {
            var iso = pais.Trim().ToUpperInvariant();
            consulta = consulta.Where(e => e.Paises.Any(p => p.Pais!.CodigoIso == iso));
        }

        var casam = await consulta.Select(e => e.Id).ToListAsync(ct);
        var todos = await db.Eventos.AsNoTracking().ToListAsync(ct);
        var porId = todos.ToDictionary(e => e.Id);

        var incluidos = new HashSet<int>();
        foreach (var id in casam)
        {
            int? atual = id;
            for (var i = 0; atual is not null && i < NiveisMaximos; i++)
            {
                incluidos.Add(atual.Value);
                atual = porId.TryGetValue(atual.Value, out var ev) ? ev.EventoPaiId : null;
            }
        }

        var porPai = todos.Where(e => incluidos.Contains(e.Id)).ToLookup(e => e.EventoPaiId);
        var raizes = porPai[null]
            .OrderBy(x => x.DataInicio).ThenBy(x => x.Nome)
            .Select(x => new NoEvento(
                x.Slug, x.Nome, Maiusculo(x.Tipo), x.DataInicio, x.DataFim, Montar(x.Id, porPai, 1)))
            .ToList();

        return Results.Ok(new { raizes });
    }

    private static async Task<IResult> Detalhe(PoderBelicoContext db, string slug, CancellationToken ct)
    {
        var e = await db.Eventos.AsNoTracking()
            .Include(x => x.EventoPai)
            .Include(x => x.Paises).ThenInclude(p => p.Pais)
            .Include(x => x.Fontes).ThenInclude(f => f.Fonte)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Slug == slug, ct);

        if (e is null)
            return Results.NotFound(new { erro = $"Evento '{slug}' não encontrado." });

        var beligerantes = e.Paises
            .Where(p => p.Pais is not null)
            .GroupBy(p => string.IsNullOrWhiteSpace(p.Lado) ? null : p.Lado)
            .OrderBy(g => g.Key is null ? 1 : 0).ThenBy(g => g.Key)
            .Select(g => new LadoDto(
                g.Key,
                g.Select(p => new PaisRef(p.Pais!.CodigoIso, p.Pais.Nome)).OrderBy(p => p.Nome).ToList()))
            .ToList();

        var fontes = e.Fontes
            .Where(f => f.Fonte is not null)
            .OrderBy(f => f.Fonte!.Titulo)
            .Select(f => new FonteDto(
                f.Fonte!.Slug, Maiusculo(f.Fonte.Tipo), f.Fonte.Titulo, f.Fonte.Autor, f.Fonte.Ano,
                f.Fonte.Url, f.Fonte.Provisoria, f.Referencia))
            .ToList();

        var detalhe = new EventoDetalhe(
            e.Slug, e.Nome, Maiusculo(e.Tipo), e.DataInicio, e.DataFim, e.Local, e.Descricao, e.Resultado,
            Maiusculo(e.Confiabilidade), fontes.Count > 0 && fontes.All(f => f.Provisoria),
            e.EventoPai is null ? null : new EventoRef(e.EventoPai.Slug, e.EventoPai.Nome, Maiusculo(e.EventoPai.Tipo)),
            beligerantes, await SubeventosAsync(db, e.Id, ct), await ParticipacoesAsync(db, e.Id, ct), fontes);

        return Results.Ok(detalhe);
    }

    /// <summary>Todos os descendentes do evento, em árvore e em ordem cronológica.</summary>
    private static async Task<IReadOnlyList<NoEvento>> SubeventosAsync(PoderBelicoContext db, int eventoId, CancellationToken ct)
    {
        var todos = new List<Evento>();
        var nivel = new List<int> { eventoId };
        for (var i = 0; i < NiveisMaximos && nivel.Count > 0; i++)
        {
            var pais = nivel;
            var filhos = await db.Eventos.AsNoTracking()
                .Where(x => x.EventoPaiId != null && pais.Contains(x.EventoPaiId.Value))
                .ToListAsync(ct);
            todos.AddRange(filhos);
            nivel = filhos.Select(f => f.Id).ToList();
        }

        var porPai = todos.ToLookup(x => x.EventoPaiId);
        return Montar(eventoId, porPai, 0);
    }

    private static List<NoEvento> Montar(int paiId, ILookup<int?, Evento> porPai, int profundidade)
    {
        if (profundidade >= NiveisMaximos) return [];
        return porPai[paiId]
            .OrderBy(x => x.DataInicio).ThenBy(x => x.Nome)
            .Select(x => new NoEvento(
                x.Slug, x.Nome, Maiusculo(x.Tipo), x.DataInicio, x.DataFim, Montar(x.Id, porPai, profundidade + 1)))
            .ToList();
    }

    /// <summary>Equipamentos do evento, agrupados por país e, dentro dele, por força.</summary>
    private static async Task<IReadOnlyList<PaisGrupo>> ParticipacoesAsync(PoderBelicoContext db, int eventoId, CancellationToken ct)
    {
        var linhas = await db.Participacoes.AsNoTracking()
            .Where(p => p.EventoId == eventoId)
            .Select(p => new
            {
                PaisCodigo = p.Pais!.CodigoIso, PaisNome = p.Pais!.Nome,
                Forca = p.Equipamento!.Forca!.Codigo,
                p.Equipamento!.Slug, p.Equipamento!.Nome,
                CategoriaCodigo = p.Equipamento!.Categoria!.Codigo, CategoriaNome = p.Equipamento!.Categoria!.Nome,
                p.Papel, p.QuantidadeAprox, p.Observacao,
            })
            .ToListAsync(ct);

        return linhas
            .GroupBy(l => new { l.PaisCodigo, l.PaisNome })
            .OrderBy(g => g.Key.PaisNome)
            .Select(g => new PaisGrupo(
                new PaisRef(g.Key.PaisCodigo, g.Key.PaisNome),
                g.GroupBy(l => l.Forca)
                    .OrderBy(f => (int)f.Key)
                    .Select(f => new ForcaGrupo(
                        Maiusculo(f.Key),
                        f.OrderBy(l => l.Nome)
                            .Select(l => new EquipamentoNoEvento(
                                l.Slug, l.Nome, new CategoriaRef(l.CategoriaCodigo, l.CategoriaNome),
                                l.Papel, l.QuantidadeAprox, l.Observacao))
                            .ToList()))
                    .ToList()))
            .ToList();
    }
}
