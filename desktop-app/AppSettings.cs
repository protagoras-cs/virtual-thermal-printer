using System.Text.Json;

namespace VirtualThermalPrinterDesktop;

public sealed class AppSettings
{
    public int HttpPort { get; set; } = 8555;
    public int SocketPort { get; set; } = 9101;

    public string PrinterUrl => $"http://localhost:{HttpPort}/";

    public static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    public static AppSettings Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            if (settings is null)
            {
                return new AppSettings();
            }

            settings.Normalize();
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        Normalize();

        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true,
        });

        File.WriteAllText(SettingsPath, json);
    }

    private void Normalize()
    {
        if (HttpPort is < 1 or > 65535)
        {
            HttpPort = 8555;
        }

        if (SocketPort is < 1 or > 65535)
        {
            SocketPort = 9101;
        }
    }
}
