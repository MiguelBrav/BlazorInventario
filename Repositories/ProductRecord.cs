using System;
using System.ComponentModel.DataAnnotations;

namespace BlazorInventario.Repositories
{
    public class ProductRecord
    {
        public int id { get; set; }
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(200, ErrorMessage = "El nombre no puede superar los 200 caracteres")]
        public string? name { get; set; }
        public int? category_id { get; set; }
        [Range(0, int.MaxValue, ErrorMessage = "El stock actual no puede ser negativo")]
        public int stock_current { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El stock mínimo no puede ser negativo")]
        public int stock_minimum { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El costo promedio no puede ser negativo")]
        public decimal average_cost { get; set; }
        // logical deletion flag
        public bool is_deleted { get; set; }
        public string? status { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
    }
}
