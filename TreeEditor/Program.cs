using Microsoft.EntityFrameworkCore;
using TreeEditor.Data;
using TreeEditor.Services;

var builder = WebApplication.CreateBuilder(args);
var database = Path.Combine(builder.Environment.ContentRootPath, "tree.db");

builder.Services.AddControllers();
builder.Services.AddDbContext<TreeDbContext>(options => options.UseSqlite($"Data Source={database}"));
builder.Services.AddScoped<TreeRepository>();
builder.Services.AddScoped<TreeService>();
builder.Services.AddScoped<DatabaseSeeder>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().InitializeAsync();

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
app.Run();
