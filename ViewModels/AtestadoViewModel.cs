using System;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace AtestadoMedico.ViewModels
{
    public class AtestadoViewModel
    {
        // Propriedades para retorno da API
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public DateTime DataAtestado { get; set; }
        
        [Required]
        public string NomeMedico { get; set; } = string.Empty;
        
        [Required]
        public string CRM { get; set; } = string.Empty;
        
        public string? Descricao { get; set; }
        
        // Propriedade usada apenas no upload
        public IFormFile? Arquivo { get; set; }
        
        // Propriedades adicionais para retorno da API
        public string? NomeArquivo { get; set; }
        public string? TipoArquivo { get; set; }
        public string? CaminhoArquivo { get; set; }
        public DateTime DataCadastro { get; set; }
        public string Status { get; set; } = "Pendente";
        public string? MotivoRejeicao { get; set; }
    }
} 