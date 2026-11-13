using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PoderBelico.Dados;
using PoderBelico.Dados.Sql;

namespace PoderBelico.Importador;

/// <summary>
/// Importa fontes, equipamentos, eventos e participações. Regras (design.md, decisões 9 e 10):
/// chave natural por slug (participação: equipamento + evento + país); o registro é substituído
/// por inteiro e nada é apagado; ordem fontes → equipamentos → eventos (pai antes do filho) →
/// participações; um ponto de salvamento por registro dentro de uma transação da execução;
/// falha de infraestrutura desfaz tudo; --estrito e --simular.
/// </summary>
public sealed class ServicoImportacao(PoderBelicoContext db)
{
    private sealed record InfoFonte(int Id, bool Provisoria);

    private sealed class Lote
    {
        public List<(RegFonte Reg, ItemRelatorio Item)> Avulsas { get; } = [];
        public List<(RegEquipamento Reg, ItemRelatorio Item)> Equipamentos { get; } = [];
        public List<(RegEvento Reg, ItemRelatorio Item)> Eventos { get; } = [];
        public List<(RegParticipacao Reg, ItemRelatorio Item)> Participacoes { get; } = [];
    }

    private readonly Dictionary<string, InfoFonte> _fontes = [];
    private readonly Dictionary<string, RegFonte> _definicoes = [];
    private readonly HashSet<string> _fontesRejeitadas = [];
    private readonly HashSet<string> _equipamentosRejeitados = [];
    private readonly HashSet<string> _eventosRejeitados = [];
    private Relatorio _rel = new();
    private Dictionary<string, int> _paises = [];

