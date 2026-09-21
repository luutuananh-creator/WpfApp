namespace WpfApp
{
    /// <summary>
    /// Lưu thông tin người dùng đang đăng nhập — dùng chung cho toàn app
    /// </summary>
    public static class Session
    {
        public static int MaNguoiDung { get; set; }
        public static string TenDangNhap { get; set; }
        public static string HoTen { get; set; }
        public static string Email { get; set; }
        public static string TienTe { get; set; } = "VND";
    }
}