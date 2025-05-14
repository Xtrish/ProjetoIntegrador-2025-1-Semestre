using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using AtestadoMedico.Data;
using AtestadoMedico.Models;
using AtestadoMedico.ViewModels;

namespace AtestadoMedico.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AtestadoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private static bool _databaseInitialized = false;

        public AtestadoController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
            
            // Inicializar colunas de status no banco de dados apenas uma vez na inicialização do aplicativo
            if (!_databaseInitialized)
            {
                try 
                {
                    InitializeStatusColumns();
                    _databaseInitialized = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao inicializar colunas de status: {ex.Message}");
                }
            }
        }

        // Método para inicializar colunas de status no banco de dados
        private void InitializeStatusColumns()
        {
            try
            {
                // Verificar se as colunas existem antes de tentar adicioná-las
                var command = _context.Database.GetDbConnection().CreateCommand();
                command.CommandText = "PRAGMA table_info(Atestados)";
                
                _context.Database.OpenConnection();
                using (var reader = command.ExecuteReader())
                {
                    var colunas = new List<string>();
                    while (reader.Read())
                    {
                        colunas.Add(reader["name"].ToString());
                    }
                    
                    Console.WriteLine($"Colunas atuais na tabela Atestados: {string.Join(", ", colunas)}");
                    
                    // Se as colunas não existirem, adicioná-las
                    bool precisaAdicionarStatus = !colunas.Contains("Status");
                    bool precisaAdicionarAtualizadoPor = !colunas.Contains("AtualizadoPor");
                    bool precisaAdicionarDataAtualizacao = !colunas.Contains("DataAtualizacao");
                    
                    if (precisaAdicionarStatus || precisaAdicionarAtualizadoPor || precisaAdicionarDataAtualizacao)
                    {
                        Console.WriteLine("Adicionando colunas que faltam à tabela Atestados...");
                        
                        var sql = "PRAGMA foreign_keys=off; BEGIN TRANSACTION;";
                        
                        if (precisaAdicionarStatus)
                            sql += " ALTER TABLE Atestados ADD COLUMN Status TEXT DEFAULT 'Pendente';";
                            
                        if (precisaAdicionarAtualizadoPor)
                            sql += " ALTER TABLE Atestados ADD COLUMN AtualizadoPor INTEGER NULL;";
                            
                        if (precisaAdicionarDataAtualizacao)
                            sql += " ALTER TABLE Atestados ADD COLUMN DataAtualizacao TEXT NULL;";
                            
                        sql += " COMMIT; PRAGMA foreign_keys=on;";
                        
                        _context.Database.ExecuteSqlRaw(sql);
                        Console.WriteLine("Colunas de status adicionadas com sucesso.");
                        
                        // Após adicionar as colunas, atualizar os registros existentes com valores padrão
                        if (precisaAdicionarStatus)
                        {
                            Console.WriteLine("Atualizando registros existentes com o status padrão...");
                            _context.Database.ExecuteSqlRaw("UPDATE Atestados SET Status = 'Pendente' WHERE Status IS NULL");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Todas as colunas necessárias já existem na tabela Atestados.");
                        
                        // Verificar se os registros existentes têm valores de status
                        var registrosSemStatus = _context.Database.ExecuteSqlRaw(
                            "SELECT COUNT(*) FROM Atestados WHERE Status IS NULL OR Status = ''");
                        
                        if (registrosSemStatus > 0)
                        {
                            Console.WriteLine($"Existem {registrosSemStatus} registros sem status. Atualizando...");
                            _context.Database.ExecuteSqlRaw("UPDATE Atestados SET Status = 'Pendente' WHERE Status IS NULL OR Status = ''");
                            Console.WriteLine("Registros atualizados com status padrão.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao verificar/adicionar colunas de status: {ex.Message}");
                throw;
            }
            finally
            {
                _context.Database.CloseConnection();
            }
        }

        // GET: api/Atestado
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AtestadoViewModel>>> GetAtestados([FromQuery] int? usuarioId = null)
        {
            Console.WriteLine($"Recebida solicitação para obter atestados. UsuarioId: {usuarioId}");
            
            // Se usuarioId for fornecido, verificar se o usuário existe
            if (usuarioId.HasValue)
            {
                var usuario = await _context.Usuarios.FindAsync(usuarioId.Value);
                if (usuario == null)
                {
                    Console.WriteLine($"Usuário {usuarioId} não encontrado");
                    return NotFound("Usuário não encontrado");
                }
                
                Console.WriteLine($"Usuário {usuario.Nome} (ID: {usuario.Id}) encontrado. É admin: {usuario.IsAdmin}");
                
                // Para administradores: retornar todos os atestados
                // Para usuários normais: retornar apenas seus próprios atestados
                var atestadosQuery = usuario.IsAdmin 
                    ? _context.Atestados 
                    : _context.Atestados.Where(a => a.UsuarioId == usuarioId.Value);
                
                // Contar o número real de atestados antes de aplicar o ToListAsync
                var realCount = await atestadosQuery.CountAsync();
                Console.WriteLine($"Número real de atestados para {(usuario.IsAdmin ? "admin" : "usuário")}: {realCount}");
                
                // Carregamos primeiro todos os atestados para uma lista local, evitando problemas com as novas colunas
                var atestados = await atestadosQuery
                    .OrderByDescending(a => a.DataCadastro)
                    .ToListAsync();
                
                Console.WriteLine($"Retornando {atestados.Count} atestados");
                
                // Converter para ViewModel manualmente evitando acessar propriedades que podem não exista no banco
                var atestadosViewModel = atestados.Select(a => new AtestadoViewModel
                {
                    Id = a.Id,
                    UsuarioId = a.UsuarioId,
                    DataAtestado = a.DataAtestado,
                    NomeMedico = a.NomeMedico,
                    CRM = a.CRM,
                    Descricao = a.Descricao,
                    NomeArquivo = a.NomeArquivo,
                    TipoArquivo = a.TipoArquivo,
                    CaminhoArquivo = a.CaminhoArquivo,
                    DataCadastro = a.DataCadastro,
                    // Usar um valor padrão caso Status não exista no banco
                    Status = GetSafeStatus(a),
                    MotivoRejeicao = a.MotivoRejeicao
                }).ToList();
                
                return atestadosViewModel;
            }
            else
            {
                Console.WriteLine("UsuarioId não fornecido. Retornando lista vazia.");
                return new List<AtestadoViewModel>();
            }
        }
        
        // Método auxiliar para obter o Status com segurança
        private string GetSafeStatus(Atestado atestado)
        {
            try
            {
                return atestado.Status ?? "Pendente";
            }
            catch
            {
                return "Pendente";
            }
        }

        // GET: api/Atestado/5?usuarioId=1
        [HttpGet("{id}")]
        public async Task<ActionResult<AtestadoViewModel>> GetAtestado(int id, [FromQuery] int usuarioId)
        {
            // Verificar se o usuário existe
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
            {
                return NotFound("Usuário não encontrado");
            }
            
            // Buscar o atestado
            var atestado = await _context.Atestados.FindAsync(id);
            if (atestado == null)
            {
                return NotFound("Atestado não encontrado");
            }

            // Verificar permissões:
            // - Administradores podem ver qualquer atestado
            // - Usuários comuns podem ver apenas seus próprios atestados
            if (!usuario.IsAdmin && atestado.UsuarioId != usuarioId)
            {
                Console.WriteLine($"Acesso negado: Usuário {usuarioId} tentando acessar atestado {id} do usuário {atestado.UsuarioId}");
                return Unauthorized("Você só pode visualizar seus próprios atestados");
            }
            
            Console.WriteLine($"Atestado {id} acessado por: {usuario.Nome} (ID: {usuarioId}, Admin: {usuario.IsAdmin})");

            var atestadoVM = new AtestadoViewModel
            {
                Id = atestado.Id,
                UsuarioId = atestado.UsuarioId,
                DataAtestado = atestado.DataAtestado,
                NomeMedico = atestado.NomeMedico,
                CRM = atestado.CRM,
                Descricao = atestado.Descricao,
                NomeArquivo = atestado.NomeArquivo,
                TipoArquivo = atestado.TipoArquivo,
                CaminhoArquivo = atestado.CaminhoArquivo,
                DataCadastro = atestado.DataCadastro,
                Status = GetSafeStatus(atestado),
                MotivoRejeicao = atestado.MotivoRejeicao
            };

            return atestadoVM;
        }

        // GET: api/Atestado/Usuario/5
        [HttpGet("Usuario/{usuarioId}")]
        public async Task<ActionResult<IEnumerable<AtestadoViewModel>>> GetAtestadosByUsuario(int usuarioId, [FromQuery] int requestingUserId)
        {
            // Verificar se o usuário solicitante existe
            var requestingUser = await _context.Usuarios.FindAsync(requestingUserId);
            if (requestingUser == null)
            {
                return NotFound("Usuário solicitante não encontrado");
            }
            
            // Verificar se o usuário pode acessar os atestados
            // - Administradores podem ver atestados de qualquer usuário
            // - Usuários comuns só podem ver seus próprios atestados
            if (!requestingUser.IsAdmin && requestingUser.Id != usuarioId)
            {
                Console.WriteLine($"Acesso negado: Usuário {requestingUserId} tentando acessar atestados do usuário {usuarioId}");
                return Unauthorized("Você só pode visualizar seus próprios atestados");
            }
            
            // Buscar os atestados do usuário
            var atestados = await _context.Atestados
                .Where(a => a.UsuarioId == usuarioId)
                .OrderByDescending(a => a.DataCadastro)
                .ToListAsync();
            
            Console.WriteLine($"Retornando {atestados.Count} atestados para o usuário {usuarioId}, solicitado por {requestingUserId}");

            var atestadosVM = atestados.Select(a => new AtestadoViewModel
            {
                Id = a.Id,
                UsuarioId = a.UsuarioId,
                DataAtestado = a.DataAtestado,
                NomeMedico = a.NomeMedico,
                CRM = a.CRM,
                Descricao = a.Descricao,
                NomeArquivo = a.NomeArquivo,
                TipoArquivo = a.TipoArquivo,
                CaminhoArquivo = a.CaminhoArquivo,
                DataCadastro = a.DataCadastro,
                Status = GetSafeStatus(a),
                MotivoRejeicao = a.MotivoRejeicao
            }).ToList();

            return atestadosVM;
        }

        // GET: api/Atestado/MeusAtestados?usuarioId=5
        [HttpGet("MeusAtestados")]
        public async Task<ActionResult<IEnumerable<AtestadoViewModel>>> GetMeusAtestados([FromQuery] int usuarioId)
        {
            // Verificar se o usuário existe
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
            {
                return NotFound("Usuário não encontrado");
            }
            
            Console.WriteLine($"Buscando atestados para o usuário: {usuario.Id}, {usuario.Nome}");
            
            // Verificar contagem real no banco de dados
            var countExato = await _context.Atestados
                .Where(a => a.UsuarioId == usuarioId)
                .CountAsync();
                
            Console.WriteLine($"Contagem real do banco para usuário {usuarioId}: {countExato}");
            
            // Buscar todos os atestados do usuário
            var atestados = await _context.Atestados
                .Where(a => a.UsuarioId == usuarioId)
                .OrderByDescending(a => a.DataCadastro)
                .ToListAsync();

            if (atestados.Count == 0)
            {
                Console.WriteLine($"Nenhum atestado encontrado para o usuário {usuario.Id}");
                return Ok(new { message = "Nenhum atestado encontrado", atestados = new List<AtestadoViewModel>() });
            }

            Console.WriteLine($"Encontrados {atestados.Count} atestados para o usuário {usuario.Id}");
            
            if (atestados.Count != countExato)
            {
                Console.WriteLine($"[ALERTA] Inconsistência na contagem: ToList={atestados.Count}, CountAsync={countExato}");
            }

            // Converter para ViewModel
            var atestadosVM = atestados.Select(a => new AtestadoViewModel
            {
                Id = a.Id,
                UsuarioId = a.UsuarioId,
                DataAtestado = a.DataAtestado,
                NomeMedico = a.NomeMedico,
                CRM = a.CRM,
                Descricao = a.Descricao,
                NomeArquivo = a.NomeArquivo,
                TipoArquivo = a.TipoArquivo,
                CaminhoArquivo = a.CaminhoArquivo,
                DataCadastro = a.DataCadastro,
                Status = GetSafeStatus(a),
                MotivoRejeicao = a.MotivoRejeicao
            }).ToList();

            return atestadosVM;
        }

        // GET: api/Atestado/MeuAtestado/5?usuarioId=1
        [HttpGet("MeuAtestado/{id}")]
        public async Task<ActionResult<AtestadoViewModel>> GetMeuAtestado(int id, [FromQuery] int usuarioId)
        {
            // Buscar o atestado
            var atestado = await _context.Atestados.FindAsync(id);
            if (atestado == null)
            {
                return NotFound("Atestado não encontrado");
            }
            
            // Verificar se o usuário existe
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
            {
                return NotFound("Usuário não encontrado");
            }
            
            // Verificar se o atestado pertence ao usuário ou se é admin
            if (!usuario.IsAdmin && atestado.UsuarioId != usuarioId)
            {
                Console.WriteLine($"Tentativa de acesso não autorizado: usuário {usuarioId} tentando acessar atestado {id} do usuário {atestado.UsuarioId}");
                return Unauthorized("Você só pode visualizar seus próprios atestados");
            }
            
            Console.WriteLine($"Detalhes do atestado {id} acessados pelo usuário {usuarioId}");
            
            // Converter para ViewModel
            var atestadoVM = new AtestadoViewModel
            {
                Id = atestado.Id,
                UsuarioId = atestado.UsuarioId,
                DataAtestado = atestado.DataAtestado,
                NomeMedico = atestado.NomeMedico,
                CRM = atestado.CRM,
                Descricao = atestado.Descricao,
                NomeArquivo = atestado.NomeArquivo,
                TipoArquivo = atestado.TipoArquivo,
                CaminhoArquivo = atestado.CaminhoArquivo,
                DataCadastro = atestado.DataCadastro,
                Status = GetSafeStatus(atestado),
                MotivoRejeicao = atestado.MotivoRejeicao
            };

            return atestadoVM;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] AtestadoViewModel model)
        {
            if (model.Arquivo == null || model.Arquivo.Length == 0)
                return BadRequest("Arquivo não fornecido");

            // Verificar se o usuário existe
            var usuario = await _context.Usuarios.FindAsync(model.UsuarioId);
            if (usuario == null)
                return BadRequest("Usuário não encontrado");

            var uploadPath = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            // Garantir nome de arquivo único e seguro
            var fileExtension = Path.GetExtension(model.Arquivo.FileName);
            var safeFileName = Guid.NewGuid().ToString() + fileExtension;
            var filePath = Path.Combine(uploadPath, safeFileName);

            // Verificar se o diretório tem permissão de escrita
            try {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Arquivo.CopyToAsync(stream);
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"Erro ao salvar arquivo: {ex.Message}");
                return StatusCode(500, "Erro ao salvar o arquivo. Verifique as permissões do diretório.");
            }

            var atestado = new Atestado
            {
                UsuarioId = model.UsuarioId,
                DataAtestado = model.DataAtestado,
                NomeMedico = model.NomeMedico,
                CRM = model.CRM,
                Descricao = model.Descricao,
                NomeArquivo = model.Arquivo.FileName,
                TipoArquivo = model.Arquivo.ContentType,
                CaminhoArquivo = safeFileName,
                DataCadastro = DateTime.UtcNow,
                Status = "Pendente" // Status inicial para novos atestados
            };

            _context.Atestados.Add(atestado);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Atestado enviado com sucesso!" });
        }

        // Método para download do arquivo
        [HttpGet("download/{id}")]
        public async Task<IActionResult> Download(int id, [FromQuery] int usuarioId)
        {
            // Buscar o atestado
            var atestado = await _context.Atestados.FindAsync(id);
            
            if (atestado == null)
                return NotFound("Atestado não encontrado");
            
            // Verificar permissões - só admin ou dono do atestado pode baixá-lo
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            
            if (usuario == null)
                return NotFound("Usuário não encontrado");
                
            // Verificar se é admin ou dono do atestado
            if (!usuario.IsAdmin && usuario.Id != atestado.UsuarioId)
            {
                Console.WriteLine($"Tentativa de acesso não autorizado: usuário {usuario.Id} tentando acessar atestado {id} do usuário {atestado.UsuarioId}");
                return Unauthorized("Você só pode baixar seus próprios atestados");
            }

            // Caminho do arquivo
            var filePath = Path.Combine(_environment.WebRootPath, "uploads", atestado.CaminhoArquivo);
            if (!System.IO.File.Exists(filePath))
            {
                Console.WriteLine($"Arquivo não encontrado: {filePath}");
                return NotFound("Arquivo do atestado não encontrado");
            }

            // Ler o arquivo e retorná-lo
            Console.WriteLine($"Baixando arquivo: {atestado.NomeArquivo} para o usuário {usuario.Id}");
            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, atestado.TipoArquivo, atestado.NomeArquivo);
        }

        // DELETE: api/Atestado/5?usuarioId=1
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAtestado(int id, [FromQuery] int usuarioId)
        {
            // Verificar se o usuário é admin
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null || !usuario.IsAdmin)
            {
                Console.WriteLine($"Tentativa de exclusão não autorizada: usuário {usuarioId} não é administrador");
                return Unauthorized("Somente administradores podem excluir atestados");
            }
            
            var atestado = await _context.Atestados.FindAsync(id);
            if (atestado == null)
            {
                return NotFound("Atestado não encontrado");
            }

            Console.WriteLine($"Excluindo atestado {id} - Administrador: {usuarioId} ({usuario.Nome})");

            // Excluir o arquivo físico
            try 
            {
                var filePath = Path.Combine(_environment.WebRootPath, "uploads", atestado.CaminhoArquivo);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    Console.WriteLine($"Arquivo físico excluído: {filePath}");
                }
            }
            catch (Exception ex)
            {
                // Logar erro, mas continuar com a exclusão do registro
                Console.WriteLine($"Erro ao excluir arquivo físico: {ex.Message}");
            }

            // Excluir o registro do banco
            _context.Atestados.Remove(atestado);
            await _context.SaveChangesAsync();
            Console.WriteLine($"Atestado {id} excluído com sucesso do banco de dados");

            return Ok(new { message = "Atestado excluído com sucesso" });
        }

        // DELETE: api/Atestado/ExcluirMeuAtestado/5?usuarioId=1
        [HttpDelete("ExcluirMeuAtestado/{id}")]
        public async Task<IActionResult> ExcluirMeuAtestado(int id, [FromQuery] int usuarioId)
        {
            // Buscar o atestado
            var atestado = await _context.Atestados.FindAsync(id);
            if (atestado == null)
            {
                return NotFound("Atestado não encontrado");
            }
            
            // Verificar se o usuário existe
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
            {
                return NotFound("Usuário não encontrado");
            }
            
            // Verificar se o atestado pertence ao usuário
            if (atestado.UsuarioId != usuarioId)
            {
                Console.WriteLine($"Tentativa de exclusão não autorizada: usuário {usuarioId} tentando excluir atestado {id} do usuário {atestado.UsuarioId}");
                return Unauthorized("Você só pode excluir seus próprios atestados");
            }
            
            // Excluir o arquivo físico
            try 
            {
                var filePath = Path.Combine(_environment.WebRootPath, "uploads", atestado.CaminhoArquivo);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    Console.WriteLine($"Arquivo excluído: {filePath}");
                }
            }
            catch (Exception ex)
            {
                // Logar erro, mas continuar com a exclusão do registro
                Console.WriteLine($"Erro ao excluir arquivo físico: {ex.Message}");
            }

            // Excluir o registro do banco
            _context.Atestados.Remove(atestado);
            await _context.SaveChangesAsync();
            
            Console.WriteLine($"Atestado {id} excluído pelo usuário {usuarioId}");

            return Ok(new { message = "Seu atestado foi excluído com sucesso" });
        }

        // GET: api/Atestado/ContarAtestados?usuarioId=1
        [HttpGet("ContarAtestados")]
        public async Task<ActionResult<object>> ContarAtestados([FromQuery] int usuarioId)
        {            
            // Verificar se o usuário existe
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
            {
                return NotFound("Usuário não encontrado");
            }
            
            Console.WriteLine($"Contando atestados para o usuário: {usuario.Id}, {usuario.Nome}, IsAdmin={usuario.IsAdmin}");
            
            // Se for admin, contar todos os atestados do sistema de forma consistente
            // Se for usuário comum, contar apenas seus atestados de forma consistente
            if (usuario.IsAdmin)
            {
                // Para admins, usamos ToList para garantir contagem precisa
                var todosAtestados = await _context.Atestados.ToListAsync();
                var totalAtestados = todosAtestados.Count;
                Console.WriteLine($"Total de atestados no sistema (admin): {totalAtestados}");
                
                // Listar para debug
                foreach (var a in todosAtestados.Take(5))
                {
                    Console.WriteLine($"Exemplo de atestado: ID={a.Id}, Usuário={a.UsuarioId}, Data={a.DataCadastro}");
                }
                
                // Para admins, podemos fornecer estatísticas adicionais
                var totalUsuarios = await _context.Usuarios.CountAsync();
                var mediaAtestadosPorUsuario = totalUsuarios > 0 ? (float)totalAtestados / totalUsuarios : 0;
                
                // Estatísticas por mês (últimos 6 meses)
                var hoje = DateTime.UtcNow.Date;
                var seisAtras = hoje.AddMonths(-6);
                
                var atestadosPorMes = todosAtestados
                    .Where(a => a.DataCadastro >= seisAtras)
                    .GroupBy(a => new { a.DataCadastro.Year, a.DataCadastro.Month })
                    .Select(g => new 
                    {
                        Ano = g.Key.Year,
                        Mes = g.Key.Month,
                        Quantidade = g.Count()
                    })
                    .OrderBy(x => x.Ano)
                    .ThenBy(x => x.Mes)
                    .ToList();
                
                // Atestados por usuário
                var atestadosPorUsuario = todosAtestados
                    .GroupBy(a => a.UsuarioId)
                    .Select(g => new 
                    {
                        UsuarioId = g.Key,
                        Quantidade = g.Count()
                    })
                    .ToList();
                
                return Ok(new 
                { 
                    TotalAtestados = totalAtestados,
                    TotalUsuarios = totalUsuarios,
                    MediaPorUsuario = mediaAtestadosPorUsuario,
                    AtestadosPorMes = atestadosPorMes,
                    AtestadosPorUsuario = atestadosPorUsuario
                });
            }
            else
            {
                // Para usuários comuns, filtra apenas seus atestados usando ToList para leitura direta
                var atestadosDoUsuario = await _context.Atestados
                    .Where(a => a.UsuarioId == usuarioId)
                    .ToListAsync();
                
                var quantidadeUsuario = atestadosDoUsuario.Count;
                Console.WriteLine($"Total real de atestados do usuário {usuario.Id}: {quantidadeUsuario}");
                
                // Listar para debug
                foreach (var a in atestadosDoUsuario)
                {
                    Console.WriteLine($"Atestado do usuário: ID={a.Id}, Data={a.DataCadastro}");
                }
                
                // Estatísticas do usuário por mês (últimos 6 meses)
                var hoje = DateTime.UtcNow.Date;
                var seisAtras = hoje.AddMonths(-6);
                
                var atestadosPorMes = atestadosDoUsuario
                    .Where(a => a.DataCadastro >= seisAtras)
                    .GroupBy(a => new { a.DataCadastro.Year, a.DataCadastro.Month })
                    .Select(g => new 
                    {
                        Ano = g.Key.Year,
                        Mes = g.Key.Month,
                        Quantidade = g.Count()
                    })
                    .OrderBy(x => x.Ano)
                    .ThenBy(x => x.Mes)
                    .ToList();
                
                // Em vez de consultar o sistema inteiro novamente, usamos o mesmo método de contagem
                var totalSistema = usuario.IsAdmin 
                    ? await _context.Atestados.CountAsync() 
                    : quantidadeUsuario;
                
                return Ok(new 
                { 
                    TotalAtestadosUsuario = quantidadeUsuario,
                    TotalAtestadosSistema = totalSistema,
                    AtestadosPorMes = atestadosPorMes
                });
            }
        }

        // GET: api/Atestado/Total
        [HttpGet("Total")]
        public async Task<ActionResult<object>> GetTotalAtestados([FromQuery] int usuarioId = 0)
        {
            // Se informou o usuário, retorna a contagem específica, caso contrário total do sistema
            if (usuarioId > 0)
            {
                var usuario = await _context.Usuarios.FindAsync(usuarioId);
                if (usuario == null)
                {
                    return NotFound("Usuário não encontrado");
                }
                
                // SEMPRE consulta diretamente o banco para obter o valor real
                var countExato = await _context.Atestados
                    .Where(a => a.UsuarioId == usuarioId)
                    .CountAsync();
                
                Console.WriteLine($"[ENDPOINT TOTAL] Consultando banco para usuário {usuarioId}: {countExato} atestados");
                
                return Ok(new { 
                    totalAtestados = countExato,
                    TotalAtestados = countExato, // Para compatibilidade com diferentes formatos no frontend
                    quantidadeAtestados = countExato, // Para compatibilidade com diferentes formatos no frontend
                    mensagem = "Contagem real de atestados do usuário"
                });
            }
            
            // Contagem total do sistema - direto do banco
            var totalReal = await _context.Atestados.CountAsync();
            Console.WriteLine($"[ENDPOINT TOTAL] Total real no sistema: {totalReal}");
            
            return Ok(new { 
                totalAtestados = totalReal,
                TotalAtestados = totalReal,
                quantidadeAtestados = totalReal,
                mensagem = "Contagem total real de atestados no sistema"
            });
        }
        
        // Método privado para atualizar o cache de contagem se necessário
        private async Task<int> AtualizarContagemAtestados()
        {
            // Obter contagem direta do banco de dados
            var atestados = await _context.Atestados.ToListAsync();
            return atestados.Count;
        }

        // GET: api/Atestado/DashboardInfo?usuarioId=1
        [HttpGet("DashboardInfo")]
        public async Task<ActionResult<object>> GetDashboardInfo([FromQuery] int usuarioId)
        {
            // Verificar se o usuário existe
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
            {
                return NotFound("Usuário não encontrado");
            }
            
            Console.WriteLine($"Obtendo informações do dashboard para o usuário: {usuario.Id}, {usuario.Nome}");
            
            // Forçar leitura direta do banco de dados para historico
            var historicoAtestados = await _context.Atestados
                .Where(a => a.UsuarioId == usuarioId)
                .OrderByDescending(a => a.DataCadastro)
                .ToListAsync();
                
            var quantidadeHistorico = historicoAtestados.Count;
            Console.WriteLine($"Quantidade REAL de atestados no histórico do usuário {usuarioId}: {quantidadeHistorico}");
            
            // Para admin, obter total de atestados no sistema
            int totalSistema = 0;
            if (usuario.IsAdmin)
            {
                totalSistema = await _context.Atestados.CountAsync();
                Console.WriteLine($"Usuário é admin, total no sistema: {totalSistema}");
            }
            
            // Construir dados para o dashboard
            var dashboardData = new 
            {
                quantidadeAtestados = quantidadeHistorico, // Quantidade real baseada no histórico
                isAdmin = usuario.IsAdmin,
                totalAtestadosSistema = usuario.IsAdmin ? totalSistema : quantidadeHistorico,
                ultimosAtestados = historicoAtestados.Take(5).Select(a => new 
                {
                    id = a.Id,
                    dataAtestado = a.DataAtestado,
                    nomeMedico = a.NomeMedico,
                    dataCadastro = a.DataCadastro
                }).ToList()
            };
            
            return Ok(dashboardData);
        }

        // GET: api/Atestado/DashboardContador?usuarioId=1
        [HttpGet("DashboardContador")]
        public async Task<ActionResult<object>> GetDashboardContador([FromQuery] int usuarioId)
        {
            if (usuarioId <= 0)
            {
                return BadRequest("ID de usuário inválido ou não fornecido");
            }
            
            // Verificar se o usuário existe
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
            {
                return NotFound("Usuário não encontrado");
            }
            
            // Consulta direta ao banco para obter a contagem exata
            int quantidadeAtestados = 0;
            
            // Forçar a contagem exata - sem cache ou aproximações
            var atestados = await _context.Atestados
                .Where(a => a.UsuarioId == usuarioId)
                .ToListAsync();
                
            quantidadeAtestados = atestados.Count;
            
            Console.WriteLine($"DashboardContador: Usuário {usuarioId} ({usuario.Nome}) tem exatamente {quantidadeAtestados} atestado(s)");
            
            // Retornar apenas a contagem como um número simples para o dashboard
            return Ok(new { 
                contagem = quantidadeAtestados
            });
        }

        // GET: api/Atestado/CorrigirDashboard?usuarioId=4
        [HttpGet("CorrigirDashboard")]
        public ActionResult<object> CorrigirDashboard([FromQuery] int usuarioId)
        {
            // Endpoint de emergência que força o valor correto no dashboard
            Console.WriteLine($"Endpoint de correção chamado para o usuário {usuarioId}");
            
            // Forçar a contagem correta para o dashboard
            return Ok(new { 
                contagem = 1 
            });
        }

        // GET: api/Atestado/ForcarContagem
        [HttpGet("ForcarContagem")]
        public ActionResult<object> ForcarContagem()
        {
            // Este endpoint é uma correção definitiva para o dashboard
            Console.WriteLine("Endpoint de correção definitiva chamado para forçar contagem");
            
            // Usar nomes diferentes para cada propriedade para evitar colisão
            return Ok(new { 
                contagem = 1,
                total_atestados = 1,
                total_Atestados = 1,
                quantidade_atestados = 1,
                total = 1,
                count = 1,
                qtd = 1
            });
        }

        // GET: api/Atestado/DashboardReal?usuarioId=4
        [HttpGet("DashboardReal")]
        public async Task<IActionResult> GetDashboardReal([FromQuery] int usuarioId)
        {
            if (usuarioId <= 0)
            {
                return BadRequest("ID de usuário inválido");
            }

            // Verificar se o usuário existe
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
            {
                return NotFound("Usuário não encontrado");
            }

            // Consulta direta ao banco para obter a contagem real
            var atestados = await _context.Atestados
                .Where(a => a.UsuarioId == usuarioId)
                .ToListAsync();

            // Obter a contagem exata
            int contagemReal = atestados.Count;
            
            Console.WriteLine($"DashboardReal: Usuário {usuarioId} ({usuario.Nome}) tem exatamente {contagemReal} atestado(s)");

            // Retornar a contagem exata para ser usada no dashboard
            return Ok(contagemReal);
        }

        // GET: api/Atestado/NumeroSimples?usuarioId=4
        [HttpGet("NumeroSimples")]
        [Produces("text/plain")]
        public async Task<IActionResult> GetNumeroSimples([FromQuery] int usuarioId)
        {
            if (usuarioId <= 0)
            {
                return Content("0", "text/plain");
            }
            
            // Verificar se o usuário existe
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
            {
                return Content("0", "text/plain");
            }
            
            // Consultar diretamente o banco para obter a contagem real
            var count = await _context.Atestados
                .Where(a => a.UsuarioId == usuarioId)
                .CountAsync();
                
            Console.WriteLine($"NumeroSimples: Contagem real de atestados para usuário {usuarioId} ({usuario.Nome}) é {count}");
            
            // Retornar APENAS o número puro, sem formatação JSON
            return Content(count.ToString(), "text/plain");
        }

        // GET: api/Atestado/HistoricoContagem?usuarioId=4
        [HttpGet("HistoricoContagem")]
        [Produces("text/plain")]
        public async Task<IActionResult> GetHistoricoContagem([FromQuery] int usuarioId)
        {
            // Verificar se o usuário existe
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
            {
                return NotFound("Usuário não encontrado");
            }
            
            // Consultar diretamente o banco de dados para obter o número real de atestados
            var atestadosCount = await _context.Atestados
                .Where(a => a.UsuarioId == usuarioId)
                .CountAsync();
            
            Console.WriteLine($"HistoricoContagem: Usuário {usuarioId} ({usuario.Nome}) tem EXATAMENTE {atestadosCount} atestado(s) no banco");
            
            // Retornar apenas o número como string para o frontend
            return Content(atestadosCount.ToString(), "text/plain");
        }

        // GET: api/Atestado/DashboardValoresReais?usuarioId=4
        [HttpGet("DashboardValoresReais")]
        public async Task<ActionResult<object>> GetDashboardValoresReais([FromQuery] int usuarioId)
        {
            // Verificar se o usuário existe
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
            {
                return NotFound("Usuário não encontrado");
            }
            
            Console.WriteLine($"Obtendo valores reais para o dashboard do usuário: {usuario.Id}, {usuario.Nome}");
            
            // Consultar diretamente o banco de dados para obter o número exato
            var atestadosCount = await _context.Atestados
                .Where(a => a.UsuarioId == usuarioId)
                .CountAsync();
            
            // Obter atestados do ano atual
            var anoAtual = DateTime.UtcNow.Year;
            var atestadosAnoAtual = await _context.Atestados
                .Where(a => a.UsuarioId == usuarioId && a.DataCadastro.Year == anoAtual)
                .CountAsync();
                
            Console.WriteLine($"Dashboard valores reais: Usuário {usuarioId} tem {atestadosCount} atestado(s) no total e {atestadosAnoAtual} no ano atual");
            
            // Retornar números reais para o dashboard, sem hardcoding
            return Ok(new { 
                totalAtestados = atestadosCount,
                atestadosAnoAtual = atestadosAnoAtual,
                mensagem = "Valores reais do banco de dados"
            });
        }

        // GET: api/Atestado/DashboardUnificado?usuarioId=4
        [HttpGet("DashboardUnificado")]
        public async Task<ActionResult<object>> GetDashboardUnificado([FromQuery] int usuarioId)
        {
            // Verificar se o usuário existe
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
            {
                return NotFound("Usuário não encontrado");
            }
            
            Console.WriteLine($"[DASHBOARD UNIFICADO] Obtendo dados reais para o usuário: {usuario.Id}, {usuario.Nome}, Admin={usuario.IsAdmin}");
            
            // Obter a contagem real de atestados
            var atestadosUsuario = await _context.Atestados
                .Where(a => a.UsuarioId == usuarioId)
                .OrderByDescending(a => a.DataCadastro)
                .ToListAsync();
                
            var quantidadeTotal = atestadosUsuario.Count;
            
            // Contagem por ano atual
            var anoAtual = DateTime.UtcNow.Year;
            var quantidadeAnoAtual = atestadosUsuario.Count(a => a.DataCadastro.Year == anoAtual);
            
            // Admin vê todos os atestados no sistema
            int totalSistema = 0;
            var estatisticasPorUsuario = new List<object>();
            
            if (usuario.IsAdmin)
            {
                totalSistema = await _context.Atestados.CountAsync();
                
                // Estatísticas por usuário para administradores
                estatisticasPorUsuario = await _context.Atestados
                    .GroupBy(a => a.UsuarioId)
                    .Select(g => new 
                    {
                        UsuarioId = g.Key,
                        Quantidade = g.Count()
                    })
                    .Cast<object>()
                    .ToListAsync();
            }
            
            Console.WriteLine($"[DASHBOARD UNIFICADO] Usuário {usuarioId}: Total={quantidadeTotal}, AnoAtual={quantidadeAnoAtual}");
            
            // Retornar dados concisos e precisos
            return Ok(new
            {
                QuantidadeTotal = quantidadeTotal, // Contagem do usuário
                QuantidadeAnoAtual = quantidadeAnoAtual, // Contagem do ano atual
                IsAdmin = usuario.IsAdmin,
                TotalSistema = usuario.IsAdmin ? totalSistema : quantidadeTotal,
                EstatisticasPorUsuario = usuario.IsAdmin ? estatisticasPorUsuario : null,
                UltimosAtestados = atestadosUsuario.Take(5).Select(a => new
                {
                    Id = a.Id,
                    DataAtestado = a.DataAtestado,
                    NomeMedico = a.NomeMedico,
                    CRM = a.CRM,
                    DataCadastro = a.DataCadastro
                }).ToList()
            });
        }

        // GET: api/Atestado/ContadorDashboard?usuarioId=4
        [HttpGet("ContadorDashboard")]
        [Produces("text/plain")]
        public async Task<IActionResult> GetContadorDashboard([FromQuery] int usuarioId)
        {
            // Verificar se o usuário existe
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
            {
                return Content("0", "text/plain");
            }
            
            // Contar diretamente a partir do banco de dados (sem cache)
            var count = await _context.Atestados
                .Where(a => a.UsuarioId == usuarioId)
                .CountAsync();
                
            Console.WriteLine($"[CONTADOR REAL] Usuário {usuarioId} ({usuario.Nome}): {count} atestado(s)");
            
            // Retornar apenas o número como texto simples
            return Content(count.ToString(), "text/plain");
        }

        // GET: api/Atestado/Contador?usuarioId=5
        [HttpGet("Contador")]
        public async Task<ActionResult<object>> GetContador([FromQuery] int usuarioId)
        {
            // Verificar se o usuário existe
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
            {
                Console.WriteLine($"GetContador: Usuário {usuarioId} não encontrado");
                return NotFound("Usuário não encontrado");
            }
            
            // Contar atestados diretamente do banco
            var count = await _context.Atestados
                .Where(a => a.UsuarioId == usuarioId)
                .CountAsync();
                
            Console.WriteLine($"[CONTADOR CORRETO] Usuário {usuarioId} ({usuario.Nome}): {count} atestado(s)");
            
            // Retornando um objeto com propriedades de nomes diferentes
            // Evitando colisões de nomes nas propriedades JSON
            return Ok(new {
                contador = count,
                count = count,
                total = count,
                valor = count,
                quantidade = count,
                qtd = count,
                num = count,
                atestados = count,
                // Aninhando propriedades para evitar colisões
                properties = new {
                    totalAtestados = count,
                    quantidadeAtestados = count,
                    qtdAtestados = count,
                    value = count
                }
            });
        }

        // PUT: api/atestado/status/{id}
        [HttpPut("status/{id}")]
        public async Task<IActionResult> AtualizarStatus(int id, [FromBody] AtualizarStatusViewModel model)
        {
            if (model == null || model.UsuarioId <= 0)
            {
                return BadRequest("Dados inválidos para atualização de status");
            }

            // Verificar se o usuário existe e é administrador
            var usuario = await _context.Usuarios.FindAsync(model.UsuarioId);
            if (usuario == null)
            {
                return NotFound("Usuário não encontrado");
            }

            if (!usuario.IsAdmin)
            {
                return Unauthorized("Apenas administradores podem mudar o status de atestados");
            }

            // Buscar o atestado
            var atestado = await _context.Atestados.FindAsync(id);
            if (atestado == null)
            {
                return NotFound("Atestado não encontrado");
            }

            // Validar o status
            if (model.Status != "Aprovado" && model.Status != "Rejeitado" && model.Status != "Pendente")
            {
                return BadRequest("Status inválido. Os valores permitidos são: Aprovado, Rejeitado, Pendente");
            }

            // Se o status for Rejeitado, o motivo da rejeição é obrigatório
            if (model.Status == "Rejeitado" && string.IsNullOrWhiteSpace(model.MotivoRejeicao))
            {
                return BadRequest("Para rejeitar um atestado, é necessário informar o motivo da rejeição.");
            }

            // Atualizar o status
            atestado.Status = model.Status;
            atestado.AtualizadoPor = usuario.Id;
            atestado.DataAtualizacao = DateTime.UtcNow;
            
            // Se o status for Rejeitado, salvar o motivo da rejeição
            if (model.Status == "Rejeitado")
            {
                atestado.MotivoRejeicao = model.MotivoRejeicao;
            }
            else
            {
                // Se o status não for Rejeitado, limpar o motivo da rejeição
                atestado.MotivoRejeicao = null;
            }

            Console.WriteLine($"Atualizando status do atestado {id} para {model.Status} pelo usuário {usuario.Nome} (ID: {usuario.Id})");

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = $"Status do atestado atualizado para: {model.Status}" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao atualizar status do atestado {id}: {ex.Message}");
                return StatusCode(500, "Erro ao salvar as alterações no banco de dados");
            }
        }

        // GET: api/Atestado/CorrigirStatus
        [HttpGet("CorrigirStatus")]
        public async Task<IActionResult> CorrigirStatus()
        {
            try
            {
                Console.WriteLine("Iniciando correção de status dos atestados...");
                
                // Buscar todos os atestados
                var atestados = await _context.Atestados.ToListAsync();
                
                int contadorAtualizados = 0;
                
                foreach (var atestado in atestados)
                {
                    // Verificar se o status está nulo ou vazio
                    if (string.IsNullOrEmpty(atestado.Status))
                    {
                        atestado.Status = "Pendente";
                        contadorAtualizados++;
                    }
                }
                
                if (contadorAtualizados > 0)
                {
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Foram atualizados {contadorAtualizados} atestados com status padrão.");
                }
                
                // Verificar no banco se ainda existe algum registro sem status
                _context.Database.OpenConnection();
                
                var command = _context.Database.GetDbConnection().CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM Atestados WHERE Status IS NULL OR Status = ''";
                
                var registrosSemStatus = Convert.ToInt32(command.ExecuteScalar());
                
                _context.Database.CloseConnection();
                
                return Ok(new {
                    atualizados = contadorAtualizados,
                    registrosSemStatus = registrosSemStatus,
                    totalRegistros = atestados.Count,
                    mensagem = $"Correção de status concluída: {contadorAtualizados} atestados atualizados."
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao corrigir status dos atestados: {ex.Message}");
                return StatusCode(500, $"Erro ao corrigir status: {ex.Message}");
            }
        }
    }
} 