    public async Task<Relatorio> ExecutarAsync(
        IReadOnlyList<Arquivo> arquivos, Opcoes opcoes, CancellationToken ct = default)
    {
        _rel = new Relatorio { Simulacao = opcoes.Simular, Estrito = opcoes.Estrito };
        _fontes.Clear();
        _definicoes.Clear();
        _fontesRejeitadas.Clear();
        _equipamentosRejeitados.Clear();
        _eventosRejeitados.Clear();

        // 1. Leitura e conferência de forma (sem tocar no banco).
        var lote = new Lote();
        foreach (var arquivo in arquivos) LerArquivo(arquivo, lote);

        // 2. Gravação em uma transação, com savepoint por registro.
        await db.Database.ExecuteSqlRawAsync(BuscaFts.Criar, ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        await GravarFontesAsync(lote, ct);

        _paises = await db.Paises.AsNoTracking().ToDictionaryAsync(p => p.CodigoIso, p => p.Id, ct);
        await GravarEquipamentosAsync(lote, ct);
        await GravarEventosAsync(lote, ct);
        await GravarParticipacoesAsync(lote, ct);

        // 3. Fim: simulação e modo estrito desfazem; caso contrário confirma.
        if (opcoes.Simular)
        {
            await tx.RollbackAsync(ct);
        }
        else if (opcoes.Estrito && _rel.TemRejeicao)
        {
            await tx.RollbackAsync(ct);
            _rel.DesfeitoPorEstrito = true;
            foreach (var i in _rel.Itens.Where(i => i.Resultado is Resultado.Criado or Resultado.Atualizado or Resultado.Ignorado))
                i.Resultado = Resultado.Revertido;
        }
        else
        {
            await tx.CommitAsync(ct);
        }

        return _rel;
    }

    // ================================================================= leitura
    private void LerArquivo(Arquivo arquivo, Lote lote)
    {
        var ext = Path.GetExtension(arquivo.Nome).ToLowerInvariant();
        if (ext != ".json")
        {
            _rel.Novo("arquivo", arquivo.Nome).Rejeitar(ext == ".csv"
                ? "formato CSV não aceito: nesta versão só JSON é aceito"
                : $"formato '{ext}' não aceito: só JSON é aceito");
            return;
        }

        JsonDocument doc;
        try { doc = JsonDocument.Parse(arquivo.Conteudo); }
        catch (JsonException ex)
        {
            _rel.Novo("arquivo", arquivo.Nome).Rejeitar($"JSON inválido: {ex.Message}");
            return;
        }

        using (doc)
        {
            var registros = new List<JsonElement>();
            if (doc.RootElement.ValueKind == JsonValueKind.Array) registros.AddRange(doc.RootElement.EnumerateArray());
            else registros.Add(doc.RootElement);

            var posicao = 0;
            foreach (var o in registros)
            {
                posicao++;
                var fallback = $"{arquivo.Nome}#{posicao}";

                if (o.ValueKind != JsonValueKind.Object)
                {
                    _rel.Novo("registro", fallback).Rejeitar("esperado um objeto JSON");
                    continue;
                }

                var slug = Leitor.SlugDe(o);
                var erros = new List<string>();

                if (Leitor.EhParticipacao(o))
                {
                    var item = _rel.Novo("participacao", Leitor.RotuloParticipacao(o) ?? fallback);
                    var reg = Leitor.LerParticipacao(o, erros);
                    if (erros.Count > 0) RejeitarTodos(item, erros);
                    else lote.Participacoes.Add((reg, item));
                }
                else if (Leitor.EhEvento(o))
                {
                    var item = _rel.Novo("evento", slug ?? fallback);
                    var reg = Leitor.LerEvento(o, erros);
                    if (erros.Count > 0)
                    {
                        RejeitarTodos(item, erros);
                        if (slug is not null) _eventosRejeitados.Add(slug);
                    }
                    else lote.Eventos.Add((reg, item));
                }
                else if (Leitor.EhFonte(o))
                {
                    var item = _rel.Novo("fonte", slug ?? fallback);
                    var reg = Leitor.LerFonte(o, erros);
                    if (erros.Count > 0)
                    {
                        RejeitarTodos(item, erros);
                        if (slug is not null) _fontesRejeitadas.Add(slug);
                    }
                    else lote.Avulsas.Add((reg, item));
                }
                else if (Leitor.EhEquipamento(o))
                {
                    var item = _rel.Novo("equipamento", slug ?? fallback);
                    var reg = Leitor.LerEquipamento(o, erros);
                    if (erros.Count > 0)
                    {
                        RejeitarTodos(item, erros);
                        if (slug is not null) _equipamentosRejeitados.Add(slug);
                    }
                    else lote.Equipamentos.Add((reg, item));
                }
                else
                {
                    _rel.Novo("registro", slug ?? fallback)
                        .Rejeitar("registro não reconhecido (esperado fonte, equipamento, evento ou participação)");
                }
            }
        }
    }

    private static void RejeitarTodos(ItemRelatorio item, IEnumerable<string> erros)
    {
        foreach (var e in erros) item.Rejeitar(e);
    }

    // ================================================================== fontes
    private async Task GravarFontesAsync(Lote lote, CancellationToken ct)
    {
        // Primeiro as avulsas, depois as definidas dentro de equipamentos e eventos.
        var definidas = new HashSet<string>();
        foreach (var (reg, item) in lote.Avulsas)
        {
            definidas.Add(reg.Slug);
            _definicoes[reg.Slug] = reg;
            if (!await RegistroAsync(item, () => GravarFonteAsync(reg, ct), ct))
                _fontesRejeitadas.Add(reg.Slug);
        }

        var donos = lote.Equipamentos.Select(x => (x.Reg.Fontes, x.Item))
            .Concat(lote.Eventos.Select(x => (x.Reg.Fontes, x.Item)));
        foreach (var (fontes, dono) in donos)
        {
            foreach (var lig in fontes.Where(l => l.Definicao is not null))
            {
                var def = lig.Definicao!;
                if (!definidas.Add(def.Slug))
                {
                    if (_definicoes.TryGetValue(def.Slug, out var anterior) && !MesmaDefinicao(anterior, def))
                        dono.Avisos.Add($"fonte '{def.Slug}' redefinida com valores diferentes; mantida a primeira definição");
                    continue;
                }
                _definicoes[def.Slug] = def;
                var itemFonte = _rel.Novo("fonte", def.Slug);
                if (!await RegistroAsync(itemFonte, () => GravarFonteAsync(def, ct), ct))
                    _fontesRejeitadas.Add(def.Slug);
            }
        }
    }

    private static bool MesmaDefinicao(RegFonte a, RegFonte b) =>
        a.Tipo == b.Tipo && a.Titulo == b.Titulo && a.Autor == b.Autor && a.Ano == b.Ano
        && a.Url == b.Url && a.AcessadoEm == b.AcessadoEm && a.Provisoria == b.Provisoria;

    private async Task<InfoFonte?> ResolverFonteAsync(string slug, CancellationToken ct)
    {
        if (_fontes.TryGetValue(slug, out var cache)) return cache;
        var info = await db.Fontes.AsNoTracking()
            .Where(f => f.Slug == slug)
            .Select(f => new InfoFonte(f.Id, f.Provisoria))
            .FirstOrDefaultAsync(ct);
        if (info is not null) _fontes[slug] = info;
        return info;
    }

    /// <summary>Resolve as fontes de uma ficha; erros de fonte ausente ou rejeitada vão para a lista.</summary>
    private async Task<List<(int FonteId, string? Referencia, bool Provisoria)>> ResolverFontesAsync(
        List<LigacaoFonte> fontes, List<string> erros, CancellationToken ct)
    {
        var ligacoes = new List<(int FonteId, string? Referencia, bool Provisoria)>();
        foreach (var lig in fontes.GroupBy(l => l.Slug).Select(g => g.Last()))
        {
            if (_fontesRejeitadas.Contains(lig.Slug))
            {
                erros.Add($"fontes: dependência rejeitada (a fonte '{lig.Slug}' foi rejeitada)");
                continue;
            }
            var info = await ResolverFonteAsync(lig.Slug, ct);
            if (info is null) erros.Add($"fontes: fonte '{lig.Slug}' não existe");
            else ligacoes.Add((info.Id, lig.Referencia, info.Provisoria));
        }
        return ligacoes;
    }

    /// <summary>Ficha "confirmada" só com fontes provisórias vira "estimada", com aviso.</summary>
    private static Confiabilidade Rebaixar(
        Confiabilidade declarada, List<(int FonteId, string? Referencia, bool Provisoria)> ligacoes, ItemRelatorio item)
    {
        if (declarada == Confiabilidade.Confirmado && ligacoes.All(l => l.Provisoria))
        {
            item.Avisos.Add("confiabilidade rebaixada de CONFIRMADO para ESTIMADO: todas as fontes são provisórias");
            return Confiabilidade.Estimado;
        }
        return declarada;
    }

    private async Task<Resultado> GravarFonteAsync(RegFonte r, CancellationToken ct)
    {
        var f = await db.Fontes.FirstOrDefaultAsync(x => x.Slug == r.Slug, ct);
        var nova = f is null;
        if (f is null)
        {
            f = new Fonte { Slug = r.Slug, Titulo = r.Titulo };
            db.Fontes.Add(f);
        }

        f.Tipo = r.Tipo;
        f.Titulo = r.Titulo;
        f.Autor = r.Autor;
        f.Ano = r.Ano;
        f.Url = r.Url;
        f.AcessadoEm = r.AcessadoEm;
        f.Provisoria = r.Provisoria;

        if (!nova && !db.ChangeTracker.HasChanges())
        {
            _fontes[r.Slug] = new InfoFonte(f.Id, f.Provisoria);
            return Resultado.Ignorado;
        }

        await db.SaveChangesAsync(ct);
        _fontes[r.Slug] = new InfoFonte(f.Id, f.Provisoria);
        return nova ? Resultado.Criado : Resultado.Atualizado;
    }

    // =============================================================== equipamentos
    private async Task GravarEquipamentosAsync(Lote lote, CancellationToken ct)
    {
        var forcas = await db.Forcas.AsNoTracking().ToDictionaryAsync(f => f.Codigo, f => f.Id, ct);
        var categorias = await db.Categorias.AsNoTracking().ToDictionaryAsync(c => c.Codigo, c => c.Id, ct);

        foreach (var (eq, item) in lote.Equipamentos)
        {
            var erros = new List<string>();

            _paises.TryGetValue(eq.Pais, out var paisId);
            if (paisId == 0) erros.Add($"pais: '{eq.Pais}' não existe");

            var operadores = new List<int>();
            foreach (var iso in eq.Operadores)
            {
                if (_paises.TryGetValue(iso, out var idOp)) operadores.Add(idOp);
                else erros.Add($"operadores: país '{iso}' não existe");
            }

            if (!forcas.TryGetValue(eq.Forca, out var forcaId))
                erros.Add($"forca: '{eq.Forca.ToString().ToUpperInvariant()}' não existe");

            var categoriaOk = categorias.TryGetValue(eq.Categoria, out var categoriaId);
            if (!categoriaOk) erros.Add($"categoria: '{eq.Categoria}' não existe");

            var ligacoes = await ResolverFontesAsync(eq.Fontes, erros, ct);

            if (categoriaOk)
                erros.AddRange(await ValidarEspecificacoesAsync(eq));

            if (erros.Count > 0)
            {
                RejeitarTodos(item, erros);
                _equipamentosRejeitados.Add(eq.Slug);
                continue;
            }

            var confiabilidade = Rebaixar(eq.Confiabilidade, ligacoes, item);
            if (!await RegistroAsync(item,
                    () => GravarEquipamentoAsync(eq, paisId, forcaId, categoriaId, operadores, ligacoes, confiabilidade, ct), ct))
                _equipamentosRejeitados.Add(eq.Slug);
        }
    }

    private static async Task<IEnumerable<string>> ValidarEspecificacoesAsync(RegEquipamento eq)
    {
        try
        {
            var erros = await EspecificacoesValidador.ValidarAsync(eq.Categoria, eq.EspecificacoesJson);
            return erros.Select(Traduzir).ToList();
        }
        catch (ArgumentException ex)
        {
            return [$"categoria: {ex.Message}"];
        }
    }

    /// <summary>Converte "#/campo: Tipo" do validador em mensagem com o campo e o valor esperado.</summary>
    private static string Traduzir(string erro)
    {
        var corte = erro.LastIndexOf(": ", StringComparison.Ordinal);
        var caminho = corte < 0 ? "#" : erro[..corte];
        var tipo = corte < 0 ? erro : erro[(corte + 2)..];

        var campo = caminho.TrimStart('#').Trim('/').Replace('/', '.');
        var local = campo.Length == 0 ? "especificacoes" : $"especificacoes.{campo}";

        var mensagem = tipo switch
        {
            "NoAdditionalPropertiesAllowed" => "campo desconhecido para esta categoria",
            "PropertyRequired" => "campo obrigatório ausente",
            "NumberExpected" => "esperado número (na unidade padrão, sem texto)",
            "IntegerExpected" => "esperado número inteiro",
            "StringExpected" => "esperado texto",
            "ArrayExpected" => "esperado lista",
            "ObjectExpected" => "esperado objeto",
            "BooleanExpected" => "esperado verdadeiro ou falso",
            "NotInEnumeration" => "valor fora da lista permitida",
            "NumberTooSmall" or "NumberTooBig" => "número fora do limite permitido",
            "StringTooShort" => "texto vazio",
            _ => tipo,
        };
        return $"{local}: {mensagem}";
    }

    private async Task<Resultado> GravarEquipamentoAsync(
        RegEquipamento r, int paisId, int forcaId, int categoriaId,
        List<int> operadores, List<(int FonteId, string? Referencia, bool Provisoria)> fontes,
        Confiabilidade confiabilidade, CancellationToken ct)
    {
        var e = await db.Equipamentos
            .Include(x => x.Operadores)
            .Include(x => x.Fontes)
            .FirstOrDefaultAsync(x => x.Slug == r.Slug, ct);

        var novo = e is null;
        if (e is null)
        {
            e = new Equipamento { Slug = r.Slug, Nome = r.Nome, Confiabilidade = confiabilidade };
            db.Equipamentos.Add(e);
        }

        // O registro é substituído por inteiro: campo ausente na ficha vira vazio.
        e.Nome = r.Nome;
        e.Variante = r.Variante;
        e.PaisId = paisId;
        e.ForcaId = forcaId;
        e.CategoriaId = categoriaId;
        e.Fabricante = r.Fabricante;
        e.AnoPrimeiroUso = r.AnoPrimeiroUso;
        e.AnoEntradaServico = r.AnoEntradaServico;
        e.UnidadesProduzidas = r.UnidadesProduzidas;
        e.Status = r.Status;
        e.DescricaoCurta = r.DescricaoCurta;
        e.Curiosidade = r.Curiosidade;
        e.ImagemUrl = r.ImagemUrl;
        e.ImagemCredito = r.ImagemCredito;
        e.Especificacoes = r.EspecificacoesJson;
        e.Confiabilidade = confiabilidade;

        // Operadores e fontes: remove o que saiu, inclui o que entrou.
        foreach (var antigo in e.Operadores.Where(o => !operadores.Contains(o.PaisId)).ToList())
            db.EquipamentoOperadores.Remove(antigo);
        foreach (var id in operadores.Where(id => e.Operadores.All(o => o.PaisId != id)))
            e.Operadores.Add(new EquipamentoOperador { PaisId = id });

        var idsFontes = fontes.Select(f => f.FonteId).ToHashSet();
        foreach (var antiga in e.Fontes.Where(f => !idsFontes.Contains(f.FonteId)).ToList())
            db.EquipamentoFontes.Remove(antiga);
        foreach (var (fonteId, referencia, _) in fontes)
        {
            var existente = e.Fontes.FirstOrDefault(f => f.FonteId == fonteId);
            if (existente is null) e.Fontes.Add(new EquipamentoFonte { FonteId = fonteId, Referencia = referencia });
            else existente.Referencia = referencia;
        }

        if (!novo && !db.ChangeTracker.HasChanges()) return Resultado.Ignorado;

        await db.SaveChangesAsync(ct);
        await AtualizarBuscaAsync("equipamento", r.Slug,
            Juntar(r.Nome, r.Variante), Juntar(r.DescricaoCurta, r.Fabricante, r.Curiosidade), ct);

        return novo ? Resultado.Criado : Resultado.Atualizado;
    }

    // ================================================================== eventos
    private async Task GravarEventosAsync(Lote lote, CancellationToken ct)
    {
        // Pai antes do filho: repete até não haver mais evento que possa ser gravado.
        var restantes = lote.Eventos.ToList();
        var gravados = new HashSet<string>();

        while (restantes.Count > 0)
        {
            var progresso = false;
            foreach (var par in restantes.ToList())
            {
                var (ev, item) = par;
                var pai = ev.EventoPai;

                if (pai is not null && pai != ev.Slug && _eventosRejeitados.Contains(pai))
                {
                    item.Rejeitar($"evento_pai: dependência rejeitada (o evento '{pai}' foi rejeitado)");
                    _eventosRejeitados.Add(ev.Slug);
                }
                else if (pai is not null && restantes.Any(r => r.Reg.Slug == pai) && !gravados.Contains(pai))
                {
                    continue; // o pai ainda está na fila
                }
                else
                {
                    if (await ProcessarEventoAsync(ev, item, ct)) gravados.Add(ev.Slug);
                    else _eventosRejeitados.Add(ev.Slug);
                }

                restantes.Remove(par);
                progresso = true;
            }

            if (!progresso) break;
        }

        foreach (var (ev, item) in restantes)
        {
            item.Rejeitar("evento_pai: referência circular entre eventos");
            _eventosRejeitados.Add(ev.Slug);
        }
    }

    /// <summary>Valida e grava um evento. Devolve falso se ele foi rejeitado.</summary>
    private async Task<bool> ProcessarEventoAsync(RegEvento ev, ItemRelatorio item, CancellationToken ct)
    {
        var erros = new List<string>();
        var avisos = new List<string>();

        int? paiId = null;
        if (ev.EventoPai is not null)
        {
            if (ev.EventoPai == ev.Slug) erros.Add("evento_pai: um evento não pode ser pai de si mesmo");
            else
            {
                paiId = await ObterIdEventoAsync(ev.EventoPai, ct);
                if (paiId is null) erros.Add($"evento_pai: evento '{ev.EventoPai}' não existe");
                else
                {
                    if (await CriaCicloAsync(ev.Slug, paiId, ct))
                        erros.Add($"evento_pai: '{ev.EventoPai}' já descende deste evento (referência circular)");

                    var periodo = await db.Eventos.AsNoTracking().Where(x => x.Id == paiId)
                        .Select(x => new { x.DataInicio, x.DataFim }).FirstAsync(ct);
                    if (ev.DataInicio < periodo.DataInicio || (periodo.DataFim is { } fimPai && (ev.DataFim ?? ev.DataInicio) > fimPai))
                        avisos.Add($"o período do evento sai do período do evento pai '{ev.EventoPai}'");
                }
            }
        }

        var beligerantes = new List<(int PaisId, string? Lado)>();
        foreach (var (iso, lado) in ev.Beligerantes)
        {
            if (_paises.TryGetValue(iso, out var id)) beligerantes.Add((id, lado));
            else erros.Add($"beligerantes: país '{iso}' não existe");
        }

        var ligacoes = await ResolverFontesAsync(ev.Fontes, erros, ct);

        if (erros.Count > 0)
        {
            RejeitarTodos(item, erros);
            return false;
        }

        item.Avisos.AddRange(avisos);
        var confiabilidade = Rebaixar(ev.Confiabilidade, ligacoes, item);
        return await RegistroAsync(item, () => GravarEventoAsync(ev, paiId, beligerantes, ligacoes, confiabilidade, ct), ct);
    }

    private async Task<int?> ObterIdEventoAsync(string slug, CancellationToken ct) =>
        await db.Eventos.AsNoTracking().Where(e => e.Slug == slug).Select(e => (int?)e.Id).FirstOrDefaultAsync(ct);

    /// <summary>Verdadeiro se o novo pai já descende do evento (já gravado) que está sendo reimportado.</summary>
    private async Task<bool> CriaCicloAsync(string slug, int? paiId, CancellationToken ct)
    {
        var idEvento = await ObterIdEventoAsync(slug, ct);
        if (idEvento is null) return false;

        var atual = paiId;
        for (var i = 0; i < 100 && atual is not null; i++)
        {
            if (atual == idEvento) return true;
            var id = atual;
            atual = await db.Eventos.AsNoTracking().Where(x => x.Id == id).Select(x => x.EventoPaiId).FirstOrDefaultAsync(ct);
        }
        return false;
    }

    private async Task<Resultado> GravarEventoAsync(
        RegEvento r, int? paiId, List<(int PaisId, string? Lado)> beligerantes,
        List<(int FonteId, string? Referencia, bool Provisoria)> fontes,
        Confiabilidade confiabilidade, CancellationToken ct)
    {
        var e = await db.Eventos
            .Include(x => x.Paises)
            .Include(x => x.Fontes)
            .FirstOrDefaultAsync(x => x.Slug == r.Slug, ct);

        var novo = e is null;
        if (e is null)
        {
            e = new Evento { Slug = r.Slug, Nome = r.Nome, Confiabilidade = confiabilidade };
            db.Eventos.Add(e);
        }

        e.Nome = r.Nome;
        e.Tipo = r.Tipo;
        e.EventoPaiId = paiId;
        e.DataInicio = r.DataInicio;
        e.DataFim = r.DataFim;
        e.Local = r.Local;
        e.Descricao = r.Descricao;
        e.Resultado = r.Resultado;
        e.Confiabilidade = confiabilidade;

        var idsPaises = beligerantes.Select(b => b.PaisId).ToHashSet();
        foreach (var antigo in e.Paises.Where(p => !idsPaises.Contains(p.PaisId)).ToList())
            db.EventoPaises.Remove(antigo);
        foreach (var (paisId, lado) in beligerantes)
        {
            var existente = e.Paises.FirstOrDefault(p => p.PaisId == paisId);
            if (existente is null) e.Paises.Add(new EventoPais { PaisId = paisId, Lado = lado });
            else existente.Lado = lado;
        }

        var idsFontes = fontes.Select(f => f.FonteId).ToHashSet();
        foreach (var antiga in e.Fontes.Where(f => !idsFontes.Contains(f.FonteId)).ToList())
            db.EventoFontes.Remove(antiga);
        foreach (var (fonteId, referencia, _) in fontes)
        {
            var existente = e.Fontes.FirstOrDefault(f => f.FonteId == fonteId);
            if (existente is null) e.Fontes.Add(new EventoFonte { FonteId = fonteId, Referencia = referencia });
            else existente.Referencia = referencia;
        }

        if (!novo && !db.ChangeTracker.HasChanges()) return Resultado.Ignorado;

        await db.SaveChangesAsync(ct);
        await AtualizarBuscaAsync("evento", r.Slug, r.Nome, Juntar(r.Descricao, r.Local, r.Resultado), ct);

        return novo ? Resultado.Criado : Resultado.Atualizado;
    }

    // ============================================================== participações
    private async Task GravarParticipacoesAsync(Lote lote, CancellationToken ct)
    {
        foreach (var (p, item) in lote.Participacoes)
        {
            var erros = new List<string>();
            var avisos = new List<string>();

            int? equipamentoId = null;
            if (_equipamentosRejeitados.Contains(p.Equipamento))
                erros.Add($"equipamento: dependência rejeitada (o equipamento '{p.Equipamento}' foi rejeitado)");
            else
            {
                equipamentoId = await db.Equipamentos.AsNoTracking().Where(e => e.Slug == p.Equipamento)
                    .Select(e => (int?)e.Id).FirstOrDefaultAsync(ct);
                if (equipamentoId is null) erros.Add($"equipamento: '{p.Equipamento}' não existe");
            }

            int? eventoId = null;
            if (_eventosRejeitados.Contains(p.Evento))
                erros.Add($"evento: dependência rejeitada (o evento '{p.Evento}' foi rejeitado)");
            else
            {
                eventoId = await ObterIdEventoAsync(p.Evento, ct);
                if (eventoId is null) erros.Add($"evento: '{p.Evento}' não existe");
            }

            var paisOk = _paises.TryGetValue(p.Pais, out var paisId);
            if (!paisOk) erros.Add($"pais: '{p.Pais}' não existe");

            if (paisOk && eventoId is { } ev)
            {
                var beligerante = await db.EventoPaises.AsNoTracking()
                    .AnyAsync(x => x.EventoId == ev && x.PaisId == paisId, ct);
                if (!beligerante) erros.Add($"pais: '{p.Pais}' não é beligerante do evento '{p.Evento}'");
            }

            if (erros.Count > 0)
            {
                RejeitarTodos(item, erros);
                continue;
            }

            var eqId = equipamentoId!.Value;
            var evId = eventoId!.Value;
            var opera = await db.EquipamentoOperadores.AsNoTracking().AnyAsync(x => x.EquipamentoId == eqId && x.PaisId == paisId, ct)
                || await db.Equipamentos.AsNoTracking().AnyAsync(x => x.Id == eqId && x.PaisId == paisId, ct);
            if (!opera)
                avisos.Add($"o país '{p.Pais}' não consta entre os operadores nem é o país de origem do equipamento '{p.Equipamento}'");
            item.Avisos.AddRange(avisos);

            await RegistroAsync(item, () => GravarParticipacaoAsync(p, eqId, evId, paisId, ct), ct);
        }
    }

    private async Task<Resultado> GravarParticipacaoAsync(
        RegParticipacao r, int equipamentoId, int eventoId, int paisId, CancellationToken ct)
    {
        var p = await db.Participacoes.FirstOrDefaultAsync(
            x => x.EquipamentoId == equipamentoId && x.EventoId == eventoId && x.PaisId == paisId, ct);

        var nova = p is null;
        if (p is null)
        {
            p = new Participacao { EquipamentoId = equipamentoId, EventoId = eventoId, PaisId = paisId, Papel = r.Papel };
            db.Participacoes.Add(p);
        }

        p.Papel = r.Papel;
        p.QuantidadeAprox = r.QuantidadeAprox;
        p.Observacao = r.Observacao;

        if (!nova && !db.ChangeTracker.HasChanges()) return Resultado.Ignorado;

        await db.SaveChangesAsync(ct);
        return nova ? Resultado.Criado : Resultado.Atualizado;
    }

    // ================================================================ utilitários
    private static string Juntar(params string?[] partes) =>
        string.Join(' ', partes.Where(s => !string.IsNullOrWhiteSpace(s)));

    /// <summary>Índice de busca (FTS5): substitui a linha do registro.</summary>
    private async Task AtualizarBuscaAsync(string tipo, string slug, string nome, string descricao, CancellationToken ct)
    {
        await db.Database.ExecuteSqlAsync($"DELETE FROM busca WHERE tipo = {tipo} AND slug = {slug}", ct);
        await db.Database.ExecuteSqlAsync(
            $"INSERT INTO busca (tipo, slug, nome, descricao) VALUES ({tipo}, {slug}, {nome}, {descricao})", ct);
    }

    /// <summary>
    /// Grava um registro dentro de um savepoint. Violação de restrição do banco rejeita só o
    /// registro; qualquer outra falha sobe e desfaz a execução inteira.
    /// </summary>
    private async Task<bool> RegistroAsync(ItemRelatorio item, Func<Task<Resultado>> gravar, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync("SAVEPOINT reg", ct);
        try
        {
            item.Resultado = await gravar();
            await db.Database.ExecuteSqlRawAsync("RELEASE reg", ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is SqliteException { SqliteErrorCode: 19 })
        {
            await db.Database.ExecuteSqlRawAsync("ROLLBACK TO reg", ct);
            await db.Database.ExecuteSqlRawAsync("RELEASE reg", ct);
            item.Rejeitar($"violação de restrição do banco: {ex.GetBaseException().Message}");
            return false;
        }
        finally
        {
            db.ChangeTracker.Clear();
        }
    }
}
