using System.Net;
using System.Net.Http.Json;

namespace PoderBelico.Api.Tests;

public class SaudeTests(FabricaApi fabrica) : IClassFixture<FabricaApi>
{
    private record Saude(string Status, bool Banco);

    [Fact]
    public async Task Saude_retorna_ok_e_banco_acessivel()
    {
        var cliente = fabrica.CreateClient();

        var resposta = await cliente.GetAsync("/api/saude");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<Saude>();
        Assert.Equal("ok", corpo?.Status);
        Assert.True(corpo?.Banco);
    }

    [Fact]
    public async Task Rota_api_desconhecida_retorna_404()
    {
        var cliente = fabrica.CreateClient();

        var resposta = await cliente.GetAsync("/api/nao-existe");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
}
