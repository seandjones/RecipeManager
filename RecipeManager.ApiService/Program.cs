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
}

if (!builder.Services.Any(sd => sd.ServiceType == typeof(DbContextOptions<IngredientListDbContext>)))
{
    builder.AddNpgsqlDbContext<IngredientListDbContext>("recipedb");
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
builder.Services.AddSignalR();

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
app.MapHub<IngredientListHub>("/hubs/ingredient-list");

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

// Ingredient list endpoints
var ingredientListGroup = app.MapGroup("/api/ingredient-lists")
    .WithTags("IngredientLists");

ingredientListGroup.MapPost("/", async (
    IngredientListRequest request,
    IngredientListDbContext db,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var currentUserId = ResolveCurrentUserId(httpContext);
    if (!currentUserId.HasValue)
    {
        return Results.Unauthorized();
    }

    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new { error = "Name is required." });
    }

    var listValidationError = ValidateIngredientListRequest(request);
    if (listValidationError is not null)
    {
        return Results.BadRequest(new { error = listValidationError });
    }

    var now = DateTime.UtcNow;
    var list = new IngredientList
    {
        Id = Guid.NewGuid(),
        Name = request.Name.Trim(),
        Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
        OwnerId = currentUserId.Value,
        CreatedAt = now,
        UpdatedAt = now
    };

    db.IngredientLists.Add(list);
    await db.SaveChangesAsync(cancellationToken);

    var response = new IngredientListSummaryResponse(
        list.Id,
        list.Name,
        list.Description,
        list.OwnerId,
        list.CreatedAt,
        list.UpdatedAt);

    return Results.Created($"/api/ingredient-lists/{list.Id}", response);
})
.WithName("CreateIngredientList")
.WithOpenApi()
.Produces<IngredientListSummaryResponse>(201)
.Produces(400)
.Produces(401);

ingredientListGroup.MapGet("/", async (
    IngredientListDbContext db,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var currentUserId = ResolveCurrentUserId(httpContext);
    if (!currentUserId.HasValue)
    {
        return Results.Unauthorized();
    }

    var sharedListIds = await db.ListSharings
        .Where(s => s.SharedWithUserId == currentUserId.Value)
        .Select(s => s.IngredientListId)
        .ToListAsync(cancellationToken);

    var lists = await db.IngredientLists
        .Where(l => l.OwnerId == currentUserId.Value || sharedListIds.Contains(l.Id))
        .OrderByDescending(l => l.UpdatedAt)
        .Select(l => new IngredientListSummaryResponse(
            l.Id,
            l.Name,
            l.Description,
            l.OwnerId,
            l.CreatedAt,
            l.UpdatedAt))
        .ToListAsync(cancellationToken);

    return Results.Ok(lists);
})
.WithName("GetIngredientLists")
.WithOpenApi()
.Produces<List<IngredientListSummaryResponse>>(200)
.Produces(401);

ingredientListGroup.MapGet("/{id:guid}", async (
    Guid id,
    IngredientListDbContext db,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var currentUserId = ResolveCurrentUserId(httpContext);
    if (!currentUserId.HasValue)
    {
        return Results.Unauthorized();
    }

    var hasAccess = await HasIngredientListAccessAsync(db, id, currentUserId.Value, cancellationToken);
    if (!hasAccess)
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    var list = await db.IngredientLists
        .Include(l => l.Ingredients)
        .Include(l => l.RecipeLinks)
        .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    if (list is null)
    {
        return Results.NotFound();
    }

    var recipeIds = list.RecipeLinks.Select(r => r.RecipeId).ToList();
    var recipeMap = await db.Set<Recipe>()
        .Where(r => recipeIds.Contains(r.Id))
        .Select(r => new RecipeSummaryResponse(r.Id, r.Name, r.Description))
        .ToListAsync(cancellationToken);

    var response = new IngredientListDetailResponse(
        list.Id,
        list.Name,
        list.Description,
        list.OwnerId,
        list.CreatedAt,
        list.UpdatedAt,
        list.Ingredients
            .OrderBy(i => i.CreatedAt)
            .Select(i => new IngredientItemResponse(i.Id, i.Name, i.Quantity, i.Unit, i.IsChecked, i.CreatedAt))
            .ToList(),
        recipeMap);

    return Results.Ok(response);
})
.WithName("GetIngredientList")
.WithOpenApi()
.Produces<IngredientListDetailResponse>(200)
.Produces(401)
.Produces(403)
.Produces(404);

