using Microsoft.EntityFrameworkCore;
using PoderBelico.Dados;
using PoderBelico.Importador;
using static PoderBelico.Api.Tests.ImportadorTests;

namespace PoderBelico.Api.Tests;

public class ImportadorEventosTests
{
    private static string Evento(
        string slug,
        string? pai = null,
        string inicio = "1939-09-01",
        string? fim = "1945-09-02",
        string confiabilidade = "\"ESTIMADO\"",
        string beligerantes = """[ { "pais": "RUS", "lado": "A" }, { "pais": "DEU", "lado": "B" } ]""",
        string fontes = FonteDefinitiva,
        string extras = "")
    {
        var linhaPai = pai is null ? "" : $"\"evento_pai\": \"{pai}\",";
        var linhaFim = fim is null ? "" : $"\"data_fim\": \"{fim}\",";
        var linhaConf = confiabilidade.Length == 0 ? "" : "\"confiabilidade\": " + confiabilidade + ",";
        return $$"""
            {
              "slug": "{{slug}}",
              "nome": "Evento {{slug}}",
              "tipo": "BATALHA",
              {{linhaPai}}
              "data_inicio": "{{inicio}}",
              {{linhaFim}}
              {{linhaConf}}
              {{extras}}
              "beligerantes": {{beligerantes}},
              "fontes": [ {{fontes}} ]
            }
            """;
    }

    private static string Participacao(string equipamento, string evento, string pais = "RUS", string extras = "") =>
        $$"""
        { "equipamento": "{{equipamento}}", "evento": "{{evento}}", "pais": "{{pais}}", "papel": "Linha de frente"{{extras}} }
        """;

    private static ItemRelatorio ItemDe(Relatorio r, string tipo, string slug) =>
        r.Itens.Single(i => i.Tipo == tipo && i.Slug == slug);

    // ------------------------------------------------------------ eventos
    [Fact]
    public async Task Importa_evento_com_beligerantes_e_fontes()
    {
        using var banco = await NovoBancoAsync();

        var rel = await ImportarAsync(banco, null, Evento("guerra-a"));

        Assert.Equal(0, rel.CodigoSaida);
        Assert.Equal(Resultado.Criado, ItemDe(rel, "evento", "guerra-a").Resultado);
        Assert.Equal(2, await banco.Contexto.EventoPaises.CountAsync());
        Assert.Equal(1, await banco.Contexto.EventoFontes.CountAsync());
    }

    [Fact]
    public async Task Filho_antes_do_pai_no_arquivo_e_gravado_depois_dele()
    {
        using var banco = await NovoBancoAsync();

        var rel = await ImportarAsync(banco, null,
            Evento("batalha-b", pai: "guerra-a", inicio: "1942-06-04", fim: "1942-06-07"),
            Evento("guerra-a"));

        Assert.Equal(0, rel.CodigoSaida);
        var filho = await banco.Contexto.Eventos.Include(e => e.EventoPai).SingleAsync(e => e.Slug == "batalha-b");
        Assert.Equal("guerra-a", filho.EventoPai!.Slug);
    }

    [Fact]
    public async Task Pai_inexistente_rejeita_o_filho()
    {
        using var banco = await NovoBancoAsync();

        var rel = await ImportarAsync(banco, null, Evento("batalha-b", pai: "nao-existe"));

        var item = ItemDe(rel, "evento", "batalha-b");
        Assert.Equal(Resultado.Rejeitado, item.Resultado);
        Assert.Contains(item.Erros, e => e.Contains("evento_pai") && e.Contains("nao-existe"));
        Assert.Equal(0, await banco.Contexto.Eventos.CountAsync());
    }

