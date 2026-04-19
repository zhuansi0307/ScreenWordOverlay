namespace ScreenWordOverlay.Models;

/// <summary>
/// 翻译区域模型，记录一个框选区域的完整状态
/// </summary>
public class TranslationRegion
{
    /// <summary>
    /// 区域唯一ID
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 屏幕上的区域矩形（屏幕绝对坐标）
    /// </summary>
    public ScreenRect ScreenBounds { get; set; }

    /// <summary>
    /// OCR识别出的单词列表
    /// </summary>
    public List<OcrWord> Words { get; set; } = new();

    /// <summary>
    /// 翻译是否可见
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 上次截图的图像哈希，用于变化检测
    /// </summary>
    public string? LastImageHash { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
