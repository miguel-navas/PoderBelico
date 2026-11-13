using System.Text.Json;
using PoderBelico.Dados;

namespace PoderBelico.Api.Tests;

public class EspecificacoesValidadorTests
{
    private static string Especificacoes(string arquivo)
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "exemplos", arquivo);
        using var doc = JsonDocument.Parse(File.ReadAllText(caminho));
        return doc.RootElement.GetProperty("especificacoes").GetRawText();
    }

    [Fact]
    public async Task Ficha_do_t14_e_valida()
    {
        var erros = await EspecificacoesValidador.ValidarAsync("TANQUE", Especificacoes("t-14-armata.json"));

        Assert.Empty(erros);
    }

    [Fact]
    public async Task Ficha_do_a10_e_valida()
    {
        var erros = await EspecificacoesValidador.ValidarAsync("AERONAVE", Especificacoes("a-10-thunderbolt-ii.json"));

        Assert.Empty(erros);
    }

    [Fact]
    public async Task Campo_de_aeronave_em_tanque_e_rejeitado()
    {
        const string json = """{ "subtipo": "tanque_de_batalha", "peso_kg": 55000, "tripulacao": 3, "envergadura_m": 17.5 }""";

        var erros = await EspecificacoesValidador.ValidarAsync("TANQUE", json);

        Assert.Contains(erros, e => e.Contains("envergadura_m") || e.Contains("NoAdditionalPropertiesAllowed"));
    }

    [Fact]
    public async Task Velocidade_como_texto_e_rejeitada()
    {
        const string json = """{ "subtipo": "ataque_ao_solo", "tripulacao": 1, "velocidade_max_kmh": "706 km/h" }""";

        var erros = await EspecificacoesValidador.ValidarAsync("AERONAVE", json);

        Assert.NotEmpty(erros);
    }

    [Fact]
    public async Task Subtipo_fora_da_lista_e_rejeitado()
    {
        const string json = """{ "subtipo": "disco_voador", "peso_kg": 100, "tripulacao": 1 }""";

        var erros = await EspecificacoesValidador.ValidarAsync("TANQUE", json);

        Assert.NotEmpty(erros);
    }

    [Fact]
    public async Task Campo_obrigatorio_ausente_e_rejeitado()
    {
        const string json = """{ "subtipo": "tanque_leve" }""";

        var erros = await EspecificacoesValidador.ValidarAsync("TANQUE", json);

        Assert.NotEmpty(erros);
    }

    [Fact]
    public async Task Categoria_sem_esquema_gera_erro()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            EspecificacoesValidador.ValidarAsync("FOGUETE", "{}"));
    }

    [Fact]
    public async Task Aeronave_nao_tripulada_nao_precisa_de_tripulacao()
    {
        const string json = """{ "subtipo": "nao_tripulada", "envergadura_m": 20.1, "velocidade_max_kmh": 445 }""";

        Assert.Empty(await EspecificacoesValidador.ValidarAsync("AERONAVE", json));
    }

    [Fact]
    public async Task Aeronave_tripulada_continua_exigindo_tripulacao()
    {
        const string json = """{ "subtipo": "caca", "velocidade_max_kmh": 1200 }""";

        Assert.NotEmpty(await EspecificacoesValidador.ValidarAsync("AERONAVE", json));
    }
}
