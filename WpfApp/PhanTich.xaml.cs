using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfApp
{
    public partial class PhanTich : Page
    {
        // ============================================================
        // MODEL cho DataGrid
        // ============================================================
        public class TopDanhMucModel
        {
            public int Hang { get; set; }
            public string TenDanhMuc { get; set; }
            public decimal SoTien { get; set; }
            public string SoTienHienThi => $"{SoTien:N0} đ";
            public double TyLe { get; set; }
            public string TyLeHienThi => $"{TyLe:F1}%";
        }

        public class ThangNamItem
        {
            public int Thang { get; set; }
            public int Nam { get; set; }
            public string HienThi => $"Tháng {Thang}/{Nam}";
        }

        private DateTime tuNgay;
        private DateTime denNgay;
        private string _loaiXem = "Tổng hợp";
        private string _kieuSoSanh = "📊 So sánh 2 tháng";

        private int _thang1 = DateTime.Now.Month;
        private int _nam1 = DateTime.Now.Year;
        private int _thang2 = DateTime.Now.AddMonths(-1).Month;
        private int _nam2 = DateTime.Now.AddMonths(-1).Year;
        private int _thangDuyNhat = DateTime.Now.Month;
        private int _namDuyNhat = DateTime.Now.Year;

        public PhanTich()
        {
            InitializeComponent();
            this.Loaded += PhanTich_Loaded;
        }

        private void PhanTich_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDanhSachThangVaoComboBox();
            CapNhatKhoangThoiGian();
            LoadTatCa();
        }

        // ============================================================
        // LOAD DANH SÁCH THÁNG
        // ============================================================
        private void LoadDanhSachThangVaoComboBox()
        {
            try
            {
                string query = @"
                    SELECT DISTINCT YEAR(NgayGiaoDich) AS Nam, MONTH(NgayGiaoDich) AS Thang
                    FROM GiaoDich
                    WHERE MaNguoiDung = @MaNguoiDung
                    ORDER BY Nam DESC, Thang DESC";

                SqlParameter[] p = {
                    new SqlParameter("@MaNguoiDung", DangNhap.MaNguoiDungHienTai)
                };

                DataTable dt = DatabaseHelper.GetData(query, p);

                List<ThangNamItem> danhSach = new List<ThangNamItem>();

                foreach (DataRow row in dt.Rows)
                {
                    danhSach.Add(new ThangNamItem
                    {
                        Thang = Convert.ToInt32(row["Thang"]),
                        Nam = Convert.ToInt32(row["Nam"])
                    });
                }

                if (danhSach.Count == 0)
                {
                    DateTime now = DateTime.Now;
                    for (int i = 0; i < 12; i++)
                    {
                        DateTime m = now.AddMonths(-i);
                        danhSach.Add(new ThangNamItem
                        {
                            Thang = m.Month,
                            Nam = m.Year
                        });
                    }
                }

                if (cboThang1 != null)
                {
                    cboThang1.ItemsSource = danhSach;
                    cboThang1.DisplayMemberPath = "HienThi";
                    cboThang1.SelectedIndex = 0;
                }

                if (cboThang2 != null)
                {
                    cboThang2.ItemsSource = danhSach;
                    cboThang2.DisplayMemberPath = "HienThi";
                    cboThang2.SelectedIndex = danhSach.Count >= 2 ? 1 : 0;
                }

                if (cboThangDuyNhat != null)
                {
                    cboThangDuyNhat.ItemsSource = danhSach;
                    cboThangDuyNhat.DisplayMemberPath = "HienThi";
                    cboThangDuyNhat.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi load tháng: " + ex.Message);
            }
        }

        // ============================================================
        // TÍNH KHOẢNG THỜI GIAN
        // ============================================================
        private void CapNhatKhoangThoiGian()
        {
            if (cboLocThoiGian == null || cboLocThoiGian.SelectedItem == null) return;

            string selected = ((ComboBoxItem)cboLocThoiGian.SelectedItem).Content.ToString();
            DateTime now = DateTime.Now;
            DateTime today = now.Date;

            if (pnlChonKhoangNgay != null)
                pnlChonKhoangNgay.Visibility = Visibility.Collapsed;

            switch (selected)
            {
                case "Hôm nay":
                    tuNgay = today;
                    denNgay = today.AddHours(23).AddMinutes(59).AddSeconds(59);
                    break;

                case "Hôm qua":
                    tuNgay = today.AddDays(-1);
                    denNgay = today.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);
                    break;

                case "7 ngày gần đây":
                    tuNgay = today.AddDays(-7);
                    denNgay = today.AddHours(23).AddMinutes(59).AddSeconds(59);
                    break;

                case "30 ngày gần đây":
                    tuNgay = today.AddDays(-30);
                    denNgay = today.AddHours(23).AddMinutes(59).AddSeconds(59);
                    break;

                case "📅 Tùy chọn khoảng ngày":
                    if (pnlChonKhoangNgay != null)
                        pnlChonKhoangNgay.Visibility = Visibility.Visible;

                    if (dpTuNgay != null && dpTuNgay.SelectedDate == null)
                        dpTuNgay.SelectedDate = today.AddDays(-30);
                    if (dpDenNgay != null && dpDenNgay.SelectedDate == null)
                        dpDenNgay.SelectedDate = today;

                    if (dpTuNgay != null && dpTuNgay.SelectedDate.HasValue)
                        tuNgay = dpTuNgay.SelectedDate.Value.Date;
                    else
                        tuNgay = today.AddDays(-30);

                    if (dpDenNgay != null && dpDenNgay.SelectedDate.HasValue)
                        denNgay = dpDenNgay.SelectedDate.Value.Date
                                    .AddHours(23).AddMinutes(59).AddSeconds(59);
                    else
                        denNgay = today.AddHours(23).AddMinutes(59).AddSeconds(59);
                    break;

                case "Tuần này":
                    int diff = (int)now.DayOfWeek - 1;
                    if (diff < 0) diff = 6;
                    tuNgay = now.AddDays(-diff).Date;
                    denNgay = tuNgay.AddDays(6).AddHours(23).AddMinutes(59);
                    break;

                case "Tháng trước":
                    DateTime lastMonth = now.AddMonths(-1);
                    tuNgay = new DateTime(lastMonth.Year, lastMonth.Month, 1);
                    denNgay = new DateTime(lastMonth.Year, lastMonth.Month,
                                DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month), 23, 59, 59);
                    break;

                case "Năm nay":
                    tuNgay = new DateTime(now.Year, 1, 1);
                    denNgay = new DateTime(now.Year, 12, 31, 23, 59, 59);
                    break;

                case "Tháng này":
                default:
                    tuNgay = new DateTime(now.Year, now.Month, 1);
                    denNgay = new DateTime(now.Year, now.Month,
                                DateTime.DaysInMonth(now.Year, now.Month), 23, 59, 59);
                    break;
            }
        }

        private void LoadTatCa()
        {
            LoadBieuDoTron();
            LoadBieuDoCot();
            LoadTopDanhMuc();
        }

        // ============================================================
        // BIỂU ĐỒ TRÒN — Hỗ trợ "Tổng hợp" + Fix 1 danh mục
        // ============================================================
        private void LoadBieuDoTron()
        {
            if (canvasBieuDoTron == null) return;
            canvasBieuDoTron.Children.Clear();

            try
            {
                string loaiDanhMuc = null;
                string tieuDe;
                bool laTongHop = false;

                switch (_loaiXem)
                {
                    case "Chỉ xem Thu nhập":
                        loaiDanhMuc = "Thu nhập";
                        tieuDe = "CƠ CẤU THU NHẬP";
                        break;
                    case "Chỉ xem Chi tiêu":
                        loaiDanhMuc = "Chi tiêu";
                        tieuDe = "CƠ CẤU CHI TIÊU";
                        break;
                    case "Tổng hợp":
                    default:
                        loaiDanhMuc = null;
                        tieuDe = "CƠ CẤU THU CHI (TỔNG HỢP)";
                        laTongHop = true;
                        break;
                }

                if (txtTieuDeBieuDoTron != null)
                    txtTieuDeBieuDoTron.Text = tieuDe;

                string query;
                SqlParameter[] p;

                if (laTongHop)
                {
                    query = @"
                        SELECT dm.TenDanhMuc + N' (' + dm.LoaiDanhMuc + N')' AS TenHienThi,
                               SUM(gd.SoTien) AS TongTien
                        FROM GiaoDich gd
                        INNER JOIN DanhMuc dm ON gd.MaDanhMuc = dm.MaDanhMuc
                        WHERE gd.MaNguoiDung = @MaNguoiDung
                          AND gd.NgayGiaoDich >= @TuNgay
                          AND gd.NgayGiaoDich <= @DenNgay
                        GROUP BY dm.TenDanhMuc, dm.LoaiDanhMuc
                        ORDER BY TongTien DESC";

                    p = new SqlParameter[] {
                        new SqlParameter("@MaNguoiDung", DangNhap.MaNguoiDungHienTai),
                        new SqlParameter("@TuNgay", tuNgay),
                        new SqlParameter("@DenNgay", denNgay)
                    };
                }
                else
                {
                    query = @"
                        SELECT dm.TenDanhMuc AS TenHienThi,
                               SUM(gd.SoTien) AS TongTien
                        FROM GiaoDich gd
                        INNER JOIN DanhMuc dm ON gd.MaDanhMuc = dm.MaDanhMuc
                        WHERE gd.MaNguoiDung = @MaNguoiDung
                          AND dm.LoaiDanhMuc = @LoaiDanhMuc
                          AND gd.NgayGiaoDich >= @TuNgay
                          AND gd.NgayGiaoDich <= @DenNgay
                        GROUP BY dm.TenDanhMuc
                        ORDER BY TongTien DESC";

                    p = new SqlParameter[] {
                        new SqlParameter("@MaNguoiDung", DangNhap.MaNguoiDungHienTai),
                        new SqlParameter("@LoaiDanhMuc", loaiDanhMuc),
                        new SqlParameter("@TuNgay", tuNgay),
                        new SqlParameter("@DenNgay", denNgay)
                    };
                }

                DataTable dt = DatabaseHelper.GetData(query, p);

                if (dt == null || dt.Rows.Count == 0)
                {
                    TextBlock txtEmpty = new TextBlock
                    {
                        Text = "Chưa có dữ liệu trong khoảng thời gian này",
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF")),
                        FontSize = 13
                    };
                    Canvas.SetLeft(txtEmpty, 40);
                    Canvas.SetTop(txtEmpty, 120);
                    canvasBieuDoTron.Children.Add(txtEmpty);
                    return;
                }

                decimal tongTien = 0;
                foreach (DataRow row in dt.Rows)
                    tongTien += Convert.ToDecimal(row["TongTien"]);

                double canvasWidth = canvasBieuDoTron.ActualWidth > 0 ? canvasBieuDoTron.ActualWidth : 400;
                double canvasHeight = canvasBieuDoTron.ActualHeight > 0 ? canvasBieuDoTron.ActualHeight : 280;

                double centerX = canvasWidth * 0.35;
                double centerY = canvasHeight * 0.5;
                double radius = Math.Min(canvasWidth * 0.3, canvasHeight * 0.4);

                Brush[] colors = new Brush[]
                {
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")),
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B5CF6")),
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EC4899")),
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#06B6D4"))
                };

                double startAngle = 0;

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    string tenDM = dt.Rows[i]["TenHienThi"].ToString();
                    decimal soTien = Convert.ToDecimal(dt.Rows[i]["TongTien"]);
                    double tyLe = tongTien > 0 ? (double)(soTien / tongTien) * 100 : 0;
                    double sweepAngle = tyLe / 100 * 360;

                    // ⭐ XỬ LÝ ĐẶC BIỆT: 1 danh mục hoặc sweepAngle >= 360°
                    if (dt.Rows.Count == 1 || sweepAngle >= 359.9)
                    {
                        // Vẽ hình tròn đầy
                        Ellipse circle = new Ellipse
                        {
                            Width = radius * 2,
                            Height = radius * 2,
                            Fill = colors[i % colors.Length],
                            Stroke = Brushes.White,
                            StrokeThickness = 1
                        };
                        Canvas.SetLeft(circle, centerX - radius);
                        Canvas.SetTop(circle, centerY - radius);

                        circle.ToolTip = new ToolTip
                        {
                            Content = $"{tenDM}\n{soTien:N0} đ\n({tyLe:F1}%)",
                            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F2937")),
                            Foreground = Brushes.White,
                            FontSize = 13,
                            FontWeight = FontWeights.SemiBold,
                            Padding = new Thickness(12, 8, 12, 8),
                            BorderThickness = new Thickness(0),
                            HasDropShadow = true
                        };
                        circle.Cursor = System.Windows.Input.Cursors.Hand;

                        Brush origColor = colors[i % colors.Length];
                        Brush hovColor = LightenBrush(origColor, 0.3);
                        circle.MouseEnter += (s, e) => { circle.Opacity = 0.85; circle.Fill = hovColor; };
                        circle.MouseLeave += (s, e) => { circle.Opacity = 1; circle.Fill = origColor; };

                        canvasBieuDoTron.Children.Add(circle);
                    }
                    else
                    {
                        // Vẽ pie slice bình thường
                        Path slice = CreatePieSlice(centerX, centerY, radius, startAngle, sweepAngle, colors[i % colors.Length]);

                        ToolTip tip = new ToolTip
                        {
                            Content = $"{tenDM}\n{soTien:N0} đ\n({tyLe:F1}%)",
                            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F2937")),
                            Foreground = Brushes.White,
                            FontSize = 13,
                            FontWeight = FontWeights.SemiBold,
                            Padding = new Thickness(12, 8, 12, 8),
                            BorderThickness = new Thickness(0),
                            HasDropShadow = true
                        };

                        slice.ToolTip = tip;
                        slice.Cursor = System.Windows.Input.Cursors.Hand;

                        Brush originalColor = colors[i % colors.Length];
                        Brush hoverColor = LightenBrush(originalColor, 0.3);

                        slice.MouseEnter += (s, e) =>
                        {
                            slice.Opacity = 0.85;
                            slice.StrokeThickness = 3;
                            slice.Stroke = Brushes.White;
                            slice.Fill = hoverColor;
                        };

                        slice.MouseLeave += (s, e) =>
                        {
                            slice.Opacity = 1;
                            slice.StrokeThickness = 1;
                            slice.Stroke = Brushes.White;
                            slice.Fill = originalColor;
                        };

                        canvasBieuDoTron.Children.Add(slice);
                    }

                    startAngle += sweepAngle;
                }

                // Legend
                double legendX = canvasWidth * 0.65;
                double legendY = 20;

                for (int i = 0; i < dt.Rows.Count && i < 7; i++)
                {
                    string tenDM = dt.Rows[i]["TenHienThi"].ToString();
                    decimal soTien = Convert.ToDecimal(dt.Rows[i]["TongTien"]);
                    double tyLe = tongTien > 0 ? (double)(soTien / tongTien) * 100 : 0;

                    Rectangle colorBox = new Rectangle
                    {
                        Width = 14,
                        Height = 14,
                        Fill = colors[i % colors.Length],
                        RadiusX = 3,
                        RadiusY = 3
                    };
                    Canvas.SetLeft(colorBox, legendX);
                    Canvas.SetTop(colorBox, legendY);
                    canvasBieuDoTron.Children.Add(colorBox);

                    TextBlock lbl = new TextBlock
                    {
                        Text = $"{tenDM}: {tyLe:F1}%",
                        FontSize = 11,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#374151"))
                    };
                    Canvas.SetLeft(lbl, legendX + 20);
                    Canvas.SetTop(lbl, legendY - 2);
                    canvasBieuDoTron.Children.Add(lbl);

                    legendY += 25;
                }

                TextBlock txtTong = new TextBlock
                {
                    Text = $"{tongTien:N0}\nđ",
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F2937")),
                    TextAlignment = TextAlignment.Center,
                    Width = 80
                };
                Canvas.SetLeft(txtTong, centerX - 40);
                Canvas.SetTop(txtTong, centerY - 15);
                canvasBieuDoTron.Children.Add(txtTong);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi vẽ biểu đồ tròn: " + ex.Message);
            }
        }

        private Path CreatePieSlice(double cx, double cy, double radius, double startAngle, double sweepAngle, Brush fill)
        {
            double startRad = (startAngle - 90) * Math.PI / 180;
            double endRad = (startAngle + sweepAngle - 90) * Math.PI / 180;

            double x1 = cx + radius * Math.Cos(startRad);
            double y1 = cy + radius * Math.Sin(startRad);
            double x2 = cx + radius * Math.Cos(endRad);
            double y2 = cy + radius * Math.Sin(endRad);

            PathFigure fig = new PathFigure
            {
                StartPoint = new Point(cx, cy),
                IsClosed = true
            };

            fig.Segments.Add(new LineSegment(new Point(x1, y1), true));
            fig.Segments.Add(new ArcSegment
            {
                Point = new Point(x2, y2),
                Size = new Size(radius, radius),
                IsLargeArc = sweepAngle > 180,
                SweepDirection = SweepDirection.Clockwise
            });

            PathGeometry geo = new PathGeometry();
            geo.Figures.Add(fig);

            return new Path
            {
                Fill = fill,
                Data = geo,
                Stroke = Brushes.White,
                StrokeThickness = 1
            };
        }

        private Brush LightenBrush(Brush brush, double amount)
        {
            if (brush is SolidColorBrush solid)
            {
                Color c = solid.Color;
                byte r = (byte)Math.Min(255, c.R + (255 - c.R) * amount);
                byte g = (byte)Math.Min(255, c.G + (255 - c.G) * amount);
                byte b = (byte)Math.Min(255, c.B + (255 - c.B) * amount);
                return new SolidColorBrush(Color.FromRgb(r, g, b));
            }
            return brush;
        }

        // ============================================================
        // BIỂU ĐỒ CỘT — Điều hướng theo kiểu
        // ============================================================
        private void LoadBieuDoCot()
        {
            if (canvasBieuDoCot == null) return;
            canvasBieuDoCot.Children.Clear();

            if (pnlChon2Thang != null) pnlChon2Thang.Visibility = Visibility.Collapsed;
            if (pnlChon1Thang != null) pnlChon1Thang.Visibility = Visibility.Collapsed;

            if (_kieuSoSanh == "📈 Biểu đồ 12 tháng")
            {
                VeBieuDo12Thang();
            }
            else if (_kieuSoSanh == "📅 1 tháng cụ thể")
            {
                if (pnlChon1Thang != null)
                    pnlChon1Thang.Visibility = Visibility.Visible;

                VeBieuDo1Thang();
            }
            else
            {
                if (pnlChon2Thang != null)
                    pnlChon2Thang.Visibility = Visibility.Visible;

                VeBieuDo2Thang();
            }
        }

        // ============================================================
        // VẼ BIỂU ĐỒ 2 THÁNG
        // ============================================================
        private void VeBieuDo2Thang()
        {
            try
            {
                DateTime thang1 = new DateTime(_nam1, _thang1, 1);
                DateTime den1 = new DateTime(_nam1, _thang1,
                    DateTime.DaysInMonth(_nam1, _thang1), 23, 59, 59);

                DateTime thang2 = new DateTime(_nam2, _thang2, 1);
                DateTime den2 = new DateTime(_nam2, _thang2,
                    DateTime.DaysInMonth(_nam2, _thang2), 23, 59, 59);

                decimal thuT1 = LayTongTheoLoai("Thu nhập", thang1, den1);
                decimal chiT1 = LayTongTheoLoai("Chi tiêu", thang1, den1);
                decimal thuT2 = LayTongTheoLoai("Thu nhập", thang2, den2);
                decimal chiT2 = LayTongTheoLoai("Chi tiêu", thang2, den2);

                double phanTramThu = TinhPhanTram(thuT1, thuT2);
                double phanTramChi = TinhPhanTram(chiT1, chiT2);

                decimal maxVal = Math.Max(Math.Max(thuT1, chiT1), Math.Max(thuT2, chiT2));
                if (maxVal == 0) maxVal = 1;

                double canvasWidth = canvasBieuDoCot.ActualWidth > 0 ? canvasBieuDoCot.ActualWidth : 400;
                double canvasHeight = canvasBieuDoCot.ActualHeight > 0 ? canvasBieuDoCot.ActualHeight : 280;

                double barWidth = 50;
                double gap = 30;
                double startX = (canvasWidth - (4 * barWidth + 3 * gap)) / 2;
                double maxBarHeight = canvasHeight - 80;

                string[] labels = {
                    $"Thu T{_thang1}/{_nam1 % 100}",
                    $"Chi T{_thang1}/{_nam1 % 100}",
                    $"Thu T{_thang2}/{_nam2 % 100}",
                    $"Chi T{_thang2}/{_nam2 % 100}"
                };

                decimal[] values = { thuT1, chiT1, thuT2, chiT2 };
                Brush[] colors = {
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6EE7B7")),
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCA5A5"))
                };

                for (int i = 0; i < 4; i++)
                {
                    double barHeight = maxVal > 0 ? (double)(values[i] / maxVal) * maxBarHeight : 0;
                    if (barHeight < 5 && values[i] > 0) barHeight = 5;

                    double x = startX + i * (barWidth + gap);
                    double y = canvasHeight - barHeight - 40;

                    Rectangle bar = new Rectangle
                    {
                        Width = barWidth,
                        Height = barHeight,
                        Fill = colors[i],
                        RadiusX = 4,
                        RadiusY = 4
                    };

                    string thangLabel = (i == 0 || i == 1)
                        ? $"Tháng {_thang1}/{_nam1}"
                        : $"Tháng {_thang2}/{_nam2}";

                    bar.ToolTip = new ToolTip
                    {
                        Content = $"{labels[i]}\n{values[i]:N0} đ\n({thangLabel})",
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F2937")),
                        Foreground = Brushes.White,
                        FontSize = 12,
                        Padding = new Thickness(10, 6, 10, 6)
                    };
                    bar.Cursor = System.Windows.Input.Cursors.Hand;

                    Brush origBar = colors[i];
                    Brush hovBar = LightenBrush(origBar, 0.3);
                    bar.MouseEnter += (s, e) => { bar.Opacity = 0.85; bar.Fill = hovBar; };
                    bar.MouseLeave += (s, e) => { bar.Opacity = 1; bar.Fill = origBar; };

                    Canvas.SetLeft(bar, x);
                    Canvas.SetTop(bar, y);
                    canvasBieuDoCot.Children.Add(bar);

                    TextBlock lblTien = new TextBlock
                    {
                        Text = values[i] >= 1000000
                            ? $"{values[i] / 1000000:F1}M"
                            : $"{values[i] / 1000:F0}K",
                        FontSize = 11,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#374151")),
                        Width = barWidth + 20,
                        TextAlignment = TextAlignment.Center
                    };
                    Canvas.SetLeft(lblTien, x - 10);
                    Canvas.SetTop(lblTien, y - 20);
                    canvasBieuDoCot.Children.Add(lblTien);

                    if (i == 0)
                        HienThiPhanTram(canvasBieuDoCot, x, y - 42, phanTramThu, barWidth, true);
                    else if (i == 1)
                        HienThiPhanTram(canvasBieuDoCot, x, y - 42, phanTramChi, barWidth, false);

                    TextBlock lblTen = new TextBlock
                    {
                        Text = labels[i],
                        FontSize = 10,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B5563")),
                        Width = barWidth + 20,
                        TextAlignment = TextAlignment.Center
                    };
                    Canvas.SetLeft(lblTen, x - 10);
                    Canvas.SetTop(lblTen, canvasHeight - 25);
                    canvasBieuDoCot.Children.Add(lblTen);
                }

                if (txtTieuDeBieuDoCot != null)
                {
                    string xuHuongThu = phanTramThu > 0 ? "↑" : (phanTramThu < 0 ? "↓" : "=");
                    string xuHuongChi = phanTramChi > 0 ? "↑" : (phanTramChi < 0 ? "↓" : "=");
                    txtTieuDeBieuDoCot.Text = $"SO SÁNH T{_thang1}/{_nam1} VS T{_thang2}/{_nam2} | Thu {xuHuongThu} | Chi {xuHuongChi}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi vẽ biểu đồ 2 tháng: " + ex.Message);
            }
        }

        // ============================================================
        // VẼ BIỂU ĐỒ 12 THÁNG
        // ============================================================
        private void VeBieuDo12Thang()
        {
            try
            {
                int nam = DateTime.Now.Year;

                decimal[] thuThang = new decimal[12];
                decimal[] chiThang = new decimal[12];

                for (int thang = 1; thang <= 12; thang++)
                {
                    DateTime tu = new DateTime(nam, thang, 1);
                    DateTime den = new DateTime(nam, thang,
                        DateTime.DaysInMonth(nam, thang), 23, 59, 59);

                    thuThang[thang - 1] = LayTongTheoLoai("Thu nhập", tu, den);
                    chiThang[thang - 1] = LayTongTheoLoai("Chi tiêu", tu, den);
                }

                if (txtTieuDeBieuDoCot != null)
                    txtTieuDeBieuDoCot.Text = $"THU CHI CẢ NĂM {nam}";

                decimal maxVal = 0;
                for (int i = 0; i < 12; i++)
                {
                    if (thuThang[i] > maxVal) maxVal = thuThang[i];
                    if (chiThang[i] > maxVal) maxVal = chiThang[i];
                }
                if (maxVal == 0) maxVal = 1;

                double canvasWidth = canvasBieuDoCot.ActualWidth > 0 ? canvasBieuDoCot.ActualWidth : 400;
                double canvasHeight = canvasBieuDoCot.ActualHeight > 0 ? canvasBieuDoCot.ActualHeight : 280;

                double paddingLeft = 10;
                double paddingRight = 10;
                double paddingBottom = 40;
                double paddingTop = 20;

                double chartWidth = canvasWidth - paddingLeft - paddingRight;
                double chartHeight = canvasHeight - paddingTop - paddingBottom;

                double groupWidth = chartWidth / 12;
                double barWidth = groupWidth * 0.35;
                double barGap = groupWidth * 0.05;

                Brush colorThu = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                Brush colorChi = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));

                for (int i = 0; i < 12; i++)
                {
                    double groupX = paddingLeft + i * groupWidth;

                    double thuHeight = maxVal > 0 ? (double)(thuThang[i] / maxVal) * chartHeight : 0;
                    if (thuHeight < 2 && thuThang[i] > 0) thuHeight = 2;

                    Rectangle barThu = new Rectangle
                    {
                        Width = barWidth,
                        Height = thuHeight,
                        Fill = colorThu,
                        RadiusX = 2,
                        RadiusY = 2
                    };
                    Canvas.SetLeft(barThu, groupX + barGap);
                    Canvas.SetTop(barThu, paddingTop + chartHeight - thuHeight);

                    barThu.ToolTip = new ToolTip
                    {
                        Content = $"Tháng {i + 1} - Thu nhập\n{thuThang[i]:N0} đ",
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F2937")),
                        Foreground = Brushes.White,
                        FontSize = 12,
                        Padding = new Thickness(10, 6, 10, 6)
                    };
                    barThu.Cursor = System.Windows.Input.Cursors.Hand;

                    Brush hovThu = LightenBrush(colorThu, 0.3);
                    barThu.MouseEnter += (s, e) => { barThu.Opacity = 0.85; barThu.Fill = hovThu; };
                    barThu.MouseLeave += (s, e) => { barThu.Opacity = 1; barThu.Fill = colorThu; };

                    canvasBieuDoCot.Children.Add(barThu);

                    double chiHeight = maxVal > 0 ? (double)(chiThang[i] / maxVal) * chartHeight : 0;
                    if (chiHeight < 2 && chiThang[i] > 0) chiHeight = 2;

                    Rectangle barChi = new Rectangle
                    {
                        Width = barWidth,
                        Height = chiHeight,
                        Fill = colorChi,
                        RadiusX = 2,
                        RadiusY = 2
                    };
                    Canvas.SetLeft(barChi, groupX + barGap + barWidth + barGap);
                    Canvas.SetTop(barChi, paddingTop + chartHeight - chiHeight);

                    barChi.ToolTip = new ToolTip
                    {
                        Content = $"Tháng {i + 1} - Chi tiêu\n{chiThang[i]:N0} đ",
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F2937")),
                        Foreground = Brushes.White,
                        FontSize = 12,
                        Padding = new Thickness(10, 6, 10, 6)
                    };
                    barChi.Cursor = System.Windows.Input.Cursors.Hand;

                    Brush hovChi = LightenBrush(colorChi, 0.3);
                    barChi.MouseEnter += (s, e) => { barChi.Opacity = 0.85; barChi.Fill = hovChi; };
                    barChi.MouseLeave += (s, e) => { barChi.Opacity = 1; barChi.Fill = colorChi; };

                    canvasBieuDoCot.Children.Add(barChi);

                    TextBlock lblThang = new TextBlock
                    {
                        Text = $"T{i + 1}",
                        FontSize = 10,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B5563")),
                        Width = groupWidth,
                        TextAlignment = TextAlignment.Center
                    };
                    Canvas.SetLeft(lblThang, groupX);
                    Canvas.SetTop(lblThang, canvasHeight - 25);
                    canvasBieuDoCot.Children.Add(lblThang);
                }

                // Legend
                double legendX = canvasWidth - 130;
                double legendY = 5;

                Rectangle boxThu = new Rectangle
                {
                    Width = 12,
                    Height = 12,
                    Fill = colorThu,
                    RadiusX = 2,
                    RadiusY = 2
                };
                Canvas.SetLeft(boxThu, legendX);
                Canvas.SetTop(boxThu, legendY);
                canvasBieuDoCot.Children.Add(boxThu);

                TextBlock lblLegendThu = new TextBlock
                {
                    Text = "Thu",
                    FontSize = 10,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#374151"))
                };
                Canvas.SetLeft(lblLegendThu, legendX + 16);
                Canvas.SetTop(lblLegendThu, legendY - 1);
                canvasBieuDoCot.Children.Add(lblLegendThu);

                Rectangle boxChi = new Rectangle
                {
                    Width = 12,
                    Height = 12,
                    Fill = colorChi,
                    RadiusX = 2,
                    RadiusY = 2
                };
                Canvas.SetLeft(boxChi, legendX + 55);
                Canvas.SetTop(boxChi, legendY);
                canvasBieuDoCot.Children.Add(boxChi);

                TextBlock lblLegendChi = new TextBlock
                {
                    Text = "Chi",
                    FontSize = 10,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#374151"))
                };
                Canvas.SetLeft(lblLegendChi, legendX + 71);
                Canvas.SetTop(lblLegendChi, legendY - 1);
                canvasBieuDoCot.Children.Add(lblLegendChi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi vẽ biểu đồ 12 tháng: " + ex.Message);
            }
        }

        // ============================================================
        // VẼ BIỂU ĐỒ 1 THÁNG CỤ THỂ
        // ============================================================
        private void VeBieuDo1Thang()
        {
            try
            {
                DateTime tu = new DateTime(_namDuyNhat, _thangDuyNhat, 1);
                DateTime den = new DateTime(_namDuyNhat, _thangDuyNhat,
                    DateTime.DaysInMonth(_namDuyNhat, _thangDuyNhat), 23, 59, 59);

                decimal thu = LayTongTheoLoai("Thu nhập", tu, den);
                decimal chi = LayTongTheoLoai("Chi tiêu", tu, den);

                if (txtTieuDeBieuDoCot != null)
                    txtTieuDeBieuDoCot.Text = $"THU CHI THÁNG {_thangDuyNhat}/{_namDuyNhat}";

                decimal maxVal = Math.Max(thu, chi);
                if (maxVal == 0) maxVal = 1;

                double canvasWidth = canvasBieuDoCot.ActualWidth > 0 ? canvasBieuDoCot.ActualWidth : 400;
                double canvasHeight = canvasBieuDoCot.ActualHeight > 0 ? canvasBieuDoCot.ActualHeight : 280;

                double barWidth = 80;
                double gap = 60;
                double startX = (canvasWidth - (2 * barWidth + gap)) / 2;
                double maxBarHeight = canvasHeight - 100;

                string[] labels = {
                    $"Thu T{_thangDuyNhat}",
                    $"Chi T{_thangDuyNhat}"
                };

                decimal[] values = { thu, chi };
                Brush[] colors = {
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"))
                };

                for (int i = 0; i < 2; i++)
                {
                    double barHeight = maxVal > 0 ? (double)(values[i] / maxVal) * maxBarHeight : 0;
                    if (barHeight < 5 && values[i] > 0) barHeight = 5;

                    double x = startX + i * (barWidth + gap);
                    double y = canvasHeight - barHeight - 50;

                    Rectangle bar = new Rectangle
                    {
                        Width = barWidth,
                        Height = barHeight,
                        Fill = colors[i],
                        RadiusX = 6,
                        RadiusY = 6
                    };

                    bar.ToolTip = new ToolTip
                    {
                        Content = $"{labels[i]}\n{values[i]:N0} đ",
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F2937")),
                        Foreground = Brushes.White,
                        FontSize = 12,
                        Padding = new Thickness(10, 6, 10, 6)
                    };
                    bar.Cursor = System.Windows.Input.Cursors.Hand;

                    Brush origBar = colors[i];
                    Brush hovBar = LightenBrush(origBar, 0.3);
                    bar.MouseEnter += (s, e) => { bar.Opacity = 0.85; bar.Fill = hovBar; };
                    bar.MouseLeave += (s, e) => { bar.Opacity = 1; bar.Fill = origBar; };

                    Canvas.SetLeft(bar, x);
                    Canvas.SetTop(bar, y);
                    canvasBieuDoCot.Children.Add(bar);

                    TextBlock lblTien = new TextBlock
                    {
                        Text = values[i] >= 1000000
                            ? $"{values[i] / 1000000:F1}M đ"
                            : $"{values[i]:N0} đ",
                        FontSize = 12,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#374151")),
                        Width = barWidth + 40,
                        TextAlignment = TextAlignment.Center
                    };
                    Canvas.SetLeft(lblTien, x - 20);
                    Canvas.SetTop(lblTien, y - 25);
                    canvasBieuDoCot.Children.Add(lblTien);

                    TextBlock lblTen = new TextBlock
                    {
                        Text = labels[i],
                        FontSize = 12,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B5563")),
                        Width = barWidth + 40,
                        TextAlignment = TextAlignment.Center
                    };
                    Canvas.SetLeft(lblTen, x - 20);
                    Canvas.SetTop(lblTen, canvasHeight - 30);
                    canvasBieuDoCot.Children.Add(lblTen);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi vẽ biểu đồ 1 tháng: " + ex.Message);
            }
        }

        private double TinhPhanTram(decimal thangMoi, decimal thangCu)
        {
            if (thangCu == 0)
                return thangMoi > 0 ? 100 : 0;
            return (double)((thangMoi - thangCu) / thangCu) * 100;
        }

        private void HienThiPhanTram(Canvas canvas, double x, double y, double phanTram,
                                      double barWidth, bool isThuNhap)
        {
            string text;
            Brush color;

            if (phanTram > 0)
            {
                text = $"↑ +{phanTram:F1}%";
                color = isThuNhap
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
            }
            else if (phanTram < 0)
            {
                text = $"↓ {phanTram:F1}%";
                color = isThuNhap
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
            }
            else
            {
                text = "= 0%";
                color = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"));
            }

            Border bg = new Border
            {
                Background = color,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Child = new TextBlock
                {
                    Text = text,
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White
                }
            };

            bg.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double bgWidth = bg.DesiredSize.Width;

            Canvas.SetLeft(bg, x + (barWidth - bgWidth) / 2);
            Canvas.SetTop(bg, y);
            canvas.Children.Add(bg);
        }

        // ============================================================
        // HELPER: Lấy tổng theo loại
        // ============================================================
        private decimal LayTongTheoLoai(string loai, DateTime tuNgay, DateTime denNgay)
        {
            string query = @"
                SELECT ISNULL(SUM(gd.SoTien), 0) AS Tong
                FROM GiaoDich gd
                INNER JOIN DanhMuc dm ON gd.MaDanhMuc = dm.MaDanhMuc
                WHERE gd.MaNguoiDung = @MaNguoiDung
                  AND dm.LoaiDanhMuc = @Loai
                  AND gd.NgayGiaoDich >= @TuNgay
                  AND gd.NgayGiaoDich <= @DenNgay";

            SqlParameter[] p = {
                new SqlParameter("@MaNguoiDung", DangNhap.MaNguoiDungHienTai),
                new SqlParameter("@Loai", loai),
                new SqlParameter("@TuNgay", tuNgay),
                new SqlParameter("@DenNgay", denNgay)
            };

            DataTable dt = DatabaseHelper.GetData(query, p);
            if (dt != null && dt.Rows.Count > 0)
                return Convert.ToDecimal(dt.Rows[0]["Tong"]);
            return 0;
        }

        // ============================================================
        // BẢNG TOP 5 DANH MỤC
        // ============================================================
        private void LoadTopDanhMuc()
        {
            try
            {
                string query = @"
                    SELECT TOP 5 dm.TenDanhMuc, SUM(gd.SoTien) AS TongTien
                    FROM GiaoDich gd
                    INNER JOIN DanhMuc dm ON gd.MaDanhMuc = dm.MaDanhMuc
                    WHERE gd.MaNguoiDung = @MaNguoiDung
                      AND dm.LoaiDanhMuc = N'Chi tiêu'
                      AND gd.NgayGiaoDich >= @TuNgay
                      AND gd.NgayGiaoDich <= @DenNgay
                    GROUP BY dm.TenDanhMuc
                    ORDER BY TongTien DESC";

                SqlParameter[] p = {
                    new SqlParameter("@MaNguoiDung", DangNhap.MaNguoiDungHienTai),
                    new SqlParameter("@TuNgay", tuNgay),
                    new SqlParameter("@DenNgay", denNgay)
                };

                DataTable dt = DatabaseHelper.GetData(query, p);

                decimal tongChi = 0;
                foreach (DataRow row in dt.Rows)
                    tongChi += Convert.ToDecimal(row["TongTien"]);

                List<TopDanhMucModel> list = new List<TopDanhMucModel>();
                int hang = 1;

                foreach (DataRow row in dt.Rows)
                {
                    decimal soTien = Convert.ToDecimal(row["TongTien"]);
                    list.Add(new TopDanhMucModel
                    {
                        Hang = hang++,
                        TenDanhMuc = row["TenDanhMuc"].ToString(),
                        SoTien = soTien,
                        TyLe = tongChi > 0 ? (double)(soTien / tongChi) * 100 : 0
                    });
                }

                dgvTopDanhMuc.ItemsSource = list;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải top danh mục: " + ex.Message);
            }
        }

        // ============================================================
        // SỰ KIỆN
        // ============================================================
        private void cboLocThoiGian_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded) return;
            CapNhatKhoangThoiGian();
            LoadTatCa();
        }

        private void cboLoaiBieuDo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded) return;
            _loaiXem = (cboLoaiBieuDo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Tổng hợp";
            LoadBieuDoTron();
        }

        private void cboKieuSoSanh_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded) return;
            _kieuSoSanh = (cboKieuSoSanh.SelectedItem as ComboBoxItem)?.Content.ToString()
                          ?? "📊 So sánh 2 tháng";
            LoadBieuDoCot();
        }

        private void cboThang1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded) return;
            if (cboThang1.SelectedItem is ThangNamItem item)
            {
                _thang1 = item.Thang;
                _nam1 = item.Nam;
                LoadBieuDoCot();
            }
        }

        private void cboThang2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded) return;
            if (cboThang2.SelectedItem is ThangNamItem item)
            {
                _thang2 = item.Thang;
                _nam2 = item.Nam;
                LoadBieuDoCot();
            }
        }

        private void cboThangDuyNhat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded) return;
            if (cboThangDuyNhat.SelectedItem is ThangNamItem item)
            {
                _thangDuyNhat = item.Thang;
                _namDuyNhat = item.Nam;
                LoadBieuDoCot();
            }
        }

        private void btnApDungLoc_Click(object sender, RoutedEventArgs e)
        {
            CapNhatKhoangThoiGian();
            LoadTatCa();
        }

        private void btnNhanhT9vsT8_Click(object sender, RoutedEventArgs e)
        {
            DateTime now = DateTime.Now;
            DateTime prev = now.AddMonths(-1);

            foreach (var item in cboThang1.Items)
            {
                if (item is ThangNamItem tn && tn.Thang == now.Month && tn.Nam == now.Year)
                {
                    cboThang1.SelectedItem = item;
                    break;
                }
            }

            foreach (var item in cboThang2.Items)
            {
                if (item is ThangNamItem tn && tn.Thang == prev.Month && tn.Nam == prev.Year)
                {
                    cboThang2.SelectedItem = item;
                    break;
                }
            }
        }

        private void btnNhanh6Thang_Click(object sender, RoutedEventArgs e)
        {
            DateTime now = DateTime.Now;
            DateTime sixMonthsAgo = now.AddMonths(-6);

            foreach (var item in cboThang1.Items)
            {
                if (item is ThangNamItem tn && tn.Thang == now.Month && tn.Nam == now.Year)
                {
                    cboThang1.SelectedItem = item;
                    break;
                }
            }

            foreach (var item in cboThang2.Items)
            {
                if (item is ThangNamItem tn && tn.Thang == sixMonthsAgo.Month && tn.Nam == sixMonthsAgo.Year)
                {
                    cboThang2.SelectedItem = item;
                    break;
                }
            }
        }

        private void btnNhanhHomNay_Click(object sender, RoutedEventArgs e)
        {
            if (dpTuNgay != null) dpTuNgay.SelectedDate = DateTime.Today;
            if (dpDenNgay != null) dpDenNgay.SelectedDate = DateTime.Today;
        }

        private void btnNhanh7Ngay_Click(object sender, RoutedEventArgs e)
        {
            if (dpTuNgay != null) dpTuNgay.SelectedDate = DateTime.Today.AddDays(-7);
            if (dpDenNgay != null) dpDenNgay.SelectedDate = DateTime.Today;
        }

        private void btnNhanhThangNay_Click(object sender, RoutedEventArgs e)
        {
            DateTime now = DateTime.Now;
            if (dpTuNgay != null) dpTuNgay.SelectedDate = new DateTime(now.Year, now.Month, 1);
            if (dpDenNgay != null) dpDenNgay.SelectedDate =
                new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
        }

        private void dpTuNgay_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded) return;
            if (cboLocThoiGian.SelectedItem is ComboBoxItem item &&
                item.Content.ToString() == "📅 Tùy chọn khoảng ngày")
            {
                CapNhatKhoangThoiGian();
                LoadTatCa();
            }
        }

        private void dpDenNgay_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded) return;
            if (cboLocThoiGian.SelectedItem is ComboBoxItem item &&
                item.Content.ToString() == "📅 Tùy chọn khoảng ngày")
            {
                CapNhatKhoangThoiGian();
                LoadTatCa();
            }
        }

        private void canvasBieuDoTron_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.IsLoaded) LoadBieuDoTron();
        }

        private void canvasBieuDoCot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.IsLoaded) LoadBieuDoCot();
        }

        // ============================================================
        // ĐIỀU HƯỚNG
        // ============================================================
        private void btnXemGiaoDich_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new Uri("GiaoDich.xaml", UriKind.Relative));
        }

        private void btnPhanTichAI_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new Uri("TroLyAI.xaml", UriKind.Relative));
        }
    }
}