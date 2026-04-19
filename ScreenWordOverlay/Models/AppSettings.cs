namespace ScreenWordOverlay.Models;

/// <summary>
/// 应用配置数据模型
/// </summary>
public class AppSettings
{
    /// <summary>
    /// 覆盖层透明度 0.0-1.0
    /// </summary>
    public double OverlayOpacity { get; set; } = 0.85;

    /// <summary>
    /// 翻译文字字体大小
    /// </summary>
    public double FontSize { get; set; } = 14;

    /// <summary>
    /// 翻译文字颜色（ARGB十六进制）
    /// </summary>
    public string FontColor { get; set; } = "#FFFFFF";

    /// <summary>
    /// 是否显示背景底色
    /// </summary>
    public bool ShowBackground { get; set; } = true;

    /// <summary>
    /// 背景底色颜色（ARGB十六进制）
    /// </summary>
    public string BackgroundColor { get; set; } = "#CC000000";

    /// <summary>
    /// OCR刷新间隔（毫秒）
    /// </summary>
    public int RefreshIntervalMs { get; set; } = 300;

    /// <summary>
    /// 用户术语表路径
    /// </summary>
    public string UserTerminologyPath { get; set; } = "config/user_terminology.json";

    /// <summary>
    /// 内置词典路径
    /// </summary>
    public string DictionaryPath { get; set; } = "Data/dictionary.json";

    /// <summary>
    /// 内置术语表路径
    /// </summary>
    public string TerminologyPath { get; set; } = "Data/terminology.json";

    /// <summary>
    /// 是否启用在线翻译（本地词典查不到时调用在线API）
    /// </summary>
    public bool UseOnlineTranslation { get; set; } = true;
}
