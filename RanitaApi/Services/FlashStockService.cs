using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;

namespace RanitaApi.Services
{
    public class FlashStockService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public FlashStockService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var now = DateTime.UtcNow;

                    var expiredFlashes = await db.FlashSales
                        .Where(f => f.EndDate < now && f.FlashStock > f.FlashStockSold)
                        .ToListAsync(stoppingToken);

                    foreach (var flash in expiredFlashes)
                    {
                        var stockNonVendu = flash.FlashStock - flash.FlashStockSold;

                        if (flash.VariantId.HasValue)
                        {
                            var variant = await db.ProductVariants.FindAsync(flash.VariantId.Value);
                            if (variant != null)
                            {
                                variant.Stock += stockNonVendu;
                                Console.WriteLine($"[FlashService] Variante #{flash.VariantId} +{stockNonVendu} unités restituées");
                            }
                        }
                        else
                        {
                            var product = await db.Products.FindAsync(flash.ProductId);
                            if (product != null)
                            {
                                product.Stock += stockNonVendu;
                                Console.WriteLine($"[FlashService] Produit #{flash.ProductId} +{stockNonVendu} unités restituées");
                            }
                        }

                        // ✅ Marquer comme traité
                        flash.FlashStock = flash.FlashStockSold;
                    }

                    if (expiredFlashes.Any())
                        await db.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FlashService] Erreur: {ex.Message}");
                }

                // ✅ Vérifier toutes les 5 minutes
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}