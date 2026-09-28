using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ROI.Helpers;

namespace ROI
{
    public partial class MainWindow : Window
    {
        private BitmapSource? _currentCroppedImage;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Automatically load a sample machine vision frame on startup
            LoadSampleImage();
        }

        #region Toolbar Actions

        private void BtnOpenImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Chọn tệp hình ảnh để tải vào khung ROI",
                Filter = "Tệp hình ảnh (*.png;*.jpg;*.jpeg;*.bmp;*.tiff)|*.png;*.jpg;*.jpeg;*.bmp;*.tiff|Tất cả tệp (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var uri = new Uri(dialog.FileName);
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = uri;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    RoiCtrl.ImageSource = bitmap;
                    UpdateImageInfo(bitmap, Path.GetFileName(dialog.FileName));
                    UpdateRoiDisplay(RoiCtrl.Roi);

                    TxtStatus.Text = $"Đã mở ảnh: {Path.GetFileName(dialog.FileName)} ({bitmap.PixelWidth} × {bitmap.PixelHeight} px)";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không thể mở file ảnh: {ex.Message}", "Lỗi tải ảnh", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnSampleImage_Click(object sender, RoutedEventArgs e)
        {
            LoadSampleImage();
        }

        private void LoadSampleImage()
        {
            var sampleBmp = SampleImageGenerator.CreateMachineVisionSample(1280, 720);
            RoiCtrl.ImageSource = sampleBmp;

            // Set default initial ROI around the PCB chip target
            RoiCtrl.SetRoiCoordinates(140, 100, 260, 180);

            UpdateImageInfo(sampleBmp, "Ảnh mẫu thị giác máy (1280x720)");
            UpdateRoiDisplay(RoiCtrl.Roi);
            TxtStatus.Text = "Đã tạo khung ảnh mẫu giả lập camera thị giác máy (1280 × 720 px).";
        }

        private void BtnFullRoi_Click(object sender, RoutedEventArgs e)
        {
            RoiCtrl.ResetToFullImage();
            UpdateRoiDisplay(RoiCtrl.Roi);
            TxtStatus.Text = "Đã đặt vùng ROI bao trọn toàn bộ khung hình ảnh.";
        }

        private void BtnCenterRoi_Click(object sender, RoutedEventArgs e)
        {
            if (RoiCtrl.ImageSource is BitmapSource bmp)
            {
                double w = Math.Round(bmp.PixelWidth * 0.5);
                double h = Math.Round(bmp.PixelHeight * 0.5);
                double x = Math.Round((bmp.PixelWidth - w) / 2.0);
                double y = Math.Round((bmp.PixelHeight - h) / 2.0);
                RoiCtrl.SetRoiCoordinates(x, y, w, h);
                UpdateRoiDisplay(RoiCtrl.Roi);
                TxtStatus.Text = "Đã đặt ROI tại trung tâm khung hình (50% kích thước ảnh).";
            }
        }

        private void BtnClearRoi_Click(object sender, RoutedEventArgs e)
        {
            RoiCtrl.ClearRoi();
            UpdateRoiDisplay(Rect.Empty);
            ClearCroppedPreview();
            TxtStatus.Text = "Đã xóa vùng ROI.";
        }

        #endregion

        #region Display Options

