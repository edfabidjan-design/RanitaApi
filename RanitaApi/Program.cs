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

app.Run();