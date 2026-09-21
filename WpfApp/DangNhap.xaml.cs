using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using WpfApp.Helpers;

namespace WpfApp
{
    public partial class DangNhap : Window
    {
        // Đường dẫn file lưu tài khoản ghi nhớ: %APPDATA%\QuanLyTaiChinh\remember.txt
        private static readonly string RememberFile =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "QuanLyTaiChinh", "remember.txt");

        public static int MaNguoiDungHienTai { get; private set; } = 0;
        public static string TenNguoiDungHienTai { get; private set; } = "";

        public DangNhap()
        {
            InitializeComponent();
            Loaded += DangNhap_Loaded;
        }

        // ============ LOAD: Kiểm tra ghi nhớ ============
        private void DangNhap_Loaded(object sender, RoutedEventArgs e)
        {
            string rememberUser = DocTaiKhoanGhiNho();

            if (!string.IsNullOrEmpty(rememberUser))
            {
                // Có ghi nhớ → hiện màn hình xin chào
                spDangNhapDayDu.Visibility = Visibility.Collapsed;
                spXinChao.Visibility = Visibility.Visible;
                txtLoiChao.Text = $"Xin chào, {rememberUser}";
            }
            else
            {
                // Chưa ghi nhớ → hiện form đầy đủ
                spDangNhapDayDu.Visibility = Visibility.Visible;
                spXinChao.Visibility = Visibility.Collapsed;
                txtTenDangNhap.Focus();
            }
        }

        // ============ ĐĂNG NHẬP ============
        private void btnDangNhap_Click(object sender, RoutedEventArgs e)
        {
            string taiKhoan = txtTenDangNhap.Text.Trim();
            string matKhau = txtMatKhau.Password;
            bool ghiNho = chkGhiNho.IsChecked == true;

            DangNhapThuc(taiKhoan, matKhau, ghiNho);
        }

        // ============ ĐĂNG NHẬP NHANH ============
        private void btnDangNhapNhanh_Click(object sender, RoutedEventArgs e)
        {
            string taiKhoan = DocTaiKhoanGhiNho();

            if (string.IsNullOrEmpty(taiKhoan))
            {
                MessageBox.Show("Không tìm thấy tài khoản ghi nhớ!",
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Chuyển về form đăng nhập, điền sẵn tên
            spDangNhapDayDu.Visibility = Visibility.Visible;
            spXinChao.Visibility = Visibility.Collapsed;

            txtTenDangNhap.Text = taiKhoan;
            txtTenDangNhap.IsReadOnly = true;
            chkGhiNho.IsChecked = true;

            txtMatKhau.Focus();

            MessageBox.Show($"Vui lòng nhập mật khẩu cho tài khoản '{taiKhoan}'!",
                            "Đăng nhập nhanh",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ============ QUAY LẠI (XÓA GHI NHỚ) ============
        private void btnQuayLai_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Bạn muốn quay lại màn hình đăng nhập?\nThông tin ghi nhớ sẽ bị xóa.",
                "Xác nhận",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            // Xóa file ghi nhớ
            XoaTaiKhoanGhiNho();

            // Reset form
            spDangNhapDayDu.Visibility = Visibility.Visible;
            spXinChao.Visibility = Visibility.Collapsed;

            txtTenDangNhap.Text = "";
            txtMatKhau.Password = "";
            txtTenDangNhap.IsReadOnly = false;
            chkGhiNho.IsChecked = false;
            txtTenDangNhap.Focus();
        }

        // ============ HÀM XỬ LÝ ĐĂNG NHẬP CHÍNH ============
        private void DangNhapThuc(string taiKhoan, string matKhau, bool ghiNho)
        {
            if (string.IsNullOrEmpty(taiKhoan) || string.IsNullOrEmpty(matKhau))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ tài khoản và mật khẩu!",
                                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string query = @"SELECT MaNguoiDung, TenDangNhap, Email, MatKhau, HoTen 
                                 FROM NguoiDung 
                                 WHERE TenDangNhap = @Ten OR Email = @Ten";

                var parameters = new[] { new SqlParameter("@Ten", taiKhoan) };
                DataTable dt = DatabaseHelper.GetData(query, parameters);

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Tài khoản hoặc mật khẩu không chính xác!",
                                    "Đăng nhập thất bại",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string matKhauDB = dt.Rows[0]["MatKhau"].ToString();

                // Verify — tự nhận diện plain text hay BCrypt hash
                bool hopLe;
                if (matKhauDB.StartsWith("$2"))
                    hopLe = SecurityHelpers.VerifyPassword(matKhau, matKhauDB);
                else
                    hopLe = (matKhau == matKhauDB);

                if (!hopLe)
                {
                    MessageBox.Show("Tài khoản hoặc mật khẩu không chính xác!",
                                    "Đăng nhập thất bại",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Lưu session
                MaNguoiDungHienTai = Convert.ToInt32(dt.Rows[0]["MaNguoiDung"]);
                TenNguoiDungHienTai = dt.Rows[0]["HoTen"].ToString();

                Session.MaNguoiDung = MaNguoiDungHienTai;
                Session.HoTen = TenNguoiDungHienTai;
                Session.TenDangNhap = dt.Rows[0]["TenDangNhap"].ToString();
                Session.Email = dt.Rows[0]["Email"].ToString();

                // Lưu/xóa ghi nhớ theo checkbox
                if (ghiNho)
                    LuuTaiKhoanGhiNho(dt.Rows[0]["TenDangNhap"].ToString());
                else
                    XoaTaiKhoanGhiNho();

                MessageBox.Show($"Chào mừng {TenNguoiDungHienTai} quay trở lại!",
                                "Đăng nhập thành công",
                                MessageBoxButton.OK, MessageBoxImage.Information);

                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối CSDL: " + ex.Message,
                                "Lỗi Database",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ============ LƯU/ĐỌC/XÓA GHI NHỚ ============
        private void LuuTaiKhoanGhiNho(string tenDangNhap)
        {
            try
            {
                string folder = Path.GetDirectoryName(RememberFile);
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                File.WriteAllText(RememberFile, tenDangNhap);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi lưu ghi nhớ: {ex.Message}");
            }
        }

        private string DocTaiKhoanGhiNho()
        {
            try
            {
                if (File.Exists(RememberFile))
                    return File.ReadAllText(RememberFile).Trim();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi đọc ghi nhớ: {ex.Message}");
            }
            return "";
        }

        private void XoaTaiKhoanGhiNho()
        {
            try
            {
                if (File.Exists(RememberFile))
                    File.Delete(RememberFile);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi xóa ghi nhớ: {ex.Message}");
            }
        }

        // ============ CÁC NÚT KHÁC ============
        private void btnDangKy_Click(object sender, RoutedEventArgs e)
        {
            DangKy dangKyWin = new DangKy();
            dangKyWin.Show();
            this.Close();
        }

        // ✅ SỬA: Mở trang DoiMatKhau (đã làm chức năng Quên mật khẩu)
        private void btnQuenMatKhau_Click(object sender, RoutedEventArgs e)
        {
            DoiMatKhau quenMatKhau = new DoiMatKhau();
            quenMatKhau.Show();
            this.Close();
        }
    }
}