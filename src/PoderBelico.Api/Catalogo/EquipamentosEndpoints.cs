using Microsoft.EntityFrameworkCore;
using PoderBelico.Dados;

namespace PoderBelico.Api.Catalogo;

public sealed record PaisRef(string Codigo, string Nome);
public sealed record CategoriaRef(string Codigo, string Nome);
public sealed record ImagemDto(string Url, string Credito);

public sealed record EquipamentoResumo(
    string Slug, string Nome, string? Variante, PaisRef Pais, string Forca, CategoriaRef Categoria,
    int? AnoEntradaServico, string Status, string Confiabilidade, string? DescricaoCurta, ImagemDto? Imagem);

public sealed record ListaPaginada<T>(
    IReadOnlyList<T> Itens, int Pagina, int TamanhoPagina, int Total, int TotalPaginas);

public sealed record FonteDto(
    string Slug, string Tipo, string Titulo, string? Autor, int? Ano, string? Url, bool Provisoria, string? Referencia);

public sealed record ParticipacaoNaFicha(
    EventoRef Evento, DateOnly DataInicio, DateOnly? DataFim, PaisRef Pais,
    string Papel, int? QuantidadeAprox, string? Observacao);

public sealed record EquipamentoFicha(
    string Slug, string Nome, string? Variante, PaisRef Pais, string Forca, CategoriaRef Categoria,
    string? Fabricante, int? AnoPrimeiroUso, int? AnoEntradaServico, int? UnidadesProduzidas,
    string Status, string? DescricaoCurta, string? Curiosidade, ImagemDto? Imagem,
    string Confiabilidade, bool SomenteFontesProvisorias,
    IReadOnlyList<PaisRef> Operadores, EspecificacoesDto Especificacoes, IReadOnlyList<FonteDto> Fontes,
    IReadOnlyList<ParticipacaoNaFicha> Eventos);

public static class EquipamentosEndpoints
{
    private const int TamanhoPadrao = 24;
    private const int TamanhoMaximo = 100;

    public static void MapEquipamentos(this RouteGroupBuilder api)
    {
        api.MapGet("/filtros", Filtros);
        api.MapGet("/equipamentos", Listar);
        api.MapGet("/equipamentos/{slug}", Ficha);
    }

    private static string Maiusculo<T>(T valor) where T : struct, Enum => valor.ToString().ToUpperInvariant();

    /// <summary>Imagem só é exibida quando tem crédito registrado.</summary>
    private static ImagemDto? Imagem(string? url, string? credito) =>
        string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(credito) ? null : new ImagemDto(url, credito);

    private static IResult Invalido(string mensagem) => Results.BadRequest(new { erro = mensagem });

    private static async Task<IResult> Filtros(PoderBelicoContext db, CancellationToken ct)
    {
        var forcas = await db.Forcas.AsNoTracking().OrderBy(f => f.Id).ToListAsync(ct);
        var categorias = await db.Categorias.AsNoTracking().Include(c => c.Forca).OrderBy(c => c.Nome).ToListAsync(ct);
        var paises = await db.Paises.AsNoTracking()
            .Where(p => db.Equipamentos.Any(e => e.PaisId == p.Id))
            .OrderBy(p => p.Nome).ToListAsync(ct);
        var paisesEventos = await db.Paises.AsNoTracking()
            .Where(p => db.EventoPaises.Any(x => x.PaisId == p.Id))
            .OrderBy(p => p.Nome).ToListAsync(ct);

        return Results.Ok(new
        {
            forcas = forcas.Select(f => new PaisRef(Maiusculo(f.Codigo), f.Nome)),
            categorias = categorias.Select(c => new
            {
                codigo = c.Codigo,
                nome = c.Nome,
                forca = c.Forca is null ? null : Maiusculo(c.Forca.Codigo),
            }),
            paises = paises.Select(p => new PaisRef(p.CodigoIso, p.Nome)),
            status = Enum.GetValues<StatusEquipamento>().Select(s => Maiusculo(s)),
            paisesEventos = paisesEventos.Select(p => new PaisRef(p.CodigoIso, p.Nome)),
            tiposEvento = Enum.GetValues<TipoEvento>().Select(t => Maiusculo(t)),
        });
    }

    private static async Task<IResult> Listar(
        PoderBelicoContext db, string? pais, string? forca, string? categoria,
        int? de, int? ate, string? status, int? pagina, int? tamanho, CancellationToken ct)
    {
        var consulta = db.Equipamentos.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(pais))
        {
            var iso = pais.Trim().ToUpperInvariant();
            consulta = consulta.Where(e => e.Pais!.CodigoIso == iso);
        }

        if (!string.IsNullOrWhiteSpace(forca))
        {
            if (!Enum.TryParse<CodigoForca>(forca.Trim(), true, out var f) || !Enum.IsDefined(f))
                return Invalido($"forca inválida: '{forca}' (use MARINHA, AEREA ou EXERCITO)");
            consulta = consulta.Where(e => e.Forca!.Codigo == f);
        }

