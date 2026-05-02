using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();




builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


var app = builder.Build();

app.UseHttpsRedirection();

app.UseStaticFiles(); // ✅ permet d’ouvrir les fichiers HTML dans wwwroot

app.UseAuthorization();

app.MapControllers();

app.Run();