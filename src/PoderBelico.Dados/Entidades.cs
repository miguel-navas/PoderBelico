namespace PoderBelico.Dados;

public enum CodigoForca { Marinha, Aerea, Exercito }
public enum StatusEquipamento { Ativo, Aposentado, Prototipo }
public enum Confiabilidade { Confirmado, Estimado, Contestado }
public enum TipoEvento { Guerra, Campanha, Batalha }
public enum TipoFonte { Livro, Artigo, Site, Arquivo }

public class Pais
{
    public int Id { get; set; }
    /// <summary>ISO 3166-1 alpha-3, ou código do projeto para países extintos (ex.: SUN).</summary>
    public required string CodigoIso { get; set; }
    public required string Nome { get; set; }
    /// <summary>Ano em que passou a existir. Nulo quando não se aplica (países atuais).</summary>
    public int? AnoInicio { get; set; }
    /// <summary>Ano em que deixou de existir. Nulo para países que ainda existem.</summary>
    public int? AnoFim { get; set; }
}

public class Forca
{
    public int Id { get; set; }
    public CodigoForca Codigo { get; set; }
    public required string Nome { get; set; }
}

public class Categoria
{
    public int Id { get; set; }
    public int ForcaId { get; set; }
    public Forca? Forca { get; set; }
    /// <summary>Ex.: TANQUE, AERONAVE, NAVIO.</summary>
    public required string Codigo { get; set; }
    public required string Nome { get; set; }
}

public class Equipamento
{
    public int Id { get; set; }
    public required string Slug { get; set; }
    public required string Nome { get; set; }
    public string? Variante { get; set; }

    public int PaisId { get; set; }
    public Pais? Pais { get; set; }
    public int ForcaId { get; set; }
    public Forca? Forca { get; set; }
    public int CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    public string? Fabricante { get; set; }
    public int? AnoPrimeiroUso { get; set; }
    public int? AnoEntradaServico { get; set; }
    public int? UnidadesProduzidas { get; set; }
    public StatusEquipamento Status { get; set; }
    public string? DescricaoCurta { get; set; }
    public string? Curiosidade { get; set; }
    public string? ImagemUrl { get; set; }
    public string? ImagemCredito { get; set; }

    /// <summary>JSON com as especificações da categoria, em unidade padrão.</summary>
    public string Especificacoes { get; set; } = "{}";
    /// <summary>Sem valor padrão: "confirmado" é sempre uma decisão explícita.</summary>
    public required Confiabilidade Confiabilidade { get; set; }

    public ICollection<EquipamentoOperador> Operadores { get; set; } = [];
    public ICollection<Participacao> Participacoes { get; set; } = [];
    public ICollection<EquipamentoFonte> Fontes { get; set; } = [];
}

public class EquipamentoOperador
{
    public int EquipamentoId { get; set; }
    public Equipamento? Equipamento { get; set; }
    public int PaisId { get; set; }
    public Pais? Pais { get; set; }
    public string? Observacao { get; set; }
}

public class Evento
{
    public int Id { get; set; }
    public required string Slug { get; set; }
    public required string Nome { get; set; }
    public TipoEvento Tipo { get; set; }

    public int? EventoPaiId { get; set; }
    public Evento? EventoPai { get; set; }
    public ICollection<Evento> Subeventos { get; set; } = [];

    public DateOnly DataInicio { get; set; }
    /// <summary>Nulo quando o evento está em andamento.</summary>
    public DateOnly? DataFim { get; set; }
    public string? Local { get; set; }
    public string? Descricao { get; set; }
    public string? Resultado { get; set; }
    /// <summary>Sem valor padrão: "confirmado" é sempre uma decisão explícita.</summary>
    public required Confiabilidade Confiabilidade { get; set; }

    public ICollection<EventoPais> Paises { get; set; } = [];
    public ICollection<Participacao> Participacoes { get; set; } = [];
    public ICollection<EventoFonte> Fontes { get; set; } = [];
}

public class EventoPais
{
    public int EventoId { get; set; }
    public Evento? Evento { get; set; }
    public int PaisId { get; set; }
    public Pais? Pais { get; set; }
    /// <summary>Ex.: Aliados, Eixo, Coalizão.</summary>
    public string? Lado { get; set; }
}

public class Participacao
{
    public int Id { get; set; }
    public int EquipamentoId { get; set; }
    public Equipamento? Equipamento { get; set; }
    public int EventoId { get; set; }
    public Evento? Evento { get; set; }
    public int PaisId { get; set; }
    public Pais? Pais { get; set; }
    public required string Papel { get; set; }
    public int? QuantidadeAprox { get; set; }
    public string? Observacao { get; set; }
}

public class Fonte
{
    public int Id { get; set; }
    /// <summary>Chave natural para a importação idempotente.</summary>
    public required string Slug { get; set; }
    /// <summary>Origem não verificada (conversa com IA, anotação própria). Não sustenta "confirmado".</summary>
    public bool Provisoria { get; set; }
    public TipoFonte Tipo { get; set; }
    public required string Titulo { get; set; }
    public string? Autor { get; set; }
    public int? Ano { get; set; }
    public string? Url { get; set; }
    public DateOnly? AcessadoEm { get; set; }
}

public class EquipamentoFonte
{
    public int EquipamentoId { get; set; }
    public Equipamento? Equipamento { get; set; }
    public int FonteId { get; set; }
    public Fonte? Fonte { get; set; }
    /// <summary>Página ou seção.</summary>
    public string? Referencia { get; set; }
}

public class EventoFonte
{
    public int EventoId { get; set; }
    public Evento? Evento { get; set; }
    public int FonteId { get; set; }
    public Fonte? Fonte { get; set; }
    public string? Referencia { get; set; }
}
