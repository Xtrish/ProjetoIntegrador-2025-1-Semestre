using System.ComponentModel.DataAnnotations;

namespace AtestadoMedico.Models
{
    public class Usuario
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Senha { get; set; } = string.Empty;
        
        [Required]
        public bool IsAdmin { get; set; } = false;
        
        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;
        
        // Relacionamento com Atestados
        public ICollection<Atestado> Atestados { get; set; } = new List<Atestado>();
    }
} 