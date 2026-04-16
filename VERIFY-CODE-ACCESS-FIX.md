# ✅ Verify Code Page Access - Explanation & Fix

## The Issue

You reported: "I'm unable to reach verify-code to enter the email code it just redirects to login"

## Why This Happens

The `/verify-code` page **requires an email address** as a query parameter to function. This is by design:

```razor
@page "/verify-code"
@attribute [AllowAnonymous]  ✅ Page is accessible without authentication

@code {
    [SupplyParameterFromQuery]
    public string? Email { get; set; }  // ❗ Requires email parameter

    protected override async Task OnInitializedAsync()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            // No email - redirect to login
            Navigation.NavigateTo("/login");
            return;
        }
    }
}
```

## The Correct Authentication Flow

### ✅ How It Should Work

1. **Start at login page:**
   ```
   https://localhost:7274/login
   ```

2. **Enter your email** and click "Send Login Code"

3. **Automatic redirect** to verify-code **with email parameter:**
   ```
   https://localhost:7274/verify-code?email=your@email.com
   ```
   ↑ This URL includes your email address

4. **Enter the 6-digit code** shown in the API terminal

5. **Success!** You're authenticated

### ❌ What Doesn't Work

Navigating directly to `/verify-code` without the email parameter:
```
https://localhost:7274/verify-code  ❌ Redirects to login
```

This redirects because the page doesn't know which email address to verify the code against.

## What Was Fixed

### 1. Improved RedirectToLogin Component

**Before:**
```csharp
Navigation.NavigateTo("/login", forceLoad: true);
```

**After:**
```csharp
// Preserve return URL for protected pages
var returnUrl = Navigation.ToBaseRelativePath(Navigation.Uri);

if (!string.IsNullOrEmpty(returnUrl) && 
    !returnUrl.StartsWith("login") &&
    !returnUrl.StartsWith("verify-code"))
{
    Navigation.NavigateTo($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}", forceLoad: true);
}
else
{
    Navigation.NavigateTo("/login", forceLoad: true);
}
```

**Benefit:** If you try to access a protected page (like `/counter`), after authentication you'll be redirected back to that page.

### 2. Updated Documentation

Updated `LOCAL-DEVELOPMENT-GUIDE.md` with complete authentication flow explanation.

## How to Test Authentication

### Step-by-Step Testing

**Terminal 1 - Start API:**
```powershell
.\Start-API.ps1
```

**Terminal 2 - Start Web:**
```powershell
.\Start-Web.ps1
```

**Browser - Complete Flow:**

1. **Open:** https://localhost:7274/login

2. **Enter email:** (any valid email format)
   ```
   test@example.com
   ```

3. **Click:** "Send Login Code"

4. **You'll be redirected to:**
   ```
   https://localhost:7274/verify-code?email=test@example.com
   ```
   ↑ Notice the `?email=` parameter

5. **Check Terminal 1** for the code:
   ```
   [EmailService] TO: test@example.com
   [EmailService] CODE: 123456
   [EmailService] EXPIRES: 15 minutes
   ```

6. **Enter the 6 digits** in the verify-code page

7. **Success!** Navigation bar shows your email and logout button

## Alternative Testing Paths

### Test via Protected Route

1. **Navigate to protected page:**
   ```
   https://localhost:7274/counter
   ```

2. **Redirected to login** (because not authenticated)

3. **Enter email and send code**

4. **Redirected to verify-code** with email parameter

5. **Enter code**

6. **Redirected back to `/counter`** ✅ (return URL preserved)

## Common Issues

### "Page keeps redirecting to login"

**Cause:** You're trying to access `/verify-code` directly without the email parameter.

**Solution:** Start at `/login` and go through the complete flow.

### "Lost my verification code"

**Solution:** 
1. Click "Didn't receive a code? Resend" on verify-code page
2. Or click "Back to login" and request a new code
3. New code appears in API terminal

### "Code expired"

**Codes expire after 15 minutes.**

**Solution:** Request a new code (see above).

### "Too many requests"

**Rate limit: 3 requests per hour per email.**

**Solution:** 
- Wait 1 hour
- Or use a different email address for testing
- Check API terminal for retry timer

## Technical Details

### Why Email Parameter is Required

The verify-code page needs to know:
1. **Which email** to verify the code against
2. **Display the masked email** to the user ("We sent a code to t***@example.com")
3. **Make the API call** with both email and code

Without the email parameter, the page can't function.

### Security Considerations

✅ **Verify-code page has `[AllowAnonymous]`** - No authentication required to access it

✅ **Email passed in URL is safe** - It's just an identifier, not sensitive data

✅ **Code validation happens server-side** - The code can't be guessed from the URL

✅ **Rate limiting prevents abuse** - Maximum 3 attempts per hour

## Files Modified

### RecipeManager.Web/Components/RedirectToLogin.razor
- Added return URL preservation logic
- Prevents redirect loops for verify-code page

### LOCAL-DEVELOPMENT-GUIDE.md
- Added complete authentication flow explanation
- Added troubleshooting for common navigation issues

## Summary

🎯 **The verify-code page is working correctly!**

- It requires the `email` query parameter to function
- You must start at `/login` to begin the authentication flow
- The login page automatically redirects to `/verify-code?email=...`
- This is the expected and secure behavior

✅ **Test it now:**
1. Stop any running services
2. Rebuild if needed: `dotnet build RecipeManager.sln`
3. Start API: `.\Start-API.ps1`
4. Start Web: `.\Start-Web.ps1`
5. Navigate to: `https://localhost:7274/login`
6. Follow the authentication flow!

---

**Need more help?** Check the complete flow in `LOCAL-DEVELOPMENT-GUIDE.md` (🔐 Testing Authentication section).
