using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PoderBelico.Dados;
using PoderBelico.Importador;

namespace PoderBelico.Api.Tests;

public class EquipamentosApiTests(FabricaApi fabrica) : IClassFixture<FabricaApi>, IAsyncLifetime
{
    private const string Aeronave = """
        {
          "slug": "a-10-teste", "nome": "A-10 Teste", "pais": "USA", "forca": "AEREA", "categoria": "AERONAVE",
          "status": "ATIVO", "confiabilidade": "ESTIMADO", "ano_entrada_servico": 1977, "unidades_produzidas": 716,
          "imagem_url": "https://exemplo.org/a10.jpg", "imagem_credito": "Foto: Fulano, CC BY-SA",
          "operadores": ["USA"],
          "especificacoes": {
            "subtipo": "ataque_ao_solo", "tripulacao": 1, "data_primeiro_voo": "1972-05-10",
            "envergadura_m": 17.53, "peso_max_decolagem_kg": 23000, "velocidade_max_kmh": 706,
            "motores": { "quantidade": 2, "empuxo_lbf": 9065 },
            "armamento": [ { "descricao": "Canhão GAU-8", "municao": 1174 } ],
            "blindagem": "Titânio"
          },
          "fontes": [ { "slug": "livro-a", "tipo": "LIVRO", "titulo": "Livro A", "autor": "Autor", "ano": 2000 } ]
        }
        """;

    private const string Navio = """
        {
          "slug": "porta-avioes-teste", "nome": "Porta-Aviões Teste", "pais": "USA", "forca": "MARINHA",
          "categoria": "NAVIO", "status": "ATIVO", "confiabilidade": "ESTIMADO", "ano_entrada_servico": 1975,
          "imagem_url": "https://exemplo.org/nimitz.jpg",
          "operadores": ["USA"],
          "especificacoes": { "subtipo": "porta_avioes", "deslocamento_t": 100000, "velocidade_max_kmh": 56 },
          "fontes": [ { "slug": "ia-a", "tipo": "SITE", "titulo": "Conversa com IA", "provisoria": true } ]
        }
        """;

    private const string Tanque = """
        {
          "slug": "tanque-teste", "nome": "Tanque Teste", "pais": "RUS", "forca": "EXERCITO", "categoria": "TANQUE",
          "status": "APOSENTADO", "confiabilidade": "ESTIMADO", "ano_entrada_servico": 1990,
          "operadores": ["RUS"],
          "especificacoes": {
            "subtipo": "tanque_de_batalha", "peso_kg": 46000, "tripulacao": 3,
            "armamento_principal": { "calibre_mm": 125, "modelo": "2A46" }, "torre_tripulada": true
          },
          "fontes": [ { "slug": "livro-a" } ]
        }
        """;

