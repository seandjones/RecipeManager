# Plan Format Specification

Plans are stored as JSON files in `.harness/plans/{slug}.json`.

## Schema

```json
{
  "slug": "add-recipe-crud",
  "title": "Add Recipe CRUD Operations",
  "category": "feature",
  "created_at": "2026-04-12T12:00:00Z",
  "status": "in_progress",
  "description": "Implement full CRUD operations for recipe management including database models, API endpoints, and Blazor UI components.",
  "verification_command": "dotnet test",
  "tasks": [
    {
      "id": 1,
      "title": "Create Recipe domain model and database schema",
      "status": "complete",
      "acceptance_criteria": [
        "Recipe entity has Id, Name, Description, Ingredients, Instructions, CreatedAt, UpdatedAt",
        "RecipeDbContext is configured with Recipe entity",
        "Database migration created and can be applied successfully",
        "Unit tests verify model validation rules"
      ],
      "notes": "Using EF Core with PostgreSQL. Migration applied successfully."
    },
    {
      "id": 2,
      "title": "Implement Recipe API endpoints",
      "status": "in_progress",
      "acceptance_criteria": [
        "GET /api/recipes returns all recipes",
        "GET /api/recipes/{id} returns single recipe or 404",
        "POST /api/recipes creates new recipe with validation",
        "PUT /api/recipes/{id} updates existing recipe",
        "DELETE /api/recipes/{id} deletes recipe",
        "All endpoints have OpenAPI documentation",
        "Integration tests verify all CRUD operations"
      ],
      "notes": null
    },
    {
      "id": 3,
      "title": "Create RecipeApiClient in Web project",
      "status": "pending",
      "acceptance_criteria": [
        "RecipeApiClient registered with service discovery",
        "Methods for GetRecipesAsync, GetRecipeAsync, CreateRecipeAsync, UpdateRecipeAsync, DeleteRecipeAsync",
        "Proper error handling and cancellation token support",
        "Unit tests verify client methods"
      ],
      "notes": null
    },
    {
      "id": 4,
      "title": "Build Recipes list page",
      "status": "pending",
      "acceptance_criteria": [
        "Page accessible at /recipes route",
        "Displays all recipes in a grid/table",
        "Shows recipe name, description, and action buttons",
        "Uses StreamRendering and OutputCache",
        "Loading state while fetching data",
        "Link to create new recipe",
        "Link to view recipe details"
      ],
      "notes": null
    },
    {
      "id": 5,
      "title": "Build Create/Edit recipe form",
      "status": "pending",
      "acceptance_criteria": [
        "Form accessible at /recipes/create and /recipes/{id}/edit",
        "EditForm with validation for Name, Description, Ingredients, Instructions",
        "Client-side validation with ValidationSummary",
        "Saves to API and redirects to list on success",
        "Shows error message if save fails",
        "Cancel button returns to list"
      ],
      "notes": null
    },
    {
      "id": 6,
      "title": "Add recipe details view",
      "status": "pending",
      "acceptance_criteria": [
        "Page accessible at /recipes/{id}",
        "Displays full recipe with all fields",
        "Edit and Delete buttons",
        "Confirms before delete",
        "Returns to list after delete",
        "404 page if recipe not found"
      ],
      "notes": null
    }
  ],
  "dependencies": [
    "Requires PostgreSQL added to AppHost",
    "Requires EF Core packages installed"
  ]
}
```

## Field Descriptions

### Top Level

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `slug` | string | Yes | URL-safe identifier (lowercase, hyphenated, max 40 chars) |
| `title` | string | Yes | Human-readable plan title |
| `category` | enum | Yes | One of: `bug`, `feature`, `improvement`, `chore` |
| `created_at` | ISO8601 | Yes | When the plan was created |
| `status` | enum | Yes | One of: `pending`, `in_progress`, `complete`, `blocked` |
| `description` | string | Yes | Detailed description of what this plan accomplishes |
| `verification_command` | string | Yes | Command to verify implementation (e.g., `dotnet test`) |
| `tasks` | array | Yes | List of task objects |
| `dependencies` | array | No | External dependencies or prerequisites |

### Task Object

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | integer | Yes | Sequential task number (1-based) |
| `title` | string | Yes | What this task accomplishes |
| `status` | enum | Yes | One of: `pending`, `in_progress`, `complete`, `blocked` |
| `acceptance_criteria` | array | Yes | List of testable requirements (read by evaluator) |
| `notes` | string | No | Implementation notes, gotchas, decisions |

## Acceptance Criteria Guidelines

Good acceptance criteria are:

✅ **Testable** - Can be verified by looking at code or running tests  
✅ **Specific** - No ambiguity about what "done" means  
✅ **Independent** - Each criterion tests one thing  
✅ **Observable** - Can be checked by the evaluator subagent

Examples:

**Good:**
- "GET /api/recipes returns 200 and array of recipes"
- "Recipe entity has Id, Name, Description fields"
- "Form shows validation error if Name is empty"

**Bad:**
- "API works correctly" (not specific)
- "Good user experience" (not testable)
- "Code is clean" (subjective)

## Status Transitions

```
pending -> in_progress -> complete
pending -> blocked
in_progress -> blocked
blocked -> in_progress
```

## Slug Generation Rules

1. Take the plan title
2. Convert to lowercase
3. Replace spaces with hyphens
4. Remove special characters
5. Truncate to 40 characters
6. Ensure uniqueness

Examples:
- "Add Recipe CRUD Operations" → `add-recipe-crud-operations`
- "Fix API Service Health Check Timeout" → `fix-api-service-health-check-timeout`
- "Improve Database Query Performance in Recipe Search" → `improve-database-query-performance-in-r`
