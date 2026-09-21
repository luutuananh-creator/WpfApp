using System.Windows;
using System.Windows.Controls;
using System.Data.SqlClient;

namespace WpfApp
{
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
            var result = MessageBox.Show(
                "Bạn có chắc chắn muốn đăng xuất?",
                "Xác nhận đăng xuất",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            DangNhap dangNhapWin = new DangNhap();
            dangNhapWin.Show();

            Window.GetWindow(this)?.Close();
        }
    }
}