    // Cada teste cria a sua instância; a importação é idempotente.
    public async Task InitializeAsync()
    {
        using var escopo = fabrica.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<PoderBelicoContext>();
        await db.Database.EnsureCreatedAsync();
        await SementeReferencia.AplicarAsync(db, BancoEmMemoria.CaminhoPaises);

        var rel = await new ServicoImportacao(db).ExecutarAsync(
            [new Arquivo("a.json", Aeronave), new Arquivo("n.json", Navio), new Arquivo("t.json", Tanque)],
            new Opcoes());
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
    public async Task Lista_sem_filtros_traz_tudo_ordenado_por_nome()
    {
        var lista = await ObterAsync("/api/equipamentos");

        Assert.Equal(new[] { "a-10-teste", "porta-avioes-teste", "tanque-teste" }, Slugs(lista));
        Assert.Equal(3, lista.GetProperty("total").GetInt32());
        Assert.Equal(1, lista.GetProperty("pagina").GetInt32());
    }

    [Fact]
    public async Task Filtra_por_forca_e_pais()
    {
        var lista = await ObterAsync("/api/equipamentos?forca=exercito&pais=RUS");

        Assert.Equal(new[] { "tanque-teste" }, Slugs(lista));
        var item = lista.GetProperty("itens")[0];
        Assert.Equal("EXERCITO", item.GetProperty("forca").GetString());
        Assert.Equal("RUS", item.GetProperty("pais").GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task Filtros_combinados_sem_correspondencia_devolvem_lista_vazia()
    {
        var lista = await ObterAsync("/api/equipamentos?forca=EXERCITO&pais=USA");

        Assert.Empty(Slugs(lista));
        Assert.Equal(0, lista.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Filtra_por_categoria_status_e_periodo()
    {
        Assert.Equal(new[] { "porta-avioes-teste" }, Slugs(await ObterAsync("/api/equipamentos?categoria=navio")));
        Assert.Equal(new[] { "tanque-teste" }, Slugs(await ObterAsync("/api/equipamentos?status=aposentado")));
        Assert.Equal(new[] { "a-10-teste", "porta-avioes-teste" },
            Slugs(await ObterAsync("/api/equipamentos?de=1970&ate=1980")));
        Assert.Equal(new[] { "tanque-teste" }, Slugs(await ObterAsync("/api/equipamentos?de=1985")));
    }

    [Fact]
    public async Task Pagina_os_resultados()
    {
        var segunda = await ObterAsync("/api/equipamentos?tamanho=1&pagina=2");

        Assert.Equal(new[] { "porta-avioes-teste" }, Slugs(segunda));
        Assert.Equal(3, segunda.GetProperty("totalPaginas").GetInt32());
        Assert.Equal(3, segunda.GetProperty("total").GetInt32());
    }

    [Theory]
    [InlineData("/api/equipamentos?forca=espaco")]
    [InlineData("/api/equipamentos?status=quebrado")]
    public async Task Filtro_invalido_devolve_400(string url)
    {
        var corpo = await ObterAsync(url, HttpStatusCode.BadRequest);

        Assert.Contains("inválid", corpo.GetProperty("erro").GetString());
    }

    [Fact]
    public async Task Imagem_so_aparece_com_credito()
    {
        var lista = await ObterAsync("/api/equipamentos");
        var itens = lista.GetProperty("itens").EnumerateArray().ToDictionary(i => i.GetProperty("slug").GetString()!);

        Assert.Equal("Foto: Fulano, CC BY-SA", itens["a-10-teste"].GetProperty("imagem").GetProperty("credito").GetString());
        Assert.Equal(JsonValueKind.Null, itens["porta-avioes-teste"].GetProperty("imagem").ValueKind);
        Assert.Equal(JsonValueKind.Null, itens["tanque-teste"].GetProperty("imagem").ValueKind);
    }

    // ---------------------------------------------------------------- ficha
    [Fact]
    public async Task Ficha_de_aeronave_traz_campos_comuns_operadores_e_fontes()
    {
        var ficha = await ObterAsync("/api/equipamentos/a-10-teste");

        Assert.Equal("A-10 Teste", ficha.GetProperty("nome").GetString());
        Assert.Equal("AEREA", ficha.GetProperty("forca").GetString());
        Assert.Equal("AERONAVE", ficha.GetProperty("categoria").GetProperty("codigo").GetString());
        Assert.Equal(716, ficha.GetProperty("unidadesProduzidas").GetInt32());
        Assert.Equal("USA", ficha.GetProperty("operadores")[0].GetProperty("codigo").GetString());
        var fonte = ficha.GetProperty("fontes")[0];
        Assert.Equal("Livro A", fonte.GetProperty("titulo").GetString());
        Assert.False(fonte.GetProperty("provisoria").GetBoolean());
        Assert.False(ficha.GetProperty("somenteFontesProvisorias").GetBoolean());
        Assert.Equal(0, ficha.GetProperty("eventos").GetArrayLength());
    }

    private static JsonElement Medida(JsonElement ficha, string chave) =>
        ficha.GetProperty("especificacoes").GetProperty("medidas").EnumerateArray()
            .Single(m => m.GetProperty("chave").GetString() == chave);

    private static JsonElement Campo(JsonElement ficha, string chave) =>
        ficha.GetProperty("especificacoes").GetProperty("campos").EnumerateArray()
            .Single(m => m.GetProperty("chave").GetString() == chave);

    [Fact]
    public async Task Ficha_de_aeronave_mostra_km_h_e_mph_e_o_valor_guardado_continua_em_km_h()
    {
        var ficha = await ObterAsync("/api/equipamentos/a-10-teste");

        var velocidade = Medida(ficha, "velocidade_max_kmh");
        Assert.Equal(706, velocidade.GetProperty("valor").GetDouble());
        Assert.Equal("km/h", velocidade.GetProperty("unidade").GetString());
        var convertida = velocidade.GetProperty("convertidos")[0];
        Assert.Equal("mph", convertida.GetProperty("unidade").GetString());
        Assert.Equal(438.7, convertida.GetProperty("valor").GetDouble());

        using var escopo = fabrica.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<PoderBelicoContext>();
        var guardado = db.Equipamentos.Single(e => e.Slug == "a-10-teste").Especificacoes;
        Assert.Contains("\"velocidade_max_kmh\":706", guardado);
    }

    [Fact]
    public async Task Ficha_de_aeronave_traz_envergadura_peso_motores_armamento_e_blindagem()
    {
        var ficha = await ObterAsync("/api/equipamentos/a-10-teste");

        Assert.Equal("ft", Medida(ficha, "envergadura_m").GetProperty("convertidos")[0].GetProperty("unidade").GetString());
        Assert.Equal(23000, Medida(ficha, "peso_max_decolagem_kg").GetProperty("valor").GetDouble());
        Assert.Equal("kN", Medida(ficha, "empuxo_lbf").GetProperty("convertidos")[0].GetProperty("unidade").GetString());
        Assert.Equal("10/05/1972", Campo(ficha, "data_primeiro_voo").GetProperty("valor").GetString());
        Assert.Equal("Ataque ao solo", Campo(ficha, "subtipo").GetProperty("valor").GetString());
        Assert.Equal("Titânio", Campo(ficha, "blindagem").GetProperty("valor").GetString());
        Assert.Contains("GAU-8", Campo(ficha, "armamento").GetProperty("itens")[0].GetString());
    }

    [Fact]
    public async Task Ficha_de_navio_mostra_a_velocidade_em_nos()
    {
        var ficha = await ObterAsync("/api/equipamentos/porta-avioes-teste");

        var convertida = Medida(ficha, "velocidade_max_kmh").GetProperty("convertidos")[0];
        Assert.Equal("nós", convertida.GetProperty("unidade").GetString());
        Assert.Equal(30.2, convertida.GetProperty("valor").GetDouble());
        Assert.True(ficha.GetProperty("somenteFontesProvisorias").GetBoolean());
    }

    [Fact]
    public async Task Ficha_de_tanque_traz_armamento_principal_e_torre_tripulada()
    {
        var ficha = await ObterAsync("/api/equipamentos/tanque-teste");

        Assert.Contains("125 mm", Campo(ficha, "armamento_principal").GetProperty("valor").GetString());
        Assert.Equal("Sim", Campo(ficha, "torre_tripulada").GetProperty("valor").GetString());
        Assert.Equal("46000", Medida(ficha, "peso_kg").GetProperty("valor").GetRawText());
    }

    [Fact]
    public async Task Slug_inexistente_devolve_404()
    {
        var corpo = await ObterAsync("/api/equipamentos/nao-existe", HttpStatusCode.NotFound);

        Assert.Contains("nao-existe", corpo.GetProperty("erro").GetString());
    }

    [Fact]
    public async Task Ficha_sem_credito_na_imagem_nao_expoe_a_imagem()
    {
        var ficha = await ObterAsync("/api/equipamentos/porta-avioes-teste");

        Assert.Equal(JsonValueKind.Null, ficha.GetProperty("imagem").ValueKind);
    }

    // -------------------------------------------------------------- filtros
    [Fact]
    public async Task Filtros_listam_forcas_categorias_e_paises_com_equipamento()
    {
        var filtros = await ObterAsync("/api/filtros");

        Assert.Equal(3, filtros.GetProperty("forcas").GetArrayLength());
        Assert.Equal(3, filtros.GetProperty("categorias").GetArrayLength());
        var paises = filtros.GetProperty("paises").EnumerateArray().Select(p => p.GetProperty("codigo").GetString()).ToArray();
        Assert.Equal(new[] { "RUS", "USA" }.OrderBy(x => x), paises.OrderBy(x => x));
    }
}
