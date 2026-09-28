using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Security.Cryptography;
using System.Text;
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
            var (rememberUser, rememberPass) = DocTaiKhoanGhiNho();

            if (!string.IsNullOrEmpty(rememberUser) && !string.IsNullOrEmpty(rememberPass))
            {
                // Điền sẵn thông tin vào form
                txtTenDangNhap.Text = rememberUser;
                txtMatKhau.Password = rememberPass;
                chkGhiNho.IsChecked = true;

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
            var (taiKhoan, matKhau) = DocTaiKhoanGhiNho();

            if (string.IsNullOrEmpty(taiKhoan) || string.IsNullOrEmpty(matKhau))
            {
                MessageBox.Show("Không tìm thấy thông tin đăng nhập ghi nhớ!",
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);

                // Trở về màn hình đăng nhập thường
                spDangNhapDayDu.Visibility = Visibility.Visible;
                spXinChao.Visibility = Visibility.Collapsed;
                return;
            }

            // Đăng nhập tự động ngay lập tức bằng dữ liệu đã ghi nhớ
            DangNhapThuc(taiKhoan, matKhau, true);
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

                // Lưu/xóa ghi nhớ theo checkbox (Lưu cả tên + mật khẩu đã mã hóa)
                if (ghiNho)
                    LuuTaiKhoanGhiNho(dt.Rows[0]["TenDangNhap"].ToString(), matKhau);
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

        // ============ LƯU/ĐỌC/XÓA GHI NHỚ MÃ HÓA (DPAPI) ============
        private void LuuTaiKhoanGhiNho(string tenDangNhap, string matKhau)
        {
            try
            {
                string folder = Path.GetDirectoryName(RememberFile);
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string rawData = $"{tenDangNhap}|{matKhau}";
                string encryptedData = EncryptDPAPI(rawData);

                File.WriteAllText(RememberFile, encryptedData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi lưu ghi nhớ: {ex.Message}");
            }
        }

        private (string username, string password) DocTaiKhoanGhiNho()
        {
            try
            {
                if (File.Exists(RememberFile))
                {
                    string encryptedData = File.ReadAllText(RememberFile).Trim();
                    string decryptedData = DecryptDPAPI(encryptedData);

                    if (!string.IsNullOrEmpty(decryptedData) && decryptedData.Contains("|"))
                    {
                        string[] parts = decryptedData.Split('|');
                        return (parts[0], parts[1]);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi đọc ghi nhớ: {ex.Message}");
            }
            return ("", "");
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

        // Helper mã hóa DPAPI an toàn (dựa theo tài khoản Windows)
        private string EncryptDPAPI(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return "";
            byte[] data = Encoding.UTF8.GetBytes(plainText);
            byte[] encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encrypted);
        }

        private string DecryptDPAPI(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText)) return "";
            try
            {
                byte[] data = Convert.FromBase64String(encryptedText);
                byte[] decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decrypted);
            }
            catch
            {
                return "";
            }
        }

        // ============ CÁC NÚT KHÁC ============
        private void btnDangKy_Click(object sender, RoutedEventArgs e)
        {
            DangKy dangKyWin = new DangKy();
            dangKyWin.Show();
            this.Close();
        }

        private void btnQuenMatKhau_Click(object sender, RoutedEventArgs e)
        {
            DoiMatKhau quenMatKhau = new DoiMatKhau();
            quenMatKhau.Show();
            this.Close();
        }
    }
}