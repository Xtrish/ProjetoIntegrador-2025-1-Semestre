using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AtestadoMedico.Data;
using AtestadoMedico.Models;

namespace AtestadoMedico.Importacao
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Iniciando importação de dados...");

            // Configurar o contexto do SQLite
            var sqliteOptionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            sqliteOptionsBuilder.UseSqlite("Data Source=AtestadoMedico.db");
            using var sqliteContext = new ApplicationDbContext(sqliteOptionsBuilder.Options);

            // Configurar o contexto do PostgreSQL
            var pgOptionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            var password = Environment.GetEnvironmentVariable("PGPASSWORD") ?? "well";
            var connStr = $"Host=localhost;Database=AtestadoMedicoDB;Username=postgres;Password={password}";
            Console.WriteLine($"Usando string de conexão: {connStr}");
            pgOptionsBuilder.UseNpgsql(connStr);
            using var pgContext = new ApplicationDbContext(pgOptionsBuilder.Options);

            try
            {
                // Limpar tabelas do PostgreSQL
                pgContext.Atestados.RemoveRange(pgContext.Atestados);
                pgContext.Usuarios.RemoveRange(pgContext.Usuarios);
                pgContext.SaveChanges();

                Console.WriteLine("Importando usuários...");
                var usuarios = sqliteContext.Usuarios.ToList();
                foreach (var usuario in usuarios)
                {
                    pgContext.Usuarios.Add(new Usuario
                    {
                        Id = usuario.Id,
                        Nome = usuario.Nome,
                        Email = usuario.Email,
                        Senha = usuario.Senha,
                        IsAdmin = usuario.IsAdmin,
                        DataCadastro = DateTime.SpecifyKind(usuario.DataCadastro, DateTimeKind.Utc)
                    });
                }
                pgContext.SaveChanges();
                Console.WriteLine($"Importados {usuarios.Count} usuários.");

                Console.WriteLine("Importando atestados...");
                var atestados = sqliteContext.Atestados.ToList();
                foreach (var atestado in atestados)
                {
                    pgContext.Atestados.Add(new Atestado
                    {
                        Id = atestado.Id,
                        UsuarioId = atestado.UsuarioId,
                        DataAtestado = DateTime.SpecifyKind(atestado.DataAtestado, DateTimeKind.Utc),
                        NomeMedico = atestado.NomeMedico,
                        CRM = atestado.CRM,
                        Descricao = atestado.Descricao,
                        DataCadastro = DateTime.SpecifyKind(atestado.DataCadastro, DateTimeKind.Utc),
                        Status = atestado.Status,
                        AtualizadoPor = atestado.AtualizadoPor,
                        DataAtualizacao = atestado.DataAtualizacao.HasValue ? 
                            DateTime.SpecifyKind(atestado.DataAtualizacao.Value, DateTimeKind.Utc) : (DateTime?)null,
                        NomeArquivo = atestado.NomeArquivo,
                        TipoArquivo = atestado.TipoArquivo,
                        CaminhoArquivo = atestado.CaminhoArquivo
                    });
                }
                pgContext.SaveChanges();
                Console.WriteLine($"Importados {atestados.Count} atestados.");

                Console.WriteLine("Importação concluída com sucesso!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro durante a importação: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}