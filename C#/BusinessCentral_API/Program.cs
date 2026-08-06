namespace BusinessCentral_API;

using System.Text.Json.Nodes;
using System.Reflection;
using API_Management;

internal class Program
{
    private static async Task Main(string[] args)
    {
        string projectName = Assembly.GetEntryAssembly()?.GetName().Name ?? throw new InvalidOperationException("Failed to get project name.");
        var oAuthSecrets = new OAuthPrimer(projectName);
        bool demoMode;

        if (oAuthSecrets.SecretFileIsSet && !string.IsNullOrWhiteSpace(oAuthSecrets.SecretsFilePath))
        {
            string configPath = oAuthSecrets.SecretsFilePath; // now non-null
            Authenticator authenticator = new Authenticator(configPath);
            await authenticator.AcquireToken();

            var httpReq = new HttpReq(authenticator, null);
            demoMode = SelectMode();

            var requestManager = new RequestManager(httpReq, configPath, demoMode);
            await requestManager.EstablishConnection();

            if (!demoMode)
            {
                await QuerySelectedEndpoint(requestManager);
            }
            else
            {
                JsonArray mastersJArray = await requestManager.GetValues("?$top=10&$orderby=unitPrice desc");
                Console.WriteLine(mastersJArray);
            }
        }
        else
        {
            Console.WriteLine("Secrets file path is missing.");
        }
    }

    private static bool SelectMode()
    {
        Console.WriteLine("Do you want to run a preconfigured demo API request or select your own API?");
        Console.WriteLine("1. Demo");
        Console.WriteLine("2. Custom");

        int choice;
        while (true)
        {
            Console.Write("> Enter your choice (1 or 2): ");
            string? input = Console.ReadLine();

            if (int.TryParse(input, out choice) && (choice == 1 || choice == 2))
            {
                break;
            }
            else
            {
                Console.WriteLine("Invalid input. Please enter 1 for Demo or 2 for Custom.");
            }
        }
        Console.WriteLine();
        return choice == 1;
    }

    private static async Task QuerySelectedEndpoint(RequestManager requestManager)
    {
        while (true)
        {
            Console.WriteLine("You're about to ping the selected URI. It's advisable to add a query to reduce the payload, for example \"?$top=10\". Press \"Enter\" to skip query selection.");
            Console.Write("> Enter your query here: ");
            string input = Console.ReadLine() ?? throw new InvalidOperationException("Couldn't parse input.");

            if ((!string.IsNullOrEmpty(input) && input.StartsWith('?')) || string.IsNullOrEmpty(input))
            {
                JsonArray mastersJArray = await requestManager.GetValues(input);
                JSONParser.PrintJsonArrayContent(mastersJArray);
                break;
            }
            else
            {
                Console.WriteLine("Input a valid query for example \"?$top=10\", \"?$filter=<attributeName> eq 'stringValue'\", \"?$filter=<attributeName> eq <int/decValue>\"");
            }
        }
    }
}