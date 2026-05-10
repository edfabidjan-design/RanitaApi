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


app.Run();