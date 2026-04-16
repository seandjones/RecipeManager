var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL - will use local instance if connection string is provided, otherwise container
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("recipedb");

var cache = builder.AddRedis("cache");

var apiService = builder.AddProject<Projects.RecipeManager_ApiService>("apiservice")
    .WithReference(postgres)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.RecipeManager_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
