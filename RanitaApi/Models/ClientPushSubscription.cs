namespace RanitaApi.Models;

public class ClientPushSubscription
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string Endpoint { get; set; } = "";
    public string P256dh { get; set; } = "";
    public string Auth { get; set; } = "";
}