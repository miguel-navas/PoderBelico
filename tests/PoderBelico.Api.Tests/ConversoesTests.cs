using PoderBelico.Api.Catalogo;

namespace PoderBelico.Api.Tests;

public class ConversoesTests
{
    [Theory]
    [InlineData("velocidade_max_kmh", "km/h")]
    [InlineData("peso_kg", "kg")]
    [InlineData("alcance_km", "km")]
    [InlineData("envergadura_m", "m")]
    [InlineData("deslocamento_t", "t")]
    [InlineData("calibre_mm", "mm")]
    [InlineData("potencia_hp", "hp")]
    [InlineData("empuxo_lbf", "lbf")]
    public void Reconhece_a_unidade_pelo_sufixo(string chave, string esperada)
    {
        Assert.True(Conversoes.TentarUnidade(chave, out var unidade));
        Assert.Equal(esperada, unidade);
    }

    [Fact]
    public void Campo_sem_sufixo_de_unidade_nao_e_medida()
    {
        Assert.False(Conversoes.TentarUnidade("tripulacao", out _));
        Assert.False(Conversoes.TentarUnidade("subtipo", out _));
    }

    [Fact]
    public void Velocidade_de_aeronave_vira_mph()
    {
        var c = Assert.Single(Conversoes.Converter("km/h", 706, navio: false));
        Assert.Equal("mph", c.Unidade);
        Assert.Equal(438.7, c.Valor);
    }

    [Fact]
    public void Velocidade_de_navio_vira_nos()
    {
        var c = Assert.Single(Conversoes.Converter("km/h", 56, navio: true));
        Assert.Equal("nós", c.Unidade);
        Assert.Equal(30.2, c.Valor);
    }

    [Fact]
    public void Peso_comprimento_e_alcance_sao_convertidos()
    {
        Assert.Equal(50706.3, Conversoes.Converter("kg", 23000, false).Single().Valor);
        Assert.Equal(57.5, Conversoes.Converter("m", 17.53, false).Single().Valor);
        Assert.Equal(310.7, Conversoes.Converter("km", 500, false).Single().Valor);
        Assert.Equal(270.0, Conversoes.Converter("km", 500, true).Single().Valor);
    }

    [Fact]
    public void Unidade_sem_conversao_devolve_lista_vazia()
    {
        Assert.Empty(Conversoes.Converter("t", 100000, false));
    }
}
