using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using RecipeManager.Web;
using RecipeManager.Web.Components;
using RecipeManager.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddRedisOutputCache("cache");

// Configure authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "RecipeManager.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Register custom authentication state provider
builder.Services.AddScoped<AuthenticationStateProvider, CookieAuthenticationStateProvider>();

// Register authentication service
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuthenticationService>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register AuthApiClient for authentication API calls
builder.Services.AddHttpClient<AuthApiClient>(client =>
    {
        client.BaseAddress = new("https+http://apiservice");
    });

// Register RecipeApiClient
builder.Services.AddHttpClient<RecipeApiClient>(client =>
    {
        client.BaseAddress = new("https+http://apiservice");
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
