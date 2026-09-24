using System;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp
{
    /// <summary>
    /// Cửa sổ Thông tin cá nhân.
    /// Cho phép cập nhật: HoTen, Email.
    /// Không cho sửa: TenDangNhap, MatKhau, TienTeMacDinh.
    /// </summary>
    public partial class ThongTinCaNhan : Window
    {
        public ThongTinCaNhan()
        {
            InitializeComponent();
            LoadDuLieu();
        }

        // ============================================================
        // LOAD DỮ LIỆU TỪ DATABASE
        // ============================================================
        private void LoadDuLieu()
        {
            try
            {
                string sql = @"
                    SELECT TenDangNhap, HoTen, Email, NgayTao
                    FROM NguoiDung
                    WHERE MaNguoiDung = @MaNguoiDung";

                SqlParameter[] p = {
                    new SqlParameter("@MaNguoiDung", Session.MaNguoiDung)
                };

                var dt = DatabaseHelper.GetData(sql, p);

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];

                    // Tên đăng nhập (readonly — chỉ hiển thị)
                    string tenDangNhap = row["TenDangNhap"]?.ToString() ?? "";
                    txtTenDangNhapHienThi.Text = tenDangNhap;

                    // Họ tên
                    txtHoTen.Text = row["HoTen"]?.ToString() ?? "";

                    // Email
                    txtEmail.Text = row["Email"]?.ToString() ?? "";

                    // Ngày tạo
                    if (row["NgayTao"] != DBNull.Value)
                    {
                        DateTime ngayTao = Convert.ToDateTime(row["NgayTao"]);
                        txtNgayTao.Text = ngayTao.ToString("dd/MM/yyyy HH:mm");
                    }
                    else
                    {
                        txtNgayTao.Text = "—";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải thông tin cá nhân: " + ex.Message,
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ============================================================
        // VALIDATE EMAIL
        // ============================================================
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            // Regex đơn giản — đủ dùng cho đồ án
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern);
        }

        // ============================================================
        // NÚT: LƯU
        // ============================================================
        private void btnLuu_Click(object sender, RoutedEventArgs e)
        {
            // -------- 1. LẤY DỮ LIỆU --------
            string hoTen = txtHoTen.Text?.Trim() ?? "";
            string email = txtEmail.Text?.Trim() ?? "";

            // -------- 2. VALIDATE --------
            if (string.IsNullOrWhiteSpace(hoTen))
            {
                MessageBox.Show("Vui lòng nhập họ và tên.",
                    "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtHoTen.Focus();
                return;
            }

            if (hoTen.Length > 100)
            {
                MessageBox.Show("Họ và tên không được vượt quá 100 ký tự.",
                    "Quá dài", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtHoTen.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Vui lòng nhập email.",
                    "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmail.Focus();
                return;
            }

            if (email.Length > 100)
            {
                MessageBox.Show("Email không được vượt quá 100 ký tự.",
                    "Quá dài", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmail.Focus();
                return;
            }

            if (!IsValidEmail(email))
            {
                MessageBox.Show("Email không hợp lệ.\nVí dụ: ten@domain.com",
                    "Sai định dạng", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmail.Focus();
                return;
            }

            // -------- 3. KIỂM TRA EMAIL TRÙNG --------
            try
            {
                string sqlCheck = @"
                    SELECT COUNT(*) AS SoLuong
                    FROM NguoiDung
                    WHERE Email = @Email
                      AND MaNguoiDung <> @MaNguoiDung";

                SqlParameter[] pCheck = {
                    new SqlParameter("@Email", email),
                    new SqlParameter("@MaNguoiDung", Session.MaNguoiDung)
                };

                var dtCheck = DatabaseHelper.GetData(sqlCheck, pCheck);
                int soLuong = dtCheck.Rows.Count > 0
                    ? Convert.ToInt32(dtCheck.Rows[0]["SoLuong"])
                    : 0;

                if (soLuong > 0)
                {
                    MessageBox.Show(
                        "Email này đã được sử dụng bởi tài khoản khác.\n" +
                        "Vui lòng chọn email khác.",
                        "Email trùng", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtEmail.Focus();
                    txtEmail.SelectAll();
                    return;
                }

                // -------- 4. UPDATE DB --------
                string sql = @"
                    UPDATE NguoiDung
                    SET HoTen = @HoTen,
                        Email = @Email
                    WHERE MaNguoiDung = @MaNguoiDung";

                SqlParameter[] p = {
                    new SqlParameter("@HoTen", hoTen),
                    new SqlParameter("@Email", email),
                    new SqlParameter("@MaNguoiDung", Session.MaNguoiDung)
                };

                int rows = DatabaseHelper.ExecuteQuery(sql, p);

                if (rows > 0)
                {
                    // Cập nhật Session
                    Session.HoTen = hoTen;
                    Session.Email = email;

                    MessageBox.Show(
                        "Đã cập nhật thông tin cá nhân thành công!",
                        "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Không có dòng nào được cập nhật.",
                        "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lưu thông tin: " + ex.Message,
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ============================================================
        // NÚT: HỦY
        // ============================================================
        private void btnHuy_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}