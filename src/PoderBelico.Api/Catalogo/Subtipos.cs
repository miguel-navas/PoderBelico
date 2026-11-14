using System.Text.Json;

namespace PoderBelico.Api.Catalogo;

/// <summary>Subtipo do equipamento (bombardeiro, fragata, lança-foguetes...), guardado em especificacoes.subtipo.</summary>
public sealed record SubtipoRef(string Codigo, string Nome, string Forca, string Categoria);

public static class Subtipos
{
    private static readonly Dictionary<string, string> Nomes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["caca"] = "Caça",
        ["interceptador"] = "Interceptador",
        ["bombardeiro"] = "Bombardeiro",
        ["ataque_ao_solo"] = "Ataque ao solo",
        ["helicoptero_de_ataque"] = "Helicóptero de ataque",
        ["helicoptero_utilitario"] = "Helicóptero utilitário",
        ["transporte"] = "Transporte",
        ["reconhecimento"] = "Reconhecimento",
        ["nao_tripulada"] = "Não tripulada (drone)",
        ["tanque_de_batalha"] = "Tanque de batalha",
        ["tanque_medio"] = "Tanque médio",
        ["tanque_leve"] = "Tanque leve",
        ["veiculo_blindado_de_combate"] = "Veículo blindado de combate",
        ["lanca_foguetes"] = "Lança-foguetes",
        ["artilharia_autopropulsada"] = "Artilharia autopropulsada",
        ["porta_avioes"] = "Porta-aviões",
        ["destroier"] = "Destróier",
        ["fragata"] = "Fragata",
        ["corveta"] = "Corveta",
        ["cruzador"] = "Cruzador",
        ["submarino"] = "Submarino",
    };

    /// <summary>Lê o subtipo do JSON de especificações; null se não houver.</summary>
    public static string? Ler(string? especificacoes)
    {
        if (string.IsNullOrWhiteSpace(especificacoes)) return null;
        try
        {
            using var doc = JsonDocument.Parse(especificacoes);
            return doc.RootElement.ValueKind == JsonValueKind.Object
                   && doc.RootElement.TryGetProperty("subtipo", out var v)
                   && v.ValueKind == JsonValueKind.String
                ? v.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string Nome(string codigo)
    {
        if (Nomes.TryGetValue(codigo, out var nome)) return nome;
        var texto = codigo.Replace('_', ' ');
        return texto.Length == 0 ? codigo : char.ToUpperInvariant(texto[0]) + texto[1..];
    }
}
