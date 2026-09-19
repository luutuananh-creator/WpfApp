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
    /// Interaction logic for DangNhap.xaml
    /// </summary>
    public partial class DangNhap : Window
    {
        public DangNhap()
        {
            InitializeComponent();
        }

        private void btnDangNhap_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        private void btnDangKy_Click(object sender, RoutedEventArgs e)
        {
            DangKy dangKyWin = new DangKy();
            dangKyWin.Show();
            this.Close();
        }
        private void btnQuenMatKhau_Click(object sender, RoutedEventArgs e)
        {
            // CÁCH 1: Nếu bạn CHƯA tạo cửa sổ Quên mật khẩu, tạm thời hiện thông báo:
            MessageBox.Show("Vui lòng kiểm tra email của bạn để lấy lại mật khẩu, hoặc liên hệ Quản trị viên.", "Quên mật khẩu", MessageBoxButton.OK, MessageBoxImage.Information);

            /* 
            // CÁCH 2: Nếu bạn đã tạo thêm 1 file Window mới tên là QuenMatKhau (Window 21)
            QuenMatKhau quenMKWin = new QuenMatKhau();
            quenMKWin.Show();
            this.Close(); 
            */
        }
    }
}
