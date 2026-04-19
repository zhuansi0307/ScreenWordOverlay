using System.Windows;

namespace ScreenWordOverlay;

public partial class App : Application
{
    public static Services.SettingsService SettingsService { get; } = new();
    public static Services.OcrService OcrService { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 加载配置
        SettingsService.LoadAll();

        // 初始化 OCR
        var ocrAvailable = OcrService.Initialize();
        if (!ocrAvailable)
        {
            MessageBox.Show(
                "无法初始化 OCR 引擎。请确保 tessdata/eng.traineddata 文件存在。\n\n" +
                "下载地址: https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata\n" +
                "放置路径: 程序目录/tessdata/eng.traineddata",
                "OCR 初始化失败",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
