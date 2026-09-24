using System;
using System.Windows;
using Microsoft.Data.SqlClient;

namespace WpfApp
{
    public partial class ThemSuaNganSach : Window
    {
        private string connectionString = @"Server=(LocalDB)\MSSQLLocalDB;Database=QuanLyTaiChinhAI;Trusted_Connection=True;";
        private int _maNganSach = 0; // 0: Thêm mới | >0: Chỉnh sửa
        private int _currentUserId = 1;

        // Constructor dùng cho CẢ THÊM VÀ SỬA
        public ThemSuaNganSach(int maNganSach = 0)
        {
            InitializeComponent();
            _maNganSach = maNganSach;
            this.Loaded += ThemSuaNganSach_Loaded;
        }

        private void ThemSuaNganSach_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDanhMuc();
            InitThangNam();

            if (_maNganSach > 0)
            {
                // Chế độ SỬA
                txtTieuDe.Text = "CHỈNH SỬA NGÂN SÁCH";
                txtTieuDe.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DarkOrange);
                LoadDataChiTiet();
            }
        }

        private void InitThangNam()
        {
            for (int i = 1; i <= 12; i++) cboThang.Items.Add($"Tháng {i}");
            cboThang.SelectedIndex = DateTime.Now.Month - 1;
            txtNam.Text = DateTime.Now.Year.ToString();
        }

        private void LoadDanhMuc()
        {
            // Code load danh mục chi tiêu từ bảng DanhMuc lên cboDanhMuc
        }

        private void LoadDataChiTiet()
        {
            // Query lấy thông tin ngân sách theo _maNganSach và đổ lên cboDanhMuc, txtHanMuc...
        }

        private void btnLuu_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(txtHanMuc.Text, out decimal hanMuc) || hanMuc <= 0)
            {
                MessageBox.Show("Vui lòng nhập hạn mức hợp lệ!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int maDanhMuc = Convert.ToInt32(cboDanhMuc.SelectedValue);
            int thang = cboThang.SelectedIndex + 1;
            int nam = Convert.ToInt32(txtNam.Text);

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "";

                    if (_maNganSach == 0) // LỆNH INSERT
                    {
                        query = @"INSERT INTO NganSach (MaNguoiDung, MaDanhMuc, Thang, Nam, HanMuc) 
                                  VALUES (@MaNguoiDung, @MaDanhMuc, @Thang, @Nam, @HanMuc)";
                    }
                    else // LỆNH UPDATE
                    {
                        query = @"UPDATE NganSach 
                                  SET MaDanhMuc = @MaDanhMuc, Thang = @Thang, Nam = @Nam, HanMuc = @HanMuc 
                                  WHERE MaNganSach = @MaNganSach";
                    }

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@MaNguoiDung", _currentUserId);
                    cmd.Parameters.AddWithValue("@MaDanhMuc", maDanhMuc);
                    cmd.Parameters.AddWithValue("@Thang", thang);
                    cmd.Parameters.AddWithValue("@Nam", nam);
                    cmd.Parameters.AddWithValue("@HanMuc", hanMuc);
                    if (_maNganSach > 0) cmd.Parameters.AddWithValue("@MaNganSach", _maNganSach);

                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Lưu thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true; // Đóng cửa sổ và báo thành công
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lưu dữ liệu: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnHuy_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}