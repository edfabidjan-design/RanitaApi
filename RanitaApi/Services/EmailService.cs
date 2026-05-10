using RanitaApi.Models;

public async Task SendOrderStatusUpdateAsync(string clientEmail, string clientName, int orderId, string newStatus, List<OrderItem>? items = null)
{
    var (color, message) = newStatus switch
    {
        "Validée" => ("#3b82f6", "Votre commande a ete validee et est en cours de preparation."),
        "Livrée" => ("#10b981", "Votre commande a ete livree. Merci pour votre achat !"),
        "Annulée" => ("#ef4444", "Votre commande a ete annulee. Contactez-nous pour plus d'informations."),
        _ => ("#f59e0b", "Le statut de votre commande a ete mis a jour.")
    };

    var avisSection = "";
    if (newStatus == "Livrée" && items != null && items.Count > 0)
    {
        var produitsHtml = string.Join("", items.Select(i => $@"
        <div style='display:flex;align-items:center;gap:12px;margin-bottom:10px;padding:10px;background:#f9fafb;border-radius:10px;'>
            <img src='{i.ImageUrl}' style='width:50px;height:50px;object-fit:cover;border-radius:8px;'>
            <div style='flex:1;font-size:14px;font-weight:600;color:#111827;'>{i.ProductName}</div>
            <a href='https://www.ranita-shop.com/review.html?orderId={orderId}&productId={i.ProductId}'
               style='background:#f97316;color:white;padding:8px 14px;border-radius:8px;text-decoration:none;font-weight:700;font-size:13px;white-space:nowrap;'>
                ⭐ Avis
            </a>
        </div>"));

        avisSection = $@"
        <div style='margin-top:24px;padding-top:16px;border-top:1px solid #f3f4f6;'>
            <p style='font-weight:700;font-size:15px;color:#111827;margin:0 0 8px;'>Donnez votre avis !</p>
            <p style='color:#6b7280;font-size:13px;margin:0 0 14px;'>Votre avis aide d'autres clients a faire le bon choix.</p>
            {produitsHtml}
        </div>";
    }

    var payload = new
    {
        replyTo = new { email = "ranitabouda@gmail.com", name = "Ranita Market" },
        sender = new { name = _config["Brevo:SenderName"], email = _config["Brevo:SenderEmail"] },
        to = new[] { new { email = clientEmail } },
        subject = $"📦 Commande #{orderId} - {newStatus}",
        htmlContent = $@"
<div style='font-family:Arial;padding:20px;background:#f4f4f4'>
  <div style='max-width:500px;margin:auto;background:white;padding:24px;border-radius:10px;'>
    <h2 style='color:{color};margin:0 0 16px'>Statut de votre commande mis a jour</h2>
    <p>Bonjour <strong>{clientName}</strong>,</p>
    <p>{message}</p>
    <div style='background:#f9fafb;border-radius:8px;padding:16px;margin:16px 0;text-align:center;'>
        <div style='font-size:13px;color:#6b7280;'>Commande</div>
        <div style='font-size:28px;font-weight:800;color:#111827;'>#{orderId}</div>
        <div style='display:inline-block;margin-top:8px;background:{color};color:white;padding:6px 16px;border-radius:999px;font-weight:700;font-size:14px;'>{newStatus}</div>
    </div>
    <a href='https://www.ranita-shop.com/client-orders.html'
       style='display:inline-block;margin-top:8px;background:#059669;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:700;font-size:15px;'>
      Voir mes commandes
    </a>
    {avisSection}
    {FOOTER}
  </div>
</div>"
    };
    await SendBrevoEmail(payload);
}