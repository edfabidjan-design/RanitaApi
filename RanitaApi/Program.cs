using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;
using RanitaApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();




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


var app = builder.Build();

app.UseHttpsRedirection();

app.UseStaticFiles(); // ✅ permet d’ouvrir les fichiers HTML dans wwwroot

app.UseCors("AllowRanitaShop");

app.UseAuthorization();

app.MapControllers();

app.MapControllers();


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

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

app.Run();