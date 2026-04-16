# Task 6 Complete: AuthApiClient for Authentication API Calls

## Overview
Successfully created AuthApiClient HTTP client in the RecipeManager.Web project for calling authentication API endpoints with comprehensive error handling, cancellation support, and full test coverage.

## What Was Implemented

### 1. AuthModels (DTOs)
Created `RecipeManager.Web/Models/AuthModels.cs` with request/response models matching API contracts:

**Request Models:**
- `RequestLoginCodeRequest` - Contains Email property
- `VerifyLoginCodeRequest` - Contains Email and Code properties

**Response Models:**
- `RequestLoginCodeResponse` - Contains Success, Message, RetryAfterSeconds (nullable)
- `VerifyLoginCodeResponse` - Contains Success, Message, UserId (nullable), Email (nullable)

All models use C# record types for immutability and init-only properties.

### 2. AuthApiClient Service
Created `RecipeManager.Web/Services/AuthApiClient.cs` with primary constructor:

**Methods:**
- `RequestLoginCodeAsync(string email, CancellationToken)` 
  - Calls POST /api/auth/request-code
  - Returns RequestLoginCodeResponse
  - Handles 429 (Too Many Requests) with Retry-After header parsing
  - Includes network error handling

- `VerifyCodeAsync(string email, string code, CancellationToken)`
  - Calls POST /api/auth/verify-code
  - Returns VerifyLoginCodeResponse
  - Returns user information (UserId, Email) on success
  - Handles validation errors

- `LogoutAsync(CancellationToken)`
  - Calls POST /api/auth/logout
  - Returns bool (always true for graceful degradation)
  - Silent failure handling (frontend can clear state independently)

**Error Handling:**
- HttpRequestException → Network error message
- HTTP 429 → Rate limit with RetryAfterSeconds
- Other HTTP errors → Status code message
- Generic exceptions → Generic error message
- TaskCanceledException → Properly propagated (not caught)

**CancellationToken Support:**
All async methods accept optional CancellationToken parameter and properly propagate cancellation exceptions.

### 3. Service Registration
Updated `RecipeManager.Web/Program.cs`:
```csharp
builder.Services.AddHttpClient<AuthApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
```

Uses Aspire service discovery with `https+http://` scheme for automatic resolution to API service.

### 4. Unit Tests
Created `RecipeManager.Tests/AuthApiClientTests.cs` with 10 comprehensive tests:

**Success Cases:**
- RequestLoginCodeAsync returns success response
- VerifyCodeAsync returns user information on valid code
- LogoutAsync returns true on success

**Error Cases:**
- RequestLoginCodeAsync handles rate limiting (429) with RetryAfterSeconds
- RequestLoginCodeAsync handles network errors
- VerifyCodeAsync handles invalid code responses
- VerifyCodeAsync handles network errors
- LogoutAsync returns true even on network error (graceful degradation)

**Cancellation:**
- RequestLoginCodeAsync properly propagates TaskCanceledException
- VerifyCodeAsync properly propagates TaskCanceledException

All tests use Moq to mock HttpMessageHandler for isolated HTTP response testing.

## Files Created/Modified

### Created:
- `RecipeManager.Web/Models/AuthModels.cs` - Request/response DTOs
- `RecipeManager.Web/Services/AuthApiClient.cs` - HTTP client for auth API
- `RecipeManager.Tests/AuthApiClientTests.cs` - 10 unit tests

### Modified:
- `RecipeManager.Web/Program.cs` - Registered AuthApiClient with service discovery
- `RecipeManager.Tests/RecipeManager.Tests.csproj` - Added project reference to Web

## Acceptance Criteria ✅

All 8 acceptance criteria met:

1. ✅ AuthApiClient class created with primary constructor (HttpClient)
2. ✅ RequestLoginCodeAsync(string email) method calls POST /api/auth/request-code
3. ✅ VerifyCodeAsync(string email, string code) method calls POST /api/auth/verify-code
4. ✅ LogoutAsync() method calls POST /api/auth/logout
5. ✅ All methods have proper error handling and return structured responses
6. ✅ CancellationToken support on all async methods
7. ✅ AuthApiClient registered in Web/Program.cs with service discovery
8. ✅ Unit tests verify client methods handle success and error responses

