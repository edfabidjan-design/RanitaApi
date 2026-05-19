using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;
using RanitaApi.Models;
using RanitaApi.Services;
var builder = WebApplication.CreateBuilder(args);



builder.Services.AddScoped<EmailService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure();
        }
    ));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowRanitaShop", policy =>
    {
        policy.WithOrigins(
            "https://ranita-shop.com",
            "https://www.ranita-shop.com",
            "http://ranita-shop.com",
            "http://www.ranita-shop.com"
        )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

var app = builder.Build();





app.UseHttpsRedirection();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        ctx.Context.Response.Headers["Pragma"] = "no-cache";
        ctx.Context.Response.Headers["Expires"] = "0";
    }
});

app.UseCors("AllowRanitaShop");

app.UseAuthorization();

app.MapControllers();

app.MapGet("/", context =>
{
    context.Response.Redirect("/index.html");
    return Task.CompletedTask;
});


try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    Console.WriteLine("EnsureCreated OK");
}
catch (Exception ex) { Console.WriteLine("EnsureCreated error: " + ex.Message); }


// Test connexion DB au démarrage
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    Console.WriteLine("DB connected and tables created OK");
}
catch (Exception ex) { Console.WriteLine("STARTUP DB ERROR: " + ex.Message); }











// ── ProductVariants table ──────────────────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""ProductVariants"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""ProductId"" INT NOT NULL REFERENCES ""Products""(""Id"") ON DELETE CASCADE,
            ""Combination"" TEXT NOT NULL DEFAULT '',
            ""Stock"" INT NOT NULL DEFAULT 0,
            ""Price"" NUMERIC(18,2) NULL
        );
    ");
}
catch (Exception ex) { Console.WriteLine("ProductVariants error: " + ex.Message); }

// ── Products columns (chaque ALTER séparé pour éviter l'échec silencieux) ──
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""Images"" TEXT NOT NULL DEFAULT '[]';");
}
catch (Exception ex) { Console.WriteLine("Products.Images error: " + ex.Message); }

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""ShortDescription"" TEXT NOT NULL DEFAULT '';");
}
catch (Exception ex) { Console.WriteLine("Products.ShortDescription error: " + ex.Message); }

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""OldPrice"" NUMERIC(18,2) NULL;");
}
catch (Exception ex) { Console.WriteLine("Products.OldPrice error: " + ex.Message); }

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE;");
}
catch (Exception ex) { Console.WriteLine("Products.IsActive error: " + ex.Message); }

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""Brand"" TEXT NOT NULL DEFAULT '';");
}
catch (Exception ex) { Console.WriteLine("Products.Brand error: " + ex.Message); }

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""Slug"" TEXT NOT NULL DEFAULT '';");
}
catch (Exception ex) { Console.WriteLine("Products.Slug error: " + ex.Message); }

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""MetaDescription"" TEXT NOT NULL DEFAULT '';");
}
catch (Exception ex) { Console.WriteLine("Products.MetaDescription error: " + ex.Message); }



try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""Sku"" TEXT NOT NULL DEFAULT '';");
}
catch (Exception ex) { Console.WriteLine("Products.Sku error: " + ex.Message); }

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""ImageUrl"" TEXT NOT NULL DEFAULT '';");
}
catch (Exception ex) { Console.WriteLine("Products.ImageUrl error: " + ex.Message); }

// ── CategoryAttributes table ───────────────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""CategoryAttributes"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""CategoryId"" INT NOT NULL REFERENCES ""Categories""(""Id"") ON DELETE CASCADE,
            ""AttributeName"" TEXT NOT NULL DEFAULT '',
            ""AttributeType"" TEXT NOT NULL DEFAULT 'text',
            ""AttributeOptions"" TEXT NOT NULL DEFAULT ''
        );
    ");
}
catch (Exception ex) { Console.WriteLine("CategoryAttributes error: " + ex.Message); }

