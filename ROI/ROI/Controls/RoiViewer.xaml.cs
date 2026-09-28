using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ROI.Controls
{
    public partial class RoiViewer : UserControl
    {
        private enum DragMode
        {
            None,
            Draw,
            Move,
            ResizeNW,
            ResizeN,
            ResizeNE,
            ResizeE,
            ResizeSE,
            ResizeS,
            ResizeSW,
            ResizeW
        }

        #region Dependency Properties

        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register(
                nameof(ImageSource),
                typeof(ImageSource),
                typeof(RoiViewer),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnImageSourceChanged));

        public static readonly DependencyProperty RoiProperty =
            DependencyProperty.Register(
                nameof(Roi),
                typeof(Rect),
                typeof(RoiViewer),
                new FrameworkPropertyMetadata(Rect.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnRoiChanged));

        public static readonly DependencyProperty RoiColorProperty =
            DependencyProperty.Register(
                nameof(RoiColor),
                typeof(Brush),
                typeof(RoiViewer),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0, 230, 118)), OnRoiColorChanged));

        public static readonly DependencyProperty ShowDimmedOverlayProperty =
            DependencyProperty.Register(
                nameof(ShowDimmedOverlay),
                typeof(bool),
                typeof(RoiViewer),
                new PropertyMetadata(true, (d, e) => ((RoiViewer)d).UpdateVisuals()));

        public static readonly DependencyProperty ShowCoordinatesProperty =
            DependencyProperty.Register(
                nameof(ShowCoordinates),
                typeof(bool),
                typeof(RoiViewer),
                new PropertyMetadata(true, (d, e) => ((RoiViewer)d).UpdateVisuals()));

        public static readonly DependencyProperty ShowCenterCrosshairProperty =
            DependencyProperty.Register(
                nameof(ShowCenterCrosshair),
                typeof(bool),
                typeof(RoiViewer),
                new PropertyMetadata(true, (d, e) => ((RoiViewer)d).UpdateVisuals()));

        public static readonly DependencyProperty IsInteractiveProperty =
            DependencyProperty.Register(
                nameof(IsInteractive),
                typeof(bool),
                typeof(RoiViewer),
                new PropertyMetadata(true));

        public ImageSource? ImageSource
        {
            get => (ImageSource?)GetValue(ImageSourceProperty);
            set => SetValue(ImageSourceProperty, value);
        }

        /// <summary>
        /// ROI rectangle in Image Pixel Coordinates (X, Y, Width, Height)
        /// </summary>
        public Rect Roi
        {
            get => (Rect)GetValue(RoiProperty);
            set => SetValue(RoiProperty, value);
        }

        public Brush RoiColor
        {
            get => (Brush)GetValue(RoiColorProperty);
            set => SetValue(RoiColorProperty, value);
        }

        public bool ShowDimmedOverlay
        {
            get => (bool)GetValue(ShowDimmedOverlayProperty);
            set => SetValue(ShowDimmedOverlayProperty, value);
        }

        public bool ShowCoordinates
        {
            get => (bool)GetValue(ShowCoordinatesProperty);
            set => SetValue(ShowCoordinatesProperty, value);
        }

        public bool ShowCenterCrosshair
        {
            get => (bool)GetValue(ShowCenterCrosshairProperty);
            set => SetValue(ShowCenterCrosshairProperty, value);
        }

        public bool IsInteractive
        {
            get => (bool)GetValue(IsInteractiveProperty);
            set => SetValue(IsInteractiveProperty, value);
        }

        #endregion

        #region Events

        public event EventHandler<Rect>? RoiChangedByUser;

        #endregion

        #region Fields

        private DragMode _currentDragMode = DragMode.None;
        private Point _dragStartViewPoint;
        private Rect _initialRoi;
        private bool _isUpdatingInternally = false;
        private const double MinRoiDimension = 5.0; // Minimum ROI size in pixels

        #endregion

        public RoiViewer()
        {
            InitializeComponent();

            Loaded += (s, e) => UpdateVisuals();
            SizeChanged += (s, e) => UpdateVisuals();

            SetupHandleEvents();
            SetupCanvasEvents();
            ApplyRoiColor();
        }

        #region Setup Handlers & Events

        private void SetupHandleEvents()
        {
            HandleNW.MouseLeftButtonDown += (s, e) => StartDrag(DragMode.ResizeNW, e);
            HandleN.MouseLeftButtonDown += (s, e) => StartDrag(DragMode.ResizeN, e);
            HandleNE.MouseLeftButtonDown += (s, e) => StartDrag(DragMode.ResizeNE, e);
            HandleE.MouseLeftButtonDown += (s, e) => StartDrag(DragMode.ResizeE, e);
            HandleSE.MouseLeftButtonDown += (s, e) => StartDrag(DragMode.ResizeSE, e);
            HandleS.MouseLeftButtonDown += (s, e) => StartDrag(DragMode.ResizeS, e);
            HandleSW.MouseLeftButtonDown += (s, e) => StartDrag(DragMode.ResizeSW, e);
            HandleW.MouseLeftButtonDown += (s, e) => StartDrag(DragMode.ResizeW, e);

            RoiBorderElement.MouseLeftButtonDown += (s, e) => StartDrag(DragMode.Move, e);
        }

        private void SetupCanvasEvents()
        {
            OverlayCanvas.MouseLeftButtonDown += OverlayCanvas_MouseLeftButtonDown;
            OverlayCanvas.MouseMove += OverlayCanvas_MouseMove;
            OverlayCanvas.MouseLeftButtonUp += OverlayCanvas_MouseLeftButtonUp;
            KeyDown += RoiViewer_KeyDown;
        }

        private static void OnImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewer = (RoiViewer)d;
            if (viewer.DisplayImage == null) return;
            viewer.DisplayImage.Source = e.NewValue as ImageSource;
            if (viewer.PlaceholderPanel != null)
            {
                viewer.PlaceholderPanel.Visibility = (e.NewValue == null) ? Visibility.Visible : Visibility.Collapsed;
            }

            // When new image is loaded, if ROI is empty, set a sensible default or keep empty
            if (viewer.ImageSource is BitmapSource bmp)
            {
                if (viewer.Roi.IsEmpty || viewer.Roi.Width <= 0 || viewer.Roi.Height <= 0)
                {
                    // Create an initial ROI in the center (e.g. 50% size)
                    double w = Math.Round(bmp.PixelWidth * 0.6);
                    double h = Math.Round(bmp.PixelHeight * 0.6);
                    double x = Math.Round((bmp.PixelWidth - w) / 2.0);
                    double y = Math.Round((bmp.PixelHeight - h) / 2.0);
                    viewer.Roi = new Rect(x, y, w, h);
                }
                else
                {
                    // Clamp existing ROI to new image bounds
                    viewer.ClampRoi();
                }
            }
            viewer.UpdateVisuals();
        }

        private static void OnRoiChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewer = (RoiViewer)d;
            if (!viewer._isUpdatingInternally)
            {
                viewer.UpdateVisuals();
            }
        }

        private static void OnRoiColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewer = (RoiViewer)d;
            viewer.ApplyRoiColor();
        }

        private void ApplyRoiColor()
        {
            if (RoiBorderElement == null || CenterHLine == null || CenterVLine == null || HandleNW == null) return;

            if (RoiColor is SolidColorBrush solid)
            {
                RoiBorderElement.Stroke = solid;
                RoiBorderElement.Fill = new SolidColorBrush(Color.FromArgb(35, solid.Color.R, solid.Color.G, solid.Color.B));
                CenterHLine.Stroke = new SolidColorBrush(Color.FromArgb(128, solid.Color.R, solid.Color.G, solid.Color.B));
                CenterVLine.Stroke = new SolidColorBrush(Color.FromArgb(128, solid.Color.R, solid.Color.G, solid.Color.B));

                var handleBorder = solid;
                HandleNW.Stroke = handleBorder;
                HandleN.Stroke = handleBorder;
                HandleNE.Stroke = handleBorder;
                HandleE.Stroke = handleBorder;
                HandleSE.Stroke = handleBorder;
                HandleS.Stroke = handleBorder;
                HandleSW.Stroke = handleBorder;
                HandleW.Stroke = handleBorder;
            }
        }

        #endregion

        #region Coordinate Calculations

        /// <summary>
        /// Calculates the layout bounds of the displayed image inside the OverlayCanvas
        /// </summary>
        public Rect GetImageDisplayRect()
        {
            if (ImageSource is not BitmapSource bmp)
                return Rect.Empty;

            double containerW = OverlayCanvas.ActualWidth;
            double containerH = OverlayCanvas.ActualHeight;

            if (containerW <= 0 || containerH <= 0 || bmp.PixelWidth <= 0 || bmp.PixelHeight <= 0)
                return Rect.Empty;

            double scaleX = containerW / bmp.PixelWidth;
            double scaleY = containerH / bmp.PixelHeight;
            double scale = Math.Min(scaleX, scaleY);

            double dispW = bmp.PixelWidth * scale;
            double dispH = bmp.PixelHeight * scale;

            double offsetX = (containerW - dispW) / 2.0;
            double offsetY = (containerH - dispH) / 2.0;

            return new Rect(offsetX, offsetY, dispW, dispH);
        }

        public Point CanvasToPixel(Point canvasPoint)
        {
            var dispRect = GetImageDisplayRect();
            if (dispRect.IsEmpty || ImageSource is not BitmapSource bmp)
                return new Point(0, 0);

            double scale = dispRect.Width / bmp.PixelWidth;
            double px = (canvasPoint.X - dispRect.Left) / scale;
            double py = (canvasPoint.Y - dispRect.Top) / scale;

            px = Math.Clamp(px, 0, bmp.PixelWidth);
            py = Math.Clamp(py, 0, bmp.PixelHeight);

            return new Point(px, py);
        }

        public Rect PixelToCanvas(Rect pixelRect)
        {
            var dispRect = GetImageDisplayRect();
            if (dispRect.IsEmpty || ImageSource is not BitmapSource bmp)
                return Rect.Empty;

            double scale = dispRect.Width / bmp.PixelWidth;
            double vx = dispRect.Left + pixelRect.X * scale;
            double vy = dispRect.Top + pixelRect.Y * scale;
            double vw = pixelRect.Width * scale;
            double vh = pixelRect.Height * scale;

            return new Rect(vx, vy, Math.Max(0, vw), Math.Max(0, vh));
        }

        #endregion

        #region Mouse Drag & Interaction

        private void StartDrag(DragMode mode, MouseButtonEventArgs e)
        {
            if (!IsInteractive || ImageSource == null)
                return;

            Focus();
            _currentDragMode = mode;
            _dragStartViewPoint = e.GetPosition(OverlayCanvas);
            _initialRoi = Roi;

            OverlayCanvas.CaptureMouse();
            e.Handled = true;
        }

        private void OverlayCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsInteractive || ImageSource is not BitmapSource)
                return;

            var dispRect = GetImageDisplayRect();
            var pt = e.GetPosition(OverlayCanvas);

            // Only allow drawing if clicked inside the image display area
            if (dispRect.Contains(pt))
            {
                Focus();
                _currentDragMode = DragMode.Draw;
                _dragStartViewPoint = pt;

                var startPixel = CanvasToPixel(pt);
                _initialRoi = new Rect(startPixel.X, startPixel.Y, 0, 0);

                SetRoiInternal(_initialRoi);
                OverlayCanvas.CaptureMouse();
                e.Handled = true;
            }
        }

        private void OverlayCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_currentDragMode == DragMode.None || ImageSource is not BitmapSource bmp)
                return;

            var currentPoint = e.GetPosition(OverlayCanvas);
            var dispRect = GetImageDisplayRect();
            if (dispRect.IsEmpty)
                return;

            double scale = dispRect.Width / bmp.PixelWidth;

            switch (_currentDragMode)
            {
                case DragMode.Draw:
                    {
                        var startPixel = CanvasToPixel(_dragStartViewPoint);
                        var curPixel = CanvasToPixel(currentPoint);

                        double x = Math.Min(startPixel.X, curPixel.X);
                        double y = Math.Min(startPixel.Y, curPixel.Y);
                        double w = Math.Abs(curPixel.X - startPixel.X);
                        double h = Math.Abs(curPixel.Y - startPixel.Y);

                        SetRoiInternal(new Rect(Math.Round(x), Math.Round(y), Math.Round(w), Math.Round(h)));
                    }
                    break;

                case DragMode.Move:
                    {
                        double deltaPixelX = (currentPoint.X - _dragStartViewPoint.X) / scale;
                        double deltaPixelY = (currentPoint.Y - _dragStartViewPoint.Y) / scale;

                        double newX = _initialRoi.X + deltaPixelX;
                        double newY = _initialRoi.Y + deltaPixelY;

                        // Clamping so ROI stays completely inside image bounds
                        newX = Math.Clamp(newX, 0, Math.Max(0, bmp.PixelWidth - _initialRoi.Width));
                        newY = Math.Clamp(newY, 0, Math.Max(0, bmp.PixelHeight - _initialRoi.Height));

                        SetRoiInternal(new Rect(Math.Round(newX), Math.Round(newY), _initialRoi.Width, _initialRoi.Height));
                    }
                    break;

                case DragMode.ResizeNW:
                case DragMode.ResizeN:
                case DragMode.ResizeNE:
                case DragMode.ResizeE:
                case DragMode.ResizeSE:
                case DragMode.ResizeS:
                case DragMode.ResizeSW:
                case DragMode.ResizeW:
                    {
                        var curPixel = CanvasToPixel(currentPoint);
                        double left = _initialRoi.Left;
                        double top = _initialRoi.Top;
                        double right = _initialRoi.Right;
                        double bottom = _initialRoi.Bottom;

                        // Top adjustments
                        if (_currentDragMode is DragMode.ResizeNW or DragMode.ResizeN or DragMode.ResizeNE)
                        {
                            top = Math.Min(curPixel.Y, bottom - MinRoiDimension);
                            top = Math.Clamp(top, 0, bmp.PixelHeight);
                        }

                        // Bottom adjustments
                        if (_currentDragMode is DragMode.ResizeSW or DragMode.ResizeS or DragMode.ResizeSE)
                        {
                            bottom = Math.Max(curPixel.Y, top + MinRoiDimension);
                            bottom = Math.Clamp(bottom, 0, bmp.PixelHeight);
                        }

                        // Left adjustments
                        if (_currentDragMode is DragMode.ResizeNW or DragMode.ResizeW or DragMode.ResizeSW)
                        {
                            left = Math.Min(curPixel.X, right - MinRoiDimension);
                            left = Math.Clamp(left, 0, bmp.PixelWidth);
                        }

                        // Right adjustments
                        if (_currentDragMode is DragMode.ResizeNE or DragMode.ResizeE or DragMode.ResizeSE)
                        {
                            right = Math.Max(curPixel.X, left + MinRoiDimension);
                            right = Math.Clamp(right, 0, bmp.PixelWidth);
                        }

                        double w = Math.Max(MinRoiDimension, right - left);
                        double h = Math.Max(MinRoiDimension, bottom - top);

                        SetRoiInternal(new Rect(Math.Round(left), Math.Round(top), Math.Round(w), Math.Round(h)));
                    }
                    break;
            }
        }

        private void OverlayCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_currentDragMode != DragMode.None)
            {
                OverlayCanvas.ReleaseMouseCapture();

                // If drawing made an ROI too small, reset to minimal or previous
                if (_currentDragMode == DragMode.Draw && (Roi.Width < MinRoiDimension || Roi.Height < MinRoiDimension))
                {
                    SetRoiInternal(_initialRoi.Width >= MinRoiDimension ? _initialRoi : Rect.Empty);
                }

                _currentDragMode = DragMode.None;
                RoiChangedByUser?.Invoke(this, Roi);
            }
        }

        private void RoiViewer_KeyDown(object sender, KeyEventArgs e)
        {
            if (!IsInteractive || Roi.IsEmpty || ImageSource is not BitmapSource bmp)
                return;

            double step = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? 10.0 : 1.0;
            double dx = 0;
            double dy = 0;

            switch (e.Key)
            {
                case Key.Left: dx = -step; break;
                case Key.Right: dx = step; break;
                case Key.Up: dy = -step; break;
                case Key.Down: dy = step; break;
                case Key.Escape:
                    ClearRoi();
                    e.Handled = true;
                    return;
                default:
                    return;
            }

            double newX = Math.Clamp(Roi.X + dx, 0, Math.Max(0, bmp.PixelWidth - Roi.Width));
            double newY = Math.Clamp(Roi.Y + dy, 0, Math.Max(0, bmp.PixelHeight - Roi.Height));

            SetRoiInternal(new Rect(newX, newY, Roi.Width, Roi.Height));
            RoiChangedByUser?.Invoke(this, Roi);
            e.Handled = true;
        }

        private void SetRoiInternal(Rect newRoi)
        {
            _isUpdatingInternally = true;
            Roi = newRoi;
            _isUpdatingInternally = false;
            UpdateVisuals();
        }

        private void ClampRoi()
        {
            if (ImageSource is not BitmapSource bmp || Roi.IsEmpty)
                return;

            double x = Math.Clamp(Roi.X, 0, bmp.PixelWidth);
            double y = Math.Clamp(Roi.Y, 0, bmp.PixelHeight);
            double w = Math.Clamp(Roi.Width, 0, bmp.PixelWidth - x);
            double h = Math.Clamp(Roi.Height, 0, bmp.PixelHeight - y);

            SetRoiInternal(new Rect(x, y, w, h));
        }

        #endregion

        #region Visual Update

        public void UpdateVisuals()
        {
            if (RoiBorderElement == null || OverlayCanvas == null || DisplayImage == null || CoordBadge == null)
                return;

            if (ImageSource is not BitmapSource bmp || Roi.IsEmpty || Roi.Width <= 0 || Roi.Height <= 0)
            {
                RoiBorderElement.Visibility = Visibility.Collapsed;
                SetHandlesVisibility(Visibility.Collapsed);
                CoordBadge.Visibility = Visibility.Collapsed;
                CenterHLine.Visibility = Visibility.Collapsed;
                CenterVLine.Visibility = Visibility.Collapsed;
                DimmedMaskPath.Visibility = Visibility.Collapsed;
                return;
            }

            var dispRect = GetImageDisplayRect();
            if (dispRect.IsEmpty)
            {
                RoiBorderElement.Visibility = Visibility.Collapsed;
                SetHandlesVisibility(Visibility.Collapsed);
                CoordBadge.Visibility = Visibility.Collapsed;
                CenterHLine.Visibility = Visibility.Collapsed;
                CenterVLine.Visibility = Visibility.Collapsed;
                DimmedMaskPath.Visibility = Visibility.Collapsed;
                return;
            }

            var viewRect = PixelToCanvas(Roi);

            // 1. Update ROI Box
            RoiBorderElement.Visibility = Visibility.Visible;
            RoiBorderElement.Width = Math.Max(0, viewRect.Width);
            RoiBorderElement.Height = Math.Max(0, viewRect.Height);
            Canvas.SetLeft(RoiBorderElement, viewRect.Left);
            Canvas.SetTop(RoiBorderElement, viewRect.Top);

            // 2. Update Dimmed Mask
            if (ShowDimmedOverlay)
            {
                DimmedMaskPath.Visibility = Visibility.Visible;
                ImageBoundsGeometry.Rect = dispRect;
                RoiBoundsGeometry.Rect = viewRect;
            }
            else
            {
                DimmedMaskPath.Visibility = Visibility.Collapsed;
            }

            // 3. Update Center Crosshair
            if (ShowCenterCrosshair && viewRect.Width > 20 && viewRect.Height > 20)
            {
                CenterHLine.Visibility = Visibility.Visible;
                CenterHLine.X1 = viewRect.Left + 5;
                CenterHLine.Y1 = viewRect.Top + viewRect.Height / 2.0;
                CenterHLine.X2 = viewRect.Right - 5;
                CenterHLine.Y2 = CenterHLine.Y1;

                CenterVLine.Visibility = Visibility.Visible;
                CenterVLine.X1 = viewRect.Left + viewRect.Width / 2.0;
                CenterVLine.Y1 = viewRect.Top + 5;
                CenterVLine.X2 = CenterVLine.X1;
                CenterVLine.Y2 = viewRect.Bottom - 5;
            }
            else
            {
                CenterHLine.Visibility = Visibility.Collapsed;
                CenterVLine.Visibility = Visibility.Collapsed;
            }

            // 4. Update 8 Handles
            if (IsInteractive)
            {
                SetHandlesVisibility(Visibility.Visible);
                const double hw = 5.0; // half handle size (10 / 2)

                PositionHandle(HandleNW, viewRect.Left - hw, viewRect.Top - hw);
                PositionHandle(HandleN, viewRect.Left + viewRect.Width / 2.0 - hw, viewRect.Top - hw);
                PositionHandle(HandleNE, viewRect.Right - hw, viewRect.Top - hw);
                PositionHandle(HandleE, viewRect.Right - hw, viewRect.Top + viewRect.Height / 2.0 - hw);
                PositionHandle(HandleSE, viewRect.Right - hw, viewRect.Bottom - hw);
                PositionHandle(HandleS, viewRect.Left + viewRect.Width / 2.0 - hw, viewRect.Bottom - hw);
                PositionHandle(HandleSW, viewRect.Left - hw, viewRect.Bottom - hw);
                PositionHandle(HandleW, viewRect.Left - hw, viewRect.Top + viewRect.Height / 2.0 - hw);
            }
            else
            {
                SetHandlesVisibility(Visibility.Collapsed);
            }

            // 5. Update Coordinate Badge
            if (ShowCoordinates)
            {
                CoordBadge.Visibility = Visibility.Visible;
                CoordText.Text = $"X:{Math.Round(Roi.X)} Y:{Math.Round(Roi.Y)} | {Math.Round(Roi.Width)}×{Math.Round(Roi.Height)} px";

                CoordBadge.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                double badgeH = CoordBadge.DesiredSize.Height;

                double badgeLeft = Math.Max(dispRect.Left, viewRect.Left);
                double badgeTop = viewRect.Top - badgeH - 4;
                if (badgeTop < dispRect.Top)
                {
                    // If not enough room above ROI, show inside ROI
                    badgeTop = viewRect.Top + 4;
                }

                Canvas.SetLeft(CoordBadge, badgeLeft);
                Canvas.SetTop(CoordBadge, badgeTop);
            }
            else
            {
                CoordBadge.Visibility = Visibility.Collapsed;
            }
        }

        private static void PositionHandle(FrameworkElement handle, double left, double top)
        {
            Canvas.SetLeft(handle, left);
            Canvas.SetTop(handle, top);
        }

        private void SetHandlesVisibility(Visibility visibility)
        {
            HandleNW.Visibility = visibility;
            HandleN.Visibility = visibility;
            HandleNE.Visibility = visibility;
            HandleE.Visibility = visibility;
            HandleSE.Visibility = visibility;
            HandleS.Visibility = visibility;
            HandleSW.Visibility = visibility;
            HandleW.Visibility = visibility;
        }

        #endregion

        #region Public Helper Methods

        /// <summary>
        /// Crops the selected ROI region from the current image
        /// </summary>
        public BitmapSource? CropRoi()
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
        /// Resets the ROI to cover the entire image
        /// </summary>
        public void ResetToFullImage()
        {
            if (ImageSource is BitmapSource bmp)
            {
                Roi = new Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight);
                RoiChangedByUser?.Invoke(this, Roi);
            }
        }

        /// <summary>
        /// Clears the ROI
        /// </summary>
        public void ClearRoi()
        {
            Roi = Rect.Empty;
            RoiChangedByUser?.Invoke(this, Roi);
        }

        /// <summary>
        /// Sets the ROI with specific pixel coordinates
        /// </summary>
        public void SetRoiCoordinates(double x, double y, double width, double height)
        {
            if (ImageSource is BitmapSource bmp)
            {
                x = Math.Clamp(x, 0, bmp.PixelWidth);
                y = Math.Clamp(y, 0, bmp.PixelHeight);
                width = Math.Clamp(width, 0, bmp.PixelWidth - x);
                height = Math.Clamp(height, 0, bmp.PixelHeight - y);

                Roi = new Rect(x, y, width, height);
                RoiChangedByUser?.Invoke(this, Roi);
            }
        }

        #endregion
    }
}
