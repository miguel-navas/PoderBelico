using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PoderBelico.Dados;
using PoderBelico.Importador;
using static PoderBelico.Api.Tests.ImportadorTests;

namespace PoderBelico.Api.Tests;

public class EventosApiTests(FabricaApi fabrica) : IClassFixture<FabricaApi>, IAsyncLifetime
{
    private static string Ev(
        string slug, string tipo, string inicio, string? fim, string? pai, string beligerantes)
    {
        var linhaPai = pai is null ? "" : "\"evento_pai\": \"" + pai + "\",";
        var linhaFim = fim is null ? "" : "\"data_fim\": \"" + fim + "\",";
        return $$"""
            {
              "slug": "{{slug}}", "nome": "Evento {{slug}}", "tipo": "{{tipo}}",
              {{linhaPai}}
              "data_inicio": "{{inicio}}",
              {{linhaFim}}
              "local": "Local {{slug}}", "descricao": "Descrição {{slug}}", "resultado": "Resultado {{slug}}",
              "confiabilidade": "ESTIMADO",
              "beligerantes": {{beligerantes}},
              "fontes": [ {{FonteDefinitiva}} ]
            }
            """;
    }

    private const string Caca = """
        {
          "slug": "caca-teste", "nome": "Caça Teste", "pais": "USA", "forca": "AEREA", "categoria": "AERONAVE",
          "status": "APOSENTADO", "confiabilidade": "ESTIMADO", "ano_entrada_servico": 1940,
          "operadores": ["USA"],
          "especificacoes": { "subtipo": "caca", "tripulacao": 1 },
          "fontes": [ "livro-x" ]
        }
        """;

    private const string Participacoes = """
        [
          { "equipamento": "tanque-teste", "evento": "guerra-teste", "pais": "RUS", "papel": "Linha de frente", "quantidade_aprox": 500 },
          { "equipamento": "caca-teste", "evento": "batalha-teste", "pais": "USA", "papel": "Ataque ao solo" }
        ]
        """;

