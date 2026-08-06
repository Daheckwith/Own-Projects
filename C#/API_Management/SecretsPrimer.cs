namespace BusinessCentral_API;

using System.Diagnostics;
using System.Xml.Linq;

public class SecretsPrimer
{
    public bool SecretFileIsSet { get; protected set; }
    public string SecretsFilePath { get; }

    public SecretsPrimer(string csprojName)
    {
        string csprojPath = Directory.GetFiles(Directory.GetCurrentDirectory(), $"{csprojName}.csproj").FirstOrDefault()
            ?? throw new FileNotFoundException($"No .csproj file found for project '{csprojName}'.");

        var doc = XDocument.Load(csprojPath);
        string? userSecretsId = doc.Root?.Element("PropertyGroup")?.Element("UserSecretsId")?.Value;

        if (string.IsNullOrWhiteSpace(userSecretsId))
        {
            throw new InvalidOperationException("UserSecretsId not found in .csproj. Set a secret first with: dotnet user-secrets init");
        }

        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string userSecretsPath = Path.Combine(appDataPath, "Microsoft", "UserSecrets", userSecretsId);
        this.SecretsFilePath = Path.Combine(userSecretsPath, "secrets.json");
        this.SecretFileIsSet = File.Exists(SecretsFilePath);
    }

    public static Guid PromptGuid(string prompt)
    {
        Console.Write("> " + prompt);
        if (!Guid.TryParse(Console.ReadLine(), out Guid value))
        {
            throw new InvalidOperationException("Invalid GUID format.");
        }

        return value;
    }

    public static void BuildUserSecrets(string key, string value)
    {
        var psi = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = Directory.GetCurrentDirectory()
        };

        psi.ArgumentList.Add("user-secrets");
        psi.ArgumentList.Add("set");
        psi.ArgumentList.Add(key);
        psi.ArgumentList.Add(value);

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet user-secrets command.");
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"dotnet user-secrets set failed: {error}");
        }

        Console.WriteLine(output);
    }
}