ingredientListGroup.MapPut("/{id:guid}", async (
    Guid id,
    IngredientListRequest request,
    IngredientListDbContext db,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var currentUserId = ResolveCurrentUserId(httpContext);
    if (!currentUserId.HasValue)
    {
        return Results.Unauthorized();
    }

    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new { error = "Name is required." });
    }

    var listValidationError = ValidateIngredientListRequest(request);
    if (listValidationError is not null)
    {
        return Results.BadRequest(new { error = listValidationError });
    }

    var list = await db.IngredientLists.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    if (list is null)
    {
        return Results.NotFound();
    }

    if (list.OwnerId != currentUserId.Value)
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    list.Name = request.Name.Trim();
    list.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
    list.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync(cancellationToken);

    var response = new IngredientListSummaryResponse(
        list.Id,
        list.Name,
        list.Description,
        list.OwnerId,
        list.CreatedAt,
        list.UpdatedAt);

    return Results.Ok(response);
})
.WithName("UpdateIngredientList")
.WithOpenApi()
.Produces<IngredientListSummaryResponse>(200)
.Produces(400)
.Produces(401)
.Produces(403)
.Produces(404);

ingredientListGroup.MapDelete("/{id:guid}", async (
    Guid id,
    IngredientListDbContext db,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var currentUserId = ResolveCurrentUserId(httpContext);
    if (!currentUserId.HasValue)
    {
        return Results.Unauthorized();
    }

    var list = await db.IngredientLists.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    if (list is null)
    {
        return Results.NotFound();
    }

    if (list.OwnerId != currentUserId.Value)
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    db.IngredientLists.Remove(list);
    await db.SaveChangesAsync(cancellationToken);
    return Results.NoContent();
})
.WithName("DeleteIngredientList")
.WithOpenApi()
.Produces(204)
.Produces(401)
.Produces(403)
.Produces(404);

ingredientListGroup.MapPost("/{id:guid}/ingredients", async (
    Guid id,
    IngredientRequest request,
    IngredientListDbContext db,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var currentUserId = ResolveCurrentUserId(httpContext);
    if (!currentUserId.HasValue)
    {
        return Results.Unauthorized();
    }

    if (!await HasIngredientListAccessAsync(db, id, currentUserId.Value, cancellationToken))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new { error = "Ingredient name is required." });
    }

    var ingredientValidationError = ValidateIngredientRequest(request);
    if (ingredientValidationError is not null)
    {
        return Results.BadRequest(new { error = ingredientValidationError });
    }

    var ingredient = new Ingredient
    {
        Id = Guid.NewGuid(),
        IngredientListId = id,
        Name = request.Name.Trim(),
        Quantity = string.IsNullOrWhiteSpace(request.Quantity) ? null : request.Quantity.Trim(),
        Unit = string.IsNullOrWhiteSpace(request.Unit) ? null : request.Unit.Trim(),
        IsChecked = request.IsChecked,
        CreatedAt = DateTime.UtcNow
    };

    db.Ingredients.Add(ingredient);

    var list = await db.IngredientLists.FirstAsync(l => l.Id == id, cancellationToken);
    list.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync(cancellationToken);

    var response = new IngredientItemResponse(
        ingredient.Id,
        ingredient.Name,
        ingredient.Quantity,
        ingredient.Unit,
        ingredient.IsChecked,
        ingredient.CreatedAt);

    return Results.Created($"/api/ingredient-lists/{id}/ingredients/{ingredient.Id}", response);
})
.WithName("AddIngredientToList")
.WithOpenApi()
.Produces<IngredientItemResponse>(201)
.Produces(400)
.Produces(401)
.Produces(403);

