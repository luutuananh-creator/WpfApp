using System;

namespace WpfApp.Helpers
{
    public static class SecurityHelpers
    {
        // Sinh mã OTP 6 chữ số ngẫu nhiên
        public static string GenerateOTP()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        // Hash mật khẩu bằng BCrypt
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        // Kiểm tra mật khẩu khi đăng nhập
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}