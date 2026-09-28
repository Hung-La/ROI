# SimpleROI - Mẫu Chọn Vùng Quan Tâm (ROI) trong WPF

Dự án này là một ví dụ mẫu (template / reference) gọn gàng, độc lập và dễ hiểu về cách nhúng tính năng **chọn vùng ROI (Region of Interest)** trên hình ảnh trong ứng dụng WPF.

---

## 📁 Cấu trúc thành phần chính

Để đưa tính năng ROI vào bất kỳ dự án WPF nào khác trong tương lai, bạn **chỉ cần copy 2 file**:
1. `SimpleRoiControl.xaml`
2. `SimpleRoiControl.xaml.cs`

---

## 🚀 Cách tích hợp vào dự án khác

### 1. Khai báo trong XAML
Thêm namespace vào Window / UserControl của bạn:
```xaml
xmlns:roi="clr-namespace:SimpleROI"
```

Nhúng điều khiển vào giao diện:
```xaml
<roi:SimpleRoiControl x:Name="MyRoiControl"
                      ImageSource="{Binding YourImageSource}"
                      Roi="{Binding CurrentRoi, Mode=TwoWay}"
                      RoiColor="#00E676" />
```

### 2. Sử dụng trong C# Code-behind hoặc ViewModel

```csharp
// 1. Nạp ảnh vào điều khiển
MyRoiControl.ImageSource = new BitmapImage(new Uri("path/to/image.png"));

// 2. Lấy tọa độ vùng ROI theo PIXEL ẢNH GỐC
Rect roi = MyRoiControl.Roi;
Console.WriteLine($"X={roi.X}, Y={roi.Y}, W={roi.Width}, H={roi.Height}");

// 3. Cắt ảnh theo vùng ROI đã chọn
BitmapSource cropped = MyRoiControl.Crop();

// 4. Đặt ROI theo tọa độ pixel bằng code
MyRoiControl.SetRoi(100, 150, 400, 300);

// 5. Chọn toàn bộ ảnh hoặc xóa ROI
MyRoiControl.ResetFull(); // Chọn toàn hình
MyRoiControl.Clear();     // Xóa ROI
```

---

## 🎯 Các tính năng tương tác được hỗ trợ sẵn

- **Vẽ mới ROI**: Nhấn giữ chuột trái trên nền ảnh và kéo.
- **Di chuyển ROI**: Bấm chuột vào bên trong khung ROI (con trỏ chuột đổi thành `SizeAll`) và kéo đến vị trí mong muốn.
- **Co giãn 8 hướng**: Kéo 8 điểm neo trắng tại 4 góc và 4 cạnh (NW, N, NE, E, SE, S, SW, W).
- **Tự động giới hạn (Clamping)**: Khung ROI không bao giờ bị kéo lệch ra ngoài phạm vi biên của ảnh.
- **Vi chỉnh bằng bàn phím**:
  - Phím mũi tên (←, ↑, →, ↓): Dịch chuyển 1 pixel.
  - Phím `Shift` + mũi tên: Dịch chuyển 10 pixel.
  - Phím `Escape`: Xóa vùng ROI hiện tại.
- **Độ co giãn tỉ lệ chuẩn (`Stretch="Uniform"`)**: Khi phóng to / thu nhỏ cửa sổ hoặc đổi tỉ lệ hiển thị, tọa độ pixel thực tế của ROI luôn giữ nguyên độ chính xác tuyệt đối.
