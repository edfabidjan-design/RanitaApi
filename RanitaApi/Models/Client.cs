namespace RanitaApi.Models
{
    public class Client
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? ResetCode { get; set; }

        public DateTime? ResetCodeExpiresAt { get; set; }
    }
}