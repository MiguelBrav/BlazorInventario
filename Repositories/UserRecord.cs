using System.ComponentModel.DataAnnotations;

namespace BlazorInventario.Repositories
{
    public class UserRecord
    {
        public int id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres")]
        public string? name { get; set; }

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El email no tiene un formato válido")]
        [StringLength(255, ErrorMessage = "El email no puede superar los 255 caracteres")]
        public string? email { get; set; }

        public string? password_hash { get; set; }
        public string? role { get; set; }
        public bool is_active { get; set; }
    }
}
