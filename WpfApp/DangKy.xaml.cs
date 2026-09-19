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

namespace WpfApp
{
    /// <summary>
    /// Interaction logic for DangKy.xaml
    /// </summary>
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

            // Kiểm tra dữ liệu cơ bản
            if (string.IsNullOrEmpty(taiKhoan) || string.IsNullOrEmpty(matKhau) || string.IsNullOrEmpty(xacNhanMatKhau))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (matKhau != xacNhanMatKhau)
            {
                MessageBox.Show("Mật khẩu xác nhận không khớp!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Tiến hành lưu vào cơ sở dữ liệu ở đây...
            MessageBox.Show("Tạo tài khoản thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnQuayLai_Click(object sender, RoutedEventArgs e)
        {
            // Đoạn code quay lại cửa sổ trước (ví dụ: màn hình đăng nhập)
             DangNhap manHinhDangNhap = new DangNhap();
             manHinhDangNhap.Show();
            this.Close();
        }
    }
    }

