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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp
{
    /// <summary>
    /// Interaction logic for CaiDat.xaml
    /// </summary>
    public partial class CaiDat : Page
    {
        public CaiDat()
        {
            InitializeComponent();
        }

        private void btnThongTinCaNhan_Click(object sender, RoutedEventArgs e)
        {
            ThongTinCaNhan win = new ThongTinCaNhan();
            win.ShowDialog();
        }

        private void btnDoiMatKhau_Click(object sender, RoutedEventArgs e)
        {
            DoiMatKhau win = new DoiMatKhau();
            win.ShowDialog();
        }

        private void btnCaiDatChung_Click(object sender, RoutedEventArgs e)
        {
            CaiDatChung win = new CaiDatChung();
            win.ShowDialog();
        }

        private void btnDangXuat_Click(object sender, RoutedEventArgs e)
        {
            // Mở lại cửa sổ Đăng nhập
            DangNhap dangNhapWin = new DangNhap();
            dangNhapWin.Show();

            // Đóng MainWindow hiện tại (cửa sổ đang chứa Frame và Page Cài đặt này)
            Window.GetWindow(this).Close();
        }
    }
}
