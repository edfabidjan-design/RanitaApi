using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;
using RanitaApi.Models;
using WebPush;

namespace RanitaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private const string PublicKey = "BK0OMo2QWE4SuKh0RTa6yvHfpkBXcPzL5sZkaJe3nNLesXQjRDhMzyimA8UNBCGvB9AOYpv_Q0RQrmgmA9YdNdY";
    private const string PrivateKey = "lBGZ5H6iym-tYNbvfp-XOhNIFhDbdLO1Qjq6WqtBVLs";
    private const string Subject = "mailto:admin@ranita-shop.com";

    public NotificationsController(AppDbContext db) => _db = db;

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionModel sub)
    {
        var exists = await _db.PushSubscriptions
            .AnyAsync(s => s.Endpoint == sub.Endpoint);
        if (!exists)
        {
            _db.PushSubscriptions.Add(sub);
            await _db.SaveChangesAsync();
        }
        return Ok();
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] string message)
    {
        var subs = await _db.PushSubscriptions.ToListAsync();
        var client = new WebPushClient();
        foreach (var s in subs)
        {
            try
            {
                var sub = new WebPush.PushSubscription(s.Endpoint, s.P256dh, s.Auth);
                var vapid = new VapidDetails(Subject, PublicKey, PrivateKey);
                await client.SendNotificationAsync(sub, message, vapid);
            }
            catch { }
        }
        return Ok();
    }


    [HttpPost("subscribe-client")]
    public async Task<IActionResult> SubscribeClient([FromBody] ClientPushSubscription sub)
    {
        var exists = await _db.ClientPushSubscriptions
            .AnyAsync(s => s.Endpoint == sub.Endpoint);
        if (!exists)
        {
            _db.ClientPushSubscriptions.Add(sub);
            await _db.SaveChangesAsync();
        }
        return Ok();
    }
}