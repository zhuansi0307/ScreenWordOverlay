using System.IO;
using System.Text.Json;

namespace ScreenWordOverlay.Services;

/// <summary>
/// JSON 配置和术语表读写服务
/// </summary>
public class SettingsService
{
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public Models.AppSettings Settings { get; private set; } = new();

    /// <summary>
    /// 用户术语表 (英文小写 → 中文)
    /// </summary>
    public Dictionary<string, string> UserTerminology { get; private set; } = new();

    /// <summary>
    /// 内置术语表 (英文小写 → 中文)
    /// </summary>
    public Dictionary<string, string> BuiltInTerminology { get; private set; } = new();

    /// <summary>
    /// 本地词典 (英文小写 → 中文)
    /// </summary>
    public Dictionary<string, string> Dictionary { get; private set; } = new();

    public SettingsService()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _settingsPath = System.IO.Path.Combine(baseDir, "config", "settings.json");
    }

    /// <summary>
    /// 加载所有配置
    /// </summary>
    public void LoadAll()
    {
        LoadSettings();
        LoadTerminology();
        LoadDictionary();
    }

    /// <summary>
    /// 加载应用设置
    /// </summary>
    private void LoadSettings()
    {
        try
        {
            if (System.IO.File.Exists(_settingsPath))
            {
                var json = System.IO.File.ReadAllText(_settingsPath);
                Settings = JsonSerializer.Deserialize<Models.AppSettings>(json, _jsonOptions) ?? new();
            }
            else
            {
                Settings = new Models.AppSettings();
                SaveSettings();
            }
        }
        catch
        {
            Settings = new Models.AppSettings();
        }
    }

    /// <summary>
    /// 保存应用设置
    /// </summary>
    public void SaveSettings()
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(Settings, _jsonOptions);
            System.IO.File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // 静默处理保存失败
        }
    }

    /// <summary>
    /// 加载术语表和词典
    /// </summary>
    private void LoadTerminology()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;

        // 用户术语表
        var userTermPath = System.IO.Path.Combine(baseDir, Settings.UserTerminologyPath);
        UserTerminology = LoadKeyValueFile(userTermPath);

        // 内置术语表
        var builtInTermPath = System.IO.Path.Combine(baseDir, Settings.TerminologyPath);
        BuiltInTerminology = LoadKeyValueFile(builtInTermPath);
    }

    private void LoadDictionary()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var dictPath = System.IO.Path.Combine(baseDir, Settings.DictionaryPath);
        Dictionary = LoadKeyValueFile(dictPath);
    }

    /// <summary>
    /// 加载键值对 JSON 文件
    /// </summary>
    private static Dictionary<string, string> LoadKeyValueFile(string path)
    {
        try
        {
            if (!System.IO.File.Exists(path)) return new();
            var json = System.IO.File.ReadAllText(path);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    /// <summary>
    /// 保存用户术语表
    /// </summary>
    public void SaveUserTerminology()
    {
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var path = System.IO.Path.Combine(baseDir, Settings.UserTerminologyPath);
            var dir = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(UserTerminology, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(path, json);
        }
        catch
        {
            // 静默处理
        }
    }
}
