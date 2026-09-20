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
    /// Interaction logic for ThemSuaGiaoDich.xaml
    /// </summary>
    public partial class ThemSuaGiaoDich : Window
    {
        public ThemSuaGiaoDich()
        {
            InitializeComponent();
        }
        private void btnQuanLyDanhMuc_Click(object sender, RoutedEventArgs e)
        {
            // Do DanhMuc là một Page, nếu muốn mở nó từ một Window phụ, 
            // bạn có thể sẽ phải hiện một Window quản lý danh mục dạng Dialog
            // WindowThemSuaDanhMuc win = new WindowThemSuaDanhMuc();
            // win.ShowDialog();
            MessageBox.Show("Mở màn hình Quản lý danh mục.");
        }

        private void btnLuu_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Đã lưu!");
            this.Close();
        }

        private void btnHuy_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void radChiTieu_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void radThuNhap_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void txtSoTien_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void cboDanhMuc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void cboPhuongThuc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
