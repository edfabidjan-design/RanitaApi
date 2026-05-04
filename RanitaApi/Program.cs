using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;
using RanitaApi.Models;
using RanitaApi.Services;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();


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
                "https://www.ranita-shop.com"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });


var app = builder.Build();

app.UseHttpsRedirection();

app.UseStaticFiles(); // ✅ permet d’ouvrir les fichiers HTML dans wwwroot

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
    using (var scope = app.Services.CreateScope())
    {
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
            db.Users.Add(new User
            {
                Username = "admin",
                Password = "1234"
            });

            db.SaveChanges();
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine("User seed error: " + ex.Message);
}



try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Database.ExecuteSqlRaw("""
            ALTER TABLE "Products"
            ADD COLUMN IF NOT EXISTS "ImageUrl" text NOT NULL DEFAULT '';
        """);
    }
}
catch (Exception ex)
{
    Console.WriteLine("DB fix error: " + ex.Message);
}


try
{
    using (var scope = app.Services.CreateScope())
    {
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
}
catch (Exception ex)
{
    Console.WriteLine("Orders table error: " + ex.Message);
}


try
{
    using (var scope = app.Services.CreateScope())
    {
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

        db.Database.ExecuteSqlRaw(@"
            ALTER TABLE ""Orders""
            ADD COLUMN IF NOT EXISTS ""ClientId"" INT NULL;
        ");
    }
}
catch (Exception ex)
{
    Console.WriteLine("Clients table error: " + ex.Message);
}



app.Run();