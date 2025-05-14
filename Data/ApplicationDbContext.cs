using Microsoft.EntityFrameworkCore;
using AtestadoMedico.Models;

namespace AtestadoMedico.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Atestado> Atestados { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuração do relacionamento entre Atestado e Usuario
            modelBuilder.Entity<Atestado>()
                .HasOne(a => a.Usuario)
                .WithMany(u => u.Atestados)
                .HasForeignKey(a => a.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed de dados iniciais para teste
            modelBuilder.Entity<Usuario>().HasData(
                new Usuario
                {
                    Id = 1,
                    Nome = "Administrador",
                    Email = "admin@admin.com",
                    Senha = "admin",
                    IsAdmin = true,
                    DataCadastro = DateTime.SpecifyKind(new DateTime(2024, 3, 18, 10, 0, 0), DateTimeKind.Utc)
                },
                new Usuario
                {
                    Id = 2,
                    Nome = "Usuário Teste",
                    Email = "usuario@teste.com",
                    Senha = "123456",
                    IsAdmin = false,
                    DataCadastro = DateTime.SpecifyKind(new DateTime(2024, 3, 18, 10, 0, 0), DateTimeKind.Utc)
                },
                new Usuario
                {
                    Id = 3,
                    Nome = "Junior",
                    Email = "junior@gmail.com",
                    Senha = "junior@123",
                    IsAdmin = true,
                    DataCadastro = DateTime.SpecifyKind(new DateTime(2024, 3, 18, 10, 0, 0), DateTimeKind.Utc)
                }
            );
        }
    }
} 