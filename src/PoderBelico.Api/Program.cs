using Microsoft.EntityFrameworkCore;
using PoderBelico.Api;
using PoderBelico.Api.Catalogo;
using PoderBelico.Dados;

var builder = WebApplication.CreateBuilder(args);

// O importador e a API precisam abrir o MESMO arquivo de banco. Em desenvolvimento, um caminho
// relativo é resolvido a partir da pasta da solução (onde está o PoderBelico.slnx), e não da
// pasta em que o comando foi executado.
var conexao = ResolverBanco(
    builder.Configuration.GetConnectionString("Padrao"), builder.Environment.ContentRootPath);
builder.Services.AddDbContext<PoderBelicoContext>(opcoes => opcoes.UseSqlite(conexao));
builder.Services.AddHostedService<InicializacaoBanco>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Serve o build do React (wwwroot) e cai no index.html para rotas do front.
app.UseDefaultFiles();
app.UseStaticFiles();

var api = app.MapGroup("/api");

api.MapGet("/saude", async (PoderBelicoContext db) =>
{
    var bancoOk = await db.Database.CanConnectAsync();
    return Results.Ok(new { status = "ok", banco = bancoOk });
});

api.MapEquipamentos();
api.MapEventos();
api.MapBusca();

// Rota /api desconhecida deve dar 404, e não devolver o index.html.
api.MapFallback("{**caminho}", () => Results.NotFound());

app.MapFallbackToFile("index.html");

app.Run();

static string? ResolverBanco(string? texto, string pastaDaAplicacao)
{
    if (string.IsNullOrWhiteSpace(texto)) return texto;

    var construtor = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(texto);
    var arquivo = construtor.DataSource;
    if (string.IsNullOrWhiteSpace(arquivo) || arquivo.StartsWith(':') || Path.IsPathRooted(arquivo))
        return texto;

    for (var pasta = new DirectoryInfo(pastaDaAplicacao); pasta is not null; pasta = pasta.Parent)
    {
        if (File.Exists(Path.Combine(pasta.FullName, "PoderBelico.slnx")))
        {
            construtor.DataSource = Path.Combine(pasta.FullName, arquivo);
            return construtor.ToString();
        }
    }
    return texto; // publicado: segue relativo à pasta de execução
}

// Necessário para o WebApplicationFactory nos testes.
public partial class Program;
