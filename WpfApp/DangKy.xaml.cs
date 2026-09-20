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
using System.Windows.Shapes;


using System.Data;


namespace WpfApp // Lưu ý: Nếu Project của bạn tên khác, hãy đổi lại namespace cho đúng
{
    public partial class DangKy : Window
    {
        public DangKy()
        {
            InitializeComponent();
        }

        private void btnTaoTaiKhoan_Click(object sender, RoutedEventArgs e)
        {
            string taiKhoan = txtTaiKhoan.Text.Trim();
            string matKhau = txtMatKhau.Password;
            string xacNhanMatKhau = txtXacNhanMatKhau.Password;

            // 1. Kiểm tra dữ liệu trống
            if (string.IsNullOrEmpty(taiKhoan) || string.IsNullOrEmpty(matKhau) || string.IsNullOrEmpty(xacNhanMatKhau))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Kiểm tra mật khẩu khớp
            if (matKhau != xacNhanMatKhau)
            {
                MessageBox.Show("Mật khẩu xác nhận không khớp!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // 3. Kiểm tra xem tên đăng nhập đã tồn tại trong CSDL chưa
                string checkQuery = $"SELECT COUNT(*) FROM NguoiDung WHERE TenDangNhap = '{taiKhoan}'";
                DataTable dt = DatabaseHelper.GetData(checkQuery);

                if (dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0][0]) > 0)
                {
                    MessageBox.Show("Tên tài khoản này đã có người sử dụng. Vui lòng chọn tên khác!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 4. Tiến hành lưu vào CSDL
                // Vì Database bắt buộc phải có Email (NOT NULL), ta tạo tạm 1 email mặc định để tránh lỗi CSDL
                string emailTam = taiKhoan + "@app.com";
                string hoTenTam = taiKhoan; // Tạm dùng tên tài khoản làm họ tên hiển thị

                // Câu lệnh INSERT
                string insertQuery = $@"
                    INSERT INTO NguoiDung (TenDangNhap, MatKhau, Email, HoTen) 
                    VALUES ('{taiKhoan}', '{matKhau}', '{emailTam}', N'{hoTenTam}')";

                int result = DatabaseHelper.ExecuteQuery(insertQuery);

                if (result > 0)
                {
                    MessageBox.Show("Tạo tài khoản thành công! Hãy đăng nhập nhé.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Chuyển sang màn hình Đăng nhập
                    DangNhap manHinhDangNhap = new DangNhap();
                    manHinhDangNhap.Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối CSDL: " + ex.Message, "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnQuayLai_Click(object sender, RoutedEventArgs e)
        {
            DangNhap manHinhDangNhap = new DangNhap();
            manHinhDangNhap.Show();
            this.Close();
        }
    }
}