    [Fact]
    public async Task Pai_rejeitado_gera_dependencia_rejeitada_no_filho()
    {
        using var banco = await NovoBancoAsync();

        var rel = await ImportarAsync(banco, null,
            Evento("guerra-a", confiabilidade: ""),
            Evento("batalha-b", pai: "guerra-a", inicio: "1942-06-04", fim: "1942-06-07"));

        Assert.Equal(Resultado.Rejeitado, ItemDe(rel, "evento", "guerra-a").Resultado);
        Assert.Contains(ItemDe(rel, "evento", "batalha-b").Erros, e => e.Contains("dependência rejeitada"));
        Assert.Equal(0, await banco.Contexto.Eventos.CountAsync());
    }

    [Fact]
    public async Task Referencia_circular_entre_eventos_e_rejeitada()
    {
        using var banco = await NovoBancoAsync();

        var rel = await ImportarAsync(banco, null,
            Evento("a", pai: "b"), Evento("b", pai: "a"));

        Assert.Equal(2, rel.Rejeitados);
        Assert.Equal(0, await banco.Contexto.Eventos.CountAsync());
    }

    [Fact]
    public async Task Data_fim_anterior_ao_inicio_e_rejeitada()
    {
        using var banco = await NovoBancoAsync();

        var rel = await ImportarAsync(banco, null, Evento("guerra-a", inicio: "1945-01-01", fim: "1944-01-01"));

        Assert.Equal(Resultado.Rejeitado, ItemDe(rel, "evento", "guerra-a").Resultado);
    }

    [Fact]
    public async Task Evento_sem_confiabilidade_ou_sem_fonte_e_rejeitado()
    {
        using var banco = await NovoBancoAsync();

        var rel = await ImportarAsync(banco, null,
            Evento("sem-conf", confiabilidade: ""),
            Evento("sem-fonte", fontes: ""));

        Assert.Equal(2, rel.Rejeitados);
    }

    [Fact]
    public async Task Evento_confirmado_so_com_fonte_provisoria_vira_estimado()
    {
        using var banco = await NovoBancoAsync();

        var rel = await ImportarAsync(banco, null,
            Evento("guerra-a", confiabilidade: "\"CONFIRMADO\"", fontes: FonteProvisoria));

        Assert.Contains(ItemDe(rel, "evento", "guerra-a").Avisos, a => a.Contains("rebaixada"));
        Assert.Equal(Confiabilidade.Estimado, (await banco.Contexto.Eventos.SingleAsync()).Confiabilidade);
    }

    [Fact]
    public async Task Pais_beligerante_inexistente_rejeita_o_evento()
    {
        using var banco = await NovoBancoAsync();

        var rel = await ImportarAsync(banco, null,
            Evento("guerra-a", beligerantes: """[ { "pais": "ZZZ" } ]"""));

        Assert.Equal(Resultado.Rejeitado, ItemDe(rel, "evento", "guerra-a").Resultado);
    }

    [Fact]
    public async Task Reimportar_evento_nao_muda_nada_e_alteracao_atualiza()
    {
        using var banco = await NovoBancoAsync();
        var ev = Evento("guerra-a");
        await ImportarAsync(banco, null, ev);

        var igual = await ImportarAsync(banco, null, ev);
        Assert.Equal(Resultado.Ignorado, ItemDe(igual, "evento", "guerra-a").Resultado);

        var alterado = await ImportarAsync(banco, null,
            Evento("guerra-a", beligerantes: """[ { "pais": "RUS", "lado": "A" } ]"""));
        Assert.Equal(Resultado.Atualizado, ItemDe(alterado, "evento", "guerra-a").Resultado);
        Assert.Equal(1, await banco.Contexto.EventoPaises.CountAsync());
    }

    [Fact]
    public async Task Campo_omitido_no_evento_vira_vazio()
    {
        using var banco = await NovoBancoAsync();
        await ImportarAsync(banco, null, Evento("guerra-a", extras: "\"local\": \"Europa\","));
        Assert.Equal("Europa", (await banco.Contexto.Eventos.SingleAsync()).Local);

        await ImportarAsync(banco, null, Evento("guerra-a"));

        Assert.Null((await banco.Contexto.Eventos.SingleAsync()).Local);
    }

