using Azure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StackUnderflow.Data;
using StackUnderflow.Models;
using StackUnderflow.Services;
using StackUnderflow.Utilities;

var builder = WebApplication.CreateBuilder(args);

// Load secrets from Azure Key Vault into configuration. Secret names use '--' in place
// of ':' (e.g. "Authentication--GitHub--ClientSecret" maps to "Authentication:GitHub:ClientSecret").
var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
    {
        // Skip the Managed Identity/IMDS probe locally — it hangs on machines where
        // 169.254.169.254 isn't quickly refused. In Azure it IS available, so only exclude in dev.
        ExcludeManagedIdentityCredential = builder.Environment.IsDevelopment()
    });
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), credential);
}

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("ServerConnection") ??
                       throw new InvalidOperationException("Connection string 'ServerConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<User>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddAuthentication()
    .AddGitHub(options =>
    {
        options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"];
    });
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<StackUnderflow.Services.ThreadVoteService>();
builder.Services.AddScoped<StackUnderflow.Services.PostVoteService>();
builder.Services.AddSingleton<ContentSafetyAnalyzer>();
builder.Services.AddScoped<IAuthorizationHandler, ModeratorUserHandler>();
builder.Services.AddHostedService<StackUnderflow.Services.ThreadAutoLockService>();

// Let fetch()-based API calls send the antiforgery token via a request header.
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("IsModerator", policy => policy.AddRequirements(new ModeratorUserRequirement()));
});

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

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Attribute-routed API controllers (e.g. Areas/Api).
app.MapControllers();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
    .WithStaticAssets();

app.Run();
