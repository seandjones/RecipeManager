# RecipeManager

A modern recipe management application built with .NET 10 Aspire, demonstrating cloud-native microservices architecture with distributed tracing, health monitoring, and resilient service communication.

## 🏗️ Architecture

This is a **.NET Aspire distributed application** with the following components:

```
RecipeManager/
├── RecipeManager.AppHost/          # Aspire orchestrator (service topology & infrastructure)
├── RecipeManager.Web/              # Blazor Server frontend (Interactive Server)
├── RecipeManager.ApiService/       # Minimal API backend service
├── RecipeManager.ServiceDefaults/  # Shared Aspire defaults (telemetry, health, resilience)
└── RecipeManager.Tests/            # Integration tests with Aspire.Hosting.Testing
```

### Key Features

- ✅ **Service Discovery** - Automatic service-to-service communication via `https+http://` scheme
- ✅ **Distributed Caching** - Redis-backed output caching for web frontend
- ✅ **Observability** - Built-in OpenTelemetry (metrics, traces, logs) via Aspire Dashboard
- ✅ **Health Checks** - Automatic readiness/liveness probes for all services
- ✅ **Resilience** - Retry policies and circuit breakers on all HTTP clients
- ✅ **Integration Testing** - Full-stack tests using `DistributedApplicationTestingBuilder`

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- [Visual Studio 2026](https://visualstudio.microsoft.com/) (18.4+) or [Visual Studio Code](https://code.visualstudio.com/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for Redis container)

### Running the Application

1. **Clone the repository:**
   ```bash
   git clone <repository-url>
   cd RecipeManager
   ```

2. **Open in Visual Studio:**
   - Open `RecipeManager.sln`
   - Set `RecipeManager.AppHost` as the startup project
   - Press `F5` to run

3. **Access the applications:**
   - **Aspire Dashboard**: Automatically opens in browser (shows all services, logs, traces)
   - **Web Frontend**: Navigate from dashboard or check console output for URL
   - **API Service**: Available via service discovery at `https+http://apiservice`

### Local Run Notes

When starting the app from the command line, use the exact dashboard login URL that `dotnet run --project RecipeManager.AppHost` prints in the terminal. Aspire generates a fresh one-time token on each launch, so old tabs, bookmarks, or copied dashboard root URLs will fail with `Invalid token`.

Recommended local workflow:

1. Stop any running `dotnet` or Aspire processes.
2. Run `dotnet run --project RecipeManager.AppHost`.
3. Copy the `Login to the dashboard at .../login?t=...` URL from the terminal and open that exact link once.
4. From the dashboard, open the `webfrontend` resource and sign in to the app.
5. Use `/ingredient-lists` to test ingredient list creation, sharing, and real-time updates.

If the dashboard still shows `Invalid token`, close all existing dashboard tabs and start a fresh run. If needed, clear any cached site data for `recipemanager.dev.localhost` in your browser and retry with the new login URL.

### Running Tests

```bash
dotnet test RecipeManager.Tests/RecipeManager.Tests.csproj
```

Or use Visual Studio Test Explorer (Ctrl+E, T).

## 📚 Documentation & Workflow

### Harness Skill - Structured AI Development Workflow

This project uses an **Agent Harness** system for structured feature development:

**📂 [tools/harness-skill/](tools/harness-skill/)** - Workflow system for AI agents
- **[INDEX.md](tools/harness-skill/INDEX.md)** - Skill definition & overview
- **[README.md](tools/harness-skill/README.md)** - Complete workflow documentation
- **[references/](tools/harness-skill/references/)** - Session protocol, TDD guide, evaluator guide
- **[CODE-EXAMPLES.md](tools/harness-skill/CODE-EXAMPLES.md)** - RecipeManager code patterns

**📂 [.harness/](.harness/)** - Work artifacts
- **[plans/](.harness/plans/)** - Feature/bug plans with acceptance criteria
- **[progress.md](.harness/progress.md)** - Implementation progress log
- **[runner.py](.harness/runner.py)** - Plan status and automation

### How It Works

```
User Request → Triage → Clarify → Plan → Execute → Evaluate → Complete
```

1. **User shares feature request or bug** (e.g., "Add recipe CRUD")
2. **AI triages and clarifies** requirements
3. **AI creates plan** with testable tasks in `.harness/plans/{slug}.json`
4. **AI implements each task** following TDD for backend, manual testing for frontend
5. **Evaluator subagent verifies** against acceptance criteria (independent check)
6. **AI commits and tracks progress** in `.harness/progress.md`

See [Harness Documentation](tools/harness-skill/README.md) for complete details.

### Architecture Documentation

**[.github/copilot-instructions.md](.github/copilot-instructions.md)** - Aspire architecture, patterns, and conventions for AI coding assistants.

## 📋 Project Structure

### RecipeManager.AppHost
Aspire orchestrator that defines service dependencies and infrastructure:
```csharp
var cache = builder.AddRedis("cache");
var apiService = builder.AddProject<Projects.RecipeManager_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.RecipeManager_Web>("webfrontend")
    .WithReference(cache).WaitFor(cache)
    .WithReference(apiService).WaitFor(apiService);
```

### RecipeManager.Web
Blazor Server application with:
- Interactive Server render mode
- Streaming rendering with `@attribute [StreamRendering(true)]`
- Redis output caching with `@attribute [OutputCache(Duration = 5)]`
- Typed HTTP clients for API communication

### RecipeManager.ApiService
Minimal API backend with:
- OpenAPI/Swagger in development
- Problem Details for error responses
- Health check endpoints (`/health`, `/alive`)

### RecipeManager.ServiceDefaults
Shared library providing:
- OpenTelemetry configuration (logs, metrics, traces)
- Service discovery with resilience handlers
- Health check infrastructure
- HTTP client defaults with retry policies

### RecipeManager.Tests
Integration tests using MSTest with MSTestRunner:
- Spins up full Aspire application
- Tests inter-service communication
- Validates health checks and readiness

## 🔐 Authentication

RecipeManager implements a **passwordless authentication system** using email-based verification codes.

### How It Works

1. **Request Code**: User enters email address on `/login`
2. **Send Email**: System generates 6-digit code, stores in database with 15-minute expiration
3. **Verify Code**: User enters code on `/verify-code`
4. **Authenticated**: System creates authentication cookie (30-day sliding expiration)

### Key Features

- **Passwordless**: No passwords to remember or manage
- **Rate Limiting**: 3 login requests per hour per email address
- **Code Expiration**: Verification codes expire after 15 minutes
- **Security**: Codes stored with expiration timestamps, deleted after verification
- **Cookie Auth**: 30-day sliding expiration with secure, HTTP-only cookies

### Protected Routes

All pages are protected by default except:
- `/login` - Email entry page
- `/verify-code` - Code verification page
- `/access-denied` - Unauthorized access page

Unauthenticated users are redirected to `/login` with return URL preservation.

### API Endpoints

- `POST /api/auth/request-code` - Request verification code
  - Body: `{ "email": "user@example.com" }`
  - Returns: Success message or rate limit error

- `POST /api/auth/verify-code` - Verify code and authenticate
  - Body: `{ "email": "user@example.com", "code": "123456" }`
  - Returns: Success or validation error

- `POST /api/auth/logout` - Sign out user
  - Returns: Success message

### Email Service

**Development Mode**: Logs codes to console
```bash
[EmailService] TO: user@example.com
[EmailService] CODE: 123456
```

**Production Mode**: Sends via SendGrid
- Configure in `RecipeManager.ApiService/appsettings.json`:
  ```json
  {
    "SendGrid": {
      "ApiKey": "your-api-key",
      "FromEmail": "noreply@yourapp.com",
      "FromName": "RecipeManager"
    }
  }
  ```

### Database

Uses PostgreSQL with two tables:

**Users**
- `Id` (UUID, primary key)
- `Email` (unique index)
- `CreatedAt`, `LastLoginAt`

**LoginCodes**
- `Id` (UUID, primary key)
- `Email` (indexed)
- `Code` (6 digits)
- `ExpiresAt` (UTC timestamp)
- `CreatedAt`

### Testing Authentication

1. Run the application (F5 in Visual Studio)
2. Navigate to protected page (e.g., `/counter`)
3. Redirected to `/login`
4. Enter email and click "Send Code"
5. Check console logs for 6-digit code
6. Enter code on `/verify-code`
7. Successfully authenticated!

## 🔧 Development

### Adding a New Service

1. Create a new ASP.NET Core project
2. Add reference to `RecipeManager.ServiceDefaults`
3. In `Program.cs`:
   ```csharp
   builder.AddServiceDefaults();
   // ... configure services
   app.MapDefaultEndpoints();
   ```
4. Register in `AppHost.cs`:
   ```csharp
   builder.AddProject<Projects.YourNewService>("servicename")
       .WithHttpHealthCheck("/health");
   ```

### Service Communication

Use typed HTTP clients with service discovery:
```csharp
builder.Services.AddHttpClient<MyApiClient>(client =>
{
    client.BaseAddress = new("https+http://servicename");
});
```

### Viewing Telemetry

The Aspire Dashboard provides:
- **Resources**: All running services and their status
- **Console Logs**: Real-time logs from each service
- **Structured Logs**: Filterable, searchable log entries
- **Traces**: Distributed request traces across services
- **Metrics**: Performance counters and custom metrics

## 🛠️ Technology Stack

- **.NET 10** - Latest .NET runtime and SDK
- **Aspire 13.1.0** - Cloud-native orchestration and observability
- **Blazor Server** - Interactive server-side rendering
- **Minimal APIs** - Lightweight HTTP API endpoints
- **Redis** - Distributed caching via Aspire.StackExchange.Redis
- **OpenTelemetry** - Industry-standard observability
- **MSTest** - Unit and integration testing framework

## 🐳 Deploying to Azure Container Apps

The Aspire AppHost is a **local development tool only** and is not deployed. In production, `RecipeManager.ApiService` and `RecipeManager.Web` each run as a separate container in Azure Container Apps (ACA).

### Prerequisites

- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) (≥ 2.60)
- Docker CLI
- Azure subscription with Contributor access

