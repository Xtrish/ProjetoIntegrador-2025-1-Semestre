using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AtestadoMedico.Data;
using AtestadoMedico.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AtestadoMedico.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsuarioController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Usuario
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuarios()
        {
            return await _context.Usuarios.ToListAsync();
        }

        // GET: api/Usuario/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Usuario>> GetUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound();
            }

            return usuario;
        }

        // POST: api/Usuario
        [HttpPost]
        public async Task<ActionResult<Usuario>> PostUsuario(Usuario usuario)
        {
            // Verificar se o e-mail já existe
            if (await _context.Usuarios.AnyAsync(u => u.Email == usuario.Email))
            {
                return BadRequest("Este e-mail já está em uso.");
            }
            
            // Em um sistema real, a senha deveria ser hasheada antes de ser armazenada
            
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUsuario), new { id = usuario.Id }, usuario);
        }
        
        // Autenticação simplificada (simulação)
        [HttpPost("login")]
        public async Task<ActionResult<Usuario>> Login(LoginModel login)
        {
            // Debug para verificar os dados recebidos
            Console.WriteLine($"Tentativa de login: Email={login.Email}, Senha={login.Senha}");
            
            // Listar todos os usuários para debug
            var todosUsuarios = await _context.Usuarios.ToListAsync();
            foreach (var u in todosUsuarios)
            {
                Console.WriteLine($"Usuário: {u.Id}, {u.Nome}, {u.Email}, {u.Senha}, IsAdmin={u.IsAdmin}");
            }
            
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == login.Email && u.Senha == login.Senha);

            if (usuario == null)
            {
                return Unauthorized("Credenciais inválidas");
            }

            // Em um sistema real, deveria gerar um token JWT aqui
            return Ok(new { usuario.Id, usuario.Nome, usuario.Email, usuario.IsAdmin });
        }
        
        // Esta ação só deve ser acessível por administradores
        [HttpPost("cadastrar")]
        public async Task<ActionResult<Usuario>> CadastrarUsuario([FromBody] Usuario usuario, [FromQuery] int adminId)
        {
            // Verificar se quem está cadastrando é um administrador
            var admin = await _context.Usuarios.FindAsync(adminId);
            if (admin == null || !admin.IsAdmin)
            {
                return Forbid("Somente administradores podem cadastrar novos usuários");
            }
            
            // Verificar se o e-mail já existe
            if (await _context.Usuarios.AnyAsync(u => u.Email == usuario.Email))
            {
                return BadRequest("Este e-mail já está em uso.");
            }
            
            // Em um sistema real, a senha deveria ser hasheada antes de ser armazenada
            
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUsuario), new { id = usuario.Id }, usuario);
        }

        // PUT: api/Usuario/AlterarSenha
        [HttpPut("AlterarSenha")]
        public async Task<IActionResult> AlterarSenha([FromBody] AlterarSenhaModel model)
        {
            // Verificar se o solicitante é admin
            var admin = await _context.Usuarios.FindAsync(model.AdminId);
            if (admin == null || !admin.IsAdmin)
            {
                return Forbid("Somente administradores podem alterar senhas");
            }

            // Buscar o usuário para alterar a senha
            var usuario = await _context.Usuarios.FindAsync(model.UsuarioId);
            if (usuario == null)
            {
                return NotFound("Usuário não encontrado");
            }

            // Atualizar a senha
            usuario.Senha = model.NovaSenha;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Senha alterada com sucesso!" });
        }
        
        // DELETE: api/Usuario/ExcluirUsuario
        [HttpDelete("ExcluirUsuario")]
        public async Task<IActionResult> ExcluirUsuario([FromBody] ExcluirUsuarioModel model)
        {
            // Verificar se o solicitante é admin
            var admin = await _context.Usuarios.FindAsync(model.AdminId);
            if (admin == null || !admin.IsAdmin)
            {
                return Forbid("Somente administradores podem excluir usuários");
            }

            // Buscar o usuário a ser excluído
            var usuario = await _context.Usuarios.FindAsync(model.UsuarioId);
            if (usuario == null)
            {
                return NotFound("Usuário não encontrado");
            }
            
            // Não permitir excluir o próprio administrador
            if (usuario.Id == model.AdminId)
            {
                return BadRequest("Não é possível excluir seu próprio usuário");
            }
            
            // Não permitir excluir o usuário administrador principal (admin@admin.com)
            if (usuario.Email == "admin@admin.com")
            {
                return BadRequest("Não é possível excluir o administrador principal do sistema");
            }
            
            // Verificar se o usuário possui atestados
            var possuiAtestados = await _context.Atestados.AnyAsync(a => a.UsuarioId == model.UsuarioId);
            if (possuiAtestados && !model.ForcarExclusao)
            {
                return BadRequest(new { 
                    message = "Este usuário possui atestados registrados. Deseja excluir mesmo assim?",
                    requiresForce = true
                });
            }
            
            try
            {
                // Se forçar exclusão, remover também os atestados do usuário
                if (model.ForcarExclusao)
                {
                    var atestados = await _context.Atestados.Where(a => a.UsuarioId == model.UsuarioId).ToListAsync();
                    _context.Atestados.RemoveRange(atestados);
                }
                
                // Remover o usuário
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();
                
                return Ok(new { message = "Usuário excluído com sucesso!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro ao excluir usuário: {ex.Message}");
            }
        }
    }

    public class LoginModel
    {
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
    }

    public class AlterarSenhaModel
    {
        public int AdminId { get; set; }
        public int UsuarioId { get; set; }
        public string NovaSenha { get; set; } = string.Empty;
    }
    
    public class ExcluirUsuarioModel
    {
        public int AdminId { get; set; }
        public int UsuarioId { get; set; }
        public bool ForcarExclusao { get; set; } = false;
    }
} 