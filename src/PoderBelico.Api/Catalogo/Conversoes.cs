namespace PoderBelico.Api.Catalogo;

public sealed record Convertido(double Valor, string Unidade);

/// <summary>
/// Conversão de unidades só para exibição. O banco guarda tudo em unidade padrão
/// (km/h, kg, m, km, t, mm, hp, lbf); o sufixo do nome do campo diz qual é.
/// </summary>
public static class Conversoes
{
    private const double MilhaPorKm = 0.621371;
    private const double KmPorMilhaNautica = 1.852;
    private const double LibraPorKg = 2.20462;
    private const double PesPorMetro = 3.28084;
    private const double KwPorHp = 0.7457;
    private const double KnPorLbf = 0.00444822;
    private const double MmPorPolegada = 25.4;

    // Sufixos mais longos primeiro ("_kmh" e "_km" antes de "_m").
    private static readonly (string Sufixo, string Unidade)[] Sufixos =
    [
        ("_kmh", "km/h"), ("_kg", "kg"), ("_km", "km"), ("_mm", "mm"),
        ("_lbf", "lbf"), ("_hp", "hp"), ("_m", "m"), ("_t", "t"),
    ];

    /// <summary>Unidade padrão de um campo, pelo sufixo do nome (ex.: peso_kg → kg).</summary>
    public static bool TentarUnidade(string chave, out string unidade)
    {
        foreach (var (sufixo, un) in Sufixos)
        {
            if (chave.EndsWith(sufixo, StringComparison.Ordinal))
            {
                unidade = un;
                return true;
            }
        }
        unidade = "";
        return false;
    }

    /// <summary>Valores em outras unidades. Navio usa nós e milhas náuticas.</summary>
    public static List<Convertido> Converter(string unidade, double valor, bool navio)
    {
        return unidade switch
        {
            "km/h" when navio => [Arredondar(valor / KmPorMilhaNautica, "nós")],
            "km/h" => [Arredondar(valor * MilhaPorKm, "mph")],
            "kg" => [Arredondar(valor * LibraPorKg, "lb")],
            "m" => [Arredondar(valor * PesPorMetro, "ft")],
            "km" when navio => [Arredondar(valor / KmPorMilhaNautica, "milhas náuticas")],
            "km" => [Arredondar(valor * MilhaPorKm, "mi")],
            "mm" => [Arredondar(valor / MmPorPolegada, "pol")],
            "hp" => [Arredondar(valor * KwPorHp, "kW")],
            "lbf" => [Arredondar(valor * KnPorLbf, "kN")],
            _ => [],
        };
    }

    private static Convertido Arredondar(double valor, string unidade) =>
        new(Math.Round(valor, 1, MidpointRounding.AwayFromZero), unidade);
}
