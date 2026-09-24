using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.SqlClient;
namespace WpfApp
{
    public partial class NganSach : Page
    {
        private string connectionString = @"Server=(LocalDB)\MSSQLLocalDB;Database=QuanLyTaiChinhAI;Trusted_Connection=True;";

        public NganSach()
        {
            InitializeComponent();
            this.Loaded += NganSach_Loaded;
        }

        private void NganSach_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDanhMucIntoComboBox();
            TaiDanhSachNganSach();
        }

        #region 1. MODEL DÙNG ĐỂ BINDING LÊN DATAGRID
        public class NganSachViewModel
        {
            public int MaNganSach { get; set; }
            public int MaDanhMuc { get; set; }
            public string TenDanhMuc { get; set; }
            public decimal HanMuc { get; set; }
            public decimal DaChi { get; set; }

            // Các thuộc tính tự tính toán
            public decimal ConLai => HanMuc - DaChi;

            public double PhanTramDung => HanMuc > 0 ? (double)(DaChi / HanMuc * 100) : 0;

            public string PhanTramHienThi => $"{Math.Round(PhanTramDung, 1)}%";

            // Đổi màu thanh tiến độ: Xanh (<80%), Cam (80%-100%), Đỏ (>100%)
            public SolidColorBrush MauTienDo
            {
                get
                {
                    if (PhanTramDung >= 100)
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")); // Đỏ (Vượt hạn mức)
                    if (PhanTramDung >= 80)
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")); // Cam (Cảnh báo 80%)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));     // Xanh (An toàn)
                }
            }
        }
        #endregion

        #region 2. TẢI DỮ LIỆU TỪ DATABASE
        private void LoadDanhMucIntoComboBox()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    // Lấy các danh mục Chi tiêu
                    string query = "SELECT MaDanhMuc, TenDanhMuc FROM DanhMuc";
                    SqlCommand cmd = new SqlCommand(query, conn);
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
            List<NganSachViewModel> list = new List<NganSachViewModel>();

            // Lấy tháng và năm hiện tại (Hoặc dựa theo cboLocThang)
            int thang = DateTime.Now.Month;
            int nam = DateTime.Now.Year;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // SQL Query: Lấy hạn mức từ NganSach + Tự động SUM(SoTien) chi tiêu từ bảng GiaoDich trong cùng Tháng/Năm
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
                        WHERE ns.Thang = @Thang AND ns.Nam = @Nam
                        GROUP BY ns.MaNganSach, ns.MaDanhMuc, dm.TenDanhMuc, ns.HanMuc";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Thang", thang);
                    cmd.Parameters.AddWithValue("@Nam", nam);

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

                // Đổ dữ liệu vào DataGrid
                dgvNganSach.ItemsSource = list;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối CSDL: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region 3. SỰ KIỆN NÚT BẤM (CÁC HÀNH ĐỘNG)

        // Nút Thêm/Thiết lập Ngân sách mới
        // Khi bấm nút THÊM
        private void btnThemNganSach_Click(object sender, RoutedEventArgs e)
        {
            ThemSuaNganSach popup = new ThemSuaNganSach(0); // Truyền 0 để Thêm
            if (popup.ShowDialog() == true)
            {
                TaiDanhSachNganSach(); // Refresh lại DataGrid sau khi Thêm thành công
            }
        }

        // Khi bấm nút SỬA
        private void btnSuaNganSach_Click(object sender, RoutedEventArgs e)
        {
            if (dgvNganSach.SelectedItem is NganSachViewModel itemChon)
            {
                ThemSuaNganSach popup = new ThemSuaNganSach(itemChon.MaNganSach); // Truyền ID để Sửa
                if (popup.ShowDialog() == true)
                {
                    TaiDanhSachNganSach(); // Refresh lại DataGrid sau khi Sửa thành công
                }
            }
        }

        // Nút Xóa ngân sách
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
                        using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            conn.Open();
                            string query = "DELETE FROM NganSach WHERE MaNganSach = @MaNganSach";
                            SqlCommand cmd = new SqlCommand(query, conn);
                            cmd.Parameters.AddWithValue("@MaNganSach", itemChon.MaNganSach);
                            cmd.ExecuteNonQuery();
                        }

                        MessageBox.Show("Đã xóa thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        TaiDanhSachNganSach(); // Tải lại danh sách
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

        // Nút Xem giao dịch phát sinh của danh mục đó
        private void btnXemGiaoDich_Click(object sender, RoutedEventArgs e)
        {
            if (dgvNganSach.SelectedItem is NganSachViewModel itemChon)
            {
                MessageBox.Show($"Xem danh sách các hóa đơn chi tiêu thuộc danh mục '{itemChon.TenDanhMuc}' trong tháng.",
                                "Xem giao dịch", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một danh mục để xem chi tiết giao dịch!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        #endregion
    }
}