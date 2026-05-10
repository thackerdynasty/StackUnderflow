using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StackUnderflow.Data;
using StackUnderflow.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var sqliteConnectionString = builder.Configuration.GetConnectionString("SqliteConnection");
var sqlServerConnectionString = builder.Configuration.GetConnectionString("ServerConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (builder.Environment.IsDevelopment() && !string.IsNullOrWhiteSpace(sqliteConnectionString))
    {
        options.UseSqlite(sqliteConnectionString);
        return;
    }

    if (!string.IsNullOrWhiteSpace(sqlServerConnectionString))
    {
        options.UseSqlServer(sqlServerConnectionString);
        return;
    }

    throw new InvalidOperationException("No database connection string configured.");
});
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<User>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await DatabaseSeeder.SeedAsync(scope.ServiceProvider, app.Configuration, app.Environment);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
    .WithStaticAssets();

app.Run();
