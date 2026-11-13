using Microsoft.EntityFrameworkCore;
using PoderBelico.Dados;
using PoderBelico.Importador;

namespace PoderBelico.Api.Tests;

public class ImportadorTests
{
    // ------------------------------------------------------------ utilitários
    internal static async Task<BancoEmMemoria> NovoBancoAsync()
    {
        var banco = new BancoEmMemoria();
        await SementeReferencia.AplicarAsync(banco.Contexto, BancoEmMemoria.CaminhoPaises);
        return banco;
    }

    internal const string FonteDefinitiva =
        """{ "slug": "livro-x", "tipo": "LIVRO", "titulo": "Livro X", "autor": "Autor", "ano": 2020 }""";

    internal const string FonteProvisoria =
        """{ "slug": "ia-x", "tipo": "SITE", "titulo": "Conversa com IA", "provisoria": true }""";

    internal static string Tanque(
        string slug,
        string pais = "RUS",
        string confiabilidade = "\"ESTIMADO\"",
        string fontes = FonteDefinitiva,
        string especificacoes = """{ "subtipo": "tanque_de_batalha", "peso_kg": 46000, "tripulacao": 3 }""",
        string extras = "")
    {
        var linhaConfiabilidade = confiabilidade.Length == 0 ? "" : "\"confiabilidade\": " + confiabilidade + ",";
        return $$"""
            {
              "slug": "{{slug}}",
              "nome": "Tanque {{slug}}",
              "pais": "{{pais}}",
              "forca": "EXERCITO",
              "categoria": "TANQUE",
              "status": "ATIVO",
              {{linhaConfiabilidade}}
              {{extras}}
              "operadores": ["{{pais}}"],
              "especificacoes": {{especificacoes}},
              "fontes": [ {{fontes}} ]
            }
            """;
    }

    internal static Arquivo Json(string conteudo, string nome = "teste.json") => new(nome, conteudo);

    internal static Task<Relatorio> ImportarAsync(
        BancoEmMemoria banco, Opcoes? opcoes = null, params string[] fichas) =>
        new ServicoImportacao(banco.Contexto).ExecutarAsync(
            fichas.Select((f, i) => Json(f, $"arq{i}.json")).ToList(), opcoes ?? new Opcoes());

    internal static ItemRelatorio Item(Relatorio r, string slug) =>
        r.Itens.Single(i => i.Tipo == "equipamento" && i.Slug == slug);

    // ----------------------------------------------------- formato e idempotência
    [Fact]
    public async Task Importa_ficha_valida()
    {
        using var banco = await NovoBancoAsync();

        var rel = await ImportarAsync(banco, null, Tanque("t-90"));

        Assert.Equal(0, rel.CodigoSaida);
        Assert.Equal(Resultado.Criado, Item(rel, "t-90").Resultado);
        Assert.Equal(1, await banco.Contexto.Equipamentos.CountAsync());
        Assert.Equal(1, await banco.Contexto.Fontes.CountAsync());
        Assert.Equal(1, await banco.Contexto.EquipamentoOperadores.CountAsync());
    }

    [Fact]
    public async Task Reimportar_o_mesmo_arquivo_nao_duplica()
    {
        using var banco = await NovoBancoAsync();
        var ficha = Tanque("t-90");

        await ImportarAsync(banco, null, ficha);
        var segunda = await ImportarAsync(banco, null, ficha);

        Assert.Equal(1, await banco.Contexto.Equipamentos.CountAsync());
        Assert.Equal(1, await banco.Contexto.Fontes.CountAsync());
        Assert.Equal(Resultado.Ignorado, Item(segunda, "t-90").Resultado);
        Assert.Equal(0, segunda.Criados + segunda.Atualizados);
    }

    [Fact]
    public async Task Slug_existente_com_valores_diferentes_atualiza()
    {
        using var banco = await NovoBancoAsync();
        await ImportarAsync(banco, null, Tanque("t-90"));

        var rel = await ImportarAsync(banco, null,
            Tanque("t-90", especificacoes: """{ "subtipo": "tanque_de_batalha", "peso_kg": 48000, "tripulacao": 3 }"""));

        Assert.Equal(Resultado.Atualizado, Item(rel, "t-90").Resultado);
        var e = await banco.Contexto.Equipamentos.SingleAsync();
        Assert.Contains("48000", e.Especificacoes);
    }

    [Fact]
    public async Task Campo_omitido_na_reimportacao_fica_vazio()
    {
        using var banco = await NovoBancoAsync();
        await ImportarAsync(banco, null, Tanque("t-90", extras: "\"curiosidade\": \"Tem curiosidade\","));
        Assert.Equal("Tem curiosidade", (await banco.Contexto.Equipamentos.SingleAsync()).Curiosidade);

        await ImportarAsync(banco, null, Tanque("t-90"));

        Assert.Null((await banco.Contexto.Equipamentos.SingleAsync()).Curiosidade);
    }