ingredientListGroup.MapPut("/{id:guid}/ingredients/{ingredientId:guid}", async (
    Guid id,
    Guid ingredientId,
    IngredientRequest request,
    IngredientListDbContext db,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var currentUserId = ResolveCurrentUserId(httpContext);
    if (!currentUserId.HasValue)
    {
        return Results.Unauthorized();
    }

    if (!await HasIngredientListAccessAsync(db, id, currentUserId.Value, cancellationToken))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new { error = "Ingredient name is required." });
    }

    var ingredientValidationError = ValidateIngredientRequest(request);
    if (ingredientValidationError is not null)
    {
        return Results.BadRequest(new { error = ingredientValidationError });
    }

    var ingredient = await db.Ingredients
        .FirstOrDefaultAsync(i => i.Id == ingredientId && i.IngredientListId == id, cancellationToken);

    if (ingredient is null)
    {
        return Results.NotFound();
    }

    ingredient.Name = request.Name.Trim();
    ingredient.Quantity = string.IsNullOrWhiteSpace(request.Quantity) ? null : request.Quantity.Trim();
    ingredient.Unit = string.IsNullOrWhiteSpace(request.Unit) ? null : request.Unit.Trim();
    ingredient.IsChecked = request.IsChecked;

    var list = await db.IngredientLists.FirstAsync(l => l.Id == id, cancellationToken);
    list.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync(cancellationToken);

    var response = new IngredientItemResponse(
        ingredient.Id,
        ingredient.Name,
        ingredient.Quantity,
        ingredient.Unit,
        ingredient.IsChecked,
        ingredient.CreatedAt);

    return Results.Ok(response);
})
.WithName("UpdateIngredientInList")
.WithOpenApi()
.Produces<IngredientItemResponse>(200)
.Produces(400)
.Produces(401)
.Produces(403)
.Produces(404);

ingredientListGroup.MapDelete("/{id:guid}/ingredients/{ingredientId:guid}", async (
    Guid id,
    Guid ingredientId,
    IngredientListDbContext db,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var currentUserId = ResolveCurrentUserId(httpContext);
    if (!currentUserId.HasValue)
    {
        return Results.Unauthorized();
    }

    if (!await HasIngredientListAccessAsync(db, id, currentUserId.Value, cancellationToken))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    var ingredient = await db.Ingredients
        .FirstOrDefaultAsync(i => i.Id == ingredientId && i.IngredientListId == id, cancellationToken);

    if (ingredient is null)
    {
        return Results.NotFound();
    }

    db.Ingredients.Remove(ingredient);

    var list = await db.IngredientLists.FirstAsync(l => l.Id == id, cancellationToken);
    list.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync(cancellationToken);
    return Results.NoContent();
})
.WithName("DeleteIngredientFromList")
.WithOpenApi()
.Produces(204)
.Produces(401)
.Produces(403)
.Produces(404);

