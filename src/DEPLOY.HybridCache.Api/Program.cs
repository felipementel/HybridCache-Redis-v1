using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHybridCache(options =>
{
    options.ReportTagMetrics = true;
    options.MaximumPayloadBytes = 1024 * 1024;
    options.MaximumKeyLength = 1024;
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromSeconds(5),
        LocalCacheExpiration = TimeSpan.FromSeconds(5)
    };
});

builder.Services.AddStackExchangeRedisCache(options =>
{
    //options.InstanceName = "redis-DEPLOY";
    options.Configuration =
        builder.Configuration.GetConnectionString("RedisConnectionString");
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapGet(
    "/get-using-cache/{name}/{age}",
    async (
        [FromRoute] string name,
        [FromRoute] long age,
        HybridCache hybridCache,
        CancellationToken token) =>
    {
        return await hybridCache.GetOrCreateAsync(
        key: $"{name}-{age}",
          async cancel => await GetDataFromTheSourceAsync(name, age, cancel),
          cancellationToken: token
      );
    })
.WithOpenApi(operation => new(operation)
{
    OperationId = "funcao-app-post1",
    Summary = "Cache simples",
    Description = "Cache simples",
    Tags = new List<OpenApiTag> { new() { Name = "Cache" } }
});

app.MapGet("/get-using-cache/v2/{name}/{age}", async (
    [FromRoute] string name,
    [FromRoute] long age,
    HybridCache hybridCache,
    CancellationToken token) =>
{
    var tags = new List<string> { "tag1", "tag2", "tag3" };
    var entryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(1),
        LocalCacheExpiration = TimeSpan.FromMinutes(1)
    };
    return await hybridCache.GetOrCreateAsync(
        $"{name}-{age}", // Unique key to the cache entry
        async cancel => await GetDataFromTheSourceAsync(name, age, cancel),
        entryOptions,
        tags,
        cancellationToken: token
    );
})
.Produces<IEnumerable<object>>(200, "application/json")
.Produces(201)
.Produces(401)
.Produces(422)
.Produces(500)
.WithOpenApi(operation => new(operation)
{
    OperationId = "funcao-app-post",
    Summary = "Cache completo",
    Description = "Operação para app",
    Tags = new List<OpenApiTag> { new() { Name = "Cache" } }
});

await app.RunAsync();

async Task<string> GetDataFromTheSourceAsync(string name, long age, CancellationToken token)
{
    Thread.Sleep(5000);
    string someInfo = await Task.FromResult($"canalDEPLOY-{name}-{age}");
    return someInfo;
}