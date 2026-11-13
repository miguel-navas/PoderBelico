using System.Collections.Concurrent;
using NJsonSchema;

namespace PoderBelico.Dados;

/// <summary>
/// Valida a coluna JSON "especificacoes" de um equipamento contra o esquema
/// da sua categoria (Esquemas/{categoria}.schema.json, embutido no assembly).
/// </summary>
public static class EspecificacoesValidador
{
    private static readonly ConcurrentDictionary<string, Task<JsonSchema>> _cache = new();

    /// <summary>Devolve a lista de erros; lista vazia significa que o JSON é válido.</summary>
    public static async Task<IReadOnlyList<string>> ValidarAsync(string categoria, string especificacoesJson)
    {
        var esquema = await _cache.GetOrAdd(categoria.ToLowerInvariant(), Carregar);
        return esquema.Validate(especificacoesJson)
            .Select(e =>
            {
                var caminho = string.IsNullOrEmpty(e.Path) ? "#" : e.Path;
                // Em campo desconhecido/obrigatório, garante que o nome do campo apareça.
                if (!string.IsNullOrEmpty(e.Property) && !caminho.EndsWith("/" + e.Property, StringComparison.Ordinal))
                    caminho += "/" + e.Property;
                return $"{caminho}: {e.Kind}";
            })
            .ToList();
    }

    private static async Task<JsonSchema> Carregar(string categoria)
    {
        var assembly = typeof(EspecificacoesValidador).Assembly;
        var recurso = assembly.GetManifestResourceNames().FirstOrDefault(n =>
                n.EndsWith($".{categoria}.schema.json", StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Categoria sem esquema: {categoria}");

        await using var fluxo = assembly.GetManifestResourceStream(recurso)!;
        using var leitor = new StreamReader(fluxo);
        return await JsonSchema.FromJsonAsync(await leitor.ReadToEndAsync());
    }
}
