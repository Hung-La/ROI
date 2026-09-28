using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SimpleROI
{
    /// <summary>
    /// Điều khiển chọn vùng quan tâm (ROI - Region of Interest) đơn giản,
    /// dễ sao chép và tái sử dụng cho các dự án WPF khác.
    /// Tọa độ ROI được tính chính xác theo PIXEL THỰC TẾ của ảnh gốc.
    /// </summary>
    public partial class SimpleRoiControl : UserControl
    {
        private enum DragMode
        {
            None,
            Draw,
            Move,
            ResizeNW, ResizeN, ResizeNE, ResizeE,
            ResizeSE, ResizeS, ResizeSW, ResizeW
        }

        #region Dependency Properties

        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register(
                nameof(ImageSource),
                typeof(ImageSource),
                typeof(SimpleRoiControl),
                new FrameworkPropertyMetadata(null, OnImageSourceChanged));

        public static readonly DependencyProperty RoiProperty =
            DependencyProperty.Register(
                nameof(Roi),
                typeof(Rect),
                typeof(SimpleRoiControl),
                new FrameworkPropertyMetadata(Rect.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnRoiChanged));

        public static readonly DependencyProperty RoiColorProperty =
            DependencyProperty.Register(
                nameof(RoiColor),
                typeof(Brush),
                typeof(SimpleRoiControl),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0, 230, 118)), OnRoiColorChanged));

        /// <summary>
        /// Nguồn hình ảnh hiển thị
        /// </summary>
        public ImageSource? ImageSource
        {
            get => (ImageSource?)GetValue(ImageSourceProperty);
            set => SetValue(ImageSourceProperty, value);
        }

        /// <summary>
        /// Tọa độ vùng ROI theo pixel của ảnh gốc (X, Y, Width, Height)
        /// </summary>
        public Rect Roi
        {
            get => (Rect)GetValue(RoiProperty);
            set => SetValue(RoiProperty, value);
        }

        /// <summary>
        /// Màu viền của khung ROI (mặc định: xanh lá sáng)
        /// </summary>
        public Brush RoiColor
        {
            get => (Brush)GetValue(RoiColorProperty);
            set => SetValue(RoiColorProperty, value);
        }

        #endregion

        #region Events

        /// <summary>
        /// Sự kiện kích hoạt khi người dùng thay đổi vùng ROI bằng chuột hoặc phím
        /// </summary>
        public event EventHandler<Rect>? RoiChanged;

        #endregion

        #region Private Fields

        private DragMode _dragMode = DragMode.None;
        private Point _dragStartPoint;
        private Rect _startRoi;
        private bool _isInternalUpdate = false;
        private const double MinSize = 5.0; // Kích thước tối thiểu của ROI (pixel)

        #endregion

        public SimpleRoiControl()
        {
            InitializeComponent();

            Loaded += (s, e) => UpdateVisuals();
            SizeChanged += (s, e) => UpdateVisuals();

            // Đăng ký sự kiện chuột cho 8 điểm neo co giãn
            H_NW.MouseLeftButtonDown += (s, e) => StartDrag(DragMode.ResizeNW, e);
            H_N.MouseLeftButtonDown += (s, e) => StartDrag(DragMode.ResizeN, e);
            H_NE.MouseLeftButtonDown += (s, e) => StartDrag(DragMode.ResizeNE, e);
            H_E.MouseLeftButtonDown += (s, e) => StartDrag(DragMode.ResizeE, e);
            H_SE.MouseLeftButtonDown += (s, e) => StartDrag(DragMode.ResizeSE, e);
            H_S.MouseLeftButtonDown += (s, e) => StartDrag(DragMode.ResizeS, e);
            H_SW.MouseLeftButtonDown += (s, e) => StartDrag(DragMode.ResizeSW, e);
            H_W.MouseLeftButtonDown += (s, e) => StartDrag(DragMode.ResizeW, e);

            // Đăng ký sự kiện di chuyển khung ROI
            RoiRectElement.MouseLeftButtonDown += (s, e) => StartDrag(DragMode.Move, e);

            // Đăng ký sự kiện trên Canvas
            OverlayCanvas.MouseLeftButtonDown += OverlayCanvas_MouseLeftButtonDown;
            OverlayCanvas.MouseMove += OverlayCanvas_MouseMove;
            OverlayCanvas.MouseLeftButtonUp += OverlayCanvas_MouseLeftButtonUp;
            KeyDown += SimpleRoiControl_KeyDown;

            ApplyColor();
        }

        #region Property Change Handlers

        private static void OnImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (SimpleRoiControl)d;
            if (c.ImgDisplay == null) return;

            c.ImgDisplay.Source = e.NewValue as ImageSource;
            c.TxtNoImage.Visibility = (e.NewValue == null) ? Visibility.Visible : Visibility.Collapsed;

            // Khởi tạo vùng ROI mặc định ở giữa ảnh khi vừa nạp ảnh mới
            if (c.ImageSource is BitmapSource bmp)
            {
                if (c.Roi.IsEmpty || c.Roi.Width <= 0 || c.Roi.Height <= 0)
                {
                    double w = Math.Round(bmp.PixelWidth * 0.6);
                    double h = Math.Round(bmp.PixelHeight * 0.6);
                    double x = Math.Round((bmp.PixelWidth - w) / 2.0);
                    double y = Math.Round((bmp.PixelHeight - h) / 2.0);
                    c.Roi = new Rect(x, y, w, h);
                }
            }
            c.UpdateVisuals();
        }

        private static void OnRoiChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (SimpleRoiControl)d;
            if (!c._isInternalUpdate)
            {
                c.UpdateVisuals();
            }
        }

        private static void OnRoiColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SimpleRoiControl)d).ApplyColor();
        }

        private void ApplyColor()
        {
            if (RoiRectElement == null || H_NW == null) return;

            if (RoiColor is SolidColorBrush solid)
            {
                RoiRectElement.Stroke = solid;
                RoiRectElement.Fill = new SolidColorBrush(Color.FromArgb(35, solid.Color.R, solid.Color.G, solid.Color.B));

                H_NW.Stroke = solid; H_N.Stroke = solid; H_NE.Stroke = solid;
                H_E.Stroke = solid; H_SE.Stroke = solid; H_S.Stroke = solid;
                H_SW.Stroke = solid; H_W.Stroke = solid;
            }
        }

        #endregion

        #region Coordinate Mapping (Chuyển đổi tọa độ Màn hình <-> Pixel ảnh gốc)

        /// <summary>
        /// Lấy vùng hình chữ nhật thực tế mà ảnh đang hiển thị trên Canvas (sau khi co giãn Stretch="Uniform")
        /// </summary>
        public Rect GetImageDisplayBounds()
        {
            if (ImageSource is not BitmapSource bmp)
                return Rect.Empty;

            double containerW = OverlayCanvas.ActualWidth;
            double containerH = OverlayCanvas.ActualHeight;

            if (containerW <= 0 || containerH <= 0 || bmp.PixelWidth <= 0 || bmp.PixelHeight <= 0)
                return Rect.Empty;

            double scale = Math.Min(containerW / bmp.PixelWidth, containerH / bmp.PixelHeight);
            double dispW = bmp.PixelWidth * scale;
            double dispH = bmp.PixelHeight * scale;

            double offsetX = (containerW - dispW) / 2.0;
            double offsetY = (containerH - dispH) / 2.0;

            return new Rect(offsetX, offsetY, dispW, dispH);
        }

        /// <summary>
        /// Chuyển điểm trên Canvas thành tọa độ Pixel trên ảnh gốc
        /// </summary>
        public Point CanvasToPixel(Point pt)
        {
            var bounds = GetImageDisplayBounds();
            if (bounds.IsEmpty || ImageSource is not BitmapSource bmp)
                return new Point(0, 0);

            double scale = bounds.Width / bmp.PixelWidth;
            double px = Math.Clamp((pt.X - bounds.Left) / scale, 0, bmp.PixelWidth);
            double py = Math.Clamp((pt.Y - bounds.Top) / scale, 0, bmp.PixelHeight);

            return new Point(px, py);
        }

        /// <summary>
        /// Chuyển vùng chữ nhật Pixel ảnh gốc thành vùng chữ nhật trên Canvas
        /// </summary>
        public Rect PixelToCanvas(Rect pixelRect)
        {
            var bounds = GetImageDisplayBounds();
            if (bounds.IsEmpty || ImageSource is not BitmapSource bmp)
                return Rect.Empty;

            double scale = bounds.Width / bmp.PixelWidth;
            double vx = bounds.Left + pixelRect.X * scale;
            double vy = bounds.Top + pixelRect.Y * scale;
            double vw = pixelRect.Width * scale;
            double vh = pixelRect.Height * scale;

            return new Rect(vx, vy, Math.Max(0, vw), Math.Max(0, vh));
        }

        #endregion

        #region Mouse Interactions (Kéo thả chuột)

        private void StartDrag(DragMode mode, MouseButtonEventArgs e)
        {
            if (ImageSource == null) return;

            Focus();
            _dragMode = mode;
            _dragStartPoint = e.GetPosition(OverlayCanvas);
            _startRoi = Roi;

            OverlayCanvas.CaptureMouse();
            e.Handled = true;
        }

        private void OverlayCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ImageSource is not BitmapSource) return;

            var bounds = GetImageDisplayBounds();
            var pt = e.GetPosition(OverlayCanvas);

            // Bấm chuột trên nền ảnh để bắt đầu vẽ vùng ROI mới
            if (bounds.Contains(pt))
            {
                Focus();
                _dragMode = DragMode.Draw;
                _dragStartPoint = pt;

                var pixelPt = CanvasToPixel(pt);
                _startRoi = new Rect(pixelPt.X, pixelPt.Y, 0, 0);

                SetRoiInternal(_startRoi);
                OverlayCanvas.CaptureMouse();
                e.Handled = true;
            }
        }

        private void OverlayCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_dragMode == DragMode.None || ImageSource is not BitmapSource bmp)
                return;

            var currentPt = e.GetPosition(OverlayCanvas);
            var bounds = GetImageDisplayBounds();
            if (bounds.IsEmpty) return;

            double scale = bounds.Width / bmp.PixelWidth;

            switch (_dragMode)
            {
                // 1. Vẽ mới ROI
                case DragMode.Draw:
                    {
                        var startPix = CanvasToPixel(_dragStartPoint);
                        var curPix = CanvasToPixel(currentPt);

                        double x = Math.Min(startPix.X, curPix.X);
                        double y = Math.Min(startPix.Y, curPix.Y);
                        double w = Math.Abs(curPix.X - startPix.X);
                        double h = Math.Abs(curPix.Y - startPix.Y);

                        SetRoiInternal(new Rect(Math.Round(x), Math.Round(y), Math.Round(w), Math.Round(h)));
                    }
                    break;

                // 2. Di chuyển toàn bộ ROI
                case DragMode.Move:
                    {
                        double dx = (currentPt.X - _dragStartPoint.X) / scale;
                        double dy = (currentPt.Y - _dragStartPoint.Y) / scale;

                        double newX = Math.Clamp(_startRoi.X + dx, 0, Math.Max(0, bmp.PixelWidth - _startRoi.Width));
                        double newY = Math.Clamp(_startRoi.Y + dy, 0, Math.Max(0, bmp.PixelHeight - _startRoi.Height));

                        SetRoiInternal(new Rect(Math.Round(newX), Math.Round(newY), _startRoi.Width, _startRoi.Height));
                    }
                    break;

                // 3. Co giãn 8 hướng
                default:
                    {
                        var curPix = CanvasToPixel(currentPt);
                        double left = _startRoi.Left;
                        double top = _startRoi.Top;
                        double right = _startRoi.Right;
                        double bottom = _startRoi.Bottom;

                        if (_dragMode is DragMode.ResizeNW or DragMode.ResizeN or DragMode.ResizeNE)
                            top = Math.Clamp(Math.Min(curPix.Y, bottom - MinSize), 0, bmp.PixelHeight);

                        if (_dragMode is DragMode.ResizeSW or DragMode.ResizeS or DragMode.ResizeSE)
                            bottom = Math.Clamp(Math.Max(curPix.Y, top + MinSize), 0, bmp.PixelHeight);

                        if (_dragMode is DragMode.ResizeNW or DragMode.ResizeW or DragMode.ResizeSW)
                            left = Math.Clamp(Math.Min(curPix.X, right - MinSize), 0, bmp.PixelWidth);

                        if (_dragMode is DragMode.ResizeNE or DragMode.ResizeE or DragMode.ResizeSE)
                            right = Math.Clamp(Math.Max(curPix.X, left + MinSize), 0, bmp.PixelWidth);

                        double w = Math.Max(MinSize, right - left);
                        double h = Math.Max(MinSize, bottom - top);

                        SetRoiInternal(new Rect(Math.Round(left), Math.Round(top), Math.Round(w), Math.Round(h)));
                    }
                    break;
            }
        }

        private void OverlayCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_dragMode != DragMode.None)
            {
                OverlayCanvas.ReleaseMouseCapture();

                if (_dragMode == DragMode.Draw && (Roi.Width < MinSize || Roi.Height < MinSize))
                {
                    SetRoiInternal(_startRoi.Width >= MinSize ? _startRoi : Rect.Empty);
                }

                _dragMode = DragMode.None;
                RoiChanged?.Invoke(this, Roi);
            }
        }

        private void SimpleRoiControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (Roi.IsEmpty || ImageSource is not BitmapSource bmp) return;

            double step = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? 10.0 : 1.0;
            double dx = 0, dy = 0;

            switch (e.Key)
            {
                case Key.Left: dx = -step; break;
                case Key.Right: dx = step; break;
                case Key.Up: dy = -step; break;
                case Key.Down: dy = step; break;
                case Key.Escape: Clear(); e.Handled = true; return;
                default: return;
            }

            double newX = Math.Clamp(Roi.X + dx, 0, Math.Max(0, bmp.PixelWidth - Roi.Width));
            double newY = Math.Clamp(Roi.Y + dy, 0, Math.Max(0, bmp.PixelHeight - Roi.Height));

            SetRoiInternal(new Rect(newX, newY, Roi.Width, Roi.Height));
            RoiChanged?.Invoke(this, Roi);
            e.Handled = true;
        }

        private void SetRoiInternal(Rect newRoi)
        {
            _isInternalUpdate = true;
            Roi = newRoi;
            _isInternalUpdate = false;
            UpdateVisuals();
        }

        #endregion

        #region Cập nhật giao diện (Visual Update)

        public void UpdateVisuals()
        {
            if (RoiRectElement == null || OverlayCanvas == null || ImgDisplay == null || BadgeBorder == null)
                return;

            if (ImageSource is not BitmapSource bmp || Roi.IsEmpty || Roi.Width <= 0 || Roi.Height <= 0)
            {
                RoiRectElement.Visibility = Visibility.Collapsed;
                SetHandlesVisibility(Visibility.Collapsed);
                BadgeBorder.Visibility = Visibility.Collapsed;
                DimmedMask.Visibility = Visibility.Collapsed;
                return;
            }

            var bounds = GetImageDisplayBounds();
            if (bounds.IsEmpty)
            {
                RoiRectElement.Visibility = Visibility.Collapsed;
                SetHandlesVisibility(Visibility.Collapsed);
                BadgeBorder.Visibility = Visibility.Collapsed;
                DimmedMask.Visibility = Visibility.Collapsed;
                return;
            }

            var viewRect = PixelToCanvas(Roi);

            // 1. Cập nhật khung chữ nhật ROI
            RoiRectElement.Visibility = Visibility.Visible;
            RoiRectElement.Width = Math.Max(0, viewRect.Width);
            RoiRectElement.Height = Math.Max(0, viewRect.Height);
            Canvas.SetLeft(RoiRectElement, viewRect.Left);
            Canvas.SetTop(RoiRectElement, viewRect.Top);

            // 2. Lớp phủ làm tối ngoài ROI
            DimmedMask.Visibility = Visibility.Visible;
            ImageBoundsGeom.Rect = bounds;
            RoiBoundsGeom.Rect = viewRect;

            // 3. Cập nhật vị trí 8 điểm neo
            SetHandlesVisibility(Visibility.Visible);
            const double hw = 5.0; // nửa kích thước handle (10/2)
            SetHandlePos(H_NW, viewRect.Left - hw, viewRect.Top - hw);
            SetHandlePos(H_N,  viewRect.Left + viewRect.Width / 2.0 - hw, viewRect.Top - hw);
            SetHandlePos(H_NE, viewRect.Right - hw, viewRect.Top - hw);
            SetHandlePos(H_E,  viewRect.Right - hw, viewRect.Top + viewRect.Height / 2.0 - hw);
            SetHandlePos(H_SE, viewRect.Right - hw, viewRect.Bottom - hw);
            SetHandlePos(H_S,  viewRect.Left + viewRect.Width / 2.0 - hw, viewRect.Bottom - hw);
            SetHandlePos(H_SW, viewRect.Left - hw, viewRect.Bottom - hw);
            SetHandlePos(H_W,  viewRect.Left - hw, viewRect.Top + viewRect.Height / 2.0 - hw);

            // 4. Cập nhật nhãn tọa độ
            BadgeBorder.Visibility = Visibility.Visible;
            TxtBadge.Text = $"X:{Math.Round(Roi.X)} Y:{Math.Round(Roi.Y)} | {Math.Round(Roi.Width)}×{Math.Round(Roi.Height)} px";

            BadgeBorder.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double badgeH = BadgeBorder.DesiredSize.Height;
            double badgeTop = (viewRect.Top - badgeH - 4 >= bounds.Top) ? (viewRect.Top - badgeH - 4) : (viewRect.Top + 4);
            Canvas.SetLeft(BadgeBorder, Math.Max(bounds.Left, viewRect.Left));
            Canvas.SetTop(BadgeBorder, badgeTop);
        }

        private static void SetHandlePos(FrameworkElement handle, double left, double top)
        {
            Canvas.SetLeft(handle, left);
            Canvas.SetTop(handle, top);
        }

        private void SetHandlesVisibility(Visibility v)
        {
            H_NW.Visibility = v; H_N.Visibility = v; H_NE.Visibility = v;
            H_E.Visibility = v;  H_SE.Visibility = v; H_S.Visibility = v;
            H_SW.Visibility = v; H_W.Visibility = v;
        }

        #endregion

        #region Public Methods (Các hàm tiện ích công khai)

        /// <summary>
        /// Cắt vùng ảnh được chọn bởi ROI thành BitmapSource mới
        /// </summary>
        public BitmapSource? Crop()
        {
            if (ImageSource is not BitmapSource bmp || Roi.IsEmpty || Roi.Width <= 0 || Roi.Height <= 0)
                return null;

            int rx = (int)Math.Max(0, Math.Round(Roi.X));
            int ry = (int)Math.Max(0, Math.Round(Roi.Y));
            int rw = (int)Math.Min(bmp.PixelWidth - rx, Math.Round(Roi.Width));
            int rh = (int)Math.Min(bmp.PixelHeight - ry, Math.Round(Roi.Height));

            if (rw <= 0 || rh <= 0)
                return null;

            return new CroppedBitmap(bmp, new Int32Rect(rx, ry, rw, rh));
        }

        /// <summary>
        /// Đặt ROI bao trọn toàn bộ ảnh
        /// </summary>
        public void ResetFull()
        {
            if (ImageSource is BitmapSource bmp)
            {
                Roi = new Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight);
                RoiChanged?.Invoke(this, Roi);
            }
        }

        /// <summary>
        /// Xóa vùng ROI
        /// </summary>
        public void Clear()
        {
            Roi = Rect.Empty;
            RoiChanged?.Invoke(this, Roi);
        }

        /// <summary>
        /// Thiết lập tọa độ ROI bằng code C#
        /// </summary>
        public void SetRoi(double x, double y, double width, double height)
        {
            if (ImageSource is BitmapSource bmp)
            {
                x = Math.Clamp(x, 0, bmp.PixelWidth);
                y = Math.Clamp(y, 0, bmp.PixelHeight);
                width = Math.Clamp(width, 0, bmp.PixelWidth - x);
                height = Math.Clamp(height, 0, bmp.PixelHeight - y);

                Roi = new Rect(x, y, width, height);
                RoiChanged?.Invoke(this, Roi);
            }
        }

        #endregion
    }
}
