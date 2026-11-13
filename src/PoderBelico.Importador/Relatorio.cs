namespace PoderBelico.Importador;

public enum Resultado { Criado, Atualizado, Ignorado, Rejeitado, Revertido }

public sealed record Opcoes(bool Estrito = false, bool Simular = false);

/// <summary>Conteúdo de um arquivo a importar (o nome serve para mensagens e para checar a extensão).</summary>
public sealed record Arquivo(string Nome, string Conteudo);

public sealed class ItemRelatorio
{
    /// <summary>"fonte", "equipamento" ou "arquivo".</summary>
    public required string Tipo { get; init; }
    /// <summary>Slug do registro (ou nome do arquivo / posição, quando o slug não pôde ser lido).</summary>
    public required string Slug { get; init; }
    public Resultado Resultado { get; set; }
    public List<string> Erros { get; } = [];
    public List<string> Avisos { get; } = [];

    public void Rejeitar(string erro)
    {
        Erros.Add(erro);
        Resultado = Resultado.Rejeitado;
    }
}

public sealed class Relatorio
{
    public List<ItemRelatorio> Itens { get; } = [];
    public bool Simulacao { get; set; }
    public bool Estrito { get; set; }
    /// <summary>Verdadeiro quando o modo estrito desfez a importação por causa de uma rejeição.</summary>
    public bool DesfeitoPorEstrito { get; set; }

    public int Criados => Contar(Resultado.Criado);
    public int Atualizados => Contar(Resultado.Atualizado);
    public int Ignorados => Contar(Resultado.Ignorado);
    public int Rejeitados => Contar(Resultado.Rejeitado);
    public int Revertidos => Contar(Resultado.Revertido);

    public bool TemRejeicao => Rejeitados > 0;

    /// <summary>0 = tudo certo; 1 = pelo menos um registro rejeitado.</summary>
    public int CodigoSaida => TemRejeicao ? 1 : 0;

    private int Contar(Resultado r) => Itens.Count(i => i.Resultado == r);

    public ItemRelatorio Novo(string tipo, string slug)
    {
        var item = new ItemRelatorio { Tipo = tipo, Slug = slug };
        Itens.Add(item);
        return item;
    }

    public string EmTexto()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(Simulacao ? "== Relatório da importação (SIMULAÇÃO: nada foi gravado) ==" : "== Relatório da importação ==");
        sb.AppendLine($"Criados: {Criados} | Atualizados: {Atualizados} | Ignorados (sem mudança): {Ignorados} | Rejeitados: {Rejeitados}"
            + (Revertidos > 0 ? $" | Não gravados (--estrito): {Revertidos}" : ""));
        if (DesfeitoPorEstrito)
            sb.AppendLine("Modo estrito: houve rejeição, então NADA foi gravado.");

        foreach (var item in Itens.Where(i => i.Erros.Count > 0 || i.Avisos.Count > 0))
        {
            sb.AppendLine();
            sb.AppendLine($"[{item.Resultado.ToString().ToUpperInvariant()}] {item.Tipo} '{item.Slug}'");
            foreach (var e in item.Erros) sb.AppendLine($"  erro:  {e}");
            foreach (var a in item.Avisos) sb.AppendLine($"  aviso: {a}");
        }
        return sb.ToString();
    }
}