    public async Task InitializeAsync()
    {
        using var escopo = fabrica.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<PoderBelicoContext>();
        await db.Database.EnsureCreatedAsync();
        await SementeReferencia.AplicarAsync(db, BancoEmMemoria.CaminhoPaises);

        var arquivos = new List<Arquivo>
        {
            new("tanque.json", Tanque("tanque-teste")),
            new("caca.json", Caca),
            // de propósito fora de ordem: o filho vem antes do pai
            new("batalha.json", Ev("batalha-teste", "BATALHA", "1942-06-04", "1942-06-07", "campanha-teste",
                """[ { "pais": "USA", "lado": "Aliados" }, { "pais": "JPN", "lado": "Eixo" } ]""")),
            new("campanha.json", Ev("campanha-teste", "CAMPANHA", "1942-01-01", "1943-02-02", "guerra-teste",
                """[ { "pais": "USA", "lado": "Aliados" } ]""")),
            new("guerra.json", Ev("guerra-teste", "GUERRA", "1939-09-01", "1945-09-02", null,
                """[ { "pais": "USA", "lado": "Aliados" }, { "pais": "RUS", "lado": "Aliados" }, { "pais": "DEU", "lado": "Eixo" }, { "pais": "JPN", "lado": "Eixo" } ]""")),
            new("atual.json", Ev("conflito-atual", "GUERRA", "2022-02-24", null, null,
                """[ { "pais": "RUS", "lado": "A" } ]""")),
            new("participacoes.json", Participacoes),
        };
        var rel = await new ServicoImportacao(db).ExecutarAsync(arquivos, new Opcoes());
        Assert.True(rel.CodigoSaida == 0, rel.EmTexto());
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<JsonElement> ObterAsync(string url, HttpStatusCode esperado = HttpStatusCode.OK)
    {
        var resposta = await fabrica.CreateClient().GetAsync(url);
        Assert.Equal(esperado, resposta.StatusCode);
        return await resposta.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static string[] Slugs(JsonElement lista) =>
        lista.GetProperty("itens").EnumerateArray().Select(i => i.GetProperty("slug").GetString()!).ToArray();

    // ---------------------------------------------------------------- lista
    [Fact]
    public async Task Lista_em_ordem_cronologica()
    {
        var lista = await ObterAsync("/api/eventos");

        Assert.Equal(new[] { "guerra-teste", "campanha-teste", "batalha-teste", "conflito-atual" }, Slugs(lista));
        Assert.Equal(4, lista.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Filtrar_por_periodo_traz_so_os_eventos_que_cruzam_o_intervalo()
    {
        var lista = await ObterAsync("/api/eventos?de=1939&ate=1945");

        Assert.Equal(new[] { "guerra-teste", "campanha-teste", "batalha-teste" }, Slugs(lista));
    }

    [Fact]
    public async Task Evento_em_andamento_entra_em_periodo_posterior_ao_seu_inicio()
    {
        Assert.Equal(new[] { "conflito-atual" }, Slugs(await ObterAsync("/api/eventos?de=2030")));
        Assert.Equal(new[] { "guerra-teste" }, Slugs(await ObterAsync("/api/eventos?ate=1940")));
    }

    [Fact]
    public async Task Filtra_por_tipo_e_por_pais_beligerante()
    {
        Assert.Equal(new[] { "batalha-teste" }, Slugs(await ObterAsync("/api/eventos?tipo=batalha")));
        Assert.Equal(new[] { "guerra-teste", "batalha-teste" }, Slugs(await ObterAsync("/api/eventos?pais=JPN")));
    }

    [Theory]
    [InlineData("/api/eventos?tipo=duelo")]
    [InlineData("/api/eventos?de=0")]
    [InlineData("/api/eventos?ate=10000")]
    public async Task Filtro_invalido_devolve_400(string url)
    {
        var corpo = await ObterAsync(url, HttpStatusCode.BadRequest);

        Assert.Contains("inválid", corpo.GetProperty("erro").GetString());
    }

    [Fact]
    public async Task Lista_traz_o_evento_pai()
    {
        var lista = await ObterAsync("/api/eventos?tipo=batalha");

        var pai = lista.GetProperty("itens")[0].GetProperty("eventoPai");
        Assert.Equal("campanha-teste", pai.GetProperty("slug").GetString());
    }

    // -------------------------------------------------------------- detalhe
    [Fact]
    public async Task Batalha_mostra_o_evento_pai()
    {
        var evento = await ObterAsync("/api/eventos/batalha-teste");

        Assert.Equal("BATALHA", evento.GetProperty("tipo").GetString());
        Assert.Equal("campanha-teste", evento.GetProperty("eventoPai").GetProperty("slug").GetString());
        Assert.Equal("Local batalha-teste", evento.GetProperty("local").GetString());
        Assert.Equal("Resultado batalha-teste", evento.GetProperty("resultado").GetString());
        Assert.Equal("1942-06-04", evento.GetProperty("dataInicio").GetString());
    }

    [Fact]
    public async Task Guerra_lista_os_subeventos_em_arvore()
    {
        var guerra = await ObterAsync("/api/eventos/guerra-teste");

        Assert.Equal(JsonValueKind.Null, guerra.GetProperty("eventoPai").ValueKind);
        var campanha = Assert.Single(guerra.GetProperty("subeventos").EnumerateArray());
        Assert.Equal("campanha-teste", campanha.GetProperty("slug").GetString());
        var batalha = Assert.Single(campanha.GetProperty("subeventos").EnumerateArray());
        Assert.Equal("batalha-teste", batalha.GetProperty("slug").GetString());
    }

    [Fact]
    public async Task Beligerantes_vem_agrupados_por_lado()
    {
        var guerra = await ObterAsync("/api/eventos/guerra-teste");

        var lados = guerra.GetProperty("beligerantes").EnumerateArray().ToList();
        Assert.Equal(new[] { "Aliados", "Eixo" }, lados.Select(l => l.GetProperty("lado").GetString()));
        var aliados = lados[0].GetProperty("paises").EnumerateArray().Select(p => p.GetProperty("codigo").GetString());
        Assert.Equal(new[] { "RUS", "USA" }.OrderBy(x => x), aliados.OrderBy(x => x));
        var eixo = lados[1].GetProperty("paises").EnumerateArray().Select(p => p.GetProperty("codigo").GetString());
        Assert.Equal(new[] { "DEU", "JPN" }.OrderBy(x => x), eixo.OrderBy(x => x));
    }

    [Fact]
    public async Task Evento_sem_data_de_fim_vem_com_fim_nulo()
    {
        var evento = await ObterAsync("/api/eventos/conflito-atual");

        Assert.Equal(JsonValueKind.Null, evento.GetProperty("dataFim").ValueKind);
    }

    [Fact]
    public async Task Equipamentos_do_evento_vem_agrupados_por_pais_e_forca()
    {
        var guerra = await ObterAsync("/api/eventos/guerra-teste");

        var grupo = Assert.Single(guerra.GetProperty("participacoes").EnumerateArray());
        Assert.Equal("RUS", grupo.GetProperty("pais").GetProperty("codigo").GetString());
        var forca = Assert.Single(grupo.GetProperty("forcas").EnumerateArray());
        Assert.Equal("EXERCITO", forca.GetProperty("forca").GetString());
        var equipamento = Assert.Single(forca.GetProperty("equipamentos").EnumerateArray());
        Assert.Equal("tanque-teste", equipamento.GetProperty("slug").GetString());
        Assert.Equal("Linha de frente", equipamento.GetProperty("papel").GetString());
        Assert.Equal(500, equipamento.GetProperty("quantidadeAprox").GetInt32());
    }

    [Fact]
    public async Task Evento_sem_participacoes_devolve_lista_vazia()
    {
        var evento = await ObterAsync("/api/eventos/campanha-teste");

        Assert.Equal(0, evento.GetProperty("participacoes").GetArrayLength());
    }

    [Fact]
    public async Task Evento_traz_as_fontes()
    {
        var evento = await ObterAsync("/api/eventos/guerra-teste");

        Assert.Equal("Livro X", evento.GetProperty("fontes")[0].GetProperty("titulo").GetString());
        Assert.False(evento.GetProperty("somenteFontesProvisorias").GetBoolean());
    }

    [Fact]
    public async Task Slug_inexistente_devolve_404()
    {
        var corpo = await ObterAsync("/api/eventos/nao-existe", HttpStatusCode.NotFound);

        Assert.Contains("nao-existe", corpo.GetProperty("erro").GetString());
    }

    [Fact]
    public async Task Ficha_do_equipamento_lista_os_eventos_com_papel_e_link()
    {
        var ficha = await ObterAsync("/api/equipamentos/caca-teste");

        var participacao = Assert.Single(ficha.GetProperty("eventos").EnumerateArray());
        Assert.Equal("batalha-teste", participacao.GetProperty("evento").GetProperty("slug").GetString());
        Assert.Equal("BATALHA", participacao.GetProperty("evento").GetProperty("tipo").GetString());
        Assert.Equal("Ataque ao solo", participacao.GetProperty("papel").GetString());
        Assert.Equal("USA", participacao.GetProperty("pais").GetProperty("codigo").GetString());
        Assert.Equal("1942-06-04", participacao.GetProperty("dataInicio").GetString());
    }

    [Fact]
    public async Task Ficha_traz_quantidade_aproximada_da_participacao()
    {
        var ficha = await ObterAsync("/api/equipamentos/tanque-teste");

        var participacao = Assert.Single(ficha.GetProperty("eventos").EnumerateArray());
        Assert.Equal("guerra-teste", participacao.GetProperty("evento").GetProperty("slug").GetString());
        Assert.Equal(500, participacao.GetProperty("quantidadeAprox").GetInt32());
    }

    [Fact]
    public async Task Filtros_trazem_paises_de_eventos_e_tipos()
    {
        var filtros = await ObterAsync("/api/filtros");

        var paises = filtros.GetProperty("paisesEventos").EnumerateArray().Select(p => p.GetProperty("codigo").GetString());
        Assert.Equal(new[] { "DEU", "JPN", "RUS", "USA" }, paises.OrderBy(x => x));
        Assert.Equal(3, filtros.GetProperty("tiposEvento").GetArrayLength());
    }

    // ------------------------------------------------- filtro por força / linha do tempo
    [Theory]
    [InlineData("AEREA", "batalha-teste")]
    [InlineData("aerea", "batalha-teste")]
    [InlineData("EXERCITO", "guerra-teste")]
    public async Task Lista_filtra_eventos_pela_forca_dos_equipamentos(string forca, string esperado)
    {
        var r = await ObterAsync("/api/eventos?forca=" + forca);

        Assert.Equal(new[] { esperado }, Slugs(r));
    }

    [Fact]
    public async Task Lista_com_forca_sem_equipamentos_vem_vazia_e_forca_invalida_da_400()
    {
        Assert.Empty(Slugs(await ObterAsync("/api/eventos?forca=MARINHA")));
        await ObterAsync("/api/eventos?forca=XYZ", HttpStatusCode.BadRequest);
    }

    private static string[] Raizes(JsonElement r) =>
        r.GetProperty("raizes").EnumerateArray().Select(i => i.GetProperty("slug").GetString()!).ToArray();

    [Fact]
    public async Task Linha_do_tempo_agrupa_por_guerra_em_ordem_cronologica()
    {
        var r = await ObterAsync("/api/linha-do-tempo");

        Assert.Equal(new[] { "guerra-teste", "conflito-atual" }, Raizes(r));
        var campanha = r.GetProperty("raizes")[0].GetProperty("subeventos")[0];
        Assert.Equal("campanha-teste", campanha.GetProperty("slug").GetString());
        Assert.Equal("batalha-teste", campanha.GetProperty("subeventos")[0].GetProperty("slug").GetString());
    }

    [Fact]
    public async Task Linha_do_tempo_filtrada_mantem_os_ancestrais_do_evento_que_casa()
    {
        var r = await ObterAsync("/api/linha-do-tempo?forca=AEREA");

        Assert.Equal(new[] { "guerra-teste" }, Raizes(r));
        var campanha = r.GetProperty("raizes")[0].GetProperty("subeventos")[0];
        Assert.Equal("campanha-teste", campanha.GetProperty("slug").GetString());
        Assert.Equal("batalha-teste", campanha.GetProperty("subeventos")[0].GetProperty("slug").GetString());
    }

    [Fact]
    public async Task Linha_do_tempo_filtrada_por_forca_sem_subeventos_que_nao_casam()
    {
        var r = await ObterAsync("/api/linha-do-tempo?forca=EXERCITO");

        Assert.Equal(new[] { "guerra-teste" }, Raizes(r));
        Assert.Empty(r.GetProperty("raizes")[0].GetProperty("subeventos").EnumerateArray());
    }

    [Fact]
    public async Task Linha_do_tempo_filtra_por_pais_e_valida_a_forca()
    {
        Assert.Equal(new[] { "guerra-teste", "conflito-atual" }, Raizes(await ObterAsync("/api/linha-do-tempo?pais=RUS")));
        Assert.Equal(new[] { "guerra-teste" }, Raizes(await ObterAsync("/api/linha-do-tempo?pais=DEU")));
        await ObterAsync("/api/linha-do-tempo?forca=XYZ", HttpStatusCode.BadRequest);
    }
}