    [Fact]
    public async Task Registro_ausente_do_arquivo_permanece()
    {
        using var banco = await NovoBancoAsync();
        await ImportarAsync(banco, null, Tanque("t-90"), Tanque("t-72"));

        await ImportarAsync(banco, null, Tanque("t-90"));

        Assert.Equal(2, await banco.Contexto.Equipamentos.CountAsync());
    }

    [Fact]
    public async Task Arquivo_csv_e_rejeitado_com_mensagem()
    {
        using var banco = await NovoBancoAsync();

        var rel = await new ServicoImportacao(banco.Contexto).ExecutarAsync(
            [new Arquivo("fichas.csv", "slug,nome\nt-90,T-90")], new Opcoes());

        Assert.Equal(1, rel.CodigoSaida);
        Assert.Contains("só JSON", rel.Itens.Single().Erros.Single());
    }

    // ------------------------------------------------------------- validação
    [Fact]
    public async Task Pais_inexistente_rejeita_e_aponta_campo_e_slug()
    {
        using var banco = await NovoBancoAsync();

        var rel = await ImportarAsync(banco, null, Tanque("t-90", pais: "XXX"));

        var item = Item(rel, "t-90");
        Assert.Equal(Resultado.Rejeitado, item.Resultado);
        Assert.Contains(item.Erros, e => e.StartsWith("pais:") && e.Contains("XXX"));
        Assert.Equal(0, await banco.Contexto.Equipamentos.CountAsync());
    }

    [Fact]
    public async Task Campo_desconhecido_na_especificacao_rejeita()
    {
        using var banco = await NovoBancoAsync();
        var esp = """{ "subtipo": "tanque_de_batalha", "peso_kg": 46000, "tripulacao": 3, "envergadura_m": 17.5 }""";

        var rel = await ImportarAsync(banco, null, Tanque("t-90", especificacoes: esp));

        Assert.Contains(Item(rel, "t-90").Erros, e => e.Contains("envergadura_m") && e.Contains("desconhecido"));
    }

    [Fact]
    public async Task Valor_como_texto_na_especificacao_rejeita_e_indica_o_esperado()
    {
        using var banco = await NovoBancoAsync();
        var esp = """{ "subtipo": "tanque_de_batalha", "peso_kg": "46 t", "tripulacao": 3 }""";

        var rel = await ImportarAsync(banco, null, Tanque("t-90", especificacoes: esp));

        Assert.Contains(Item(rel, "t-90").Erros, e => e.Contains("peso_kg") && e.Contains("esperado"));
    }

    [Fact]
    public async Task Ficha_sem_fonte_e_rejeitada()
    {
        using var banco = await NovoBancoAsync();

        var rel = await ImportarAsync(banco, null, Tanque("t-90", fontes: ""));

        Assert.Contains(Item(rel, "t-90").Erros, e => e.StartsWith("fontes:"));
        Assert.Equal(0, await banco.Contexto.Equipamentos.CountAsync());
    }

    [Fact]
    public async Task Ficha_sem_confiabilidade_e_rejeitada()
    {
        using var banco = await NovoBancoAsync();

        var rel = await ImportarAsync(banco, null, Tanque("t-90", confiabilidade: ""));

        Assert.Contains(Item(rel, "t-90").Erros, e => e.StartsWith("confiabilidade:"));
    }

    // ------------------------------------------------- fonte provisória e rebaixamento
    [Fact]
    public async Task So_fonte_provisoria_rebaixa_confirmado_para_estimado()
    {
        using var banco = await NovoBancoAsync();

        var rel = await ImportarAsync(banco, null, Tanque("t-90", confiabilidade: "\"CONFIRMADO\"", fontes: FonteProvisoria));

        Assert.Equal(Resultado.Criado, Item(rel, "t-90").Resultado);
        Assert.Contains(Item(rel, "t-90").Avisos, a => a.Contains("rebaixada"));
        Assert.Equal(Confiabilidade.Estimado, (await banco.Contexto.Equipamentos.SingleAsync()).Confiabilidade);
    }

    [Fact]
    public async Task Fonte_definitiva_mantem_confirmado()
    {
        using var banco = await NovoBancoAsync();

        var rel = await ImportarAsync(banco, null,
            Tanque("t-90", confiabilidade: "\"CONFIRMADO\"", fontes: FonteProvisoria + "," + FonteDefinitiva));

        Assert.Empty(Item(rel, "t-90").Avisos);
        Assert.Equal(Confiabilidade.Confirmado, (await banco.Contexto.Equipamentos.SingleAsync()).Confiabilidade);
    }

    // ------------------------------------------------ gravação por registro e cascata
    [Fact]
    public async Task Erros_parciais_gravam_os_validos_e_o_codigo_de_saida_e_diferente_de_zero()
    {
        using var banco = await NovoBancoAsync();

        var rel = await ImportarAsync(banco, null, Tanque("t-90"), Tanque("t-ruim", pais: "XXX"), Tanque("t-72"));

        Assert.Equal(2, await banco.Contexto.Equipamentos.CountAsync());
        Assert.Equal(1, rel.Rejeitados);
        Assert.Equal(1, rel.CodigoSaida);
    }

