using System.Text.Json;
using System.Text.RegularExpressions;
using PoderBelico.Dados;

namespace PoderBelico.Importador;

internal sealed class RegFonte
{
    public string Slug { get; set; } = "";
    public TipoFonte Tipo { get; set; }
    public string Titulo { get; set; } = "";
    public string? Autor { get; set; }
    public int? Ano { get; set; }
    public string? Url { get; set; }
    public DateOnly? AcessadoEm { get; set; }
    public bool Provisoria { get; set; }
}

/// <summary>Ligação de um equipamento com uma fonte: por slug, com a definição embutida quando houver.</summary>
internal sealed record LigacaoFonte(string Slug, string? Referencia, RegFonte? Definicao);

internal sealed class RegEquipamento
{
    public string Slug { get; set; } = "";
    public string Nome { get; set; } = "";
    public string? Variante { get; set; }
    public string Pais { get; set; } = "";
    public CodigoForca Forca { get; set; }
    public string Categoria { get; set; } = "";
    public string? Fabricante { get; set; }
    public int? AnoPrimeiroUso { get; set; }
    public int? AnoEntradaServico { get; set; }
    public int? UnidadesProduzidas { get; set; }
    public StatusEquipamento Status { get; set; }
    public Confiabilidade Confiabilidade { get; set; }
    public string? DescricaoCurta { get; set; }
    public string? Curiosidade { get; set; }
    public string? ImagemUrl { get; set; }
    public string? ImagemCredito { get; set; }
    public List<string> Operadores { get; } = [];
    public string EspecificacoesJson { get; set; } = "{}";
    public List<LigacaoFonte> Fontes { get; } = [];
}

internal sealed class RegEvento
{
    public string Slug { get; set; } = "";
    public string Nome { get; set; } = "";
    public TipoEvento Tipo { get; set; }
    public string? EventoPai { get; set; }
    public DateOnly DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }
    public string? Local { get; set; }
    public string? Descricao { get; set; }
    public string? Resultado { get; set; }
    public Confiabilidade Confiabilidade { get; set; }
    public List<(string Pais, string? Lado)> Beligerantes { get; } = [];
    public List<LigacaoFonte> Fontes { get; } = [];
}

internal sealed class RegParticipacao
{
    public string Equipamento { get; set; } = "";
    public string Evento { get; set; } = "";
    public string Pais { get; set; } = "";
    public string Papel { get; set; } = "";
    public int? QuantidadeAprox { get; set; }
    public string? Observacao { get; set; }
}

/// <summary>Lê um registro JSON e confere forma, tipos e campos (não consulta o banco).</summary>
internal static class Leitor
{
    private static readonly Regex SlugValido = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    private static readonly HashSet<string> CamposFonte =
        ["slug", "tipo", "titulo", "autor", "ano", "url", "acessado_em", "provisoria", "referencia"];

    private static readonly HashSet<string> CamposEquipamento =
    [
        "slug", "nome", "variante", "pais", "forca", "categoria", "fabricante",
        "ano_primeiro_uso", "ano_entrada_servico", "unidades_produzidas", "status",
        "confiabilidade", "descricao_curta", "curiosidade", "imagem_url", "imagem_credito",
        "operadores", "especificacoes", "fontes",
    ];

    private static readonly HashSet<string> CamposEvento =
    [
        "slug", "nome", "tipo", "evento_pai", "data_inicio", "data_fim", "local", "descricao",
        "resultado", "confiabilidade", "beligerantes", "fontes",
    ];

    private static readonly HashSet<string> CamposParticipacao =
        ["equipamento", "evento", "pais", "papel", "quantidade_aprox", "observacao"];

    public static bool EhParticipacao(JsonElement o) =>
        o.TryGetProperty("equipamento", out _) && o.TryGetProperty("evento", out _);

    public static bool EhEvento(JsonElement o) =>
        o.TryGetProperty("data_inicio", out _) || o.TryGetProperty("beligerantes", out _);

