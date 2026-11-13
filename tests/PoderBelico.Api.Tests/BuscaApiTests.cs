using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PoderBelico.Api.Catalogo;
using PoderBelico.Dados;
using PoderBelico.Importador;
using static PoderBelico.Api.Tests.ImportadorTests;

namespace PoderBelico.Api.Tests;

public class BuscaApiTests(FabricaApi fabrica) : IClassFixture<FabricaApi>, IAsyncLifetime
{
    private static readonly string Jutlandia = $$"""
        {
          "slug": "batalha-da-jutlandia", "nome": "Batalha da Jutlândia", "tipo": "BATALHA",
          "data_inicio": "1916-05-31", "data_fim": "1916-06-01",
          "local": "Mar do Norte", "descricao": "Maior batalha naval da Primeira Guerra, com encouraçados.",
          "confiabilidade": "ESTIMADO",
          "beligerantes": [ { "pais": "GBR", "lado": "Aliados" }, { "pais": "DEU", "lado": "Potências Centrais" } ],
          "fontes": [ {{FonteDefinitiva}} ]
        }
        """;

    private static readonly string Kursk = $$"""
        {
          "slug": "batalha-de-kursk", "nome": "Batalha de Kursk", "tipo": "BATALHA",
          "data_inicio": "1943-07-05", "data_fim": "1943-08-23",
          "descricao": "A maior batalha de blindados da história.",
          "confiabilidade": "ESTIMADO",
          "beligerantes": [ { "pais": "RUS", "lado": "Soviéticos" }, { "pais": "DEU", "lado": "Eixo" } ],
          "fontes": [ "livro-x" ]
        }
        """;

    public async Task InitializeAsync()
    {
        using var escopo = fabrica.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<PoderBelicoContext>();
        await db.Database.EnsureCreatedAsync();
        await SementeReferencia.AplicarAsync(db, BancoEmMemoria.CaminhoPaises);

        var wurger = Tanque("fw-190-wurger", pais: "DEU")
            .Replace("Tanque fw-190-wurger", "Fw 190 Würger");
        var pesado = Tanque("tanque-pesado", extras: "\"descricao_curta\": \"Blindado pesado de teste\",");

        var rel = await new ServicoImportacao(db).ExecutarAsync(
            [
                new Arquivo("a.json", wurger), new Arquivo("b.json", pesado),
                new Arquivo("c.json", Jutlandia), new Arquivo("d.json", Kursk),
            ],
            new Opcoes());
        Assert.True(rel.CodigoSaida == 0, rel.EmTexto());
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<JsonElement> BuscarAsync(string q, HttpStatusCode esperado = HttpStatusCode.OK)
    {
        var url = "/api/busca?q=" + Uri.EscapeDataString(q);
        var resposta = await fabrica.CreateClient().GetAsync(url);
        Assert.Equal(esperado, resposta.StatusCode);
        return await resposta.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static string[] Slugs(JsonElement resultado, string grupo) =>
        resultado.GetProperty(grupo).EnumerateArray().Select(i => i.GetProperty("slug").GetString()!).ToArray();

    // ----------------------------------------------------- consulta (unidade)
    [Theory]
    [InlineData("jutlandia", "\"jutlandia\"*")]
    [InlineData("  Fw 190  ", "\"Fw\"* \"190\"*")]
    [InlineData("\"abc\" (def) * OR NOT", "\"abc\"* \"def\"* \"OR\"* \"NOT\"*")]
    public void Monta_a_consulta_sem_deixar_o_texto_virar_sintaxe(string texto, string esperada)
    {
        Assert.Equal(esperada, BuscaEndpoints.MontarConsulta(texto));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\"*()")]
    public void Texto_sem_letras_nem_numeros_nao_gera_consulta(string? texto)
    {
        Assert.Null(BuscaEndpoints.MontarConsulta(texto));
    }

    // ------------------------------------------------------------- endpoint
    [Fact]
    public async Task Busca_sem_acento_encontra_a_batalha_da_jutlandia()
    {
        var r = await BuscarAsync("jutlandia");

        Assert.Equal(new[] { "batalha-da-jutlandia" }, Slugs(r, "eventos"));
        Assert.Empty(Slugs(r, "equipamentos"));
        Assert.Equal(1, r.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Busca_ignora_maiusculas_e_acentos_nos_equipamentos()
    {
        Assert.Equal(new[] { "fw-190-wurger" }, Slugs(await BuscarAsync("WURGER"), "equipamentos"));
        Assert.Equal(new[] { "fw-190-wurger" }, Slugs(await BuscarAsync("würger"), "equipamentos"));
    }

    [Fact]
    public async Task Busca_por_prefixo_e_por_varias_palavras()
    {
        Assert.Equal(new[] { "batalha-da-jutlandia" }, Slugs(await BuscarAsync("jutl"), "eventos"));
        Assert.Equal(new[] { "batalha-da-jutlandia" }, Slugs(await BuscarAsync("batalha naval"), "eventos"));
    }

    [Fact]
    public async Task Busca_olha_a_descricao_e_agrupa_por_tipo()
    {
        var r = await BuscarAsync("blindad");

        Assert.Equal(new[] { "tanque-pesado" }, Slugs(r, "equipamentos"));
        Assert.Equal(new[] { "batalha-de-kursk" }, Slugs(r, "eventos"));
        Assert.Equal(2, r.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Resultado_traz_os_dados_do_resumo()
    {
        var r = await BuscarAsync("jutlandia");

        var evento = r.GetProperty("eventos")[0];
        Assert.Equal("BATALHA", evento.GetProperty("tipo").GetString());
        Assert.Equal("1916-05-31", evento.GetProperty("dataInicio").GetString());
        var r2 = await BuscarAsync("wurger");
        var equipamento = r2.GetProperty("equipamentos")[0];
        Assert.Equal("DEU", equipamento.GetProperty("pais").GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task Sem_resultados_devolve_listas_vazias()
    {
        var r = await BuscarAsync("zzzzzz");

        Assert.Equal(0, r.GetProperty("total").GetInt32());
        Assert.Empty(Slugs(r, "equipamentos"));
        Assert.Empty(Slugs(r, "eventos"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\"*()")]
    public async Task Termo_vazio_devolve_400(string q)
    {
        var corpo = await BuscarAsync(q, HttpStatusCode.BadRequest);

        Assert.Contains("termo", corpo.GetProperty("erro").GetString());
    }

    [Theory]
    [InlineData("\"jutl")]
    [InlineData("jutlandia AND (")]
    [InlineData("jutl* OR NOT")]
    [InlineData("nome: batalha")]
    public async Task Caracteres_especiais_nao_quebram_a_busca(string q)
    {
        var resposta = await fabrica.CreateClient().GetAsync("/api/busca?q=" + Uri.EscapeDataString(q));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }
}