    [Fact]
    public async Task Fonte_rejeitada_rejeita_a_ficha_que_depende_dela()
    {
        using var banco = await NovoBancoAsync();
        var fonteInvalida = """{ "slug": "livro-quebrado", "tipo": "GIBI", "titulo": "Gibi" }""";
        var ficha = Tanque("t-90", fontes: "\"livro-quebrado\"");

        var rel = await ImportarAsync(banco, null, fonteInvalida, ficha);

        Assert.Equal(Resultado.Rejeitado, rel.Itens.Single(i => i.Tipo == "fonte").Resultado);
        Assert.Contains(Item(rel, "t-90").Erros, e => e.Contains("dependência rejeitada"));
        Assert.Equal(0, await banco.Contexto.Equipamentos.CountAsync());
    }

    [Fact]
    public async Task Ficha_pode_referenciar_fonte_ja_gravada_pelo_slug()
    {
        using var banco = await NovoBancoAsync();
        await ImportarAsync(banco, null, Tanque("t-90"));

        var rel = await ImportarAsync(banco, null, Tanque("t-72", fontes: "\"livro-x\""));

        Assert.Equal(Resultado.Criado, Item(rel, "t-72").Resultado);
        Assert.Equal(1, await banco.Contexto.Fontes.CountAsync());
    }

    // --------------------------------------------------------------- opções
    [Fact]
    public async Task Estrito_com_um_invalido_nao_grava_nada()
    {
        using var banco = await NovoBancoAsync();

        var rel = await ImportarAsync(banco, new Opcoes(Estrito: true),
            Tanque("t-90"), Tanque("t-ruim", pais: "XXX"));

        Assert.True(rel.DesfeitoPorEstrito);
        Assert.Equal(1, rel.CodigoSaida);
        Assert.Equal(0, await banco.Contexto.Equipamentos.CountAsync());
        Assert.Equal(0, await banco.Contexto.Fontes.CountAsync());
        Assert.Equal(Resultado.Revertido, Item(rel, "t-90").Resultado);
    }

    [Fact]
    public async Task Simular_relata_o_que_seria_criado_sem_gravar()
    {
        using var banco = await NovoBancoAsync();

        var rel = await ImportarAsync(banco, new Opcoes(Simular: true), Tanque("t-90"));

        Assert.Equal(Resultado.Criado, Item(rel, "t-90").Resultado);
        Assert.Equal(0, await banco.Contexto.Equipamentos.CountAsync());
        Assert.Equal(0, await banco.Contexto.Fontes.CountAsync());
    }

    [Fact]
    public async Task Falha_de_infraestrutura_desfaz_toda_a_execucao()
    {
        using var banco = await NovoBancoAsync();
        // Sem esta tabela a gravação do equipamento falha por motivo que não é de validação.
        await banco.Contexto.Database.ExecuteSqlRawAsync("DROP TABLE equipamento_fonte");

        await Assert.ThrowsAnyAsync<Exception>(() => ImportarAsync(banco, null, Tanque("t-90")));

        Assert.Equal(0, await banco.Contexto.Fontes.CountAsync());
        Assert.Equal(0, await banco.Contexto.Equipamentos.CountAsync());
    }

    // ----------------------------------------------------------------- busca
    [Fact]
    public async Task Busca_encontra_o_equipamento_ignorando_acentos()
    {
        using var banco = await NovoBancoAsync();
        var ficha = Tanque("fw-190-wurger").Replace("Tanque fw-190-wurger", "Fw 190 Würger");

        await ImportarAsync(banco, null, ficha);

        var slugs = await banco.Contexto.Database
            .SqlQueryRaw<string>("SELECT slug AS Value FROM busca WHERE busca MATCH 'wurger'")
            .ToListAsync();
        Assert.Equal(new[] { "fw-190-wurger" }, slugs);
    }

    // ------------------------------------------------------- fichas de exemplo
    [Fact]
    public async Task Importa_as_fichas_de_exemplo_e_rebaixa_a_confirmada_com_fonte_provisoria()
    {
        using var banco = await NovoBancoAsync();
        var pasta = Path.Combine(AppContext.BaseDirectory, "exemplos");
        var arquivos = Directory.GetFiles(pasta, "*.json")
            .Select(f => new Arquivo(f, File.ReadAllText(f))).ToList();

        var rel = await new ServicoImportacao(banco.Contexto).ExecutarAsync(arquivos, new Opcoes());

        Assert.Equal(0, rel.CodigoSaida);
        Assert.Equal(2, await banco.Contexto.Equipamentos.CountAsync());
        Assert.Equal(1, await banco.Contexto.Fontes.CountAsync());
        Assert.All(await banco.Contexto.Equipamentos.ToListAsync(),
            e => Assert.Equal(Confiabilidade.Estimado, e.Confiabilidade));
        Assert.Contains(Item(rel, "a-10-thunderbolt-ii").Avisos, a => a.Contains("rebaixada"));
    }
}