    /// <summary>Identificação da participação no relatório: equipamento@evento@país.</summary>
    public static string? RotuloParticipacao(JsonElement o)
    {
        string? Ler(string n) => o.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        var eq = Ler("equipamento"); var ev = Ler("evento"); var pais = Ler("pais");
        return eq is null || ev is null || pais is null ? null : $"{eq}@{ev}@{pais.ToUpperInvariant()}";
    }

    public static bool EhFonte(JsonElement o) =>
        o.TryGetProperty("titulo", out _) && !o.TryGetProperty("nome", out _) && !o.TryGetProperty("categoria", out _);

    public static bool EhEquipamento(JsonElement o) =>
        o.TryGetProperty("nome", out _) || o.TryGetProperty("categoria", out _);

    /// <summary>Slug do registro, quando legível (para identificar o item no relatório).</summary>
    public static string? SlugDe(JsonElement o) =>
        o.ValueKind == JsonValueKind.Object && o.TryGetProperty("slug", out var s) && s.ValueKind == JsonValueKind.String
            ? s.GetString()
            : null;

    // ------------------------------------------------------------------ fonte
    public static RegFonte LerFonte(JsonElement o, List<string> erros, string prefixo = "")
    {
        var c = new Campos(o, erros, prefixo);
        c.CamposDesconhecidos(CamposFonte);

        var f = new RegFonte
        {
            Slug = c.Slug("slug"),
            Titulo = c.Texto("titulo", obrigatorio: true) ?? "",
            Tipo = c.Enumeracao<TipoFonte>("tipo", obrigatorio: true),
            Autor = c.Texto("autor"),
            Ano = c.Inteiro("ano"),
            Url = c.Texto("url"),
            AcessadoEm = c.Data("acessado_em"),
            Provisoria = c.Booleano("provisoria") ?? false,
        };
        return f;
    }

    // ------------------------------------------------------------ equipamento
    public static RegEquipamento LerEquipamento(JsonElement o, List<string> erros)
    {
        var c = new Campos(o, erros, "");
        c.CamposDesconhecidos(CamposEquipamento);

        var r = new RegEquipamento
        {
            Slug = c.Slug("slug"),
            Nome = c.Texto("nome", obrigatorio: true) ?? "",
            Variante = c.Texto("variante"),
            Pais = (c.Texto("pais", obrigatorio: true) ?? "").ToUpperInvariant(),
            Forca = c.Enumeracao<CodigoForca>("forca", obrigatorio: true),
            Categoria = (c.Texto("categoria", obrigatorio: true) ?? "").ToUpperInvariant(),
            Fabricante = c.Texto("fabricante"),
            AnoPrimeiroUso = c.Inteiro("ano_primeiro_uso"),
            AnoEntradaServico = c.Inteiro("ano_entrada_servico"),
            UnidadesProduzidas = c.Inteiro("unidades_produzidas"),
            Status = c.Enumeracao<StatusEquipamento>("status", obrigatorio: true),
            // Sem valor padrão: a ficha precisa declarar a confiabilidade.
            Confiabilidade = c.Enumeracao<Confiabilidade>("confiabilidade", obrigatorio: true),
            DescricaoCurta = c.Texto("descricao_curta"),
            Curiosidade = c.Texto("curiosidade"),
            ImagemUrl = c.Texto("imagem_url"),
            ImagemCredito = c.Texto("imagem_credito"),
        };

        LerOperadores(o, r, erros);
        LerEspecificacoes(o, r, erros);
        LerFontes(o, r.Fontes, erros);
        return r;
    }

