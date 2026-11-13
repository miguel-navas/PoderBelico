using Microsoft.EntityFrameworkCore;
using PoderBelico.Dados;

namespace PoderBelico.Api.Tests;

public class SementeTests
{
    [Fact]
    public async Task Modelo_cria_forcas_e_categorias_iniciais()
    {
        using var banco = new BancoEmMemoria();

        var forcas = await banco.Contexto.Forcas.OrderBy(f => f.Id).Select(f => f.Nome).ToListAsync();
        var categorias = await banco.Contexto.Categorias.OrderBy(c => c.Id).Select(c => c.Codigo).ToListAsync();

        Assert.Equal(new[] { "Marinha", "Força Aérea", "Exército" }, forcas);
        Assert.Equal(new[] { "NAVIO", "AERONAVE", "TANQUE" }, categorias);
    }

    [Fact]
    public async Task Semente_carrega_os_paises_do_mvp()
    {
        using var banco = new BancoEmMemoria();

        await SementeReferencia.AplicarAsync(banco.Contexto, BancoEmMemoria.CaminhoPaises);

        var paises = await banco.Contexto.Paises.ToListAsync();
        Assert.Equal(6, paises.Count);
        var urss = Assert.Single(paises, p => p.CodigoIso == "SUN");
        Assert.Equal(1922, urss.AnoInicio);
        Assert.Equal(1991, urss.AnoFim);
        Assert.Null(paises.Single(p => p.CodigoIso == "RUS").AnoFim);
    }

    [Fact]
    public async Task Semente_e_idempotente()
    {
        using var banco = new BancoEmMemoria();

        await SementeReferencia.AplicarAsync(banco.Contexto, BancoEmMemoria.CaminhoPaises);
        await SementeReferencia.AplicarAsync(banco.Contexto, BancoEmMemoria.CaminhoPaises);

        Assert.Equal(6, await banco.Contexto.Paises.CountAsync());
    }

    [Fact]
    public async Task Busca_ignora_acentos()
    {
        using var banco = new BancoEmMemoria();
        await SementeReferencia.AplicarAsync(banco.Contexto, BancoEmMemoria.CaminhoPaises);

        await banco.Contexto.Database.ExecuteSqlRawAsync(
            "INSERT INTO busca (tipo, slug, nome, descricao) VALUES ('evento', 'jutlandia', 'Batalha da Jutlândia', 'Batalha naval de 1916')");

        var slugs = await banco.Contexto.Database
            .SqlQueryRaw<string>("SELECT slug AS Value FROM busca WHERE busca MATCH 'jutlandia'")
            .ToListAsync();

        Assert.Equal(new[] { "jutlandia" }, slugs);
    }

    [Fact]
    public async Task Especificacoes_invalidas_sao_rejeitadas_pelo_banco()
    {
        using var banco = new BancoEmMemoria();
        await SementeReferencia.AplicarAsync(banco.Contexto, BancoEmMemoria.CaminhoPaises);
        var pais = await banco.Contexto.Paises.FirstAsync(p => p.CodigoIso == "RUS");

        banco.Contexto.Equipamentos.Add(new Equipamento
        {
            Slug = "teste",
            Nome = "Teste",
            Confiabilidade = Confiabilidade.Estimado,
            PaisId = pais.Id,
            ForcaId = 3,
            CategoriaId = 3,
            Especificacoes = "isto não é json",
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => banco.Contexto.SaveChangesAsync());
    }
}
