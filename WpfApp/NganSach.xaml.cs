using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfApp
{
    public partial class NganSach : Page
    {
        private string connectionString = @"Server=(LocalDB)\MSSQLLocalDB;Database=QuanLyTaiChinhAI;Trusted_Connection=True;";
        private bool isLoaded = false; // Cờ tránh gọi sự kiện khi UI đang khởi tạo

        public NganSach()
        {
            InitializeComponent();
            this.Loaded += NganSach_Loaded;
        }

        private void NganSach_Loaded(object sender, RoutedEventArgs e)
        {
            InitThangNam();
            LoadDanhMucIntoComboBox();
            isLoaded = true; // Đã load xong UI
            TaiDanhSachNganSach();
        }

        private void InitThangNam()
        {
            cboLocThang.Items.Clear();
            for (int i = 1; i <= 12; i++)
            {
                cboLocThang.Items.Add($"Tháng {i}");
            }
            cboLocThang.SelectedIndex = DateTime.Now.Month - 1;
            txtLocNam.Text = DateTime.Now.Year.ToString();
        }

        #region 1. MODEL BINDING
        public class NganSachViewModel
        {
            public int MaNganSach { get; set; }
            public int MaDanhMuc { get; set; }
            public string TenDanhMuc { get; set; }
            public decimal HanMuc { get; set; }
            public decimal DaChi { get; set; }

            public decimal ConLai => HanMuc - DaChi;
            public double PhanTramDung => HanMuc > 0 ? (double)(DaChi / HanMuc * 100) : 0;
            public string PhanTramHienThi => $"{Math.Round(PhanTramDung, 1)}%";

            public SolidColorBrush MauTienDo
            {
                get
                {
                    if (PhanTramDung >= 100)
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                    if (PhanTramDung >= 80)
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                }
            }
        }
        #endregion

        // 2. TẢI DỮ LIỆU VÀ LỌC
        private void LoadDanhMucIntoComboBox()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT MaDanhMuc, TenDanhMuc FROM DanhMuc WHERE MaNguoiDung = @MaNguoiDung";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@MaNguoiDung", DangNhap.MaNguoiDungHienTai);

                    SqlDataReader reader = cmd.ExecuteReader();

                    cboLocDanhMuc.Items.Clear();
                    cboLocDanhMuc.Items.Add(new ComboBoxItem { Content = "Tất cả danh mục", Tag = 0, IsSelected = true });

                    while (reader.Read())
                    {
                        cboLocDanhMuc.Items.Add(new ComboBoxItem
                        {
                            Content = reader["TenDanhMuc"].ToString(),
                            Tag = Convert.ToInt32(reader["MaDanhMuc"])
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải danh mục: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void TaiDanhSachNganSach()
        {
            if (!isLoaded) return;

            List<NganSachViewModel> list = new List<NganSachViewModel>();

            // Lấy giá trị Tháng/Năm từ Bộ lọc
            int thang = cboLocThang.SelectedIndex + 1;
            int.TryParse(txtLocNam.Text, out int nam);
            if (nam <= 0) nam = DateTime.Now.Year;

            // Lấy ID Danh Mục từ Bộ lọc
            int maDanhMucLoc = 0;
            if (cboLocDanhMuc.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                maDanhMucLoc = Convert.ToInt32(selectedItem.Tag);
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Chuỗi SQL truy vấn động
                    string query = @"
                SELECT 
                    ns.MaNganSach,
                    ns.MaDanhMuc,
                    dm.TenDanhMuc,
                    ns.HanMuc,
                    ISNULL(SUM(gd.SoTien), 0) AS DaChi
                FROM NganSach ns
                INNER JOIN DanhMuc dm ON ns.MaDanhMuc = dm.MaDanhMuc
                LEFT JOIN GiaoDich gd ON gd.MaDanhMuc = ns.MaDanhMuc 
                                      AND MONTH(gd.NgayGiaoDich) = ns.Thang 
                                      AND YEAR(gd.NgayGiaoDich) = ns.Nam
                WHERE ns.Thang = @Thang 
                  AND ns.Nam = @Nam 
                  AND ns.MaNguoiDung = @MaNguoiDung";

                    // Thêm điều kiện nếu chọn danh mục cụ thể
                    if (maDanhMucLoc > 0)
                    {
                        query += " AND ns.MaDanhMuc = @MaDanhMucLoc";
                    }

                    query += " GROUP BY ns.MaNganSach, ns.MaDanhMuc, dm.TenDanhMuc, ns.HanMuc";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Thang", thang);
                    cmd.Parameters.AddWithValue("@Nam", nam);
                    cmd.Parameters.AddWithValue("@MaNguoiDung", DangNhap.MaNguoiDungHienTai);
                    if (maDanhMucLoc > 0)
                    {
                        cmd.Parameters.AddWithValue("@MaDanhMucLoc", maDanhMucLoc);
                    }

                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        list.Add(new NganSachViewModel
                        {
                            MaNganSach = Convert.ToInt32(reader["MaNganSach"]),
                            MaDanhMuc = Convert.ToInt32(reader["MaDanhMuc"]),
                            TenDanhMuc = reader["TenDanhMuc"].ToString(),
                            HanMuc = Convert.ToDecimal(reader["HanMuc"]),
                            DaChi = Convert.ToDecimal(reader["DaChi"])
                        });
                    }
                }

                dgvNganSach.ItemsSource = list;

                
                
                if (isLoaded)
                {
                    List<string> vuotHanMuc = new List<string>();
                    List<string> sapHet = new List<string>();

                    foreach (var item in list)
                    {
                        if (item.PhanTramDung >= 100)
                        {
                            vuotHanMuc.Add($"❌ {item.TenDanhMuc}: Âm {item.DaChi - item.HanMuc:N0} đ");
                        }
                        else if (item.PhanTramDung >= 80)
                        {
                            sapHet.Add($"⚠️ {item.TenDanhMuc}: Đã dùng {item.PhanTramHienThi} (Còn {item.ConLai:N0} đ)");
                        }
                    }

                    if (vuotHanMuc.Count > 0 || sapHet.Count > 0)
                    {
                        string thongBao = "TÌNH TRẠNG NGÂN SÁCH THÁNG NÀY:\n\n";

                        if (vuotHanMuc.Count > 0)
                            thongBao += "🚨 ĐÃ VƯỢT NGÂN SÁCH:\n" + string.Join("\n", vuotHanMuc) + "\n\n";

                        if (sapHet.Count > 0)
                            thongBao += "⏳ SẮP HẾT NGÂN SÁCH:\n" + string.Join("\n", sapHet);

                        MessageBox.Show(thongBao, "Cảnh Báo Chi Tiêu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối CSDL: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        #region 3. SỰ KIỆN THAY ĐỔI BỘ LỌC
        private void cboFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TaiDanhSachNganSach();
        }

        private void txtLocNam_TextChanged(object sender, TextChangedEventArgs e)
        {
            TaiDanhSachNganSach();
        }
        #endregion

        // 4. SỰ KIỆN NÚT BẤM (THÊM, SỬA, XÓA)
        private void btnThemNganSach_Click(object sender, RoutedEventArgs e)
        {
            ThemSuaNganSach popup = new ThemSuaNganSach(0);
            if (popup.ShowDialog() == true)
            {
                TaiDanhSachNganSach();
            }
        }

        private void btnSuaNganSach_Click(object sender, RoutedEventArgs e)
        {
            if (dgvNganSach.SelectedItem is NganSachViewModel itemChon)
            {
                ThemSuaNganSach popup = new ThemSuaNganSach(itemChon.MaNganSach);
                if (popup.ShowDialog() == true)
                {
                    TaiDanhSachNganSach();
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một dòng ngân sách để sửa!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnXoaNganSach_Click(object sender, RoutedEventArgs e)
        {
            if (dgvNganSach.SelectedItem is NganSachViewModel itemChon)
            {
                var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa ngân sách của danh mục '{itemChon.TenDanhMuc}' không?",
                                             "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Dùng DatabaseHelper thay vì SqlConnection
                        string query = $"DELETE FROM NganSach WHERE MaNganSach = {itemChon.MaNganSach}";
                        DatabaseHelper.ExecuteQuery(query);

                        MessageBox.Show("Đã xóa thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        TaiDanhSachNganSach();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi khi xóa: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một dòng ngân sách để xóa!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

}