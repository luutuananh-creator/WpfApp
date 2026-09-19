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
            // Viết code lưu tài khoản vào cơ sở dữ liệu ở đây (nếu có)
            MessageBox.Show("Đăng ký thành công!");

            // Quay lại màn hình đăng nhập
            DangNhap dangNhapWin = new DangNhap();
            dangNhapWin.Show();
            this.Close();
        }

        private void btnQuayLai_Click(object sender, RoutedEventArgs e)
        {
            DangNhap dangNhapWin = new DangNhap();
            dangNhapWin.Show();
            this.Close();
        }
    }
}
