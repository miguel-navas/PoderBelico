using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PoderBelico.Dados;
using PoderBelico.Importador;

const string Ajuda = """
    Uso: PoderBelico.Importador <arquivo.json | pasta>... [opções]

    Importa fontes e equipamentos (JSON) para o banco SQLite.
    Pastas são lidas por inteiro (subpastas incluídas); pastas chamadas "rejeitadas" são ignoradas.

    Opções:
      --banco <arquivo>      banco SQLite (padrão: poder-belico.db)
      --paises <arquivo>     lista de países de referência (repetível). Padrão: dados/referencia/paises.json
                             e paises-adicionais.json, procurados a partir da pasta atual para cima
      --estrito              se qualquer registro for rejeitado, nada é gravado
      --simular              valida e relata sem gravar fichas (migrações e países de referência
                             continuam sendo aplicados, pois a validação precisa deles)
      --relatorio <arquivo>  grava o relatório também em JSON
      --sem-migrar           não aplica as migrations antes de importar

    Código de saída: 0 = ok; 1 = há registro rejeitado; 2 = erro de uso ou de infraestrutura.
    """;

var caminhos = new List<string>();
var arquivosPaises = new List<string>();
var banco = "poder-belico.db";
string? relatorioJson = null;
bool estrito = false, simular = false, migrar = true;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "-h" or "--ajuda" or "--help":
            Console.WriteLine(Ajuda);
            return 0;
        case "--estrito": estrito = true; break;
        case "--simular": simular = true; break;
        case "--sem-migrar": migrar = false; break;
        case "--banco": banco = ValorDe(args, ref i); break;
        case "--paises": arquivosPaises.Add(ValorDe(args, ref i)); break;
        case "--relatorio": relatorioJson = ValorDe(args, ref i); break;
        default:
            if (args[i].StartsWith("--"))
            {
                Console.Error.WriteLine($"Opção desconhecida: {args[i]}\n\n{Ajuda}");
                return 2;
            }
            caminhos.Add(args[i]);
            break;
    }
}

if (caminhos.Count == 0)
{
    Console.Error.WriteLine(Ajuda);
    return 2;
}

try
{
    var arquivos = new List<Arquivo>();
    foreach (var caminho in caminhos)
    {
        if (Directory.Exists(caminho))
        {
            var doDiretorio = Directory.EnumerateFiles(caminho, "*", SearchOption.AllDirectories)
                .Where(f => Path.GetExtension(f).ToLowerInvariant() is ".json" or ".csv")
                .Where(f => !Path.GetRelativePath(caminho, f).Split(Path.DirectorySeparatorChar).SkipLast(1)
                    .Any(p => p.Equals("rejeitadas", StringComparison.OrdinalIgnoreCase)))
                .Order(StringComparer.OrdinalIgnoreCase);
            foreach (var f in doDiretorio)
                arquivos.Add(new Arquivo(f, await File.ReadAllTextAsync(f)));
        }
        else if (File.Exists(caminho))
        {
            arquivos.Add(new Arquivo(caminho, await File.ReadAllTextAsync(caminho)));
        }
        else
        {
            Console.Error.WriteLine($"Não encontrado: {caminho}");
            return 2;
        }
    }

    if (arquivosPaises.Count == 0)
        arquivosPaises.AddRange(AcharPaisesPadrao());

    var opcoesDb = new DbContextOptionsBuilder<PoderBelicoContext>()
        .UseSqlite($"Data Source={banco};Default Timeout=30")
        .Options;
    await using var db = new PoderBelicoContext(opcoesDb);

    // Mantém a conexão aberta durante toda a execução (WAL + espera por bloqueio).
    await db.Database.OpenConnectionAsync();
    await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
    await db.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=30000;");

    if (migrar) await db.Database.MigrateAsync();
    foreach (var paises in arquivosPaises)
        await SementeReferencia.AplicarAsync(db, paises);

    var servico = new ServicoImportacao(db);
    var relatorio = await servico.ExecutarAsync(arquivos, new Opcoes(estrito, simular));

    Console.WriteLine(relatorio.EmTexto());

    if (relatorioJson is not null)
    {
        var json = JsonSerializer.Serialize(new
        {
            simulacao = relatorio.Simulacao,
            estrito = relatorio.Estrito,
            desfeito_por_estrito = relatorio.DesfeitoPorEstrito,
            criados = relatorio.Criados,
            atualizados = relatorio.Atualizados,
            ignorados = relatorio.Ignorados,
            rejeitados = relatorio.Rejeitados,
            itens = relatorio.Itens.Select(i => new
            {
                tipo = i.Tipo,
                slug = i.Slug,
                resultado = i.Resultado.ToString().ToUpperInvariant(),
                erros = i.Erros,
                avisos = i.Avisos,
            }),
        }, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        await File.WriteAllTextAsync(relatorioJson, json);
    }

    return relatorio.CodigoSaida;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Falha na importação (nada foi gravado nesta execução): {ex.Message}");
    return 2;
}

static string ValorDe(string[] args, ref int i)
{
    if (i + 1 >= args.Length) throw new ArgumentException($"A opção {args[i]} precisa de um valor.");
    return args[++i];
}

static IEnumerable<string> AcharPaisesPadrao()
{
    for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir is not null; dir = dir.Parent)
    {
        var pasta = Path.Combine(dir.FullName, "dados", "referencia");
        if (!Directory.Exists(pasta)) continue;
        foreach (var nome in new[] { "paises.json", "paises-adicionais.json" })
        {
            var arquivo = Path.Combine(pasta, nome);
            if (File.Exists(arquivo)) yield return arquivo;
        }
        yield break;
    }
}
