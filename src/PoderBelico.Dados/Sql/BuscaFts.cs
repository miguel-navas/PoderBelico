namespace PoderBelico.Dados.Sql;

/// <summary>
/// SQL da tabela virtual de busca (FTS5), que o EF Core não gera sozinho.
/// É criada pela semente de referência na inicialização (idempotente).
/// </summary>
public static class BuscaFts
{
    public const string Criar = """
        CREATE VIRTUAL TABLE IF NOT EXISTS busca USING fts5(
            tipo UNINDEXED,
            slug UNINDEXED,
            nome,
            descricao,
            tokenize = 'unicode61 remove_diacritics 2'
        );
        """;
}
