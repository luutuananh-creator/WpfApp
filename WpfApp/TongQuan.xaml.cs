using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfApp
{
    public partial class TongQuan : Page
    {
        private DateTime tuNgay;
        private DateTime denNgay;

        public TongQuan()
        {
            InitializeComponent();
            this.Loaded += TongQuan_Loaded;
        }

        private void TongQuan_Loaded(object sender, RoutedEventArgs e)
        {
            CapNhatKhoangThoiGian();
            TaiThongTinTongQuan();
        }

        // 1. Tính toán mốc TuNgay - DenNgay dựa trên ComboBox
        private void CapNhatKhoangThoiGian()
        {
            if (cboThoiGian == null || cboThoiGian.SelectedItem == null) return;

            string selected = ((ComboBoxItem)cboThoiGian.SelectedItem).Content.ToString();
            DateTime now = DateTime.Now;

            switch (selected)
            {
                case "Tháng trước":
                    DateTime lastMonth = now.AddMonths(-1);
                    tuNgay = new DateTime(lastMonth.Year, lastMonth.Month, 1);
                    denNgay = new DateTime(lastMonth.Year, lastMonth.Month, DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month));
                    break;

                case "3 tháng gần đây":
                    tuNgay = now.AddMonths(-3).Date;
                    denNgay = now.Date;
                    break;

                case "Năm nay":
                    tuNgay = new DateTime(now.Year, 1, 1);
                    denNgay = new DateTime(now.Year, 12, 31);
                    break;

                case "Tháng này":
                default:
                    tuNgay = new DateTime(now.Year, now.Month, 1);
                    denNgay = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
                    break;
            }
        }

        private void cboThoiGian_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Tránh lỗi NullReferenceException khi XAML đang dựng giao diện
            if (!this.IsLoaded) return;

            CapNhatKhoangThoiGian();
            TaiThongTinTongQuan();
        }

        private void TaiThongTinTongQuan()
        {
            LoadThongKeThuChi();
            LoadTinhTrangNganSach();
            VeBieuDoChiTieu();
        }

        // 2. Thống kê Tổng thu, Tổng chi, Số dư theo khoảng thời gian
        private void LoadThongKeThuChi()
        {
            try
            {
                int maNguoiDung = DangNhap.MaNguoiDungHienTai > 0 ? DangNhap.MaNguoiDungHienTai : 1;

                string query = @"
                    SELECT 
                        ISNULL(SUM(CASE WHEN dm.LoaiDanhMuc = N'Thu nhập' THEN gd.SoTien ELSE 0 END), 0) AS TongThu,
                        ISNULL(SUM(CASE WHEN dm.LoaiDanhMuc = N'Chi tiêu' THEN gd.SoTien ELSE 0 END), 0) AS TongChi
                    FROM GiaoDich gd
                    INNER JOIN DanhMuc dm ON gd.MaDanhMuc = dm.MaDanhMuc
                    WHERE gd.MaNguoiDung = @MaNguoiDung
                      AND gd.NgayGiaoDich >= @TuNgay 
                      AND gd.NgayGiaoDich <= @DenNgay";

                SqlParameter[] p = new SqlParameter[]
                {
                    new SqlParameter("@MaNguoiDung", maNguoiDung),
                    new SqlParameter("@TuNgay", tuNgay),
                    new SqlParameter("@DenNgay", denNgay)
                };

                DataTable dt = DatabaseHelper.GetData(query, p);
                if (dt != null && dt.Rows.Count > 0)
                {
                    decimal tongThu = Convert.ToDecimal(dt.Rows[0]["TongThu"]);
                    decimal tongChi = Convert.ToDecimal(dt.Rows[0]["TongChi"]);
                    decimal soDu = tongThu - tongChi;

                    if (txtTongThu != null) txtTongThu.Text = string.Format("{0:N0} đ", tongThu);
                    if (txtTongChi != null) txtTongChi.Text = string.Format("{0:N0} đ", tongChi);
                    if (txtSoDu != null) txtSoDu.Text = string.Format("{0:N0} đ", soDu);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải thống kê: " + ex.Message, "Lỗi Database");
            }
        }

        // 3. Tiến độ Ngân sách
        private void LoadTinhTrangNganSach()
        {
            try
            {
                int maNguoiDung = DangNhap.MaNguoiDungHienTai > 0 ? DangNhap.MaNguoiDungHienTai : 1;

                string query = @"
                    SELECT 
                        ISNULL(SUM(ns.HanMuc), 0) AS TongHanMuc,
                        ISNULL(SUM(gd.SoTien), 0) AS TongDaChi
                    FROM NganSach ns
                    LEFT JOIN GiaoDich gd ON ns.MaDanhMuc = gd.MaDanhMuc 
                                         AND ns.MaNguoiDung = gd.MaNguoiDung
                                         AND MONTH(gd.NgayGiaoDich) = ns.Thang 
                                         AND YEAR(gd.NgayGiaoDich) = ns.Nam
                    WHERE ns.MaNguoiDung = @MaNguoiDung 
                      AND ns.Thang = @Thang 
                      AND ns.Nam = @Nam";

                SqlParameter[] p = new SqlParameter[]
                {
                    new SqlParameter("@MaNguoiDung", maNguoiDung),
                    new SqlParameter("@Thang", tuNgay.Month),
                    new SqlParameter("@Nam", tuNgay.Year)
                };

                DataTable dt = DatabaseHelper.GetData(query, p);
                if (dt != null && dt.Rows.Count > 0)
                {
                    decimal tongHanMuc = Convert.ToDecimal(dt.Rows[0]["TongHanMuc"]);
                    decimal tongDaChi = Convert.ToDecimal(dt.Rows[0]["TongDaChi"]);

                    double phanTram = tongHanMuc > 0 ? (double)(tongDaChi / tongHanMuc * 100) : 0;

                    if (pbNganSach != null) pbNganSach.Value = phanTram > 100 ? 100 : phanTram;
                    if (txtThongTinNganSach != null)
                        txtThongTinNganSach.Text = $"Đã chi {Math.Round(phanTram, 1)}% ({tongDaChi:N0} / {tongHanMuc:N0} đ)";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải ngân sách: " + ex.Message, "Lỗi Database");
            }
        }

        // 4. Vẽ Biểu đồ Cột Chi tiêu
        private void VeBieuDoChiTieu()
        {
            if (canvasBieuDo == null) return;
            canvasBieuDo.Children.Clear();

            try
            {
                int maNguoiDung = DangNhap.MaNguoiDungHienTai > 0 ? DangNhap.MaNguoiDungHienTai : 1;

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

                SqlParameter[] p = new SqlParameter[]
                {
                    new SqlParameter("@MaNguoiDung", maNguoiDung),
                    new SqlParameter("@TuNgay", tuNgay),
                    new SqlParameter("@DenNgay", denNgay)
                };

                DataTable dt = DatabaseHelper.GetData(query, p);

                if (dt == null || dt.Rows.Count == 0)
                {
                    TextBlock txtEmpty = new TextBlock
                    {
                        Text = "Chưa có dữ liệu chi tiêu trong khoảng thời gian này",
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF")),
                        FontSize = 13
                    };
                    Canvas.SetLeft(txtEmpty, 20);
                    Canvas.SetTop(txtEmpty, 50);
                    canvasBieuDo.Children.Add(txtEmpty);
                    return;
                }

                decimal maxTien = 0;
                foreach (DataRow dr in dt.Rows)
                {
                    decimal val = Convert.ToDecimal(dr["TongTien"]);
                    if (val > maxTien) maxTien = val;
                }

                double canvasWidth = canvasBieuDo.ActualWidth > 0 ? canvasBieuDo.ActualWidth : 500;
                double canvasHeight = canvasBieuDo.ActualHeight > 0 ? canvasBieuDo.ActualHeight : 150;

                int count = dt.Rows.Count;
                double barWidth = 40;
                double gap = (canvasWidth - (count * barWidth)) / (count + 1);
                double maxBarHeight = canvasHeight - 50;

                Brush[] columnColors = new Brush[]
                {
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")),
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B5CF6"))
                };

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    string tenDM = dt.Rows[i]["TenDanhMuc"].ToString();
                    decimal tongTien = Convert.ToDecimal(dt.Rows[i]["TongTien"]);

                    double barHeight = maxTien > 0 ? (double)(tongTien / maxTien) * maxBarHeight : 0;
                    if (barHeight < 5) barHeight = 5;

                    double x = gap + i * (barWidth + gap);
                    double y = canvasHeight - barHeight - 25;

                    Rectangle bar = new Rectangle
                    {
                        Width = barWidth,
                        Height = barHeight,
                        Fill = columnColors[i % columnColors.Length],
                        RadiusX = 4,
                        RadiusY = 4
                    };
                    Canvas.SetLeft(bar, x);
                    Canvas.SetTop(bar, y);
                    canvasBieuDo.Children.Add(bar);

                    TextBlock lblTen = new TextBlock
                    {
                        Text = tenDM,
                        FontSize = 11,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B5563")),
                        Width = barWidth + 20,
                        TextAlignment = TextAlignment.Center
                    };
                    Canvas.SetLeft(lblTen, x - 10);
                    Canvas.SetTop(lblTen, canvasHeight - 20);
                    canvasBieuDo.Children.Add(lblTen);

                    TextBlock lblTien = new TextBlock
                    {
                        Text = string.Format("{0:N0}", tongTien),
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#374151")),
                        Width = barWidth + 30,
                        TextAlignment = TextAlignment.Center
                    };
                    Canvas.SetLeft(lblTien, x - 15);
                    Canvas.SetTop(lblTien, y - 18);
                    canvasBieuDo.Children.Add(lblTien);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi vẽ biểu đồ: " + ex.Message);
            }
        }

        private void canvasBieuDo_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.IsLoaded) VeBieuDoChiTieu();
        }

        // 5. Chuyển hướng các trang
        private void btnXemGiaoDich_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new Uri("GiaoDich.xaml", UriKind.Relative));
        }

        private void btnXemNganSach_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new Uri("NganSach.xaml", UriKind.Relative));
        }

        private void btnXemPhanTich_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new Uri("PhanTich.xaml", UriKind.Relative));
        }
    }
}