    [Fact]
    public async Task Periodo_fora_do_pai_gera_aviso_sem_rejeitar()
    {
        using var banco = await NovoBancoAsync();

        var rel = await ImportarAsync(banco, null,
            Evento("guerra-a", inicio: "1940-01-01", fim: "1941-01-01"),
            Evento("batalha-b", pai: "guerra-a", inicio: "1950-01-01", fim: "1950-02-01"));

        var item = ItemDe(rel, "evento", "batalha-b");
        Assert.Equal(Resultado.Criado, item.Resultado);
        Assert.Contains(item.Avisos, a => a.Contains("período"));
    }

    [Fact]
    public async Task Busca_encontra_o_evento_ignorando_acentos()
    {
        using var banco = await NovoBancoAsync();
        var ev = Evento("batalha-de-kursk", extras: "\"descricao\": \"Maior batalha de blindados da história, perto de Kúrsk\",");

        await ImportarAsync(banco, null, ev);

        var slugs = await banco.Contexto.Database
            .SqlQueryRaw<string>("SELECT slug AS Value FROM busca WHERE busca MATCH 'kursk' AND tipo = 'evento'")
            .ToListAsync();
        Assert.Equal(new[] { "batalha-de-kursk" }, slugs);
    }

    // ------------------------------------------------------- participações
    [Fact]
    public async Task Importa_participacao_e_reimportar_ignora()
    {
        using var banco = await NovoBancoAsync();
        var fichas = new[] { Tanque("t-90"), Evento("guerra-a"), Participacao("t-90", "guerra-a") };

        var rel = await ImportarAsync(banco, null, fichas);
        Assert.Equal(0, rel.CodigoSaida);
        Assert.Equal(1, await banco.Contexto.Participacoes.CountAsync());

        var de_novo = await ImportarAsync(banco, null, fichas);
        Assert.All(de_novo.Itens, i => Assert.Equal(Resultado.Ignorado, i.Resultado));
    }

    [Fact]
    public async Task Participacao_alterada_atualiza()
    {
        using var banco = await NovoBancoAsync();
        await ImportarAsync(banco, null, Tanque("t-90"), Evento("guerra-a"), Participacao("t-90", "guerra-a"));

        var rel = await ImportarAsync(banco, null,
            Participacao("t-90", "guerra-a", extras: ", \"quantidade_aprox\": 50"));

        Assert.Equal(Resultado.Atualizado, rel.Itens.Single().Resultado);
        Assert.Equal(50, (await banco.Contexto.Participacoes.SingleAsync()).QuantidadeAprox);
        Assert.Equal(1, await banco.Contexto.Participacoes.CountAsync());
    }

    [Fact]
    public async Task Pais_que_nao_e_beligerante_rejeita_a_participacao()
    {
        using var banco = await NovoBancoAsync();

        var rel = await ImportarAsync(banco, null,
            Tanque("t-90", pais: "USA"), Evento("guerra-a"), Participacao("t-90", "guerra-a", pais: "USA"));

        var item = rel.Itens.Single(i => i.Tipo == "participacao");
        Assert.Equal(Resultado.Rejeitado, item.Resultado);
        Assert.Contains(item.Erros, e => e.Contains("não é beligerante"));
    }

    [Fact]
    public async Task Equipamento_ou_evento_inexistente_rejeita_a_participacao()
    {
        using var banco = await NovoBancoAsync();

        var rel = await ImportarAsync(banco, null,
            Participacao("nao-existe", "guerra-a"), Evento("guerra-a"), Participacao("t-90", "nao-existe"));

        Assert.Equal(2, rel.Itens.Count(i => i.Tipo == "participacao" && i.Resultado == Resultado.Rejeitado));
        Assert.Equal(0, await banco.Contexto.Participacoes.CountAsync());
    }

