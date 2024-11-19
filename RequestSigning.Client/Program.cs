using System.Net.Http.Json;
using RequestSigning.Common;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;

var serviceProvider = new ServiceCollection()
    .AddHttpClient("localClient", client =>
    {
        client.BaseAddress = new Uri("http://localhost:5252");
    })
    .Services
    .BuildServiceProvider();
var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
var client = clientFactory.CreateClient("localClient");

var credentials = new ApiCredentials("client_id_1", "my-client-secret_1");
var signer = new ApiSigner(credentials.SecretKey);

try
{
    var payload = JsonNode.Parse($$"""
    {
      "message": "Hello, Server! This is a signed request from the Client.",
      "timestamp": "{{DateTimeOffset.UtcNow:yyyy-MM-ddTHH:mm:ssZ}}"
    }
    """);
    
    Console.WriteLine("sending signed request...");
    var response = await SendSignedRequest(payload!);
    
    Console.WriteLine($"""
        Response:
        Success: {response.Success}
        Message: {response.Message}
        Data: {response.Data?.ToJsonString()}
        """);
}
catch (Exception e)
{
    Console.WriteLine($"Error from this point: {e.Message}");
}

async Task<ApiResponse> SendSignedRequest(JsonNode payload)
{
    const string endpoint = "/api/data";
    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
    var signature = signer.SignRequest(HttpMethod.Post, endpoint, timestamp, payload.ToJsonString());

    using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
    {
        Headers =
        {
            {"X-Client-Id", credentials.ClientId},
            {"X-Timestamp", timestamp},
            {"X-Signature", signature}
        },
        Content = JsonContent.Create(payload)
    };

    var response = await client.SendAsync(request);
    return await response.Content.ReadFromJsonAsync<ApiResponse>();
}