        private void CmbColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RoiCtrl == null) return;

            if (CmbColor?.SelectedItem is ComboBoxItem item && item.Tag is string hex)
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(hex);
                    RoiCtrl.RoiColor = new SolidColorBrush(color);
                }
                catch
                {
                    // Ignore conversion errors
                }
            }
        }

        #endregion

        #region ROI Events & Coordinate Updates

        private void RoiCtrl_RoiChangedByUser(object? sender, Rect roi)
        {
            UpdateRoiDisplay(roi);
        }

        private void UpdateImageInfo(BitmapSource bmp, string name)
        {
            TxtImageResolution.Text = $"{bmp.PixelWidth} × {bmp.PixelHeight} px";
        }

        private void UpdateRoiDisplay(Rect roi)
        {
            if (roi.IsEmpty || roi.Width <= 0 || roi.Height <= 0)
            {
                TxtRoiX.Text = "0";
                TxtRoiY.Text = "0";
                TxtRoiWidth.Text = "0";
                TxtRoiHeight.Text = "0";
                TxtRoiArea.Text = "0 px²";
                TxtRoiAspect.Text = "0:0";
                TxtRoiStatus.Text = "ROI: Chưa thiết lập";
                return;
            }

            int x = (int)Math.Round(roi.X);
            int y = (int)Math.Round(roi.Y);
            int w = (int)Math.Round(roi.Width);
            int h = (int)Math.Round(roi.Height);

            TxtRoiX.Text = x.ToString();
            TxtRoiY.Text = y.ToString();
            TxtRoiWidth.Text = w.ToString();
            TxtRoiHeight.Text = h.ToString();

            long area = (long)w * h;
            TxtRoiArea.Text = $"{area:N0} px²";

            double aspect = (h > 0) ? (double)w / h : 0;
            TxtRoiAspect.Text = $"{w}:{h} ({aspect:F2})";

            TxtRoiStatus.Text = $"ROI: [X={x}, Y={y}, W={w}, H={h}]";
        }

        private void BtnApplyCoords_Click(object sender, RoutedEventArgs e)
        {
            ApplyManualCoordinates();
        }

        private void TxtRoiCoord_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplyManualCoordinates();
                e.Handled = true;
            }
        }

        private void ApplyManualCoordinates()
        {
            if (double.TryParse(TxtRoiX.Text, out double x) &&
                double.TryParse(TxtRoiY.Text, out double y) &&
                double.TryParse(TxtRoiWidth.Text, out double w) &&
                double.TryParse(TxtRoiHeight.Text, out double h))
            {
                if (w > 0 && h > 0)
                {
                    RoiCtrl.SetRoiCoordinates(x, y, w, h);
                    UpdateRoiDisplay(RoiCtrl.Roi);
                    TxtStatus.Text = $"Đã cập nhật tọa độ ROI: X={x}, Y={y}, W={w}, H={h}";
                }
                else
                {
                    MessageBox.Show("Chiều rộng (W) và chiều cao (H) phải lớn hơn 0.", "Tọa độ không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("Vui lòng nhập số hợp lệ cho các trường tọa độ.", "Lỗi định dạng", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region Crop & Export ROI

        private void BtnCropRoi_Click(object sender, RoutedEventArgs e)
        {
            var cropped = RoiCtrl.CropRoi();
            if (cropped != null)
            {
                _currentCroppedImage = cropped;
                ImgCroppedPreview.Source = cropped;
                TxtNoPreview.Visibility = Visibility.Collapsed;
                TxtCropDimension.Text = $"Kích thước vùng cắt: {cropped.PixelWidth} × {cropped.PixelHeight} px";
                BtnSaveCrop.IsEnabled = true;

                TxtStatus.Text = $"Đã cắt thành công vùng ROI ({cropped.PixelWidth} × {cropped.PixelHeight} px).";
            }
            else
            {
                MessageBox.Show("Không có vùng ROI hợp lệ để cắt ảnh. Vui lòng vẽ hoặc chọn một vùng ROI trước.", "Chưa chọn ROI", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ClearCroppedPreview()
        {
            _currentCroppedImage = null;
            ImgCroppedPreview.Source = null;
            TxtNoPreview.Visibility = Visibility.Visible;
            TxtCropDimension.Text = "Chưa cắt ảnh";
            BtnSaveCrop.IsEnabled = false;
        }

        private void BtnSaveCrop_Click(object sender, RoutedEventArgs e)
        {
            if (_currentCroppedImage == null)
                return;

            var dialog = new SaveFileDialog
            {
                Title = "Lưu ảnh ROI đã cắt",
                Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|Bitmap Image (*.bmp)|*.bmp",
                FileName = $"ROI_Crop_{DateTime.Now:yyyyMMdd_HHmmss}.png"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string ext = Path.GetExtension(dialog.FileName).ToLowerInvariant();
                    BitmapEncoder encoder = ext switch
                    {
                        ".jpg" or ".jpeg" => new JpegBitmapEncoder(),
                        ".bmp" => new BmpBitmapEncoder(),
                        _ => new PngBitmapEncoder()
                    };

                    encoder.Frames.Add(BitmapFrame.Create(_currentCroppedImage));

                    using (var fs = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write))
                    {
                        encoder.Save(fs);
                    }

                    TxtStatus.Text = $"Đã lưu ảnh ROI thành công vào: {dialog.FileName}";
                    MessageBox.Show("Đã lưu ảnh ROI thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi lưu ảnh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion
    }
}