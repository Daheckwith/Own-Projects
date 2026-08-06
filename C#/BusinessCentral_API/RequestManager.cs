namespace BusinessCentral_API;

using System.Text.Json.Nodes;
using API_Management;


internal class RequestManager
{
    internal HttpReq HttpReq { get; }
    internal bool isDemoMode;
    private readonly ApiEndpoints apiEndpoints;

    private class ApiEndpoints
    {
        internal string baseURL = "https://api.businesscentral.dynamics.com/v2.0/{{TenantId}}/{{EnvironmentName}}/api";
        internal string environmentName = " ";
        internal string apiBaseRoute = "/v2.0";
        internal string company = "/companies({{CompanyGUID}})";
        internal string apiEndpoint = " ";

        internal string CompleteURI()
        {
            return $"{baseURL}{apiBaseRoute}{company}{apiEndpoint}";
        }
    }

    internal RequestManager(HttpReq httpReq, string configPath, bool demoMode)
    {
        HttpReq = httpReq ?? throw new ArgumentNullException(nameof(httpReq));
        isDemoMode = demoMode;

        Console.Write("> Enter the Business Central environment you want to connect to: ");
        string environmentName = Console.ReadLine() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(environmentName))
        {
            throw new ArgumentException("Environment name cannot be null or empty.", nameof(environmentName));
        }

        apiEndpoints = new ApiEndpoints();

        IConfiguration config = new ConfigurationBuilder().AddJsonFile(configPath).Build();
        IConfigurationSection oAuth = config.GetSection("OAuth");

