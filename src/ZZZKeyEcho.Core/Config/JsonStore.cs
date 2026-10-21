using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZZZKeyEcho.Core.Config;

public static class JsonStore
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static string FallbackPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ZZZKeyEcho",
        "config.json");

    public static string DefaultPath
    {
        get
        {
            var exeDir = AppContext.BaseDirectory;
            return IsWritable(exeDir) ? Path.Combine(exeDir, "config.json") : FallbackPath;
        }
    }

    public static AppConfig Load() => Load(DefaultPath);

    public static AppConfig Load(string path)
    {
        try
        {
            return File.Exists(path)
                ? Deserialize(File.ReadAllText(path))
                : new AppConfig();
        }
        catch (Exception)
        {
            return new AppConfig();
        }
    }

    public static void Save(AppConfig config) => Save(DefaultPath, config);

    public static void Save(string path, AppConfig config)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, Serialize(config));
    }

    public static string Serialize(AppConfig config) => JsonSerializer.Serialize(config, Options);

    public static AppConfig Deserialize(string json)
    {
        var config = JsonSerializer.Deserialize<AppConfig>(json, Options) ?? new AppConfig();
        config.EnsureDefaults();
        return config;
    }

    private static bool IsWritable(string directory)
    {
        try
        {
            var probe = Path.Combine(directory, $".write-probe-{Guid.NewGuid():N}");
            File.WriteAllText(probe, string.Empty);
            File.Delete(probe);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
