namespace RanitaApi.Models
{
    public class CategoryAttribute
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public string AttributeName { get; set; } = "";
        public string AttributeType { get; set; } = "text"; // text, number, select
        public string AttributeOptions { get; set; } = ""; // "S,M,L,XL" pour select
    }
}