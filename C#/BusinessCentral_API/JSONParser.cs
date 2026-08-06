namespace BusinessCentral_API;

using System.Text.Json.Nodes;

public static class JSONParser
{
    public static JsonArray GetValueJsonArray(string jsonContent)
    {
        JsonObject result = ParseJsonContent(jsonContent);

        JsonArray jArray = result["value"] as JsonArray ?? throw new InvalidOperationException("Root does not contain a 'value' array.");

        return jArray;
    }

    public static JsonObject ParseJsonContent(string jsonContent)
    {
        JsonNode? node = JsonNode.Parse(jsonContent);
        if (node is not JsonObject result)
        {
            throw new InvalidOperationException("Response is not a valid JSON object.");
        }

        return result;
    }

    internal static void PrintJsonArrayContent(JsonArray jsonArray)
    {
        JsonObject? customJObject;

        for (int i = 0; i < jsonArray.Count; i++)
        {
            customJObject = jsonArray[i] as JsonObject;
            Console.WriteLine($"{customJObject}\n");
        }
    }
}