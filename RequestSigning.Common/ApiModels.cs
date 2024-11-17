using System.Text.Json.Nodes;

namespace RequestSigning.Common;

public record struct ApiCredentials(string ClientId, string SecretKey);

public record struct SingedRequest(
    HttpMethod Method,
    string Endpoint,
    string Timestamp,
    string Signature,
    string ClientId,
    JsonNode? Payload = default);
    
public record struct ApiResponse(
    bool Success,
    string Message,
    JsonNode? Data = default);