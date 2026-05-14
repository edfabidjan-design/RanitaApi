using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RanitaApi.Models
{
    /// <summary>
    /// Stocke le taux de commission global et les overrides par catégorie.
    /// Key="global" → taux global (ex: 0.10)
    /// Key="cat_12"  → override pour la catégorie Id=12
    /// </summary>
    public class CommissionSetting
    {
        public int Id { get; set; }

        // "global" ou "cat_{categoryId}"
        [Required]
        public string Key { get; set; } = string.Empty;

        // Label lisible : "Global" ou nom de la catégorie
        public string Label { get; set; } = string.Empty;

        // Taux : 0.10 = 10%
        [Column(TypeName = "numeric(5,4)")]
        public decimal Rate { get; set; } = 0.10m;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}