    [Fact]
    public async Task Evento_rejeitado_deixa_as_participacoes_como_dependencia_rejeitada()
    {
        using var banco = await NovoBancoAsync();

        var rel = await ImportarAsync(banco, null,
            Tanque("t-90"), Evento("guerra-a", confiabilidade: ""), Participacao("t-90", "guerra-a"));

        var item = rel.Itens.Single(i => i.Tipo == "participacao");
        Assert.Contains(item.Erros, e => e.Contains("dependência rejeitada"));
    }

    [Fact]
    public async Task Equipamento_rejeitado_deixa_as_participacoes_como_dependencia_rejeitada()
    {
        using var banco = await NovoBancoAsync();

        var rel = await ImportarAsync(banco, null,
            Tanque("t-90", confiabilidade: ""), Evento("guerra-a"), Participacao("t-90", "guerra-a"));

        var item = rel.Itens.Single(i => i.Tipo == "participacao");
        Assert.Contains(item.Erros, e => e.Contains("dependência rejeitada"));
    }

    [Fact]
    public async Task Pais_fora_dos_operadores_gera_aviso_sem_rejeitar()
    {
        using var banco = await NovoBancoAsync();

        var rel = await ImportarAsync(banco, null,
            Tanque("t-90", pais: "USA"), Evento("guerra-a"), Participacao("t-90", "guerra-a", pais: "RUS"));

        var item = rel.Itens.Single(i => i.Tipo == "participacao");
        Assert.Equal(Resultado.Criado, item.Resultado);
        Assert.Contains(item.Avisos, a => a.Contains("operadores"));
    }

    [Fact]
    public async Task Participacao_sem_papel_e_rejeitada()
    {
        using var banco = await NovoBancoAsync();

        var rel = await ImportarAsync(banco, null, Tanque("t-90"), Evento("guerra-a"),
            """{ "equipamento": "t-90", "evento": "guerra-a", "pais": "RUS" }""");

        Assert.Equal(Resultado.Rejeitado, rel.Itens.Single(i => i.Tipo == "participacao").Resultado);
    }

    [Fact]
    public async Task Modo_estrito_com_participacao_invalida_nao_grava_nada()
    {
        using var banco = await NovoBancoAsync();

        var rel = await ImportarAsync(banco, new Opcoes(Estrito: true),
            Tanque("t-90"), Evento("guerra-a"), Participacao("t-90", "nao-existe"));

        Assert.True(rel.DesfeitoPorEstrito);
        Assert.Equal(0, await banco.Contexto.Equipamentos.CountAsync());
        Assert.Equal(0, await banco.Contexto.Eventos.CountAsync());
    }

    // ------------------------------------------------- arquivos do repositório
    [Fact]
    public async Task Importa_fichas_convertidas_e_exemplos_de_eventos_sem_rejeicao()
    {
        using var banco = await NovoBancoAsync();
        await SementeReferencia.AplicarAsync(banco.Contexto,
            Path.Combine(AppContext.BaseDirectory, "dados_teste", "paises-adicionais.json"));
        var arquivos = new List<Arquivo>();
        foreach (var pasta in new[] { "importacao_aeronaves", "importacao_blindados", "importacao_navios", "exemplos", "exemplos_eventos" })
        {
            var caminho = Path.Combine(AppContext.BaseDirectory, pasta);
            if (!Directory.Exists(caminho)) continue;
            arquivos.AddRange(Directory.GetFiles(caminho, "*.json").Select(f => new Arquivo(f, File.ReadAllText(f))));
        }
        Assert.NotEmpty(arquivos);

        var rel = await new ServicoImportacao(banco.Contexto).ExecutarAsync(arquivos, new Opcoes());

        Assert.True(rel.CodigoSaida == 0, rel.EmTexto());
        Assert.Equal(1, await banco.Contexto.Eventos.CountAsync(e => e.Slug == "batalha-de-midway"));
        Assert.Equal(1, await banco.Contexto.Eventos.CountAsync(e => e.Slug == "guerra-do-golfo"));
        Assert.Equal(7, await banco.Contexto.Participacoes.CountAsync());
    }
}
