using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ROI.Helpers
{
    public static class SampleImageGenerator
    {
        public static BitmapSource CreateMachineVisionSample(int width = 1280, int height = 720)
        {
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                // 1. Background gradient (dark industrial slate)
                var bgBrush = new LinearGradientBrush(
                    Color.FromRgb(24, 27, 36),
                    Color.FromRgb(15, 17, 23),
                    new Point(0, 0),
                    new Point(1, 1));
                dc.DrawRectangle(bgBrush, null, new Rect(0, 0, width, height));

                // 2. Grid lines
                var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(28, 255, 255, 255)), 1.0);
                gridPen.Freeze();

                for (int x = 0; x < width; x += 40)
                {
                    dc.DrawLine(gridPen, new Point(x, 0), new Point(x, height));
                }
                for (int y = 0; y < height; y += 40)
                {
                    dc.DrawLine(gridPen, new Point(0, y), new Point(width, y));
                }

                // 3. Ruler / Scale marks along top and left
                var tickPen = new Pen(new SolidColorBrush(Color.FromRgb(100, 116, 139)), 1.0);
                tickPen.Freeze();
                var textBrush = new SolidColorBrush(Color.FromRgb(148, 163, 184));
                textBrush.Freeze();
                var typeface = new Typeface("Consolas");

                for (int x = 0; x < width; x += 20)
                {
                    double tickLen = (x % 100 == 0) ? 12 : ((x % 50 == 0) ? 8 : 4);
                    dc.DrawLine(tickPen, new Point(x, 0), new Point(x, tickLen));
                    if (x % 100 == 0 && x > 0 && x < width - 40)
                    {
                        var text = new FormattedText(
                            x.ToString(),
                            CultureInfo.InvariantCulture,
                            FlowDirection.LeftToRight,
                            typeface,
                            9,
                            textBrush,
                            96);
                        dc.DrawText(text, new Point(x + 2, 2));
                    }
                }

                for (int y = 0; y < height; y += 20)
                {
                    double tickLen = (y % 100 == 0) ? 12 : ((y % 50 == 0) ? 8 : 4);
                    dc.DrawLine(tickPen, new Point(0, y), new Point(tickLen, y));
                    if (y % 100 == 0 && y > 0 && y < height - 20)
                    {
                        var text = new FormattedText(
                            y.ToString(),
                            CultureInfo.InvariantCulture,
                            FlowDirection.LeftToRight,
                            typeface,
                            9,
                            textBrush,
                            96);
                        dc.DrawText(text, new Point(2, y + 2));
                    }
                }

                // 4. Center Calibration Target (Crosshair & Concentric Circles)
                double cx = width / 2.0;
                double cy = height / 2.0;
                var targetPen = new Pen(new SolidColorBrush(Color.FromRgb(56, 189, 248)), 1.5);
                targetPen.Freeze();

                double[] radii = { 40, 80, 140, 220 };
                foreach (var r in radii)
                {
                    dc.DrawEllipse(null, targetPen, new Point(cx, cy), r, r);
                }

                // Target crosshair
                var targetPenDashed = new Pen(new SolidColorBrush(Color.FromArgb(160, 56, 189, 248)), 1.0)
                {
                    DashStyle = new DashStyle(new double[] { 6, 4 }, 0)
                };
                targetPenDashed.Freeze();
                dc.DrawLine(targetPenDashed, new Point(cx - 240, cy), new Point(cx + 240, cy));
                dc.DrawLine(targetPenDashed, new Point(cx, cy - 240), new Point(cx, cy + 240));

                // 5. Simulated Inspection Target 1: Electronic PCB / Chip (Top Left)
                var pcbBrush = new SolidColorBrush(Color.FromRgb(15, 60, 40));
                var pcbBorder = new Pen(new SolidColorBrush(Color.FromRgb(34, 197, 94)), 1.5);
                dc.DrawRoundedRectangle(pcbBrush, pcbBorder, new Rect(140, 100, 260, 180), 8, 8);

                // Chip inside PCB
                var chipBrush = new SolidColorBrush(Color.FromRgb(30, 35, 45));
                var chipBorder = new Pen(new SolidColorBrush(Color.FromRgb(160, 174, 192)), 1.0);
                dc.DrawRoundedRectangle(chipBrush, chipBorder, new Rect(200, 140, 140, 100), 4, 4);

                // Chip label
                var chipText = new FormattedText(
                    "ARM-CORTEX\nM7-VISION\nSN: #9042A",
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    11,
                    new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    96);
                dc.DrawText(chipText, new Point(220, 160));

                // Pins along chip
                var pinPen = new Pen(new SolidColorBrush(Color.FromRgb(234, 179, 8)), 2.0);
                for (int px = 210; px <= 330; px += 15)
                {
                    dc.DrawLine(pinPen, new Point(px, 130), new Point(px, 140));
                    dc.DrawLine(pinPen, new Point(px, 240), new Point(px, 250));
                }

                // 6. Simulated Inspection Target 2: Mechanical Gear / Bearing (Bottom Right)
                double gx = width - 260;
                double gy = height - 200;
                var gearBrush = new SolidColorBrush(Color.FromRgb(38, 45, 60));
                var gearPen = new Pen(new SolidColorBrush(Color.FromRgb(249, 115, 22)), 1.5);
                dc.DrawEllipse(gearBrush, gearPen, new Point(gx, gy), 90, 90);
                dc.DrawEllipse(bgBrush, gearPen, new Point(gx, gy), 45, 45);

                for (int i = 0; i < 12; i++)
                {
                    double angle = i * (Math.PI / 6);
                    double tx = gx + 95 * Math.Cos(angle);
                    double ty = gy + 95 * Math.Sin(angle);
                    dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(249, 115, 22)), null, new Point(tx, ty), 7, 7);
                }

                // 7. Simulated Barcode / QR Target (Bottom Left)
                double bx = 140;
                double by = height - 190;
                dc.DrawRectangle(Brushes.White, null, new Rect(bx, by, 220, 90));

                var rand = new Random(42);
                double currentX = bx + 12;
                while (currentX < bx + 208)
                {
                    double barW = rand.Next(2, 6);
                    dc.DrawRectangle(Brushes.Black, null, new Rect(currentX, by + 10, barW, 55));
                    currentX += barW + rand.Next(2, 5);
                }
                var codeText = new FormattedText(
                    "* ROI-VISION-2026 *",
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    10,
                    Brushes.Black,
                    96);
                dc.DrawText(codeText, new Point(bx + 35, by + 70));

                // 8. Header / Info Overlay Banner
                var headerBrush = new SolidColorBrush(Color.FromArgb(200, 15, 23, 42));
                dc.DrawRectangle(headerBrush, null, new Rect(width - 430, 20, 410, 80));
                var headerBorderPen = new Pen(new SolidColorBrush(Color.FromRgb(71, 85, 105)), 1.0);
                dc.DrawRectangle(null, headerBorderPen, new Rect(width - 430, 20, 410, 80));

                var infoTitle = new FormattedText(
                    "FRAME ACQUISITION: 1280x720 @ 60 FPS",
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Consolas Bold"),
                    13,
                    new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                    96);
                dc.DrawText(infoTitle, new Point(width - 415, 30));

                var infoSubtitle = new FormattedText(
                    $"CAMERA: SONY IMX585 | STATUS: ACTIVE\nEXPOSURE: 15.0ms | GAIN: 1.2x | CH: 01",
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    10,
                    new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                    96);
                dc.DrawText(infoSubtitle, new Point(width - 415, 52));
            }

            var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);
            rtb.Freeze();
            return rtb;
        }
    }
}
