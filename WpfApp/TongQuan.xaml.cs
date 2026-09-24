using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;

namespace WpfApp
{
    public partial class TongQuan : Page
    {
        private string connectionString = @"Server=(LocalDB)\MSSQLLocalDB;Database=QuanLyTaiChinhAI;Trusted_Connection=True;";
        private int currentUserId = 1; // ID người dùng đăng nhập hiện tại

        public TongQuan()
        {
            InitializeComponent();
            this.Loaded += TongQuan_Loaded;
        }

        private void TongQuan_Loaded(object sender, RoutedEventArgs e)
        {
            LoadThongKeThuChi();
            LoadTinhTrangNganSach();
        }

        // 1. Tải Tổng Thu, Tổng Chi, Số Dư
        private void LoadThongKeThuChi()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            ISNULL(SUM(CASE WHEN dm.LoaiDanhMuc = N'Thu nhập' THEN gd.SoTien ELSE 0 END), 0) AS TongThu,
                            ISNULL(SUM(CASE WHEN dm.LoaiDanhMuc = N'Chi tiêu' THEN gd.SoTien ELSE 0 END), 0) AS TongChi
                        FROM GiaoDich gd
                        INNER JOIN DanhMuc dm ON gd.MaDanhMuc = dm.MaDanhMuc
                        WHERE gd.MaNguoiDung = @MaNguoiDung
                          AND MONTH(gd.NgayGiaoDich) = MONTH(GETDATE())
                          AND YEAR(gd.NgayGiaoDich) = YEAR(GETDATE());";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@MaNguoiDung", currentUserId);

                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        decimal tongThu = Convert.ToDecimal(reader["TongThu"]);
                        decimal tongChi = Convert.ToDecimal(reader["TongChi"]);
                        decimal soDu = tongThu - tongChi;

                        // Định dạng hiển thị tiền tệ VNĐ
                        txtTongThu.Text = string.Format("{0:N0} đ", tongThu);
                        txtTongChi.Text = string.Format("{0:N0} đ", tongChi);
                        txtSoDu.Text = string.Format("{0:N0} đ", soDu);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải thống kê: " + ex.Message);
            }
        }
        // 1. Sự kiện khi bấm nút "Xem Giao dịch"
        private void btnXemGiaoDich_Click(object sender, RoutedEventArgs e)
        {
            // Chuyển hướng sang trang Giao dịch (Thay "GiaoDich.xaml" đúng với tên trang của bạn)
            if (this.NavigationService != null)
            {
                this.NavigationService.Navigate(new Uri("GiaoDich.xaml", UriKind.Relative));
            }
        }

        // 2. Sự kiện khi bấm nút "Xem Ngân sách"
        private void btnXemNganSach_Click(object sender, RoutedEventArgs e)
        {
            // Chuyển hướng sang trang Ngân sách
            if (this.NavigationService != null)
            {
                this.NavigationService.Navigate(new Uri("NganSach.xaml", UriKind.Relative));
            }
        }

        // 3. Sự kiện khi bấm nút "Xem Phân tích"
        private void btnXemPhanTich_Click(object sender, RoutedEventArgs e)
        {
            // Chuyển hướng sang trang Phân tích
            if (this.NavigationService != null)
            {
                this.NavigationService.Navigate(new Uri("PhanTich.xaml", UriKind.Relative));
            }
        }
        // 2. Tải Tiến độ Ngân sách tháng này
        private void LoadTinhTrangNganSach()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
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
                          AND ns.Thang = MONTH(GETDATE()) 
                          AND ns.Nam = YEAR(GETDATE());";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@MaNguoiDung", currentUserId);

                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        decimal tongHanMuc = Convert.ToDecimal(reader["TongHanMuc"]);
                        decimal tongDaChi = Convert.ToDecimal(reader["TongDaChi"]);

                        double phanTram = tongHanMuc > 0 ? (double)(tongDaChi / tongHanMuc * 100) : 0;

                        // Cập nhật ProgressBar và TextBlock
                        pbNganSach.Value = phanTram > 100 ? 100 : phanTram;
                        txtThongTinNganSach.Text = $"Đã chi {Math.Round(phanTram, 1)}% ({tongDaChi:N0} / {tongHanMuc:N0} đ)";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải ngân sách: " + ex.Message);
            }
        }
    }
}