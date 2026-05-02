using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;

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


var app = builder.Build();

app.UseHttpsRedirection();

app.UseStaticFiles(); // ✅ permet d’ouvrir les fichiers HTML dans wwwroot

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();