    private static void LerOperadores(JsonElement o, RegEquipamento r, List<string> erros)
    {
        if (!o.TryGetProperty("operadores", out var ops) || ops.ValueKind == JsonValueKind.Null) return;
        if (ops.ValueKind != JsonValueKind.Array)
        {
            erros.Add("operadores: esperado lista de códigos de país");
            return;
        }
        var i = 0;
        foreach (var item in ops.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString()))
                erros.Add($"operadores[{i}]: esperado código de país em texto");
            else
            {
                var iso = item.GetString()!.Trim().ToUpperInvariant();
                if (!r.Operadores.Contains(iso)) r.Operadores.Add(iso);
            }
            i++;
        }
    }

    private static void LerEspecificacoes(JsonElement o, RegEquipamento r, List<string> erros)
    {
        if (!o.TryGetProperty("especificacoes", out var esp) || esp.ValueKind == JsonValueKind.Null)
        {
            erros.Add("especificacoes: obrigatório");
            return;
        }
        if (esp.ValueKind != JsonValueKind.Object)
        {
            erros.Add("especificacoes: esperado um objeto");
            return;
        }
        // Serialização compacta: reimportar o mesmo conteúdo não muda o registro.
        r.EspecificacoesJson = JsonSerializer.Serialize(esp);
    }

    private static void LerFontes(JsonElement o, List<LigacaoFonte> destino, List<string> erros)
    {
        if (!o.TryGetProperty("fontes", out var fontes) || fontes.ValueKind == JsonValueKind.Null)
        {
            erros.Add("fontes: ao menos uma fonte é obrigatória");
            return;
        }
        if (fontes.ValueKind != JsonValueKind.Array)
        {
            erros.Add("fontes: esperado uma lista");
            return;
        }

        var i = 0;
        foreach (var item in fontes.EnumerateArray())
        {
            var prefixo = $"fontes[{i}].";
            if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
            {
                destino.Add(new LigacaoFonte(item.GetString()!.Trim(), null, null));
            }
            else if (item.ValueKind == JsonValueKind.Object)
            {
                var c = new Campos(item, erros, prefixo);
                var referencia = c.Texto("referencia");
                if (item.TryGetProperty("titulo", out _))
                {
                    var def = LerFonte(item, erros, prefixo);
                    destino.Add(new LigacaoFonte(def.Slug, referencia, def));
                }
                else
                {
                    c.CamposDesconhecidos(["slug", "referencia"]);
                    destino.Add(new LigacaoFonte(c.Slug("slug"), referencia, null));
                }
            }
            else
            {
                erros.Add($"{prefixo.TrimEnd('.')}: esperado o slug da fonte ou um objeto de fonte");
            }
            i++;
        }

        if (destino.Count == 0 && erros.All(e => !e.StartsWith("fontes")))
            erros.Add("fontes: ao menos uma fonte é obrigatória");
    }

    // ----------------------------------------------------------------- evento
    public static RegEvento LerEvento(JsonElement o, List<string> erros)
    {
        var c = new Campos(o, erros, "");
        c.CamposDesconhecidos(CamposEvento);

        var r = new RegEvento
        {
            Slug = c.Slug("slug"),
            Nome = c.Texto("nome", obrigatorio: true) ?? "",
            Tipo = c.Enumeracao<TipoEvento>("tipo", obrigatorio: true),
            EventoPai = c.Slug("evento_pai", obrigatorio: false) is { Length: > 0 } pai ? pai : null,
            DataInicio = c.Data("data_inicio", obrigatorio: true) ?? default,
            DataFim = c.Data("data_fim"),
            Local = c.Texto("local"),
            Descricao = c.Texto("descricao"),
            Resultado = c.Texto("resultado"),
            Confiabilidade = c.Enumeracao<Confiabilidade>("confiabilidade", obrigatorio: true),
        };

        if (r.DataFim is { } fim && r.DataInicio != default && fim < r.DataInicio)
            erros.Add("data_fim: não pode ser anterior à data_inicio");

        if (o.TryGetProperty("beligerantes", out var bel) && bel.ValueKind != JsonValueKind.Null)
        {
            if (bel.ValueKind != JsonValueKind.Array)
                erros.Add("beligerantes: esperado uma lista");
            else
            {
                var i = 0;
                foreach (var item in bel.EnumerateArray())
                {
                    var prefixo = $"beligerantes[{i}].";
                    if (item.ValueKind != JsonValueKind.Object)
                        erros.Add($"beligerantes[{i}]: esperado objeto com pais e lado");
                    else
                    {
                        var cb = new Campos(item, erros, prefixo);
                        cb.CamposDesconhecidos(["pais", "lado"]);
                        var pais = cb.Texto("pais", obrigatorio: true);
                        var lado = cb.Texto("lado");
                        if (pais is not null)
                        {
                            r.Beligerantes.RemoveAll(b => b.Pais == pais.ToUpperInvariant());
                            r.Beligerantes.Add((pais.ToUpperInvariant(), lado));
                        }
                    }
                    i++;
                }
            }
        }

        LerFontes(o, r.Fontes, erros);
        return r;
    }

    // ------------------------------------------------------------ participação
    public static RegParticipacao LerParticipacao(JsonElement o, List<string> erros)
    {
        var c = new Campos(o, erros, "");
        c.CamposDesconhecidos(CamposParticipacao);

        return new RegParticipacao
        {
            Equipamento = c.Slug("equipamento"),
            Evento = c.Slug("evento"),
            Pais = (c.Texto("pais", obrigatorio: true) ?? "").ToUpperInvariant(),
            Papel = c.Texto("papel", obrigatorio: true) ?? "",
            QuantidadeAprox = c.Inteiro("quantidade_aprox", minimo: 1),
            Observacao = c.Texto("observacao"),
        };
    }

    // ---------------------------------------------------------------- campos
    private sealed class Campos(JsonElement obj, List<string> erros, string prefixo)
    {
        private string Nome(string campo) => prefixo + campo;

        private bool Tem(string campo, out JsonElement valor) =>
            obj.TryGetProperty(campo, out valor) && valor.ValueKind != JsonValueKind.Null;

        public void CamposDesconhecidos(IEnumerable<string> permitidos)
        {
            var ok = permitidos as HashSet<string> ?? permitidos.ToHashSet();
            foreach (var p in obj.EnumerateObject())
                if (!p.Name.StartsWith('_') && !ok.Contains(p.Name))
                    erros.Add($"{Nome(p.Name)}: campo desconhecido");
        }

        public string? Texto(string campo, bool obrigatorio = false)
        {
            if (!Tem(campo, out var v))
            {
                if (obrigatorio) erros.Add($"{Nome(campo)}: obrigatório");
                return null;
            }
            if (v.ValueKind != JsonValueKind.String)
            {
                erros.Add($"{Nome(campo)}: esperado texto");
                return null;
            }
            var t = v.GetString()!.Trim();
            if (t.Length == 0)
            {
                if (obrigatorio) erros.Add($"{Nome(campo)}: obrigatório");
                return null;
            }
            return t;
        }

        public string Slug(string campo, bool obrigatorio = true)
        {
            var t = Texto(campo, obrigatorio);
            if (t is null) return "";
            if (!SlugValido.IsMatch(t))
            {
                erros.Add($"{Nome(campo)}: '{t}' inválido (use minúsculas, números e hifens)");
                return "";
            }
            return t;
        }

        public int? Inteiro(string campo, int? minimo = null)
        {
            if (!Tem(campo, out var v)) return null;
            if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n))
            {
                if (minimo is { } min && n < min)
                {
                    erros.Add($"{Nome(campo)}: esperado número inteiro maior ou igual a {min}");
                    return null;
                }
                return n;
            }
            erros.Add($"{Nome(campo)}: esperado número inteiro");
            return null;
        }

        public bool? Booleano(string campo)
        {
            if (!Tem(campo, out var v)) return null;
            if (v.ValueKind is JsonValueKind.True or JsonValueKind.False) return v.GetBoolean();
            erros.Add($"{Nome(campo)}: esperado verdadeiro ou falso");
            return null;
        }

        public DateOnly? Data(string campo, bool obrigatorio = false)
        {
            var t = Texto(campo, obrigatorio);
            if (t is null) return null;
            if (DateOnly.TryParseExact(t, "yyyy-MM-dd", out var d)) return d;
            erros.Add($"{Nome(campo)}: esperado data no formato AAAA-MM-DD");
            return null;
        }

        public T Enumeracao<T>(string campo, bool obrigatorio) where T : struct, Enum
        {
            var t = Texto(campo, obrigatorio);
            if (t is null) return default;
            if (!int.TryParse(t, out _) && Enum.TryParse<T>(t, ignoreCase: true, out var v) && Enum.IsDefined(v))
                return v;
            var validos = string.Join(", ", Enum.GetNames<T>().Select(n => n.ToUpperInvariant()));
            erros.Add($"{Nome(campo)}: valor '{t}' inválido (esperado: {validos})");
            return default;
        }
    }
}
