using RequestSigning.Common;
using System.Text.Json.Nodes;
using RequestSigning.Server;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

// In-memory client credentials store (use proper storage in production like Azure Key Vault)
Dictionary<string, string> clientCredentials = new()
{
    ["client_id_1"] = "my-client-secret_1",
    ["client_id_2"] = "my-client-secret_2"
};

app.MapPost("/api/data", async (HttpContext context) =>
{
    try
    {
        var clientId = context.Request.Headers.GetHeader("X-Client-Id");
        var timestamp = context.Request.Headers.GetHeader("X-Timestamp");
        var signature = context.Request.Headers.GetHeader("X-Signature");
        
        if (!clientCredentials.TryGetValue(clientId, out var secretKey))
        {
            return Results.Json(
                new ApiResponse(Success: false, Message: "Invalid client id"),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();

        var requestTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(timestamp));
        if (Math.Abs((DateTimeOffset.UtcNow - requestTime).TotalMinutes) > 5)
        {
            return Results.Json(
                new ApiResponse(Success: false, Message: "Request expired"),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var signer = new ApiSigner(secretKey);
        var expectedSignature = signer.SignRequest(
            HttpMethod.Post,
            context.Request.Path,
            timestamp,
            body);

        if (signature != expectedSignature)
        {
            return Results.Json(
                new ApiResponse(Success: false, Message: "Invalid signature"),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return Results.Json(new ApiResponse(
            Success: true,
            Message: "Data processed successfully",
            Data: JsonNode.Parse(body)),
            statusCode: StatusCodes.Status200OK);
    }
    catch (UnauthorizedAccessException ex)
    {
        return Results.Json(
            new ApiResponse(Success: false, Message: ex.Message),
            statusCode: StatusCodes.Status401Unauthorized);
    }
    catch (Exception)
    {
        return Results.Json(
            new ApiResponse(Success: false, Message: "Internal server error"),
            statusCode: StatusCodes.Status500InternalServerError);
    }
});

await app.RunAsync();
