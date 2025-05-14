using System.ComponentModel.DataAnnotations;

namespace AtestadoMedico.ViewModels
{
    public class AtualizarStatusViewModel
    {
        [Required]
        public int UsuarioId { get; set; }
        
        [Required]
        public string Status { get; set; }
        
        public string? MotivoRejeicao { get; set; }
    }
} 