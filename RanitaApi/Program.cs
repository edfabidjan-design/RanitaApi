using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// 🔥 AJOUT IMPORTANT
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();