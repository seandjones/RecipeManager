# 🚀 Running RecipeManager Locally (No Docker/Aspire)

Since you're running in a VM (Parallels Desktop) without Docker support, this guide shows you how to run RecipeManager using **local services only**.

## ✅ What You Have

- ✅ PostgreSQL installed locally (localhost:5432)
- ✅ Database `recipedb` configured
- ✅ Code configured to use local PostgreSQL
- ✅ Aspire/Docker features temporarily disabled

## 🎯 Quick Start

### Option 1: Two Terminal Windows (Recommended)

**Terminal 1 - API Service:**
```powershell
.\Start-API.ps1
```

**Terminal 2 - Web Frontend:**
```powershell
.\Start-Web.ps1
```

### Option 2: Manual Start

**Terminal 1:**
```powershell
cd RecipeManager.ApiService
dotnet run --urls "https://localhost:7000;http://localhost:5000"
```

**Terminal 2:**
```powershell
cd RecipeManager.Web
dotnet run --urls "https://localhost:7274;http://localhost:5274"
```

## 🌐 Access the Application

Once both services are running:

- **Web Frontend**: https://localhost:7274
- **API Service**: https://localhost:7000
- **API Swagger**: https://localhost:7000/openapi/v1.json (dev only)

## 🔐 Testing Authentication

The authentication flow works like this:

### Complete Authentication Flow

1. **Navigate to a protected page** (or go directly to `/login`):
   ```
   https://localhost:7274/counter
   or
   https://localhost:7274/login
   ```

2. **Enter your email** on the login page
   - Any valid email format works in development
   - Click "Send Login Code"

3. **Check Terminal 1 (API) for the verification code:**
   ```
   [EmailService] TO: your@email.com
   [EmailService] CODE: 123456
   [EmailService] EXPIRES: 15 minutes
   ```

4. **You'll be automatically redirected to** `/verify-code?email=your@email.com`
   - The 6-digit input will be ready
   - Enter the code you saw in Terminal 1
   - Or paste all 6 digits at once (auto-fills all boxes)

5. **Code is verified automatically** when all 6 digits are entered

6. **You're authenticated!** 🎉
   - Navigation shows your email address
   - Can access all protected routes
   - Logout button available
   - Authentication persists for 30 days

### Important Notes

- **Don't navigate directly to `/verify-code`** - you must go through the login flow first (the page needs your email address)
- **Login codes expire after 15 minutes** - request a new one if needed
- **Rate limiting:** Maximum 3 code requests per hour per email address
- **Authentication cookie:** Lasts 30 days with sliding expiration

### Resending Codes

If you don't receive a code or it expires:

1. Click "Didn't receive a code? Resend" on the verify-code page
2. Or click "Back to login" and start the flow again
3. Check the API terminal for the new code

## 📁 What Was Changed

### Demo Code Removal

The following demo/template code has been removed to focus on authentication features:

**Removed Files:**
- `RecipeManager.Web/Components/Pages/Counter.razor` - Demo counter page
- `RecipeManager.Web/Components/Pages/Weather.razor` - Demo weather page
- `RecipeManager.Web/WeatherApiClient.cs` - Demo API client

**Modified Files:**
- `RecipeManager.Web/Components/Layout/NavMenu.razor` - Removed Counter and Weather links (only Home, About, and Logout remain)
- `RecipeManager.Web/Program.cs` - Removed WeatherApiClient registration
- `RecipeManager.ApiService/Program.cs` - Removed `/weatherforecast` endpoint and WeatherForecast record

**Result:** Clean project focused solely on passwordless authentication features.

### Aspire/Docker Configuration Changes

To run without Docker/Aspire, these changes were made:

### RecipeManager.Web/Program.cs
```csharp
// Commented out:
// builder.AddServiceDefaults();
// builder.AddRedisOutputCache("cache");
// app.MapDefaultEndpoints();
// app.UseOutputCache();

// Changed service discovery URLs:
client.BaseAddress = new("https://localhost:7000"); // Instead of "https+http://apiservice"
```

### RecipeManager.ApiService/Program.cs
```csharp
// Commented out:
// builder.AddServiceDefaults();
// builder.AddNpgsqlDbContext<AuthDbContext>("recipedb");
// app.MapDefaultEndpoints();

// Added direct connection string:
var connectionString = builder.Configuration.GetConnectionString("recipedb") 
    ?? "Host=localhost;Port=5432;Database=recipedb;Username=recipeuser;Password=recipe_dev_password";
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(connectionString));
```

### RecipeManager.Web/appsettings.Development.json
```json
{
  "Services": {
    "apiservice": {
      "https": ["https://localhost:7000"],
      "http": ["http://localhost:5000"]
    }
  }
}
```

### RecipeManager.Web/tsconfig.json
```json
{
  "compilerOptions": {
    "target": "ES2020",
    "lib": ["ES2020", "DOM"],
    "types": ["node"]
  }
}
```

## 🛠️ UI Development Workflow

### Stopping Services

If services are running and you need to rebuild:

