using System;
using System.ComponentModel.DataAnnotations;

namespace BlazorInventario.Repositories
{
    public class CategoryRecord
    {
        public int id { get; set; }
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(200, ErrorMessage = "El nombre no puede superar los 200 caracteres")]
        public string? name { get; set; }
        public string? description { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
    }
}
