# Session Protocol

Follow this protocol when executing each task from a plan.

## Pre-Session Checklist

Before starting any task:

- [ ] Plan file exists and is valid JSON
- [ ] Task status is `pending` or `in_progress`
- [ ] All blocking tasks are `complete`
- [ ] You have a clear understanding of acceptance criteria

## Session Phases

### Phase 1: Orient (5 min)

**Goal:** Understand context and current state

1. **Read the plan**
   ```bash
   cat .harness/plans/{slug}.json
   ```
   - What is the task trying to accomplish?
   - What are the acceptance criteria?
   - Are there dependencies from previous tasks?

2. **Check progress notes**
   ```bash
   cat .harness/progress.md
   ```
   - What was done recently?
   - Are there warnings or gotchas?
   - What is the current state?

3. **Review recent changes**
   ```bash
   git log --oneline -10
   git diff HEAD~3..HEAD --stat
   ```
   - What files were changed recently?
   - Is there ongoing work that might conflict?

### Phase 2: Verify Baseline (2 min)

**Goal:** Confirm starting point before making changes

1. **Run verification command**
   ```bash
   # From plan's verification_command field
   dotnet test
   ```

2. **Baseline expectations:**
   - Tests should pass (or have expected failures)
   - Build should succeed
   - No unrelated failures

3. **If baseline fails:**
   - STOP - do not proceed
   - Document the failure
   - Ask user to fix baseline before continuing

### Phase 3: Implement (30-90 min)

**Goal:** Implement the task according to acceptance criteria

#### For Backend Tasks (API, Services, Database)

**Follow TDD:**

1. **Write failing tests first**
   ```csharp
   [TestMethod]
   public async Task GetRecipes_ReturnsOkWithRecipes()
   {
       // Arrange
       var context = CreateTestDbContext();
       context.Recipes.Add(new Recipe { Name = "Test Recipe" });
       await context.SaveChangesAsync();
       
       // Act
       var result = await _controller.GetRecipes();
       
       // Assert
       Assert.IsInstanceOfType(result, typeof(OkObjectResult));
       var recipes = ((OkObjectResult)result).Value as Recipe[];
       Assert.IsNotNull(recipes);
       Assert.AreEqual(1, recipes.Length);
   }
   ```

2. **Run tests - expect failure**
   ```bash
   dotnet test --filter GetRecipes_ReturnsOkWithRecipes
   # Should fail with "method not found" or similar
   ```

3. **Implement minimum code to pass**
   ```csharp
   app.MapGet("/api/recipes", async (RecipeDbContext db) =>
   {
       var recipes = await db.Recipes.ToArrayAsync();
       return Results.Ok(recipes);
   });
   ```

4. **Run tests - expect success**
   ```bash
   dotnet test --filter GetRecipes_ReturnsOkWithRecipes
   # Should pass
   ```

5. **Refactor if needed** (keep tests green)

6. **Repeat for each acceptance criterion**

#### For Frontend Tasks (Blazor Components)

**Implement directly, test manually:**

1. **Create component**
   ```razor
   @page "/recipes"
   @attribute [StreamRendering(true)]
   @attribute [OutputCache(Duration = 10)]
   @inject RecipeApiClient RecipeApi
   
   <h1>Recipes</h1>
   
   @if (recipes == null)
   {
       <p><em>Loading...</em></p>
   }
   else
   {
       @foreach (var recipe in recipes)
       {
           <div>@recipe.Name</div>
       }
   }
   
   @code {
       private Recipe[]? recipes;
       
       protected override async Task OnInitializedAsync()
       {
           recipes = await RecipeApi.GetRecipesAsync();
       }
   }
   ```

2. **Add navigation**
   ```razor
   <!-- In NavMenu.razor -->
   <NavLink href="recipes">Recipes</NavLink>
   ```

3. **Test manually in browser**
   - Start app, navigate to /recipes
   - Verify loading state appears
   - Verify data loads
   - Test edge cases (empty list, errors)

