namespace API_Management;

public class HttpReq
{
    public Dictionary<string, string> Headers { get; set; }
    readonly HttpClient client = new();

    public HttpReq(Authenticator auth, Dictionary<string, string>? headers = null)
    {
        Headers = headers ?? new Dictionary<string, string>();

        client.DefaultRequestHeaders.Add("Authorization", $"{auth.TokenType} {auth.AccessToken}");
    }


    public async Task<HttpResponseMessage> SendRequest(string url, string method, string? body = null)
    {
        return method switch
        {
            "GET" => await client.GetAsync(url),
            "POST" => await client.PostAsync(url, new StringContent(body ?? "")),
            _ => throw new ArgumentException($"Unsupported method: {method}")
        };
    }
}