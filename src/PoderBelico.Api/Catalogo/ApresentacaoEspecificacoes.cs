using System.Globalization;
using System.Text.Json;

namespace PoderBelico.Api.Catalogo;

public sealed record MedidaDto(
    string Chave, string Rotulo, double Valor, string Unidade, IReadOnlyList<Convertido> Convertidos);

/// <summary>Campo sem unidade: texto simples (Valor) ou lista (Itens).</summary>
public sealed record CampoDto(string Chave, string Rotulo, string? Valor, IReadOnlyList<string>? Itens);

public sealed record EspecificacoesDto(IReadOnlyList<MedidaDto> Medidas, IReadOnlyList<CampoDto> Campos);

/// <summary>Transforma o JSON de especificações (unidade padrão) em campos prontos para exibir.</summary>
public static class ApresentacaoEspecificacoes
{
    private static readonly Dictionary<string, string> Rotulos = new()
    {
        ["subtipo"] = "Tipo",
        ["classe"] = "Classe",
        ["data_primeiro_voo"] = "Primeiro voo",
        ["tripulacao"] = "Tripulação",
        ["comprimento_m"] = "Comprimento",
        ["largura_m"] = "Largura",
        ["envergadura_m"] = "Envergadura",
        ["altura_m"] = "Altura",
        ["boca_m"] = "Boca (largura)",
        ["calado_m"] = "Calado",
        ["peso_kg"] = "Peso",
        ["peso_vazio_kg"] = "Peso vazio",
        ["peso_max_decolagem_kg"] = "Peso máximo de decolagem",
        ["deslocamento_t"] = "Deslocamento",
        ["quantidade"] = "Motores (quantidade)",
        ["modelo"] = "Motor (modelo)",
        ["motor_modelo"] = "Motor",
        ["empuxo_lbf"] = "Empuxo por motor",
        ["potencia_hp"] = "Potência",
        ["propulsao"] = "Propulsão",
        ["velocidade_max_kmh"] = "Velocidade máxima",
        ["alcance_km"] = "Alcance",
        ["teto_m"] = "Teto de serviço",
        ["armamento"] = "Armamento",
        ["armamento_principal"] = "Armamento principal",
        ["armamento_secundario"] = "Armamento secundário",
        ["blindagem"] = "Blindagem",
        ["torre_tripulada"] = "Torre tripulada",
    };

    public static EspecificacoesDto Montar(string json, string categoria)
    {
        var medidas = new List<MedidaDto>();
        var campos = new List<CampoDto>();
        var navio = string.Equals(categoria, "NAVIO", StringComparison.OrdinalIgnoreCase);

        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
        if (doc.RootElement.ValueKind == JsonValueKind.Object)
            Percorrer(doc.RootElement, medidas, campos, navio);

        return new EspecificacoesDto(medidas, campos);
    }

    private static void Percorrer(JsonElement objeto, List<MedidaDto> medidas, List<CampoDto> campos, bool navio)
    {
        foreach (var p in objeto.EnumerateObject())
        {
            var chave = p.Name;
            var valor = p.Value;
            var rotulo = Rotulo(chave);

            switch (valor.ValueKind)
            {
                case JsonValueKind.Number:
                    if (Conversoes.TentarUnidade(chave, out var unidade) && valor.TryGetDouble(out var numero))
                        medidas.Add(new MedidaDto(chave, rotulo, numero, unidade, Conversoes.Converter(unidade, numero, navio)));
                    else
                        campos.Add(new CampoDto(chave, rotulo, valor.GetRawText(), null));
                    break;

                case JsonValueKind.String:
                    campos.Add(new CampoDto(chave, rotulo, TextoDe(chave, valor.GetString() ?? ""), null));
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    campos.Add(new CampoDto(chave, rotulo, valor.GetBoolean() ? "Sim" : "Não", null));
                    break;

                case JsonValueKind.Object:
                    if (chave.StartsWith("armamento", StringComparison.Ordinal))
                        campos.Add(new CampoDto(chave, rotulo, FormatarArma(valor), null));
                    else
                        Percorrer(valor, medidas, campos, navio); // ex.: motores
                    break;

                case JsonValueKind.Array:
                    var itens = valor.EnumerateArray()
                        .Select(i => i.ValueKind == JsonValueKind.Object ? FormatarArma(i) : i.ToString())
                        .Where(s => s.Length > 0)
                        .ToList();
                    campos.Add(new CampoDto(chave, rotulo, null, itens));
                    break;
            }
        }
    }

    private static string Rotulo(string chave) =>
        Rotulos.TryGetValue(chave, out var r) ? r : Humanizar(chave);

    private static string Humanizar(string texto)
    {
        var limpo = texto.Replace('_', ' ').Trim();
        return limpo.Length == 0 ? limpo : char.ToUpperInvariant(limpo[0]) + limpo[1..];
    }

    private static string TextoDe(string chave, string valor)
    {
        if (chave == "subtipo") return Humanizar(valor);
        if (chave.StartsWith("data_", StringComparison.Ordinal)
            && DateOnly.TryParseExact(valor, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var data))
            return data.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        return valor;
    }

    private static string Texto(JsonElement o, string campo) =>
        o.TryGetProperty(campo, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

    private static string Numero(JsonElement o, string campo) =>
        o.TryGetProperty(campo, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetRawText() : "";

    /// <summary>Resume um item de armamento em uma linha de texto.</summary>
    private static string FormatarArma(JsonElement a)
    {
        var descricao = Texto(a, "descricao");
        var calibre = Numero(a, "calibre_mm");
        var modelo = Texto(a, "modelo");
        var tipo = Texto(a, "tipo");

        var texto = descricao;
        if (texto.Length == 0 && calibre.Length > 0) texto = calibre + " mm";
        if (modelo.Length > 0) texto = texto.Length == 0 ? modelo : $"{texto} ({modelo})";
        if (tipo.Length > 0) texto = texto.Length == 0 ? tipo : $"{tipo}: {texto}";

        var extras = new List<string>();
        var municao = Numero(a, "municao");
        if (municao.Length > 0) extras.Add($"munição: {municao}");
        var carga = Numero(a, "carga_max_kg");
        if (carga.Length > 0) extras.Add($"carga máx.: {carga} kg");
        var pontos = Numero(a, "pontos");
        if (pontos.Length > 0) extras.Add($"{pontos} pontos de fixação");

        return extras.Count == 0 ? texto : $"{texto} ({string.Join("; ", extras)})";
    }
}
