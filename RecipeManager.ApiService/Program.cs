using Microsoft.EntityFrameworkCore;
using RecipeManager.ApiService.Data;
using RecipeManager.ApiService.Models;
using RecipeManager.ApiService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add database contexts via Aspire Npgsql integration
// Skip if running under WebApplicationFactory (test environment)
if (!builder.Services.Any(sd => sd.ServiceType == typeof(DbContextOptions<AuthDbContext>)))
{
    builder.AddNpgsqlDbContext<AuthDbContext>("recipedb");
}
if (!builder.Services.Any(sd => sd.ServiceType == typeof(DbContextOptions<RecipeDbContext>)))
{
    builder.AddNpgsqlDbContext<RecipeDbContext>("recipedb");
if (!builder.Services.Any(sd => sd.ServiceType == typeof(DbContextOptions<IngredientListDbContext>)))
{
    builder.AddNpgsqlDbContext<IngredientListDbContext>("recipedb");
}
}

// Configure email service
builder.Services.Configure<SendGridOptions>(builder.Configuration.GetSection("SendGrid"));

// Register email service based on environment
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IEmailService, DevelopmentEmailService>();
}
else
{
    builder.Services.AddSingleton<IEmailService, SendGridEmailService>();
}

// Register authentication service
builder.Services.AddScoped<IAuthService, AuthService>();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Apply pending migrations on startup (relational databases only)
using (var scope = app.Services.CreateScope())
{
    var authDb = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    if (authDb.Database.IsRelational())
        authDb.Database.Migrate();

    var recipeDb = scope.ServiceProvider.GetRequiredService<RecipeDbContext>();
    if (recipeDb.Database.IsRelational())
        recipeDb.Database.Migrate();

    var ingredientListDb = scope.ServiceProvider.GetRequiredService<IngredientListDbContext>();
    if (ingredientListDb.Database.IsRelational())
        ingredientListDb.Database.Migrate();
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "API service is running.");

// Authentication endpoints
var authGroup = app.MapGroup("/api/auth")
    .WithTags("Authentication");

authGroup.MapPost("/request-code", async (
    RequestLoginCodeRequest request,
    IAuthService authService,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    // Validate email
    if (string.IsNullOrWhiteSpace(request.Email))
    {
        return Results.BadRequest(new { error = "Email is required." });
    }

    var emailValidator = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
    if (!emailValidator.IsValid(request.Email))
    {
        return Results.BadRequest(new { error = "Invalid email format." });
    }

    var result = await authService.RequestLoginCodeAsync(request.Email, cancellationToken);

    if (!result.Success)
    {
        if (result.RetryAfterSeconds.HasValue)
        {
            // Rate limit exceeded
            httpContext.Response.Headers["Retry-After"] = result.RetryAfterSeconds.Value.ToString();
            return Results.Json(
                new { error = result.Message, retryAfter = result.RetryAfterSeconds },
                statusCode: 429);
        }

        return Results.BadRequest(new { error = result.Message });
    }

    return Results.Ok(new { message = result.Message });
})
.WithName("RequestLoginCode")
.WithSummary("Request a login code")
.WithDescription("Sends a 6-digit login code to the specified email address. Rate limited to 3 requests per hour per email.")
.Produces(200)
.Produces(400)
.Produces(429);

authGroup.MapPost("/verify-code", async (
    VerifyLoginCodeRequest request,
    IAuthService authService,
    CancellationToken cancellationToken) =>
{
    // Validate inputs
    if (string.IsNullOrWhiteSpace(request.Email))
    {
        return Results.BadRequest(new { error = "Email is required." });
    }

    if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Length != 6 || !request.Code.All(char.IsDigit))
    {
        return Results.BadRequest(new { error = "Invalid code format. Code must be 6 digits." });
    }

    var result = await authService.VerifyLoginCodeAsync(request.Email, request.Code, cancellationToken);

    if (!result.Success)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        message = result.Message,
        userId = result.UserId,
        email = result.Email
    });
})
.WithName("VerifyLoginCode")
.WithSummary("Verify a login code")
.WithDescription("Verifies a 6-digit login code for the specified email address. Returns user information on success.")
.Produces(200)
.Produces(400)
.Produces(401);

