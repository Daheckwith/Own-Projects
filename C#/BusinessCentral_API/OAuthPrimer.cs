namespace BusinessCentral_API;

using Microsoft.Extensions.Configuration;
using System;
using Utilities;

internal class OAuthPrimer : SecretsPrimer
{
    private static readonly string[] RequiredOAuthKeys =
    [
        "TenantId",
        "ClientId",
        "ClientSecret",
        "Authority",
        "Scope"
    ];

    internal OAuthPrimer(string csprojName) : base(csprojName)
    {
        if (SecretFileIsSet)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile(SecretsFilePath, optional: false, reloadOnChange: false)
                .Build();

            ValidateOAuthConfiguration(config);

            Console.WriteLine($"secrets.json exists and OAuth configuration is complete.{Environment.NewLine}");
        }
        else
        {
            BootstrapSecrets();
            SecretFileIsSet = File.Exists(SecretsFilePath);
        }
    }

    private static void ValidateOAuthConfiguration(IConfiguration config)
    {
        IConfigurationSection oAuthSection = config.GetSection("OAuth");
        List<string> missingKeys = [];

        if (!oAuthSection.GetChildren().Any())
        {
            throw new InvalidOperationException("OAuth section was not found in secrets.json.");
        }

        foreach (string key in RequiredOAuthKeys)
        {
            string? value = oAuthSection[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                Console.Write($"> Enter value for OAuth:{key}: ");
                value = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    BuildUserSecrets($"OAuth:{key}", value);
                }
                else
                {
                    missingKeys.Add($"OAuth:{key}");
                }
            }
        }

        if (missingKeys.Count > 0)
        {
            throw new InvalidOperationException(
                $"OAuth section is missing required keys: {string.Join(", ", missingKeys)}");
        }
    }

    public override void BootstrapSecrets()
    {
        base.BootstrapSecrets();
        Console.WriteLine("Let's store the OAuth credentials in User Secrets for this project.");

        Guid tenantId = PromptGuid("Enter Tenant ID: ");
        BuildUserSecrets("OAuth:TenantId", tenantId.ToString());
        BuildUserSecrets("OAuth:Authority", $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token");
        BuildUserSecrets("OAuth:Scope", "https://api.businesscentral.dynamics.com/.default");

        Guid clientId = PromptGuid("Enter Client ID: ");
        BuildUserSecrets("OAuth:ClientId", clientId.ToString());

        Console.Write("> Enter Client Secret: ");
        string clientSecret = Console.ReadLine() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException("Client Secret cannot be empty.");
        }

        BuildUserSecrets("OAuth:ClientSecret", clientSecret);
    }
}