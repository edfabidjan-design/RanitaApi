namespace RanitaApi.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        // ── NOUVEAUX CHAMPS ──
        public string Email { get; set; } = string.Empty;

        // "SuperAdmin" | "GestionnaireCommandes" | "GestionnaireVendeurs"
        // "GestionnaireProduits" | "GestionnairePaiements" | "GestionnaireClients"
        // "ModerateurAvis" | "GestionnaireLivraisons" | "GestionnaireParametres" | "Analyste"
        public string Role { get; set; } = "Analyste";

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
    }
}