authGroup.MapPost("/logout", () =>
{
    // In this passwordless system, logout is handled client-side
    // The client should clear their session/cookie
    // This endpoint exists for consistency and future expansion
    return Results.Ok(new { message = "Logout successful." });
})
.WithName("Logout")
.WithSummary("Logout")
.WithDescription("Invalidates the current session. In passwordless authentication, this is primarily a client-side operation.")
.Produces(200);

app.MapDefaultEndpoints();

// Recipe endpoints
var recipeGroup = app.MapGroup("/api/recipes")
    .WithTags("Recipes");

recipeGroup.MapGet("/", async (RecipeDbContext db, CancellationToken cancellationToken) =>
{
    var recipes = await db.Recipes.OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);
    return Results.Ok(recipes);
})
.WithName("GetRecipes")
.WithSummary("Get all recipes")
.Produces<List<Recipe>>(200);

recipeGroup.MapGet("/{id:int}", async (int id, RecipeDbContext db, CancellationToken cancellationToken) =>
{
    var recipe = await db.Recipes.FindAsync([id], cancellationToken);
    return recipe is null ? Results.NotFound() : Results.Ok(recipe);
})
.WithName("GetRecipe")
.WithSummary("Get a recipe by ID")
.Produces<Recipe>(200)
.Produces(404);

recipeGroup.MapPost("/", async (RecipeRequest request, RecipeDbContext db, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
        return Results.BadRequest(new { error = "Name is required." });

    var recipe = new Recipe
    {
        Name = request.Name.Trim(),
        Description = request.Description?.Trim(),
        Ingredients = request.Ingredients,
        Instructions = request.Instructions,
        PrepTimeMinutes = request.PrepTimeMinutes,
        CookTimeMinutes = request.CookTimeMinutes,
        Servings = request.Servings,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    db.Recipes.Add(recipe);
    await db.SaveChangesAsync(cancellationToken);

    return Results.Created($"/api/recipes/{recipe.Id}", recipe);
})
.WithName("CreateRecipe")
.WithSummary("Create a new recipe")
.Produces<Recipe>(201)
.Produces(400);

recipeGroup.MapPut("/{id:int}", async (int id, RecipeRequest request, RecipeDbContext db, CancellationToken cancellationToken) =>
{
    var recipe = await db.Recipes.FindAsync([id], cancellationToken);
    if (recipe is null)
        return Results.NotFound();

    if (string.IsNullOrWhiteSpace(request.Name))
        return Results.BadRequest(new { error = "Name is required." });

    recipe.Name = request.Name.Trim();
    recipe.Description = request.Description?.Trim();
    recipe.Ingredients = request.Ingredients;
    recipe.Instructions = request.Instructions;
    recipe.PrepTimeMinutes = request.PrepTimeMinutes;
    recipe.CookTimeMinutes = request.CookTimeMinutes;
    recipe.Servings = request.Servings;
    recipe.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync(cancellationToken);
    return Results.Ok(recipe);
})
.WithName("UpdateRecipe")
.WithSummary("Update a recipe")
.Produces<Recipe>(200)
.Produces(400)
.Produces(404);

recipeGroup.MapDelete("/{id:int}", async (int id, RecipeDbContext db, CancellationToken cancellationToken) =>
{
    var recipe = await db.Recipes.FindAsync([id], cancellationToken);
    if (recipe is null)
        return Results.NotFound();

    db.Recipes.Remove(recipe);
    await db.SaveChangesAsync(cancellationToken);
    return Results.NoContent();
})
.WithName("DeleteRecipe")
.WithSummary("Delete a recipe")
.Produces(204)
.Produces(404);

app.Run();

// Make Program class accessible for testing
namespace RecipeManager.ApiService
{
    public partial class Program { }
}
