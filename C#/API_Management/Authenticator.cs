namespace API_Management;


using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;
using Microsoft.Extensions.Configuration;

public class Authenticator(string configPath)
{
    readonly IConfiguration config = new ConfigurationBuilder().AddJsonFile(configPath).Build();

    internal string? TokenType { get; private set; }
    internal string? AccessToken { get; private set; }

    public async Task AcquireToken()
    {

        IConfigurationSection oAuth = this.config.GetSection("OAuth");
        string? tenantId, clientId, clientSecret, authority, scope;

        tenantId = oAuth["TenantId"];
        clientId = oAuth["ClientId"];
        clientSecret = oAuth["ClientSecret"];
        authority = oAuth["Authority"];
        scope = oAuth["Scope"];

        if (authority != null)
        {
            if (authority.Contains("{{TenantId}}"))
            {
                authority = authority.Replace("{{TenantId}}", tenantId);
            }
        }

        var client = new HttpClient();

        var content = new StringContent("grant_type=client_credentials" +
                                        $"&scope={scope}" +
                                        $"&client_id={HttpUtility.UrlEncode(clientId)}" +
                                        $"&client_secret={HttpUtility.UrlEncode(clientSecret)}");

        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");


        try
        {
            var response = await client.PostAsync(authority, content);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Request failed with status code: {response.StatusCode}");
                return;
            }

            string jsonContent = await response.Content.ReadAsStringAsync();
            JsonNode? node = JsonNode.Parse(jsonContent);

            if (node is not JsonObject result)
            {
                Console.WriteLine("Response is not a valid JSON object.");
                return;
            }

            TokenType = result["token_type"]?.GetValue<string>();
            AccessToken = result["access_token"]?.GetValue<string>();

            if (TokenType is null || AccessToken is null)
            {
                Console.WriteLine("Missing required token fields in response.");
                return;
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP request failed: {ex.Message}");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Failed to parse JSON response: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }
    }
}