### Local Docker Smoke Test

Verify the images build and run together before pushing to Azure:

```bash
docker compose build
docker compose up
```

- Web: [http://localhost:5084](http://localhost:5084)
- API: [http://localhost:5540](http://localhost:5540)
- Health: [http://localhost:5540/health](http://localhost:5540/health)

Email codes are logged to the console (no SendGrid key needed for local Docker testing).

### Environment Variables Reference

| Variable | Service | Description |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | api, web | Set to `Production` |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | web | Set to `true` — required for secure cookies behind ACA's TLS proxy |
| `ConnectionStrings__recipedb` | api | PostgreSQL connection string |
| `ConnectionStrings__cache` | web | Redis connection string (`hostname:6379`) |
| `Authentication__UseConsoleEmailDelivery` | api | Set to `false` in production |
| `SendGrid__ApiKey` | api | SendGrid API key (store as an ACA secret) |
| `SendGrid__FromEmail` | api | Sender email address |
| `SendGrid__FromName` | api | Sender display name |
| `Services__apiservice__https__0` | web | Full HTTPS URL of the deployed API container app |

### Deploying to Azure

```bash
# --- Variables ---
RESOURCE_GROUP=rg-recipemanager-prod
LOCATION=eastus
ACR_NAME=acrrecipemanager        # must be globally unique
ACA_ENV=cae-recipemanager-prod
API_APP=recipemanager-api
WEB_APP=recipemanager-web

# --- Resource group ---
az group create --name $RESOURCE_GROUP --location $LOCATION

# --- Container registry ---
az acr create --name $ACR_NAME --resource-group $RESOURCE_GROUP \
  --sku Basic --admin-enabled true

# --- Build and push images (runs in ACR, no local Docker daemon needed) ---
az acr build --registry $ACR_NAME \
  --image recipemanager-api:latest --file Dockerfile.api .

az acr build --registry $ACR_NAME \
  --image recipemanager-web:latest --file Dockerfile.web .

# --- ACA environment ---
az containerapp env create \
  --name $ACA_ENV \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION

# --- API container app (internal ingress — not publicly routable) ---
az containerapp create \
  --name $API_APP \
  --resource-group $RESOURCE_GROUP \
  --environment $ACA_ENV \
  --image $ACR_NAME.azurecr.io/recipemanager-api:latest \
  --registry-server $ACR_NAME.azurecr.io \
  --ingress internal --target-port 8080 \
  --min-replicas 1 --max-replicas 3 \
  --secrets "sendgrid-api-key=<your-sendgrid-key>" \
  --env-vars \
    ASPNETCORE_ENVIRONMENT=Production \
    "ConnectionStrings__recipedb=<your-postgres-connection-string>" \
    "Authentication__UseConsoleEmailDelivery=false" \
    "SendGrid__ApiKey=secretref:sendgrid-api-key" \
    "SendGrid__FromEmail=noreply@yourdomain.com" \
    "SendGrid__FromName=RecipeManager"

# --- Get the API's internal FQDN ---
API_FQDN=$(az containerapp show \
  --name $API_APP \
  --resource-group $RESOURCE_GROUP \
  --query "properties.configuration.ingress.fqdn" -o tsv)

# --- Web container app (external ingress — publicly accessible) ---
az containerapp create \
  --name $WEB_APP \
  --resource-group $RESOURCE_GROUP \
  --environment $ACA_ENV \
  --image $ACR_NAME.azurecr.io/recipemanager-web:latest \
  --registry-server $ACR_NAME.azurecr.io \
  --ingress external --target-port 8080 \
  --min-replicas 1 --max-replicas 3 \
  --env-vars \
    ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    "ConnectionStrings__cache=<your-redis-connection-string>" \
    "Services__apiservice__https__0=https://$API_FQDN"
```

### Database Migrations

Migrations run automatically when the API container starts — no manual step needed. Ensure the PostgreSQL user in `ConnectionStrings__recipedb` has `CREATE TABLE` permissions on first deploy.

### Health Probes

ACA can be configured to use the built-in health endpoints as container probes:

| Endpoint | Purpose |
|---|---|
| `/health` | Readiness — all checks must pass before traffic is routed |
| `/alive` | Liveness — only "live"-tagged checks; failure triggers a restart |

### Updating After Code Changes

```bash
az acr build --registry $ACR_NAME --image recipemanager-api:latest --file Dockerfile.api .
az acr build --registry $ACR_NAME --image recipemanager-web:latest --file Dockerfile.web .

az containerapp update --name $API_APP --resource-group $RESOURCE_GROUP \
  --image $ACR_NAME.azurecr.io/recipemanager-api:latest

az containerapp update --name $WEB_APP --resource-group $RESOURCE_GROUP \
  --image $ACR_NAME.azurecr.io/recipemanager-web:latest
```

## ⚙️ CI/CD (GitHub Actions)

The workflow at [`.github/workflows/deploy.yml`](.github/workflows/deploy.yml) runs on every push:

| Event | Jobs run |
|---|---|
| Pull request → `main` | `test` only |
| Push to `main` | `test` → `deploy` |

**What `deploy` does:**
1. Logs in to Azure via OIDC (no stored credentials)
2. Builds and pushes both images to ACR (tagged with the commit SHA and `latest`)
3. Updates each container app to the new image

### One-time setup

#### 1. Create a service principal and configure OIDC

```bash
# Create the service principal
SP=$(az ad sp create-for-rbac --name "sp-recipemanager-github" --json-auth --output json)
CLIENT_ID=$(echo $SP | jq -r .clientId)
TENANT_ID=$(echo $SP | jq -r .tenantId)
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

# Grant it Contributor on your resource group
az role assignment create \
  --assignee $CLIENT_ID \
  --role Contributor \
  --scope /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP

# Add a federated credential so GitHub Actions can authenticate without a secret
az ad app federated-credential create \
  --id $CLIENT_ID \
  --parameters '{
    "name": "github-actions-main",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:<your-github-org>/<your-repo-name>:ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

#### 2. Add GitHub secrets

In your repository → **Settings → Secrets and variables → Actions → Secrets**, add:

| Secret | Value |
|---|---|
| `AZURE_CLIENT_ID` | The `clientId` from the step above |
| `AZURE_TENANT_ID` | The `tenantId` from the step above |
| `AZURE_SUBSCRIPTION_ID` | Your Azure subscription ID |

#### 3. Add GitHub variables

In your repository → **Settings → Secrets and variables → Actions → Variables**, add:

| Variable | Value |
|---|---|
| `ACR_NAME` | Your ACR name (e.g. `acrrecipemanager`) |
| `RESOURCE_GROUP` | Your resource group (e.g. `rg-recipemanager-prod`) |
| `API_APP_NAME` | Your API container app name (e.g. `recipemanager-api`) |
| `WEB_APP_NAME` | Your web container app name (e.g. `recipemanager-web`) |

#### 4. Grant the service principal ACR push access

```bash
ACR_ID=$(az acr show --name $ACR_NAME --resource-group $RESOURCE_GROUP --query id -o tsv)

az role assignment create \
  --assignee $CLIENT_ID \
  --role AcrPush \
  --scope $ACR_ID
```

Once these are in place, every merge to `main` automatically tests, builds, and deploys both services.

## 📚 Resources

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Blazor Documentation](https://learn.microsoft.com/aspnet/core/blazor/)
- [Service Discovery in .NET](https://aka.ms/dotnet/sdschemes)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.
