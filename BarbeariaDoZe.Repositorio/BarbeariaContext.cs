using BarbeariaDoZe.Modelos;
using Microsoft.EntityFrameworkCore;

namespace BarbeariaDoZe.Repositorio;

/// <summary>Mapeia as entidades para as tabelas já existentes no SQL Server (database-first).</summary>
public class BarbeariaContext : DbContext
{
    public BarbeariaContext(DbContextOptions<BarbeariaContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<Barbeiro> Barbeiros => Set<Barbeiro>();
    public DbSet<HorarioTrabalho> Horarios => Set<HorarioTrabalho>();
    public DbSet<Agendamento> Agendamentos => Set<Agendamento>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Usuario>(e =>
        {
            e.ToTable("Usuario");
            e.Property(x => x.Id).HasColumnName("ID_Usuario");
            e.Property(x => x.Nome).HasMaxLength(100);
            e.Property(x => x.Telefone).HasMaxLength(11);
            e.Property(x => x.Email).HasMaxLength(255);
            e.Property(x => x.Tipo).HasMaxLength(15);
            e.Property<string>("EmailNormalizado").HasComputedColumnSql("LOWER(LTRIM(RTRIM([Email])))", stored: true);
            e.HasIndex("EmailNormalizado").IsUnique();
            e.Property(x => x.SenhaHash).HasMaxLength(512);
            e.Property(x => x.SecurityStamp).HasMaxLength(32);
            e.Property(x => x.RecuperacaoHash).HasMaxLength(64);
        });

        mb.Entity<Servico>(e =>
        {
            e.ToTable("Servico");
            e.Property(x => x.Id).HasColumnName("ID_Servico");
            e.Property(x => x.Preco).HasColumnType("decimal(7,2)");
        });

        mb.Entity<Barbeiro>(e =>
        {
            e.ToTable("Barbeiro");
            e.Property(x => x.Id).HasColumnName("ID_Barbeiro").ValueGeneratedNever();
            e.Property(x => x.Avaliacao).HasColumnType("decimal(2,1)");
            e.Property(x => x.DataAdmissao).HasColumnType("date");
            e.HasOne(x => x.Usuario).WithOne().HasForeignKey<Barbeiro>(x => x.Id);
            e.HasMany(x => x.Horarios).WithOne().HasForeignKey(h => h.BarbeiroId);
        });

        mb.Entity<HorarioTrabalho>(e =>
        {
            e.ToTable("HorarioTrabalho");
            e.Property(x => x.Id).HasColumnName("ID_Horario");
            e.Property(x => x.BarbeiroId).HasColumnName("ID_Barbeiro");
        });

        mb.Entity<Agendamento>(e =>
        {
            // A tabela Agendamento possui a trigger trg_Agendamento_ValidaConflito.
            // Sem HasTrigger, o EF Core 7+ gera um OUTPUT que falha em tabelas com trigger.
            e.ToTable("Agendamento", t => t.HasTrigger("trg_Agendamento_ValidaConflito"));
            e.Property(x => x.Id).HasColumnName("ID");
            e.Property(x => x.UsuarioId).HasColumnName("ID_Usuario");
            e.Property(x => x.BarbeiroId).HasColumnName("ID_Barbeiro");
            e.Property(x => x.ServicoId).HasColumnName("ID_Servico");
            e.Property(x => x.Preco).HasColumnType("decimal(7,2)");
            e.Property(x => x.Status).HasMaxLength(15);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId);
            e.HasOne(x => x.Barbeiro).WithMany().HasForeignKey(x => x.BarbeiroId);
            e.HasOne(x => x.Servico).WithMany().HasForeignKey(x => x.ServicoId);
        });
    }
}