        apiEndpoints.baseURL = apiEndpoints.baseURL.Replace("{{TenantId}}", oAuth["TenantId"])
                                                    .Replace("{{EnvironmentName}}", environmentName);
        apiEndpoints.environmentName = environmentName;
        Console.WriteLine();
    }

    internal async Task EstablishConnection()
    {
        string jsonContent;
        JsonArray jsonArray;

        // Company selection
        jsonContent = await this.GetPayload(apiEndpoints.baseURL + "/v2.0/companies");
        jsonArray = JSONParser.GetValueJsonArray(jsonContent);
        SelectCompany(jsonArray);

        if (!isDemoMode)
        {
            // API Route selection
            await SelectApiRoute();

            // Endpoint selection
            jsonContent = await this.GetPayload(apiEndpoints.baseURL + apiEndpoints.apiBaseRoute);
            jsonArray = JSONParser.GetValueJsonArray(jsonContent);
            SelectEndpoint(jsonArray);
        }
        else
        {
            apiEndpoints.apiBaseRoute = "/v2.0";
            apiEndpoints.apiEndpoint = "/items";
        }

        Console.WriteLine("The complete selected URI is:\n" + $"{apiEndpoints.CompleteURI()}\n");
        Thread.Sleep(1500);
    }

    private void SelectCompany(JsonArray companies)
    {
        // key: JSON Attr, value: JSON Value
        Dictionary<string, string> companyJsonAttr = [];
        companyJsonAttr.Add("id", "");
        // companyJsonAttr.Add("name", "");
        companyJsonAttr.Add("displayName", "");

        string selectedCompanyId = EntityParser(companies, companyJsonAttr, 0, "company", "companies");

        apiEndpoints.company = apiEndpoints.company.Replace("{{CompanyGUID}}", selectedCompanyId);
    }

    private async Task SelectApiRoute()
    {
        Console.WriteLine("How do you want to specify the API route?");
        Console.WriteLine("1. Enter the API route manually");
        Console.WriteLine("2. Use standard Microsoft API v2.0");
        Console.WriteLine("3. List all available API routes");

        int choice;
        while (true)
        {
            Console.Write("> Select an option (1-3): ");
            string input = Console.ReadLine() ?? string.Empty;

            if (int.TryParse(input, out choice) && choice >= 1 && choice <= 3)
                break;

            Console.WriteLine("Invalid selection. Enter 1, 2, or 3.");
        }
        Console.WriteLine();

        switch (choice)
        {
            case 1:
                Console.Write("> Enter the API route - e.g. /v2.0 for standard Microsoft APIs or /apiPublisher/apiGroup/apiVersion for custom APIs: ");
                string manualRoute = Console.ReadLine() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(manualRoute))
                    throw new ArgumentException("API route cannot be empty.");
                apiEndpoints.apiBaseRoute = manualRoute;
                Console.WriteLine($"Using custom route: {manualRoute}\n");
                break;

            case 2:
                apiEndpoints.apiBaseRoute = "/v2.0";
                Console.WriteLine("Using standard Microsoft API v2.0.\n");
                break;

            case 3:
                string jsonContent = await this.GetPayload(apiEndpoints.baseURL + "/v2.0/apicategoryroutes");
                JsonArray apiCategoryRoutes = JSONParser.GetValueJsonArray(jsonContent);

                // key: JSON Attr, value: JSON Value
                Dictionary<string, string> apiCategoryRouteJsonAttr = [];
                apiCategoryRouteJsonAttr.Add("route", "");

                string selectedApiRoute = EntityParser(apiCategoryRoutes, apiCategoryRouteJsonAttr, 0, "API-route", "API-routes");
                apiEndpoints.apiBaseRoute = "/" + selectedApiRoute;
                break;
        }
    }

    private void SelectEndpoint(JsonArray endpoints)
    {
        // key: JSON Attr, value: JSON Value
        Dictionary<string, string> endpointJsonAttr = [];
        endpointJsonAttr.Add("name", "");
        // endpointJsonAttr.Add("kind", "");
        endpointJsonAttr.Add("url", "");

        string selectedEndpoint = EntityParser(endpoints, endpointJsonAttr, 1, "endpoint", "endpoints");
        apiEndpoints.apiEndpoint = "/" + selectedEndpoint;
    }

    private static string EntityParser(JsonArray jsonArray, Dictionary<string, string> jsonAttrDict, int defaultKey, string singular, string plural)
    {
        int numberOfKeys = jsonAttrDict.Count;
        if ((defaultKey < 0) || (defaultKey > numberOfKeys - 1))
        {
            throw new InvalidOperationException($"defaultKey cannot be < 0 or exceed {numberOfKeys - 1}.");
        }

        IEnumerable<string> keys = jsonAttrDict.Keys;

        if (jsonArray.Count == 0)
        {
            throw new InvalidOperationException($"No {plural} were returned for this environment.");
        }

        List<string> entities = [];
        JsonObject? jsonObject;
        string consoleOutput = "";

        Console.WriteLine($"Choose one of the available {plural}:");
        Thread.Sleep(1500);
        for (int i = 0; i < jsonArray.Count; i++)
        {
            jsonObject = jsonArray[i] as JsonObject;

            if (jsonObject is null)
            {
                continue;
            }

            foreach (string key in jsonAttrDict.Keys)
            {
                jsonAttrDict[key] = jsonObject[key]?.GetValue<string>() ?? $"missing {key}";
                consoleOutput += $"{key}: {jsonAttrDict[key]} ";
            }

            entities.Add(jsonAttrDict.Values.ElementAt(defaultKey));
            Console.WriteLine($"{entities.Count}. {consoleOutput}");
            consoleOutput = "";
        }

        if (entities.Count == 0)
        {
            throw new InvalidOperationException($"No valid {singular} objects were found in the response.");
        }

        int selectedIndex = Selector(entities, singular);
        string selectedEntity = entities[selectedIndex - 1];
        Console.WriteLine($"Selected {singular} ({selectedIndex}) {keys.ElementAt(defaultKey)}: {selectedEntity}\n");
        return selectedEntity;
    }

    private static int Selector(List<string> selectionList, string entity)
    {
        int selectedIndex;
        while (true)
        {
            Console.Write($"> Select {entity} by number: ");
            string input = Console.ReadLine() ?? string.Empty;

            if (int.TryParse(input, out selectedIndex) && selectedIndex >= 1 && selectedIndex <= selectionList.Count)
            {
                break;
            }

            Console.WriteLine($"Invalid selection. Enter a number between 1 and {selectionList.Count}.");
        }

        return selectedIndex;
    }

    internal async Task<JsonArray> GetValues(string query)
    {
        string jsonContent = await Get(query);

        JsonArray jsonArray = JSONParser.GetValueJsonArray(jsonContent);
        return jsonArray;
    }

    internal async Task<JsonObject> GetJsonObject(string query)
    {
        string jsonContent = await Get(query);

        JsonObject jsonObject = JSONParser.ParseJsonContent(jsonContent);
        return jsonObject;
    }

    private async Task<string> Get(string query)
    {
        string target = apiEndpoints.CompleteURI();
        target += query;

        string jsonContent = await this.GetPayload(target);
        Console.WriteLine("Queried: \n" + $"{target}\n");

        return jsonContent;
    }

    private async Task<string> GetPayload(string target)
    {
        HttpResponseMessage response = await HttpReq.SendRequest(target, "GET", "");

        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
            $"Request failed: {(int)response.StatusCode} {response.StatusCode}. Body: {body}",
            null,
            response.StatusCode);
        }

        string jsonContent = await response.Content.ReadAsStringAsync();
        return jsonContent;
    }
}