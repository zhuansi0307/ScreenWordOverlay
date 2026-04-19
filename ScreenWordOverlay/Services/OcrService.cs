using System.IO;
using Tesseract;
using Bitmap = System.Drawing.Bitmap;

namespace ScreenWordOverlay.Services;

/// <summary>
/// OCR 服务：使用 Tesseract 识别英文单词及坐标
/// </summary>
public class OcrService : IDisposable
{
    private TesseractEngine? _engine;
    private bool _isAvailable;

    /// <summary>
    /// OCR 是否可用
    /// </summary>
    public bool IsAvailable => _isAvailable;

    /// <summary>
    /// 初始化 OCR 引擎
    /// </summary>
    public bool Initialize()
    {
        try
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var tessdataPath = System.IO.Path.Combine(basePath, "tessdata");

            if (!System.IO.Directory.Exists(tessdataPath))
            {
                System.IO.Directory.CreateDirectory(tessdataPath);
                _isAvailable = false;
                return false;
            }

            var engDataFile = System.IO.Path.Combine(tessdataPath, "eng.traineddata");
            if (!System.IO.File.Exists(engDataFile))
            {
                _isAvailable = false;
                return false;
            }

            _engine = new TesseractEngine(tessdataPath, "eng");
            _engine.DefaultPageSegMode = PageSegMode.Auto;
            _isAvailable = true;
        }
        catch
        {
            _isAvailable = false;
        }

        return _isAvailable;
    }

    /// <summary>
    /// 识别截图中的英文单词
    /// </summary>
    /// <param name="bitmap">截图</param>
    /// <param name="regionOffsetX">区域在屏幕上的X偏移</param>
    /// <param name="regionOffsetY">区域在屏幕上的Y偏移</param>
    /// <returns>OCR单词列表（含屏幕绝对坐标）</returns>
    public List<Models.OcrWord> Recognize(Bitmap bitmap, int regionOffsetX = 0, int regionOffsetY = 0)
    {
        var words = new List<Models.OcrWord>();

        if (_engine == null) return words;

        try
        {
            // 将 Bitmap 转为 Pix
            using var pix = BitmapToPix(bitmap);
            using var page = _engine.Process(pix);

            using var iterator = page.GetIterator();
            iterator.Begin();

            do
            {
                if (iterator.TryGetBoundingBox(PageIteratorLevel.Word, out var bounds))
                {
                    var text = iterator.GetText(PageIteratorLevel.Word);
                    if (string.IsNullOrWhiteSpace(text)) continue;

                    words.Add(new Models.OcrWord
                    {
                        Text = text.Trim(),
                        Bounds = new Models.ScreenRect(
                            bounds.X1 + regionOffsetX,
                            bounds.Y1 + regionOffsetY,
                            bounds.X2 - bounds.X1,
                            bounds.Y2 - bounds.Y1),
                        IsTranslated = false
                    });
                }
            }
            while (iterator.Next(PageIteratorLevel.Word));
        }
        catch
        {
            // OCR 失败静默处理
        }

        return words;
    }

    /// <summary>
    /// 将 System.Drawing.Bitmap 转换为 Tesseract Pix
    /// </summary>
    private static Pix BitmapToPix(Bitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        ms.Position = 0;
        return Pix.LoadFromMemory(ms.ToArray());
    }

    public void Dispose()
    {
        _engine?.Dispose();
        GC.SuppressFinalize(this);
    }
}
