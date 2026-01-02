using System.IO;
using System.Text.Json;

namespace VendingService.WPF.Services;

public sealed class AppSettings
{
    public string ApiBaseUrl { get; init; } = "http://localhost:5089/";

    public static AppSettings Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new AppSettings();
        }

        var json = File.ReadAllText(filePath);
        var settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return settings ?? new AppSettings();
    }
}

