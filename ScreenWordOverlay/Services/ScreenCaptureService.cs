using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ScreenWordOverlay.Services;

/// <summary>
/// 屏幕区域截图服务
/// </summary>
public class ScreenCaptureService
{
    /// <summary>
    /// 截取指定屏幕区域
    /// </summary>
    /// <param name="x">左上角X（屏幕坐标）</param>
    /// <param name="y">左上角Y（屏幕坐标）</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <returns>截图Bitmap</returns>
    public Bitmap CaptureRegion(int x, int y, int width, int height)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);
        }
        return bitmap;
    }

    /// <summary>
    /// 计算图像哈希，用于变化检测
    /// 简单实现：采样像素取均值
    /// </summary>
    public string ComputeImageHash(Bitmap bitmap)
    {
        // 采样间隔（每4像素采样一次，加速计算）
        const int step = 4;
        long sum = 0;
        int count = 0;

        for (int y = 0; y < bitmap.Height; y += step)
        {
            for (int x = 0; x < bitmap.Width; x += step)
            {
                var pixel = bitmap.GetPixel(x, y);
                sum += pixel.R + pixel.G + pixel.B;
                count++;
            }
        }

        if (count == 0) return "0";
        return (sum / count).ToString();
    }
}