        if (!string.IsNullOrWhiteSpace(categoria))
        {
            var codigo = categoria.Trim().ToUpperInvariant();
            consulta = consulta.Where(e => e.Categoria!.Codigo == codigo);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<StatusEquipamento>(status.Trim(), true, out var s) || !Enum.IsDefined(s))
                return Invalido($"status inválido: '{status}' (use ATIVO, APOSENTADO ou PROTOTIPO)");
            consulta = consulta.Where(e => e.Status == s);
        }

        if (de is not null) consulta = consulta.Where(e => e.AnoEntradaServico >= de);
        if (ate is not null) consulta = consulta.Where(e => e.AnoEntradaServico <= ate);

        var paginaAtual = Math.Max(1, pagina ?? 1);
        var tamanhoPagina = Math.Clamp(tamanho ?? TamanhoPadrao, 1, TamanhoMaximo);

        var total = await consulta.CountAsync(ct);
        var linhas = await consulta
            .OrderBy(e => e.Nome).ThenBy(e => e.Slug)
            .Skip((paginaAtual - 1) * tamanhoPagina).Take(tamanhoPagina)
            .Select(e => new
            {
                e.Slug, e.Nome, e.Variante,
                PaisCodigo = e.Pais!.CodigoIso, PaisNome = e.Pais!.Nome,
                ForcaCodigo = e.Forca!.Codigo,
                CategoriaCodigo = e.Categoria!.Codigo, CategoriaNome = e.Categoria!.Nome,
                e.AnoEntradaServico, e.Status, e.Confiabilidade, e.DescricaoCurta, e.ImagemUrl, e.ImagemCredito,
            })
            .ToListAsync(ct);

        var itens = linhas.Select(l => new EquipamentoResumo(
            l.Slug, l.Nome, l.Variante,
            new PaisRef(l.PaisCodigo, l.PaisNome), Maiusculo(l.ForcaCodigo),
            new CategoriaRef(l.CategoriaCodigo, l.CategoriaNome),
            l.AnoEntradaServico, Maiusculo(l.Status), Maiusculo(l.Confiabilidade),
            l.DescricaoCurta, Imagem(l.ImagemUrl, l.ImagemCredito))).ToList();

        var totalPaginas = (int)Math.Ceiling(total / (double)tamanhoPagina);
        return Results.Ok(new ListaPaginada<EquipamentoResumo>(itens, paginaAtual, tamanhoPagina, total, totalPaginas));
    }

    private static async Task<IResult> Ficha(PoderBelicoContext db, string slug, CancellationToken ct)
    {
        var e = await db.Equipamentos.AsNoTracking()
            .Include(x => x.Pais).Include(x => x.Forca).Include(x => x.Categoria)
            .Include(x => x.Operadores).ThenInclude(o => o.Pais)
            .Include(x => x.Fontes).ThenInclude(f => f.Fonte)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Slug == slug, ct);

        if (e is null)
            return Results.NotFound(new { erro = $"Equipamento '{slug}' não encontrado." });

        var fontes = e.Fontes
            .Where(f => f.Fonte is not null)
            .OrderBy(f => f.Fonte!.Titulo)
            .Select(f => new FonteDto(
                f.Fonte!.Slug, Maiusculo(f.Fonte.Tipo), f.Fonte.Titulo, f.Fonte.Autor, f.Fonte.Ano,
                f.Fonte.Url, f.Fonte.Provisoria, f.Referencia))
            .ToList();

        var operadores = e.Operadores
            .Where(o => o.Pais is not null)
            .Select(o => new PaisRef(o.Pais!.CodigoIso, o.Pais.Nome))
            .OrderBy(p => p.Nome)
            .ToList();

        var ficha = new EquipamentoFicha(
            e.Slug, e.Nome, e.Variante,
            new PaisRef(e.Pais!.CodigoIso, e.Pais.Nome), Maiusculo(e.Forca!.Codigo),
            new CategoriaRef(e.Categoria!.Codigo, e.Categoria.Nome),
            e.Fabricante, e.AnoPrimeiroUso, e.AnoEntradaServico, e.UnidadesProduzidas,
            Maiusculo(e.Status), e.DescricaoCurta, e.Curiosidade, Imagem(e.ImagemUrl, e.ImagemCredito),
            Maiusculo(e.Confiabilidade), fontes.Count > 0 && fontes.All(f => f.Provisoria),
            operadores, ApresentacaoEspecificacoes.Montar(e.Especificacoes, e.Categoria.Codigo), fontes,
            await EventosDoEquipamentoAsync(db, e.Id, ct));

        return Results.Ok(ficha);
    }

    /// <summary>Eventos em que o equipamento participou, do mais antigo ao mais recente.</summary>
    private static async Task<IReadOnlyList<ParticipacaoNaFicha>> EventosDoEquipamentoAsync(
        PoderBelicoContext db, int equipamentoId, CancellationToken ct)
    {
        var linhas = await db.Participacoes.AsNoTracking()
            .Where(p => p.EquipamentoId == equipamentoId)
            .Select(p => new
            {
                EventoSlug = p.Evento!.Slug, EventoNome = p.Evento!.Nome, EventoTipo = p.Evento!.Tipo,
                p.Evento!.DataInicio, p.Evento!.DataFim,
                PaisCodigo = p.Pais!.CodigoIso, PaisNome = p.Pais!.Nome,
                p.Papel, p.QuantidadeAprox, p.Observacao,
            })
            .ToListAsync(ct);

        return linhas
            .OrderBy(l => l.DataInicio).ThenBy(l => l.EventoNome).ThenBy(l => l.PaisNome)
            .Select(l => new ParticipacaoNaFicha(
                new EventoRef(l.EventoSlug, l.EventoNome, Maiusculo(l.EventoTipo)), l.DataInicio, l.DataFim,
                new PaisRef(l.PaisCodigo, l.PaisNome), l.Papel, l.QuantidadeAprox, l.Observacao))
            .ToList();
    }
}
