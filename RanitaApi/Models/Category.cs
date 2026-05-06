using System.Text.Json.Serialization;

namespace RanitaApi.Models
{
    public class Category
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public int? ParentId { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Category? Parent { get; set; }

        public List<Category> Children { get; set; } = new();

        [JsonIgnore]
        public List<Product> Products { get; set; } = new();
    }
}