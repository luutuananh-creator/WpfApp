using System.Data.SqlClient;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using WpfApp.Helpers; // Gọi EmailService và SecurityHelpers

namespace WpfApp
{
    public partial class DangKy : Window
    {
        private string maOTP = "";

        public DangKy()
        {
            InitializeComponent();
        }

        private async void btnTaoTaiKhoan_Click(object sender, RoutedEventArgs e)
        {
            string taiKhoan = txtTaiKhoan.Text.Trim();
            string email = txtEmail.Text.Trim();
            string matKhau = txtMatKhau.Password;
            string xacNhanMatKhau = txtXacNhanMatKhau.Password;

            // 1. Kiểm tra trống
            if (string.IsNullOrEmpty(taiKhoan) || string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(matKhau) || string.IsNullOrEmpty(xacNhanMatKhau))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin!", "Thông báo",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Kiểm tra định dạng Email
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                MessageBox.Show("Email không đúng định dạng!", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 3. Kiểm tra mật khẩu khớp
            if (matKhau != xacNhanMatKhau)
            {
                MessageBox.Show("Mật khẩu xác nhận không khớp!", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 3.5. Kiểm tra tài khoản/email đã tồn tại chưa
            if (KiemTraTonTai(taiKhoan, email))
            {
                return;
            }

            btnTaoTaiKhoan.IsEnabled = false;
            btnTaoTaiKhoan.Content = "Đang gửi email...";

            // 4. Sinh mã OTP và gửi Mail
            maOTP = SecurityHelpers.GenerateOTP();
            EmailService emailService = new EmailService();
            bool daGuiMail = await emailService.SendOtpEmailAsync(email, maOTP, "Register");

            btnTaoTaiKhoan.IsEnabled = true;
            btnTaoTaiKhoan.Content = "Gửi Mã Xác Thực Email";

            if (!daGuiMail)
            {
                MessageBox.Show("Không thể gửi email xác nhận. Vui lòng kiểm tra lại kết nối hoặc địa chỉ Email!",
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 5. Mở hộp thoại nhập OTP xác nhận
            string otpNhap = Microsoft.VisualBasic.Interaction.InputBox(
                "Mã xác thực 6 chữ số đã được gửi tới email của bạn. Vui lòng nhập mã để hoàn tất đăng ký:",
                "Xác Thực Email OTP", "");

            if (otpNhap == maOTP)
            {
                // Mã hóa mật khẩu trước khi lưu DB
                string matKhauHash = SecurityHelpers.HashPassword(matKhau);

                if (LuuTaiKhoanVaoDB(taiKhoan, email, matKhauHash))
                {
                    MessageBox.Show("Đăng ký tài khoản thành công!", "Thành công",
                                    MessageBoxButton.OK, MessageBoxImage.Information);

                    DangNhap dangNhapWindow = new DangNhap();
                    dangNhapWindow.Show();
                    this.Close();
                }
            }
            else
            {
                MessageBox.Show("Mã OTP không chính xác. Đăng ký thất bại!", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Kiểm tra tài khoản/email đã tồn tại trong DB chưa
        /// </summary>
        private bool KiemTraTonTai(string taiKhoan, string email)
        {
            try
            {
                string query = "SELECT COUNT(*) FROM NguoiDung WHERE TenDangNhap = @TaiKhoan OR Email = @Email";

                var parameters = new[]
                {
                    new SqlParameter("@TaiKhoan", taiKhoan),
                    new SqlParameter("@Email", email)
                };

                // Gọi DatabaseHelper (đã có overload parameters)
                var dt = DatabaseHelper.GetData(query, parameters);
                int count = Convert.ToInt32(dt.Rows[0][0]);

                if (count > 0)
                {
                    MessageBox.Show("Tên đăng nhập hoặc Email đã tồn tại. Vui lòng chọn tên khác!",
                                    "Trùng lặp", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kiểm tra tài khoản: " + ex.Message,
                                "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
                return true;  // Trả về true để dừng flow nếu có lỗi
            }
        }

        /// <summary>
        /// Lưu tài khoản mới vào database
        /// </summary>
        private bool LuuTaiKhoanVaoDB(string taiKhoan, string email, string matKhauHash)
        {
            try
            {
                string query = @"INSERT INTO NguoiDung 
                                    (TenDangNhap, Email, MatKhau, HoTen, TienTeMacDinh) 
                                 VALUES 
                                    (@TaiKhoan, @Email, @MatKhau, @HoTen, 'VND')";

                var parameters = new[]
                {
                    new SqlParameter("@TaiKhoan", taiKhoan),
                    new SqlParameter("@Email", email),
                    new SqlParameter("@MatKhau", matKhauHash),
                    new SqlParameter("@HoTen", taiKhoan)   // Tạm dùng tên đăng nhập làm họ tên
                };

                int rows = DatabaseHelper.ExecuteQuery(query, parameters);
                return rows > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Lỗi khi lưu tài khoản:\n\n" + ex.Message,
                    "Lỗi Database",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }

        private void btnQuayLai_Click(object sender, RoutedEventArgs e)
        {
            DangNhap dangNhapWindow = new DangNhap();
            dangNhapWindow.Show();
            this.Close();
        }
    }
}