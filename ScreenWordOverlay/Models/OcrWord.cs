namespace ScreenWordOverlay.Models;

/// <summary>
/// OCR识别出的单词及其坐标
/// </summary>
public class OcrWord
{
    /// <summary>
    /// 单词文本
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 单词在截图中的矩形区域（像素坐标）
    /// </summary>
    public ScreenRect Bounds { get; set; }

    /// <summary>
    /// 翻译后的中文
    /// </summary>
    public string Translation { get; set; } = string.Empty;

    /// <summary>
    /// 是否命中翻译（未命中则保留原文）
    /// </summary>
    public bool IsTranslated { get; set; }
}

/// <summary>
/// 矩形区域
/// </summary>
public struct ScreenRect
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public ScreenRect(double x, double y, double width, double height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public bool Contains(double px, double py)
    {
        return px >= X && px <= X + Width && py >= Y && py <= Y + Height;
    }
}