## Testing

- **Build Status**: ✅ Successful
- **AuthApiClient Tests**: 10/10 passing
- **Total Tests**: 35/35 passing (no regressions)
- **Code Coverage**: All methods and error paths tested

## Key Design Decisions

### 1. Response Models Instead of Result<T>
Used the API's existing response models (RequestLoginCodeResponse, VerifyLoginCodeResponse) instead of creating a generic Result<T> pattern. These models already contain:
- Success flag
- Error messages
- Domain-specific data (RetryAfterSeconds, UserId, Email)

This approach:
- Matches API contracts exactly (no mapping needed)
- Provides rich error information
- Avoids unnecessary abstraction

### 2. Rate Limiting Handling
Special handling for HTTP 429 (Too Many Requests):
- Extracts Retry-After header (TimeSpan → seconds)
- Includes in RetryAfterSeconds property
- Enables UI to show countdown timer to users

### 3. Graceful Logout
LogoutAsync always returns true, even on failure:
- Frontend can clear local state independently
- Backend logout failure shouldn't block user experience
- Cookie can be cleared client-side if server fails

### 4. Cancellation Propagation
OperationCanceledException (and TaskCanceledException) are explicitly re-thrown:
- Allows proper async/await cancellation flow
- Enables UI to respond to user cancellation
- Follows .NET async best practices

### 5. Primary Constructor Pattern
Used C# 12 primary constructor for cleaner syntax:
```csharp
public class AuthApiClient(HttpClient httpClient)
```

## Integration Points

### Service Discovery
AuthApiClient uses Aspire service discovery:
- BaseAddress: `https+http://apiservice`
- Automatically resolves to API service via Aspire orchestration
- Supports both HTTPS (preferred) and HTTP fallback

### Used By (Future Tasks)
- **Task #7**: Login page will use `RequestLoginCodeAsync`
- **Task #8**: Code verification page will use `VerifyCodeAsync`
- **Task #9**: Logout button will use `LogoutAsync`

## Error Handling Examples

### Rate Limiting Response:
```json
{
  "Success": false,
  "Message": "Too many requests. Please wait 5 minutes.",
  "RetryAfterSeconds": 300
}
```

### Network Error Response:
```json
{
  "Success": false,
  "Message": "Network error: Connection refused"
}
```

### Success Response (Verify):
```json
{
  "Success": true,
  "Message": "Code verified",
  "UserId": "guid-here",
  "Email": "user@example.com"
}
```

## Next Steps

Ready for **Task #7**: Create Login page (email entry) with SCSS and accessibility.

The login page will:
- Use `AuthApiClient.RequestLoginCodeAsync()` to request codes
- Display rate limit messages with retry countdown
- Show validation errors and network errors
- Redirect to code verification page on success

## Technical Notes

### Testing with Moq
Tests mock HttpMessageHandler using Moq.Protected():
```csharp
mockHandler.Protected()
    .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
    .ReturnsAsync(response);
```

This approach:
- Tests HTTP client behavior without real network calls
- Verifies request serialization
- Tests response deserialization
- Validates error handling paths

### HTTP Client Best Practices
- Registered via AddHttpClient<T> (proper lifetime management)
- Service discovery integration
- No manual HttpClient disposal (managed by DI)
- Cancellation token support throughout
- Structured error responses (no exceptions to callers except cancellation)

## Dependencies

**Runtime:**
- Microsoft.AspNetCore.Components (Blazor)
- System.Net.Http.Json (JSON serialization)
- Aspire service discovery

**Testing:**
- MSTest (test framework)
- Moq (mocking framework)

## Notes

- All methods use `PostAsJsonAsync` for automatic JSON serialization
- All methods use `ReadFromJsonAsync` for automatic JSON deserialization
- Null response handling included (shouldn't happen but defensive)
- Error messages are user-friendly (can be displayed directly in UI)
- No sensitive information in error messages
- All async operations are properly awaited
- No blocking calls or .Result usage
