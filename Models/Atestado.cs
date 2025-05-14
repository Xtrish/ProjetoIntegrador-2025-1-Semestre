using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtestadoMedico.Models
{
    public class Atestado
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public DateTime DataAtestado { get; set; }
        
        [Required]
        [StringLength(100)]
        public string NomeMedico { get; set; } = string.Empty;
        
        [Required]
        [StringLength(20)]
        public string CRM { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Descricao { get; set; }
        
        [Required]
        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;
        
        // Status do atestado
        [StringLength(20)]
        public string Status { get; set; } = "Pendente";
        
        // Motivo da rejeição, quando aplicável
        [StringLength(500)]
        public string? MotivoRejeicao { get; set; }
        
        // Campos de auditoria
        public int? AtualizadoPor { get; set; }
        
        public DateTime? DataAtualizacao { get; set; }
        
        // Campos para o arquivo do atestado
        [Required]
        [StringLength(255)]
        public string NomeArquivo { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string TipoArquivo { get; set; } = string.Empty;
        
        [Required]
        [StringLength(255)]
        public string CaminhoArquivo { get; set; } = string.Empty;
        
        // Relação com o usuário
        [ForeignKey("Usuario")]
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
    }
} 