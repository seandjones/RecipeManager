# Harness Quick Start Guide

Get started with the RecipeManager harness workflow in 5 minutes.

## What is the Harness?

A structured system for AI agents to:
1. **Plan** features with testable acceptance criteria
2. **Implement** using TDD (backend) and manual testing (frontend)
3. **Verify** with independent evaluator subagent
4. **Track** progress across sessions

## Quick Start

### For Users: Request a Feature

Just tell the AI what you want:

```
"I want to add recipe CRUD operations to the app"
```

The AI will:
1. Ask clarifying questions
2. Create a plan in `.harness/plans/`
3. Execute tasks one by one
4. Update progress in `.harness/progress.md`

### For AI Agents: Execute the Workflow

When user shares a feature request:

**Step 1: Trigger detection**
```
User said: "Add recipe management"
→ Harness skill activated
```

**Step 2: Triage**
```markdown
## Triage
[FEATURE] Add Recipe CRUD (Priority: HIGH)
- Estimated: 6-8 hours across 8 tasks
- Dependencies: Requires database setup
```

**Step 3: Clarify (if needed)**
```markdown
## Clarification Questions
1. Which database? PostgreSQL / Cosmos DB / In-memory
2. Recipe fields? Basic / Detailed / Full nutrition
3. Authentication? Public / Users only / Admin only
```

**Step 4: Create Plan**
```bash
# Generate .harness/plans/add-recipe-crud.json
# with 8 tasks and acceptance criteria for each
```

**Step 5: Execute First Task**
```bash
# Task #1: Add PostgreSQL to AppHost

# 1. Orient - read plan, check progress
# 2. Verify baseline - run dotnet test (should pass)
# 3. Implement - add postgres to AppHost.cs
# 4. Verify - run dotnet test (should still pass)
# 5. Evaluate - spawn evaluator subagent
# 6. Update - mark complete, commit, log progress
```

**Step 6: Repeat for Each Task**

## Example Session

```
User: "I want to add recipe CRUD"

AI: "I'll help you add full recipe management. Quick questions:

1. Database: PostgreSQL (recommended for Aspire) or in-memory for now?
2. Recipe fields: Name, Description, Ingredients, Instructions - anything else?
3. Who can create recipes: Anyone or users only?
"

User: "PostgreSQL, keep it simple for now, anyone can create"

AI: "Perfect! I've created a plan with 8 tasks:

## Plan: add-recipe-crud

1. ✓ Add PostgreSQL to AppHost
2. ✓ Create Recipe model and database
3. ⏳ Implement API endpoints (IN PROGRESS)
4. ⏳ Create API client
5. ⏳ Build recipe list page
6. ⏳ Build create form
7. ⏳ Build edit form
8. ⏳ Build details view

Starting task #1..."

[AI implements, evaluator verifies, task marked complete]

AI: "Task #1 complete! PostgreSQL added to AppHost. 
Starting task #2: Create Recipe model..."
```

## File Overview

```
.harness/
├── plans/
│   └── add-recipe-crud.json          ← Your feature plan
├── progress.md                        ← Session notes
└── runner.py                          ← Status checker

tools/harness-skill/
├── INDEX.md                           ← Workflow overview
├── README.md                          ← Full documentation
├── references/
│   ├── plan-format.md                 ← Plan JSON spec
│   ├── session-protocol.md            ← How to execute tasks
│   ├── evaluator-guide.md             ← How to evaluate work
│   └── tdd-guide.md                   ← TDD workflow
└── templates/
    └── plan-template.json             ← Template for new plans
```

## Check Progress

**View plan status:**
```bash
python3 .harness/runner.py --plan .harness/plans/add-recipe-crud.json --status
```

**View progress log:**
```bash
cat .harness/progress.md
```

**See recent commits:**
```bash
git log --oneline -10
```

## Key Concepts

### Acceptance Criteria
Testable requirements for each task. Example:

✅ Good:
- "GET /api/recipes returns 200 status code"
- "Recipe entity has Id, Name, Description fields"
- "Form shows validation error when Name is empty"

❌ Bad:
- "API works correctly" (not specific)
- "Good user experience" (not testable)
- "Clean code" (subjective)

### TDD (Test-Driven Development)
For backend code:

1. **Red** - Write failing test
2. **Green** - Write minimal code to pass
3. **Refactor** - Clean up (keep tests passing)

Frontend code: Implement directly, test manually.

### Evaluator Subagent
Independent AI agent that:
- Reads your code
- Runs verification command
- Checks acceptance criteria
- Returns PASS | FAIL verdict

**You cannot mark a task complete without evaluator PASS.**

## Troubleshooting

**"Plan creation fails"**
- Check JSON syntax
- Ensure all required fields present
- Verify slug is unique

**"Evaluator keeps failing"**
- Read feedback carefully
- Check if ACs are too vague
- After 2 retries, ask user for help

**"Not sure how to test something"**
- Check [TDD Guide](references/tdd-guide.md)
- Look at [CODE-EXAMPLES.md](CODE-EXAMPLES.md)
- Ask user for clarification

## Next Steps

1. **Read**: [Full Documentation](README.md)
2. **Review**: [Session Protocol](references/session-protocol.md)
3. **Study**: [Example Plan](.harness/plans/example-add-recipe-crud.json)
4. **Practice**: Request a small feature and execute it

## Pro Tips

✅ Keep tasks small (< 2 hours each)  
✅ Write specific acceptance criteria  
✅ Always verify baseline before starting  
✅ Never skip the evaluator step  
✅ Commit after each completed task  
✅ Update progress.md for continuity  

❌ Don't self-evaluate  
❌ Don't work on multiple tasks at once  
❌ Don't ignore test failures  
❌ Don't skip baseline verification  

## Questions?

- Check [README.md](README.md) for detailed workflow
- Review [references/](references/) for guides
- Look at [CODE-EXAMPLES.md](CODE-EXAMPLES.md) for patterns
- Ask the user if still unclear!
