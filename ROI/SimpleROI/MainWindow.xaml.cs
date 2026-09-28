using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace SimpleROI
{
    /// <summary>
    /// Ví dụ mẫu minh họa cách sử dụng SimpleRoiControl trong ứng dụng WPF.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Tự động nạp ảnh mẫu giả lập khi vừa mở chương trình
            LoadSample();
        }

        private void LoadSample()
        {
            var bmp = SampleImage.Create(800, 500);
            RoiCtrl.ImageSource = bmp;

            // Đặt vùng ROI ban đầu quanh khối chip xanh (X: 100, Y: 80, W: 160, H: 110)
            RoiCtrl.SetRoi(100, 80, 160, 110);
            UpdateInfo(RoiCtrl.Roi);
        }

        #region Xử lý các nút bấm (Button Click Handlers)

        // 1. Mở file ảnh bất kỳ từ ổ đĩa
        private void BtnOpenImage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Chọn hình ảnh",
                Filter = "Tệp hình ảnh (*.png;*.jpg;*.bmp)|*.png;*.jpg;*.bmp|Tất cả tệp (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad; // Giải phóng file sau khi đọc
                    bmp.UriSource = new Uri(dlg.FileName);
                    bmp.EndInit();
                    bmp.Freeze();

                    RoiCtrl.ImageSource = bmp;
                    UpdateInfo(RoiCtrl.Roi);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không thể mở ảnh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 2. Tạo ảnh mẫu
        private void BtnSampleImage_Click(object sender, RoutedEventArgs e)
        {
            LoadSample();
        }

        // 3. Đặt ROI toàn khung hình
        private void BtnFullRoi_Click(object sender, RoutedEventArgs e)
        {
            RoiCtrl.ResetFull();
            UpdateInfo(RoiCtrl.Roi);
        }

        // 4. Xóa vùng ROI
        private void BtnClearRoi_Click(object sender, RoutedEventArgs e)
        {
            RoiCtrl.Clear();
            UpdateInfo(Rect.Empty);
            ImgCropPreview.Source = null;
        }

        // 5. Cắt ảnh theo vùng ROI đã chọn
        private void BtnCropRoi_Click(object sender, RoutedEventArgs e)
        {
            var croppedBitmap = RoiCtrl.Crop();
            if (croppedBitmap != null)
            {
                ImgCropPreview.Source = croppedBitmap;
            }
            else
            {
                MessageBox.Show("Chưa chọn vùng ROI hợp lệ!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion

        #region Sự kiện thay đổi ROI

        private void RoiCtrl_RoiChanged(object? sender, Rect roi)
        {
            UpdateInfo(roi);
        }

        private void UpdateInfo(Rect roi)
        {
            if (roi.IsEmpty || roi.Width <= 0 || roi.Height <= 0)
            {
                TxtCoordinates.Text = "Tọa độ: Chưa thiết lập (ROI trống)";
                TxtExtraInfo.Text = "Hướng dẫn: Nhấn giữ và kéo chuột trên ảnh để vẽ vùng ROI mới.";
                return;
            }

            int x = (int)Math.Round(roi.X);
            int y = (int)Math.Round(roi.Y);
            int w = (int)Math.Round(roi.Width);
            int h = (int)Math.Round(roi.Height);
            long area = (long)w * h;

            TxtCoordinates.Text = $"Tọa độ Pixel gốc: X = {x}, Y = {y}, Chiều rộng = {w} px, Chiều cao = {h} px";
            TxtExtraInfo.Text = $"Diện tích: {area:N0} px² | Tỷ lệ W:H = {w}:{h} | Phím mũi tên để vi chỉnh 1px.";
        }

        #endregion
    }
}