ingredientListGroup.MapPost("/{id:guid}/recipes", async (
    Guid id,
    RecipeLinkRequest request,
    IngredientListDbContext db,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var currentUserId = ResolveCurrentUserId(httpContext);
    if (!currentUserId.HasValue)
    {
        return Results.Unauthorized();
    }

    if (!await HasIngredientListAccessAsync(db, id, currentUserId.Value, cancellationToken))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    if (request.RecipeId <= 0)
    {
        return Results.BadRequest(new { error = "RecipeId must be greater than 0." });
    }

    var recipeExists = await db.Set<Recipe>().AnyAsync(r => r.Id == request.RecipeId, cancellationToken);
    if (!recipeExists)
    {
        return Results.BadRequest(new { error = "Recipe does not exist." });
    }

    var existingLink = await db.RecipeIngredientLists
        .FirstOrDefaultAsync(r => r.IngredientListId == id && r.RecipeId == request.RecipeId, cancellationToken);

    if (existingLink is not null)
    {
        return Results.BadRequest(new { error = "Recipe is already linked to this ingredient list." });
    }

    db.RecipeIngredientLists.Add(new RecipeIngredientList
    {
        Id = Guid.NewGuid(),
        IngredientListId = id,
        RecipeId = request.RecipeId,
        AddedAt = DateTime.UtcNow,
        AddedByUserId = currentUserId.Value
    });

    var list = await db.IngredientLists.FirstAsync(l => l.Id == id, cancellationToken);
    list.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync(cancellationToken);
    return Results.Ok(new { listId = id, recipeId = request.RecipeId });
})
.WithName("AddRecipeToIngredientList")
.WithOpenApi()
.Produces(200)
.Produces(400)
.Produces(401)
.Produces(403);

ingredientListGroup.MapDelete("/{id:guid}/recipes/{recipeId:int}", async (
    Guid id,
    int recipeId,
    IngredientListDbContext db,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var currentUserId = ResolveCurrentUserId(httpContext);
    if (!currentUserId.HasValue)
    {
        return Results.Unauthorized();
    }

    if (!await HasIngredientListAccessAsync(db, id, currentUserId.Value, cancellationToken))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    var recipeLink = await db.RecipeIngredientLists
        .FirstOrDefaultAsync(r => r.IngredientListId == id && r.RecipeId == recipeId, cancellationToken);

    if (recipeLink is null)
    {
        return Results.NotFound();
    }

    db.RecipeIngredientLists.Remove(recipeLink);

    var list = await db.IngredientLists.FirstAsync(l => l.Id == id, cancellationToken);
    list.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync(cancellationToken);
    return Results.NoContent();
})
.WithName("RemoveRecipeFromIngredientList")
.WithOpenApi()
.Produces(204)
.Produces(401)
.Produces(403)
.Produces(404);

static Guid? ResolveCurrentUserId(HttpContext httpContext)
{
    var user = httpContext.User;
    var claimValue = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? user?.FindFirst("userId")?.Value
        ?? user?.FindFirst("sub")?.Value;

    if (Guid.TryParse(claimValue, out var claimUserId))
    {
        return claimUserId;
    }

    if (httpContext.Request.Headers.TryGetValue("X-User-Id", out var headerValues)
        && Guid.TryParse(headerValues.FirstOrDefault(), out var headerUserId))
    {
        return headerUserId;
    }

    return null;
}

static Task<bool> HasIngredientListAccessAsync(
    IngredientListDbContext db,
    Guid ingredientListId,
    Guid userId,
    CancellationToken cancellationToken)
{
    return db.IngredientLists.AnyAsync(
        l => l.Id == ingredientListId
            && (l.OwnerId == userId
                || db.ListSharings.Any(s => s.IngredientListId == ingredientListId && s.SharedWithUserId == userId)),
        cancellationToken);
}

static string? ValidateIngredientListRequest(IngredientListRequest request)
{
    if (request.Name.Length > 255)
    {
        return "Name cannot exceed 255 characters.";
    }

    if (!string.IsNullOrEmpty(request.Description) && request.Description.Length > 1000)
    {
        return "Description cannot exceed 1000 characters.";
    }

    return null;
}

static string? ValidateIngredientRequest(IngredientRequest request)
{
    if (request.Name.Length > 255)
    {
        return "Ingredient name cannot exceed 255 characters.";
    }

    if (!string.IsNullOrEmpty(request.Quantity) && request.Quantity.Length > 100)
    {
        return "Quantity cannot exceed 100 characters.";
    }

    if (!string.IsNullOrEmpty(request.Unit) && request.Unit.Length > 50)
    {
        return "Unit cannot exceed 50 characters.";
    }

    return null;
}

app.Run();

// Make Program class accessible for testing
namespace RecipeManager.ApiService
{
    public partial class Program { }
}