**Option 1 - Close Terminal Windows:**
- Close the PowerShell windows running Start-API.ps1 and Start-Web.ps1
- This cleanly stops the services

**Option 2 - Ctrl+C in Terminals:**
- Press `Ctrl+C` in each terminal window where services are running
- Wait for graceful shutdown before starting again

**If processes are locked:**
```powershell
# Close all Visual Studio instances
# Then try:
Get-Process | Where-Object {$_.ProcessName -like "*RecipeManager*"} | Stop-Process -Force
```

### Hot Reload During Development

**Terminal 1 - API (keep running):**
```powershell
.\Start-API.ps1
```

**Terminal 2 - Web with watch:**
```powershell
cd RecipeManager.Web
dotnet watch
```

Changes to `.razor`, `.css`, and `.scss` files will hot-reload automatically!

## 📝 Making UI Adjustments

### Authentication Pages

**Login Page:**
- `RecipeManager.Web/Components/Pages/Login.razor`
- `RecipeManager.Web/Components/Pages/Login.razor.css`
- `RecipeManager.Web/wwwroot/js/login.js`

**Verify Code Page:**
- `RecipeManager.Web/Components/Pages/VerifyCode.razor`
- `RecipeManager.Web/Components/Pages/VerifyCode.razor.css`
- `RecipeManager.Web/wwwroot/js/verify-code.js`

**Access Denied Page:**
- `RecipeManager.Web/Components/Pages/AccessDenied.razor`
- `RecipeManager.Web/Components/Pages/AccessDenied.razor.css`

**Navigation:**
- `RecipeManager.Web/Components/Layout/NavMenu.razor`
- `RecipeManager.Web/Components/Layout/NavMenu.razor.css`

### Shared Styles

**SCSS Variables:**
- `RecipeManager.Web/wwwroot/css/_variables.scss` - Colors, fonts, breakpoints

**Form Styles:**
- `RecipeManager.Web/wwwroot/css/_forms.scss` - Input fields, buttons, validation

**Mixins:**
- `RecipeManager.Web/wwwroot/css/_mixins.scss` - Reusable style patterns

## 🎨 Current Design Features

Your authentication system already has:

- ✅ **Modern gradient backgrounds** (orange/red theme)
- ✅ **Auto-tabbing code input** (6 digits with paste support)
- ✅ **WCAG 2.1 AA accessibility** (color contrast, ARIA labels)
- ✅ **Responsive design** (mobile-friendly)
- ✅ **Clear error messages** (rate limiting, expired codes, validation)
- ✅ **Loading states** (buttons disable during API calls)

## 🐛 Troubleshooting

**Build Errors:**
```powershell
# If you get file lock errors, stop running processes:
Get-Process | Where-Object {$_.ProcessName -like "*RecipeManager*"} | Stop-Process -Force

# Then rebuild:
dotnet build RecipeManager.sln
```

**Port Already in Use:**
```powershell
# Check what's using port 7000:
netstat -ano | findstr :7000

# Kill the process:
Stop-Process -Id <PID> -Force
```

**PostgreSQL Connection Failed:**
```powershell
# Test connection:
$env:PGPASSWORD = "recipe_dev_password"
psql -h localhost -p 5432 -U recipeuser -d recipedb -c "SELECT 1;"

# If it fails, run setup:
.\setup-database.ps1
```

**Can't See Login Codes:**
- Codes appear in **Terminal 1** (where API is running)
- Look for lines starting with `[EmailService]`
- They appear immediately after clicking "Send Code"

**TypeScript Errors:**
- Make sure `tsconfig.json` exists (created by this guide)
- Run `dotnet build` to compile TypeScript to JavaScript

## 🔄 Reverting to Aspire/Docker (Later)

When you want to use Aspire again (e.g., on a non-VM machine):

1. Uncomment the lines in `Program.cs` files:
   ```csharp
   builder.AddServiceDefaults();
   builder.AddRedisOutputCache("cache");
   app.MapDefaultEndpoints();
   app.UseOutputCache();
   ```

2. Change service URLs back:
   ```csharp
   client.BaseAddress = new("https+http://apiservice");
   ```

3. Run with AppHost:
   ```powershell
   dotnet run --project RecipeManager.AppHost
   ```

## 📊 What's Available Now

With this setup, you have access to:

✅ **Full authentication system**
- Login page
- Code verification
- Protected routes
- User display and logout

✅ **All existing pages**
- Home, Counter, Weather, About

✅ **API endpoints**
- Weather forecast (sample)
- Authentication endpoints

✅ **Hot reload**
- Razor components
- CSS/SCSS styles
- TypeScript

## 🎯 Next Steps

1. **Start the services** (see Quick Start above)
2. **Open browser** to https://localhost:7274
3. **Test authentication** (go to /counter, follow login flow)
4. **Make UI adjustments** (edit .razor and .css files)
5. **See changes live** with hot reload!

---

**Pro tip:** Keep Terminal 1 (API) visible so you can see the login codes when testing! 📧
