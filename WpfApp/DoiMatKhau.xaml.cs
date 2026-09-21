using System;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Windows;
using WpfApp.Helpers;

namespace WpfApp
{
    public partial class DoiMatKhau : Window
    {
        private string maOTP = "";
        private int maNguoiDungQuen = 0;

        public DoiMatKhau()
        {
            InitializeComponent();
        }

        // =========================================================
        // BƯỚC 1: GỬI OTP QUA EMAIL
        // =========================================================
        private async void btnGuiOTP_Click(object sender, RoutedEventArgs e)
        {
            string email = txtEmail.Text.Trim();

            // 1. Kiểm tra trống
            if (string.IsNullOrEmpty(email))
            {
                MessageBox.Show("Vui lòng nhập email!", "Thông báo",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Kiểm tra định dạng email
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                MessageBox.Show("Email không đúng định dạng!", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // 3. Kiểm tra email có trong DB không
                string query = "SELECT MaNguoiDung FROM NguoiDung WHERE Email = @Email";
                var parameters = new[] { new SqlParameter("@Email", email) };
                DataTable dt = DatabaseHelper.GetData(query, parameters);

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Email này chưa được đăng ký trong hệ thống!\nVui lòng kiểm tra lại.",
                                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                maNguoiDungQuen = Convert.ToInt32(dt.Rows[0]["MaNguoiDung"]);

                // 4. Sinh OTP + gửi mail
                btnGuiOTP.IsEnabled = false;
                btnGuiOTP.Content = "Đang gửi...";

                maOTP = SecurityHelpers.GenerateOTP();
                EmailService emailService = new EmailService();
                bool daGui = await emailService.SendOtpEmailAsync(email, maOTP, "ForgotPassword");

                btnGuiOTP.IsEnabled = true;
                btnGuiOTP.Content = "📧 Gửi mã xác thực";

                if (!daGui)
                {
                    MessageBox.Show("Không thể gửi email. Vui lòng kiểm tra lại địa chỉ Email!",
                                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 5. Chuyển sang bước 2
                spNhapEmail.Visibility = Visibility.Collapsed;
                spNhapOTP.Visibility = Visibility.Visible;

                MessageBox.Show($"Mã OTP đã gửi tới:\n{email}\n\nVui lòng kiểm tra hộp thư!",
                                "Đã gửi OTP", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message,
                                "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =========================================================
        // GỬI LẠI OTP
        // =========================================================
        private async void btnGuiLaiOTP_Click(object sender, RoutedEventArgs e)
        {
            string email = txtEmail.Text.Trim();

            if (string.IsNullOrEmpty(email) || maNguoiDungQuen == 0)
            {
                MessageBox.Show("Vui lòng quay lại và nhập email trước!",
                                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnGuiLaiOTP.IsEnabled = false;
            btnGuiLaiOTP.Content = "Đang gửi lại...";

            maOTP = SecurityHelpers.GenerateOTP();
            EmailService emailService = new EmailService();
            bool daGui = await emailService.SendOtpEmailAsync(email, maOTP, "ForgotPassword");

            btnGuiLaiOTP.IsEnabled = true;
            btnGuiLaiOTP.Content = "🔄 Gửi lại mã OTP";

            if (daGui)
            {
                MessageBox.Show($"Mã OTP mới đã gửi tới:\n{email}",
                                "Đã gửi lại", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Không thể gửi lại email. Vui lòng thử lại sau!",
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =========================================================
        // BƯỚC 2: XÁC NHẬN ĐỔI MẬT KHẨU
        // =========================================================
        private void btnXacNhanDoi_Click(object sender, RoutedEventArgs e)
        {
            string otpNhap = txtOTP.Text.Trim();
            string matKhauMoi = txtMatKhauMoi.Password;
            string xacNhan = txtXacNhan.Password;

            // 1. Kiểm tra trống
            if (string.IsNullOrEmpty(otpNhap) ||
                string.IsNullOrEmpty(matKhauMoi) ||
                string.IsNullOrEmpty(xacNhan))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin!",
                                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Kiểm tra OTP
            if (otpNhap != maOTP)
            {
                MessageBox.Show("Mã OTP không chính xác!",
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                txtOTP.Clear();
                txtOTP.Focus();
                return;
            }

            // 3. Kiểm tra mật khẩu khớp
            if (matKhauMoi != xacNhan)
            {
                MessageBox.Show("Mật khẩu mới và xác nhận không khớp!",
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 4. Kiểm tra độ dài
            if (matKhauMoi.Length < 6)
            {
                MessageBox.Show("Mật khẩu phải có ít nhất 6 ký tự!",
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // 5. Hash mật khẩu mới
                string matKhauHash = SecurityHelpers.HashPassword(matKhauMoi);

                // 6. Update DB
                string query = "UPDATE NguoiDung SET MatKhau = @MatKhau WHERE MaNguoiDung = @Ma";
                var parameters = new[]
                {
                    new SqlParameter("@MatKhau", matKhauHash),
                    new SqlParameter("@Ma", maNguoiDungQuen)
                };

                int rows = DatabaseHelper.ExecuteQuery(query, parameters);

                if (rows > 0)
                {
                    MessageBox.Show("Đặt lại mật khẩu thành công!\nVui lòng đăng nhập lại với mật khẩu mới.",
                                    "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    DangNhap dangNhap = new DangNhap();
                    dangNhap.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Đặt lại mật khẩu thất bại! Vui lòng thử lại.",
                                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message,
                                "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =========================================================
        // QUAY LẠI ĐĂNG NHẬP
        // =========================================================
        private void btnQuayLai_Click(object sender, RoutedEventArgs e)
        {
            DangNhap dangNhap = new DangNhap();
            dangNhap.Show();
            this.Close();
        }
    }
}