using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BlazorInventario.Repositories
{
    public class MovementRecord : IValidatableObject
    {
        public long id { get; set; }
        public int product_id { get; set; }
        [Required]
        public string? type { get; set; } // 'in' or 'out'
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor que cero")]
        public int quantity { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "El costo unitario no puede ser negativo")]
        public decimal unit_cost { get; set; }
        // cancellation flag
        public bool canceled { get; set; }
        public int? supplier_id { get; set; }
        public DateTime date { get; set; }
        public int? user_id { get; set; }
        // resolved user name for display
        public string? user_name { get; set; }
        public string? notes { get; set; }
        public DateTime created_at { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // product must be selected
            if (product_id <= 0)
            {
                yield return new ValidationResult("Seleccione un producto.", new[] { nameof(product_id) });
            }

            if (quantity <= 0)
            {
                yield return new ValidationResult("La cantidad debe ser mayor que cero.", new[] { nameof(quantity) });
            }

            if (string.IsNullOrEmpty(type) || (type != "in" && type != "out"))
            {
                yield return new ValidationResult("El tipo debe ser 'in' o 'out'.", new[] { nameof(type) });
            }

            if (type == "in")
            {
                if (unit_cost < 0)
                {
                    yield return new ValidationResult("El costo unitario no puede ser negativo.", new[] { nameof(unit_cost) });
                }
            }
        }
    }
}