// ── Categories.ParentId ────────────────────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Categories"" ADD COLUMN IF NOT EXISTS ""ParentId"" INT NULL REFERENCES ""Categories""(""Id"") ON DELETE SET NULL;");
}
catch (Exception ex) { Console.WriteLine("Categories.ParentId error: " + ex.Message); }

// ── Users table + seed ─────────────────────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS "Users" (
            "Id" SERIAL PRIMARY KEY,
            "Username" text NOT NULL,
            "Password" text NOT NULL
        );
    """);
    if (!db.Users.Any())
    {
        db.Users.Add(new User { Username = "admin", Password = "1234" });
        db.SaveChanges();
    }
}
catch (Exception ex) { Console.WriteLine("Users error: " + ex.Message); }

// ── Orders + OrderItems tables ─────────────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""Orders"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""CustomerName"" TEXT,
            ""CustomerPhone"" TEXT,
            ""CustomerAddress"" TEXT,
            ""PaymentMethod"" TEXT,
            ""Total"" NUMERIC(18,2),
            ""Status"" TEXT,
            ""CreatedAt"" TIMESTAMP
        );
    ");
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""OrderItems"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""OrderId"" INT REFERENCES ""Orders""(""Id"") ON DELETE CASCADE,
            ""ProductId"" INT,
            ""ProductName"" TEXT,
            ""Price"" NUMERIC(18,2),
            ""Quantity"" INT,
            ""ImageUrl"" TEXT
        );
    ");
}
catch (Exception ex) { Console.WriteLine("Orders error: " + ex.Message); }

// ── Clients table + Orders.ClientId ───────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""Clients"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""FullName"" TEXT NOT NULL,
            ""Email"" TEXT NOT NULL UNIQUE,
            ""Phone"" TEXT,
            ""PasswordHash"" TEXT NOT NULL,
            ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
            ""ResetCode"" TEXT,
            ""ResetCodeExpiresAt"" TIMESTAMP
        );
    ");
}
catch (Exception ex) { Console.WriteLine("Clients error: " + ex.Message); }

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Orders"" ADD COLUMN IF NOT EXISTS ""ClientId"" INT NULL;");
}
catch (Exception ex) { Console.WriteLine("Orders.ClientId error: " + ex.Message); }



// ── OrderItems.VariantId ───────────────────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""OrderItems"" ADD COLUMN IF NOT EXISTS ""VariantId"" INT NULL;");
}
catch (Exception ex) { Console.WriteLine("OrderItems.VariantId error: " + ex.Message); }

// ── Orders.ShippingFee ─────────────────────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Orders"" ADD COLUMN IF NOT EXISTS ""ShippingFee"" NUMERIC(18,2) NOT NULL DEFAULT 0;");
}
catch (Exception ex) { Console.WriteLine("Orders.ShippingFee error: " + ex.Message); }




try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""OrderItems"" ADD COLUMN IF NOT EXISTS ""VariantName"" TEXT NULL;");
}
catch (Exception ex) { Console.WriteLine("OrderItems.VariantName error: " + ex.Message); }





// ── Reviews table ──────────────────────────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""Reviews"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""ProductId"" INT NOT NULL REFERENCES ""Products""(""Id"") ON DELETE CASCADE,
            ""ClientId"" INT NOT NULL REFERENCES ""Clients""(""Id"") ON DELETE CASCADE,
            ""OrderId"" INT NOT NULL,
            ""Note"" INT NOT NULL,
            ""Commentaire"" TEXT NOT NULL DEFAULT '',
            ""Approuve"" BOOLEAN NOT NULL DEFAULT TRUE,
            ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW()
        );
    ");
}
catch (Exception ex) { Console.WriteLine("Reviews error: " + ex.Message); }





// ── Products.Brand nullable ────────────────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Products"" ALTER COLUMN ""Brand"" DROP NOT NULL;");
}
catch (Exception ex) { Console.WriteLine("Products.Brand nullable error: " + ex.Message); }



// ── Fix Brand null values ──────────────────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"UPDATE ""Products"" SET ""Brand"" = '' WHERE ""Brand"" IS NULL;");
}
catch (Exception ex) { Console.WriteLine("Fix Brand null error: " + ex.Message); }



try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Orders"" ADD COLUMN IF NOT EXISTS ""RefundMotif"" TEXT NULL;");
}
catch (Exception ex) { Console.WriteLine("Orders.RefundMotif error: " + ex.Message); }



// ── PushSubscriptions table ─────────────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""PushSubscriptions"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""Endpoint"" TEXT NOT NULL,
            ""P256dh"" TEXT NOT NULL,
            ""Auth"" TEXT NOT NULL
        );
    ");
}
catch (Exception ex) { Console.WriteLine("PushSubscriptions error: " + ex.Message); }




try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""ClientPushSubscriptions"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""ClientId"" INT NOT NULL,
            ""Endpoint"" TEXT NOT NULL,
            ""P256dh"" TEXT NOT NULL,
            ""Auth"" TEXT NOT NULL
        );
    ");
}
catch (Exception ex) { Console.WriteLine("ClientPushSubscriptions error: " + ex.Message); }




// ── Sellers table ──────────────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""Sellers"" (
            ""Id""                SERIAL PRIMARY KEY,
            ""ClientId""          INT NOT NULL REFERENCES ""Clients""(""Id"") ON DELETE CASCADE,
            ""ShopName""          TEXT NOT NULL DEFAULT '',
            ""ShopDescription""   TEXT,
            ""PhoneNumber""       TEXT NOT NULL DEFAULT '',
            ""NationalIdNumber""  TEXT NOT NULL DEFAULT '',
            ""ShopLogoUrl""       TEXT,
            ""CommissionRate""    NUMERIC(5,4) NOT NULL DEFAULT 0.10,
            ""PaymentMethod""     TEXT,
            ""PaymentDetails""    TEXT,
            ""Status""            TEXT NOT NULL DEFAULT 'Pending',
            ""RejectionReason""   TEXT,
            ""CreatedAt""         TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAt""         TIMESTAMP NOT NULL DEFAULT NOW()
        );
    ");
}
catch (Exception ex) { Console.WriteLine("Sellers error: " + ex.Message); }

// ── SellerProducts table ───────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""SellerProducts"" (
            ""Id""               SERIAL PRIMARY KEY,
            ""SellerId""         INT NOT NULL REFERENCES ""Sellers""(""Id"") ON DELETE CASCADE,
            ""ProductId""        INT NULL,
            ""Name""             TEXT NOT NULL DEFAULT '',
            ""Description""      TEXT,
            ""Price""            NUMERIC(18,2) NOT NULL DEFAULT 0,
            ""OldPrice""         NUMERIC(18,2) NULL,
            ""Stock""            INT NOT NULL DEFAULT 0,
            ""Category""         TEXT,
            ""Images""           TEXT NOT NULL DEFAULT '[]',
            ""ApprovalStatus""   TEXT NOT NULL DEFAULT 'Pending',
            ""RejectionReason""  TEXT,
            ""CreatedAt""        TIMESTAMP NOT NULL DEFAULT NOW(),
            ""UpdatedAt""        TIMESTAMP NOT NULL DEFAULT NOW()
        );
    ");
}
catch (Exception ex) { Console.WriteLine("SellerProducts error: " + ex.Message); }

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""SellerProducts"" ADD COLUMN IF NOT EXISTS ""Sku"" TEXT;");
}
catch (Exception ex) { Console.WriteLine("SellerProducts.Sku error: " + ex.Message); }

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""SellerProducts"" ADD COLUMN IF NOT EXISTS ""Brand"" TEXT;");
}
catch (Exception ex) { Console.WriteLine("SellerProducts.Brand error: " + ex.Message); }

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""SellerProducts"" ADD COLUMN IF NOT EXISTS ""ShortDescription"" TEXT;");
}
catch (Exception ex) { Console.WriteLine("SellerProducts.ShortDescription error: " + ex.Message); }



// ── SellerPayouts table ────────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""SellerPayouts"" (
            ""Id""                   SERIAL PRIMARY KEY,
            ""SellerId""             INT NOT NULL REFERENCES ""Sellers""(""Id"") ON DELETE CASCADE,
            ""OrderId""              INT NULL,
            ""GrossAmount""          NUMERIC(18,2) NOT NULL DEFAULT 0,
            ""CommissionAmount""     NUMERIC(18,2) NOT NULL DEFAULT 0,
            ""NetAmount""            NUMERIC(18,2) NOT NULL DEFAULT 0,
            ""Status""               TEXT NOT NULL DEFAULT 'Pending',
            ""TransactionReference"" TEXT,
            ""Notes""                TEXT,
            ""CreatedAt""            TIMESTAMP NOT NULL DEFAULT NOW(),
            ""PaidAt""               TIMESTAMP NULL
        );
    ");
}
catch (Exception ex) { Console.WriteLine("SellerPayouts error: " + ex.Message); }




try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""SellerProducts"" ADD COLUMN IF NOT EXISTS ""Variants"" TEXT NOT NULL DEFAULT '[]';");
}
catch (Exception ex) { Console.WriteLine("SellerProducts.Variants error: " + ex.Message); }




// ── SellerPushSubscriptions table ─────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""SellerPushSubscriptions"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""SellerId"" INT NOT NULL REFERENCES ""Sellers""(""Id"") ON DELETE CASCADE,
            ""Endpoint"" TEXT NOT NULL,
            ""P256dh"" TEXT NOT NULL,
            ""Auth"" TEXT NOT NULL,
            ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW()
        );
    ");
}
catch (Exception ex) { Console.WriteLine("SellerPushSubscriptions error: " + ex.Message); }








// ═══════════════════════════════════════════════════════════════════
// FICHIER 2 : Program.cs
// Ajoute ce bloc à la fin, avant app.Run() :
// ═══════════════════════════════════════════════════════════════════

// ── CommissionSettings table ───────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""CommissionSettings"" (
            ""Id""          SERIAL PRIMARY KEY,
            ""Key""         TEXT NOT NULL UNIQUE,
            ""Label""       TEXT NOT NULL DEFAULT '',
            ""Rate""        NUMERIC(5,4) NOT NULL DEFAULT 0.10,
            ""UpdatedAt""   TIMESTAMP NOT NULL DEFAULT NOW()
        );
    ");
    // Seed : taux global par défaut si pas encore en base
    db.Database.ExecuteSqlRaw(@"
        INSERT INTO ""CommissionSettings"" (""Key"", ""Label"", ""Rate"", ""UpdatedAt"")
        VALUES ('global', 'Global', 0.10, NOW())
        ON CONFLICT (""Key"") DO NOTHING;
    ");
}
catch (Exception ex) { Console.WriteLine("CommissionSettings error: " + ex.Message); }





// ═══════════════════════════════════════════════════════════════
// AJOUTER dans Program.cs avant app.Run()
// ═══════════════════════════════════════════════════════════════

// ── Users — ajouter colonnes Role, Email, IsActive, CreatedAt ──
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""Email"" TEXT NOT NULL DEFAULT '';");
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""Role"" TEXT NOT NULL DEFAULT 'SuperAdmin';");
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE;");
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW();");
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""CreatedBy"" TEXT NULL;");

    // Mettre à jour l'admin existant en SuperAdmin
    db.Database.ExecuteSqlRaw(@"UPDATE ""Users"" SET ""Role"" = 'SuperAdmin' WHERE ""Role"" = '' OR ""Role"" IS NULL;");

    Console.WriteLine("Users migration OK");
}
catch (Exception ex) { Console.WriteLine("Users migration error: " + ex.Message); }





// ── SiteSettings table + seed ─────────────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""SiteSettings"" (
            ""Id""    SERIAL PRIMARY KEY,
            ""Key""   TEXT NOT NULL UNIQUE,
            ""Value"" TEXT NOT NULL DEFAULT ''
        );
    ");
    // Seed des valeurs par défaut
    var defaults = new Dictionary<string, string>
    {
        ["hero_title"] = "Mode & Style",
        ["hero_title_highlight"] = "Abidjan",
        ["hero_subtitle"] = "Les meilleures marques africaines et internationales livrées directement chez vous en Côte d'Ivoire.",
        ["hero_badge"] = "-60%",
        ["hero_badge_label"] = "mode",
        ["stat_vendors"] = "3 200+",
        ["stat_satisfaction"] = "98%",
        ["stat_delivery"] = "24h",
        ["promo_referral_title"] = "Invitez un ami, gagnez 2 500 F",
        ["promo_referral_desc"] = "Pour chaque ami qui effectue son premier achat sur Ranita Market.",
        ["promo_delivery_title"] = "Gratuite dès 15 000 F",
        ["promo_delivery_desc"] = "Abidjan & banlieue — livraison en 24h chrono.",
        ["referral_title"] = "🎁 Invitez un ami, gagnez 2 500 F CFA",
        ["referral_desc"] = "Pour chaque ami qui effectue son premier achat sur Ranita Market, vous recevez 2 500 F sur votre compte.",
        ["hero_btn_link"] = "products.html",
        ["card1_title1"] = "Smartphones",
        ["card1_title2"] = "& Gadgets",
        ["card1_subtitle"] = "Tecno, Samsung, iPhone...",
        ["card1_link"] = "products.html",
        ["card2_title1"] = "Produits Frais",
        ["card2_title2"] = "& Locaux",
        ["card2_subtitle"] = "Épices, céréales, boissons",
        ["card2_link"] = "products.html",
        ["slide0_title"] = "Mode & Style<br><em>Abidjan</em>",
        ["slide0_sub"] = "Les meilleures marques africaines et internationales livrées directement chez vous.",
        ["slide0_cta_text"] = "Voir les produits →",
        ["slide0_cta_link"] = "products.html",
        ["slide0_img"] = "",
        ["slide1_title"] = "Smartphones<br><em style='color:#60a5fa'>& Gadgets</em>",
        ["slide1_sub"] = "Tecno, Samsung, iPhone — les meilleures offres tech à Abidjan.",
        ["slide1_cta_text"] = "Découvrir →",
        ["slide1_cta_link"] = "products.html",
        ["slide1_img"] = "",
        ["slide2_title"] = "Produits Frais<br><em>& Locaux</em>",
        ["slide2_sub"] = "Épices, céréales, boissons — le meilleur du marché livré chez vous.",
        ["slide2_cta_text"] = "Commander →",
        ["slide2_cta_link"] = "products.html",
        ["slide2_img"] = "",
        ["promo_bar_text"] = "Livraison gratuite dès 15 000 F · Abidjan en 24h · Paiement à la livraison disponible",
        ["promo_bar_link"] = "products.html",
        ["promo_bar_hide"] = "false",
        ["urgency_title"] = "Ventes Flash — Offres limitées !",
        ["urgency_subtitle"] = "Profitez de nos meilleures réductions avant qu''il ne soit trop tard",
        ["urgency_link"] = "products.html",
        ["urgency_hide"] = "false",
        ["bc1_tag"] = "Tendances", ["bc1_name"] = "Mode & Vêtements", ["bc1_desc"] = "Les dernières tendances africaines et internationales pour femme et homme.", ["bc1_img"] = "", ["bc1_link"] = "products.html?cat=mode",
        ["bc2_tag"] = "High-Tech", ["bc2_name"] = "Électronique & Gadgets", ["bc2_desc"] = "Smartphones, accessoires et gadgets tech aux meilleurs prix d\'Abidjan.", ["bc2_img"] = "", ["bc2_link"] = "products.html?cat=elec",
        ["bc3_tag"] = "Produits frais", ["bc3_name"] = "Alimentation & Épicerie", ["bc3_desc"] = "Épices, céréales, boissons — le meilleur du marché local livré chez vous.", ["bc3_img"] = "", ["bc3_link"] = "products.html?cat=alim",
        ["bc4_tag"] = "Bien-être", ["bc4_name"] = "Beauté & Santé", ["bc4_desc"] = "Soins, parfums, cosmétiques et produits de santé pour toute la famille.", ["bc4_img"] = "", ["bc4_link"] = "products.html?cat=beau",
    };
    foreach (var (key, value) in defaults)
    {
        db.Database.ExecuteSqlRaw($@"
            INSERT INTO ""SiteSettings"" (""Key"", ""Value"")
            VALUES ('{key}', '{value.Replace("'", "''")}')
            ON CONFLICT (""Key"") DO NOTHING;
        ");
    }
    Console.WriteLine("SiteSettings OK");
}
catch (Exception ex) { Console.WriteLine("SiteSettings error: " + ex.Message); }




// ════════════════════════════════════════════════════
// FICHIER 2 : Program.cs
// Ajoute ce bloc avant app.Run() :
// ════════════════════════════════════════════════════

// ── SiteEvents table ───────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""SiteEvents"" (
            ""Id""         SERIAL PRIMARY KEY,
            ""Name""       TEXT NOT NULL DEFAULT '',
            ""Color""      TEXT NOT NULL DEFAULT '#10b981',
            ""StartDate""  TIMESTAMP NULL,
            ""EndDate""    TIMESTAMP NULL,
            ""PromoText""  TEXT NOT NULL DEFAULT '',
            ""SlideTitle"" TEXT NOT NULL DEFAULT '',
            ""SlideSub""   TEXT NOT NULL DEFAULT '',
            ""SlideCta""   TEXT NOT NULL DEFAULT 'Voir les offres →',
            ""SlideLink""  TEXT NOT NULL DEFAULT 'products.html',
            ""SlideDisc""  TEXT NOT NULL DEFAULT '',
            ""SlideImg""   TEXT NOT NULL DEFAULT '',
            ""IsActive""   BOOLEAN NOT NULL DEFAULT TRUE,
            ""CreatedAt""  TIMESTAMP NOT NULL DEFAULT NOW()
        );
    ");
    Console.WriteLine("SiteEvents OK");
}
catch (Exception ex) { Console.WriteLine("SiteEvents error: " + ex.Message); }




// ── Clients — colonnes parrainage ─────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Clients"" ADD COLUMN IF NOT EXISTS ""ReferralCode"" TEXT NOT NULL DEFAULT '';");
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Clients"" ADD COLUMN IF NOT EXISTS ""ReferredById"" INT NULL;");
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Clients"" ADD COLUMN IF NOT EXISTS ""ReferralCredits"" INT NOT NULL DEFAULT 0;");
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Clients"" ADD COLUMN IF NOT EXISTS ""ReferralCount"" INT NOT NULL DEFAULT 0;");
    Console.WriteLine("Clients referral columns OK");
}
catch (Exception ex) { Console.WriteLine("Clients referral error: " + ex.Message); }




// Générer ReferralCode pour les clients existants qui n'en ont pas
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var clients = db.Clients.Where(c => c.ReferralCode == "").ToList();
    var rnd = new Random();
    foreach (var c in clients)
    {
        var first = c.FullName.Split(' ')[0].ToUpper();
        if (first.Length > 6) first = first.Substring(0, 6);
        c.ReferralCode = first + rnd.Next(1000, 9999).ToString();
    }
    db.SaveChanges();
    Console.WriteLine($"ReferralCode généré pour {clients.Count} clients existants");
}
catch (Exception ex) { Console.WriteLine("ReferralCode seed error: " + ex.Message); }





app.Run();