4. **Add unit tests only for complex logic**
   - Validation logic
   - State management
   - Complex calculations

### Phase 4: Run Verification (5 min)

**Goal:** Confirm implementation doesn't break existing functionality

1. **Run full test suite**
   ```bash
   dotnet test
   ```

2. **Check for:**
   - All tests pass
   - No new warnings
   - Build succeeds

3. **If verification fails:**
   - Fix the issues
   - Re-run verification
   - Don't proceed to evaluation until green

### Phase 5: Evaluate (Subagent - 10 min)

**Goal:** Independent verification against acceptance criteria

**CRITICAL: Do NOT skip this step. Do NOT self-evaluate.**

1. **Spawn evaluator subagent**
   
   Use the Agent tool with this exact prompt:
   
   ```
   You are a skeptical code evaluator. Your job is to find problems, not praise.
   
   Follow the evaluation steps in tools/harness-skill/references/evaluator-guide.md exactly.
   
   Evaluate task {task_id}: "{task_title}" from plan .harness/plans/{slug}.json
   
   Do NOT fix anything. Only read code, run the verification command, and produce a VERDICT.
   ```

2. **Wait for evaluator verdict**
   - `OVERALL: PASS` → Proceed to Phase 6
   - `OVERALL: FAIL` → Fix issues and spawn new evaluator

3. **Retry limit: 2 cycles**
   - implement → evaluate → fix → evaluate → fix → evaluate
   - If still failing after 2 retries, stop and ask user

### Phase 6: Update State (5 min)

**Goal:** Record completion and progress

1. **Mark task complete** (only after evaluator PASS)
   ```json
   {
     "id": 2,
     "status": "complete",
     "notes": "Implemented all CRUD endpoints. Tests passing."
   }
   ```

2. **Commit changes**
   Use `/commit` tool with descriptive message:
   ```
   feat: implement recipe API CRUD endpoints
   
   - Added GET /api/recipes (list all)
   - Added GET /api/recipes/{id} (get by id)
   - Added POST /api/recipes (create)
   - Added PUT /api/recipes/{id} (update)
   - Added DELETE /api/recipes/{id} (delete)
   - All endpoints have OpenAPI docs
   - Integration tests verify all operations
   
   Resolves task #2 from plan: add-recipe-crud-operations
   ```

3. **Update progress notes**
   ```markdown
   ## 2026-04-12 - Recipe API CRUD (Task #2)
   
   Implemented all recipe CRUD endpoints in ApiService/Program.cs
   
   - Used minimal APIs with proper error handling
   - Added RecipeDbContext with EF Core
   - Integration tests in RecipeManager.Tests/ApiTests.cs
   - All tests passing (23/23)
   
   **Gotchas:**
   - Need to call `db.SaveChangesAsync()` in POST/PUT/DELETE
   - Validation errors return 400 with ProblemDetails
   
   **Next:** Task #3 - Create RecipeApiClient in Web project
   ```

## Time Estimates

| Phase | Typical Duration |
|-------|------------------|
| Orient | 5 minutes |
| Verify Baseline | 2 minutes |
| Implement (backend) | 30-60 minutes |
| Implement (frontend) | 30-90 minutes |
| Run Verification | 5 minutes |
| Evaluate | 10 minutes |
| Update State | 5 minutes |
| **Total per task** | **60-120 minutes** |

## Red Flags

Stop and ask for help if:

- Baseline verification fails before you start
- You can't understand the acceptance criteria
- Implementation takes > 2 hours for one task
- Evaluator fails twice in a row
- You're tempted to skip the evaluator step
- You don't know how to test something

## Best Practices

✅ **Do:**
- Follow TDD for backend code
- Test manually for frontend code
- Keep commits atomic and descriptive
- Update progress notes after each task
- Read the evaluator's feedback carefully

❌ **Don't:**
- Skip the evaluator subagent
- Mark tasks complete without PASS verdict
- Commit without running verification
- Work on multiple tasks simultaneously
- Ignore test failures
