using System;
using System.Data;
using System.Windows;

namespace WpfApp // Lưu ý: Đổi lại namespace nếu project của bạn tên khác
{
    public partial class DangNhap : Window
    {
        // Biến toàn cục để lưu ID người dùng đang đăng nhập, các trang khác (Thêm giao dịch, Thêm danh mục) có thể gọi biến này.
        public static int MaNguoiDungHienTai { get; private set; } = 0;
        public static string TenNguoiDungHienTai { get; private set; } = "";

        public DangNhap()
        {
            InitializeComponent();
        }

        private void btnDangNhap_Click(object sender, RoutedEventArgs e)
        {
            // Sử dụng đúng tên control từ XAML
            string taiKhoan = txtTenDangNhap.Text.Trim();
            string matKhau = txtMatKhau.Password;

            // 1. Kiểm tra không được để trống
            if (string.IsNullOrEmpty(taiKhoan) || string.IsNullOrEmpty(matKhau))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ tài khoản và mật khẩu!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 2. Truy vấn kiểm tra thông tin trong CSDL
                // Cập nhật: Cho phép đăng nhập bằng cả Tên đăng nhập HOẶC Email (khớp với nhãn trên XAML)
                string query = $"SELECT MaNguoiDung, HoTen FROM NguoiDung WHERE (TenDangNhap = '{taiKhoan}' OR Email = '{taiKhoan}') AND MatKhau = '{matKhau}'";
                DataTable dt = DatabaseHelper.GetData(query);

                // 3. Xử lý kết quả
                if (dt.Rows.Count > 0)
                {
                    // Đăng nhập thành công, lưu lại ID và Tên để dùng cho toàn hệ thống
                    MaNguoiDungHienTai = Convert.ToInt32(dt.Rows[0]["MaNguoiDung"]);
                    TenNguoiDungHienTai = dt.Rows[0]["HoTen"].ToString();

                    // Xử lý Checkbox Ghi nhớ (Tạm thời nhận biết trạng thái, bạn có thể bổ sung code lưu vào File/Registry sau)
                    if (chkGhiNho.IsChecked == true)
                    {
                        // TODO: Viết code lưu trạng thái đăng nhập
                    }

                    MessageBox.Show($"Chào mừng {TenNguoiDungHienTai} quay trở lại!", "Đăng nhập thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Mở MainWindow và đóng trang đăng nhập
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Tài khoản hoặc mật khẩu không chính xác!", "Đăng nhập thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối CSDL: " + ex.Message, "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDangKy_Click(object sender, RoutedEventArgs e)
        {
            DangKy dangKyWin = new DangKy();
            dangKyWin.Show();
            this.Close();
        }

        private void btnQuenMatKhau_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Vui lòng kiểm tra email của bạn để lấy lại mật khẩu, hoặc liên hệ Quản trị viên.", "Quên mật khẩu", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}