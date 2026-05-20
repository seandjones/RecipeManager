using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using RecipeManager.Web.Models;
using System.Security.Claims;

namespace RecipeManager.Web.Services;

public class IngredientListSignalRClient(
    NavigationManager navigationManager,
    AuthenticationStateProvider authenticationStateProvider,
    IConfiguration configuration) : IAsyncDisposable
{
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private HubConnection? _connection;
    private Guid? _currentListId;
    private Guid? _currentUserId;

    public event Func<Guid, IngredientItem, Task>? OnIngredientAdded;
    public event Func<Guid, Guid, Task>? OnIngredientRemoved;
    public event Func<Guid, IngredientItem, Task>? OnIngredientUpdated;
    public event Func<Guid, Guid, bool, Task>? OnIngredientCheckStateUpdated;
    public event Func<Guid, int, Task>? OnRecipeAdded;
    public event Func<Guid, int, Task>? OnRecipeRemoved;

    public virtual async Task InitializeAsync(Guid listId, CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_currentListId == listId && _connection is { State: HubConnectionState.Connected })
            {
                return;
            }

            await DisconnectInternalAsync();

            _currentUserId = await ResolveCurrentUserIdAsync();
            _connection = CreateConnection();
            _currentListId = listId;

            await _connection.StartAsync(cancellationToken);
            await _connection.InvokeAsync("JoinListGroup", listId, cancellationToken);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public virtual async Task DisconnectAsync(Guid listId, CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_currentListId == listId && _connection is { State: HubConnectionState.Connected })
            {
                await _connection.InvokeAsync("LeaveListGroup", listId, cancellationToken);
            }

            await DisconnectInternalAsync();
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public virtual async Task UpdateIngredientCheckStateAsync(Guid listId, Guid ingredientId, bool isChecked, CancellationToken cancellationToken = default)
    {
        if (_connection is null)
        {
            return;
        }

        await _connection.InvokeAsync("UpdateIngredientCheckState", listId, ingredientId, isChecked, cancellationToken);
    }

    public virtual async Task AddIngredientAsync(Guid listId, IngredientRequest ingredient, CancellationToken cancellationToken = default)
    {
        if (_connection is null)
        {
            return;
        }

        await _connection.InvokeAsync("AddIngredient", listId, new
        {
            name = ingredient.Name,
            quantity = ingredient.Quantity,
            unit = ingredient.Unit,
            isChecked = ingredient.IsChecked
        }, cancellationToken);
    }

    public virtual async Task RemoveIngredientAsync(Guid listId, Guid ingredientId, CancellationToken cancellationToken = default)
    {
        if (_connection is null)
        {
            return;
        }

        await _connection.InvokeAsync("RemoveIngredient", listId, ingredientId, cancellationToken);
    }

    public virtual async Task AddRecipeAsync(Guid listId, int recipeId, CancellationToken cancellationToken = default)
    {
        if (_connection is null)
        {
            return;
        }

        await _connection.InvokeAsync("AddRecipeToList", listId, recipeId, cancellationToken);
    }

    public virtual async Task RemoveRecipeAsync(Guid listId, int recipeId, CancellationToken cancellationToken = default)
    {
        if (_connection is null)
        {
            return;
        }

        await _connection.InvokeAsync("RemoveRecipeFromList", listId, recipeId, cancellationToken);
    }

    private HubConnection CreateConnection()
    {
        var hubUri = ResolveHubUri();

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUri, options =>
            {
                if (_currentUserId.HasValue)
                {
                    options.Headers["X-User-Id"] = _currentUserId.Value.ToString();
                }
            })
            .WithAutomaticReconnect(new ExponentialBackoffRetryPolicy())
            .Build();

        connection.On<Guid, IngredientItem>("OnIngredientAdded", async (listId, ingredient) =>
        {
            if (OnIngredientAdded is not null)
            {
                await OnIngredientAdded.Invoke(listId, ingredient);
            }
        });

        connection.On<Guid, Guid>("OnIngredientRemoved", async (listId, ingredientId) =>
        {
            if (OnIngredientRemoved is not null)
            {
                await OnIngredientRemoved.Invoke(listId, ingredientId);
            }
        });

        connection.On<Guid, IngredientItem>("OnIngredientUpdated", async (listId, ingredient) =>
        {
            if (OnIngredientUpdated is not null)
            {
                await OnIngredientUpdated.Invoke(listId, ingredient);
            }
        });

        connection.On<Guid, Guid, bool>("OnIngredientCheckStateUpdated", async (listId, ingredientId, isChecked) =>
        {
            if (OnIngredientCheckStateUpdated is not null)
            {
                await OnIngredientCheckStateUpdated.Invoke(listId, ingredientId, isChecked);
            }
        });

        connection.On<Guid, int>("OnRecipeAdded", async (listId, recipeId) =>
        {
            if (OnRecipeAdded is not null)
            {
                await OnRecipeAdded.Invoke(listId, recipeId);
            }
        });

        connection.On<Guid, int>("OnRecipeRemoved", async (listId, recipeId) =>
        {
            if (OnRecipeRemoved is not null)
            {
                await OnRecipeRemoved.Invoke(listId, recipeId);
            }
        });

        connection.Reconnected += async _ =>
        {
            if (_currentListId.HasValue)
            {
                await connection.InvokeAsync("JoinListGroup", _currentListId.Value);
            }
        };

        return connection;
    }

    private Uri ResolveHubUri()
    {
        var baseAddress = ResolveApiServiceBaseUri() ?? navigationManager.ToAbsoluteUri("/");
        return new Uri(baseAddress, "/hubs/ingredient-list");
    }

    private Uri? ResolveApiServiceBaseUri()
    {
        var apiServiceSection = configuration.GetSection("Services:apiservice");
        if (!apiServiceSection.Exists())
        {
            return null;
        }

        var httpsEndpoint = GetFirstValidEndpoint(apiServiceSection.GetSection("https"));
        if (httpsEndpoint is not null)
        {
            return httpsEndpoint;
        }

        var httpEndpoint = GetFirstValidEndpoint(apiServiceSection.GetSection("http"));
        if (httpEndpoint is not null)
        {
            return httpEndpoint;
        }

        return null;
    }

    private static Uri? GetFirstValidEndpoint(IConfigurationSection endpointSection)
    {
        if (!endpointSection.Exists())
        {
            return null;
        }

        if (Uri.TryCreate(endpointSection.Value, UriKind.Absolute, out var directEndpoint))
        {
            return directEndpoint;
        }

        foreach (var endpoint in endpointSection.GetChildren())
        {
            if (Uri.TryCreate(endpoint.Value, UriKind.Absolute, out var parsedEndpoint))
            {
                return parsedEndpoint;
            }
        }

        return null;
    }

    private async Task<Guid?> ResolveCurrentUserIdAsync()
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var claimValue = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? authState.User.FindFirst("userId")?.Value
            ?? authState.User.FindFirst("sub")?.Value;

        return Guid.TryParse(claimValue, out var userId) ? userId : null;
    }

    private async Task DisconnectInternalAsync()
    {
        if (_connection is null)
        {
            _currentListId = null;
            return;
        }

        try
        {
            await _connection.StopAsync();
        }
        catch
        {
            // Ignore disconnect errors; connection is disposed below.
        }

        await _connection.DisposeAsync();
        _connection = null;
        _currentListId = null;
        _currentUserId = null;
    }

    public async ValueTask DisposeAsync()
    {
        await _connectionLock.WaitAsync();
        try
        {
            await DisconnectInternalAsync();
        }
        finally
        {
            _connectionLock.Release();
            _connectionLock.Dispose();
        }
    }

    private sealed class ExponentialBackoffRetryPolicy : IRetryPolicy
    {
        private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(30);

        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            // Stop reconnect attempts after one minute of elapsed reconnect time.
            if (retryContext.ElapsedTime > TimeSpan.FromMinutes(1))
            {
                return null;
            }

            var attempt = Math.Max(0, retryContext.PreviousRetryCount);
            var seconds = Math.Min(Math.Pow(2, attempt), MaxDelay.TotalSeconds);
            return TimeSpan.FromSeconds(seconds);
        }
    }
}
