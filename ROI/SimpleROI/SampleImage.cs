using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SimpleROI
{
    /// <summary>
    /// Hàm tạo ảnh mẫu để kiểm thử nhanh mà không cần file ảnh từ đĩa
    /// </summary>
    public static class SampleImage
    {
        public static BitmapSource Create(int width = 800, int height = 500)
        {
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                // Nền gradient
                var bg = new LinearGradientBrush(Color.FromRgb(30, 35, 48), Color.FromRgb(18, 20, 28), new Point(0, 0), new Point(1, 1));
                dc.DrawRectangle(bg, null, new Rect(0, 0, width, height));

                // Lưới tọa độ
                var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)), 1.0);
                gridPen.Freeze();
                for (int x = 0; x < width; x += 50) dc.DrawLine(gridPen, new Point(x, 0), new Point(x, height));
                for (int y = 0; y < height; y += 50) dc.DrawLine(gridPen, new Point(0, y), new Point(width, y));

                // Tâm ngắm ở giữa
                double cx = width / 2.0;
                double cy = height / 2.0;
                var targetPen = new Pen(new SolidColorBrush(Color.FromRgb(56, 189, 248)), 2.0);
                targetPen.Freeze();
                dc.DrawEllipse(null, targetPen, new Point(cx, cy), 60, 60);
                dc.DrawEllipse(null, targetPen, new Point(cx, cy), 120, 120);
                dc.DrawLine(targetPen, new Point(cx - 150, cy), new Point(cx + 150, cy));
                dc.DrawLine(targetPen, new Point(cx, cy - 150), new Point(cx, cy + 150));

                // Đối tượng mẫu 1: Khối chip xanh
                var chipBrush = new SolidColorBrush(Color.FromRgb(22, 101, 52));
                var chipBorder = new Pen(new SolidColorBrush(Color.FromRgb(74, 222, 128)), 1.5);
                dc.DrawRoundedRectangle(chipBrush, chipBorder, new Rect(100, 80, 160, 110), 6, 6);

                // Đối tượng mẫu 2: Khối cảm biến cam
                var sensorBrush = new SolidColorBrush(Color.FromRgb(154, 52, 18));
                var sensorBorder = new Pen(new SolidColorBrush(Color.FromRgb(251, 146, 60)), 1.5);
                dc.DrawRoundedRectangle(sensorBrush, sensorBorder, new Rect(width - 240, height - 170, 150, 100), 6, 6);

                // Tiêu đề ảnh
                var typeface = new Typeface("Segoe UI Semibold");
                var text = new FormattedText(
                    "SimpleROI - Khung Ảnh Mẫu (800x500)",
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    15,
                    Brushes.White,
                    96);
                dc.DrawText(text, new Point(20, 20));
            }

            var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);
            rtb.Freeze();
            return rtb;
        }
    }
}
