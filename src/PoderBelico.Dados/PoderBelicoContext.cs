using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PoderBelico.Dados;

public class PoderBelicoContext(DbContextOptions<PoderBelicoContext> options) : DbContext(options)
{
    public DbSet<Pais> Paises => Set<Pais>();
    public DbSet<Forca> Forcas => Set<Forca>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Equipamento> Equipamentos => Set<Equipamento>();
    public DbSet<EquipamentoOperador> EquipamentoOperadores => Set<EquipamentoOperador>();
    public DbSet<Evento> Eventos => Set<Evento>();
    public DbSet<EventoPais> EventoPaises => Set<EventoPais>();
    public DbSet<Participacao> Participacoes => Set<Participacao>();
    public DbSet<Fonte> Fontes => Set<Fonte>();
    public DbSet<EquipamentoFonte> EquipamentoFontes => Set<EquipamentoFonte>();
    public DbSet<EventoFonte> EventoFontes => Set<EventoFonte>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Pais>(e =>
        {
            e.ToTable("pais");
            e.HasIndex(x => x.CodigoIso).IsUnique();
            e.Property(x => x.CodigoIso).HasMaxLength(3);
        });

        mb.Entity<Forca>(e =>
        {
            e.ToTable("forca", t => t.HasCheckConstraint(
                "ck_forca_codigo", "codigo IN ('MARINHA','AEREA','EXERCITO')"));
            e.Property(x => x.Codigo).HasConversion(Maiusculo<CodigoForca>());
            e.HasIndex(x => x.Codigo).IsUnique();
        });

        mb.Entity<Categoria>(e =>
        {
            e.ToTable("categoria");
            e.HasIndex(x => x.Codigo).IsUnique();
        });

        mb.Entity<Equipamento>(e =>
        {
            e.ToTable("equipamento", t =>
            {
                t.HasCheckConstraint("ck_equipamento_status",
                    "status IN ('ATIVO','APOSENTADO','PROTOTIPO')");
                t.HasCheckConstraint("ck_equipamento_confiabilidade",
                    "confiabilidade IN ('CONFIRMADO','ESTIMADO','CONTESTADO')");
                t.HasCheckConstraint("ck_equipamento_especificacoes",
                    "json_valid(especificacoes)");
            });
            e.Property(x => x.Status).HasConversion(Maiusculo<StatusEquipamento>());
            e.Property(x => x.Confiabilidade).HasConversion(Maiusculo<Confiabilidade>());
            e.Property(x => x.Especificacoes).HasDefaultValue("{}");
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => new { x.ForcaId, x.CategoriaId, x.PaisId, x.AnoEntradaServico })
                .HasDatabaseName("ix_equipamento_filtros");
        });

        mb.Entity<EquipamentoOperador>(e =>
        {
            e.ToTable("equipamento_operador");
            e.HasKey(x => new { x.EquipamentoId, x.PaisId });
        });

        mb.Entity<Evento>(e =>
        {
            e.ToTable("evento", t =>
            {
                t.HasCheckConstraint("ck_evento_tipo", "tipo IN ('GUERRA','CAMPANHA','BATALHA')");
                t.HasCheckConstraint("ck_evento_confiabilidade",
                    "confiabilidade IN ('CONFIRMADO','ESTIMADO','CONTESTADO')");
            });
            e.Property(x => x.Tipo).HasConversion(Maiusculo<TipoEvento>());
            e.Property(x => x.Confiabilidade).HasConversion(Maiusculo<Confiabilidade>());
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => x.DataInicio).HasDatabaseName("ix_evento_periodo");
            e.HasOne(x => x.EventoPai).WithMany(x => x.Subeventos)
                .HasForeignKey(x => x.EventoPaiId);
        });

        mb.Entity<EventoPais>(e =>
        {
            e.ToTable("evento_pais");
            e.HasKey(x => new { x.EventoId, x.PaisId });
        });

        mb.Entity<Participacao>(e =>
        {
            e.ToTable("participacao");
            e.HasIndex(x => new { x.EquipamentoId, x.EventoId, x.PaisId }).IsUnique();
        });

        mb.Entity<Fonte>(e =>
        {
            e.ToTable("fonte", t =>
            {
                t.HasCheckConstraint("ck_fonte_tipo", "tipo IN ('LIVRO','ARTIGO','SITE','ARQUIVO')");
                t.HasCheckConstraint("ck_fonte_provisoria", "provisoria IN (0,1)");
            });
            e.Property(x => x.Tipo).HasConversion(Maiusculo<TipoFonte>());
            e.Property(x => x.Provisoria).HasDefaultValue(false);
            e.HasIndex(x => x.Slug).IsUnique();
        });

        mb.Entity<EquipamentoFonte>(e =>
        {
            e.ToTable("equipamento_fonte");
            e.HasKey(x => new { x.EquipamentoId, x.FonteId });
        });

        mb.Entity<EventoFonte>(e =>
        {
            e.ToTable("evento_fonte");
            e.HasKey(x => new { x.EventoId, x.FonteId });
        });

        // Dados fixos de referência (entram na migration).
        mb.Entity<Forca>().HasData(
            new Forca { Id = 1, Codigo = CodigoForca.Marinha, Nome = "Marinha" },
            new Forca { Id = 2, Codigo = CodigoForca.Aerea, Nome = "Força Aérea" },
            new Forca { Id = 3, Codigo = CodigoForca.Exercito, Nome = "Exército" });

        mb.Entity<Categoria>().HasData(
            new Categoria { Id = 1, ForcaId = 1, Codigo = "NAVIO", Nome = "Navio" },
            new Categoria { Id = 2, ForcaId = 2, Codigo = "AERONAVE", Nome = "Aeronave" },
            new Categoria { Id = 3, ForcaId = 3, Codigo = "TANQUE", Nome = "Tanque" });

        // Nomes de colunas em snake_case (iguais ao DDL do design.md)
        // e exclusão restrita em todas as chaves estrangeiras.
        foreach (var entidade in mb.Model.GetEntityTypes())
        {
            foreach (var propriedade in entidade.GetProperties())
                propriedade.SetColumnName(ParaSnake(propriedade.Name));

            foreach (var fk in entidade.GetForeignKeys())
                fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    private static string ParaSnake(string nome) =>
        Regex.Replace(nome, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant();

    /// <summary>Grava o enum em texto maiúsculo (ex.: ATIVO), como nos CHECKs do banco.</summary>
    private static ValueConverter<T, string> Maiusculo<T>() where T : struct, Enum =>
        new(v => v.ToString().ToUpperInvariant(), s => Enum.Parse<T>(s, true));
}
