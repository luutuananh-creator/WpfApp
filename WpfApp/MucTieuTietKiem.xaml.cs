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
    /// Interaction logic for MucTieuTietKiem.xaml
    /// </summary>
    public partial class MucTieuTietKiem : Page
    {
        public MucTieuTietKiem()
        {
            InitializeComponent();
        }

        private void btnThemMucTieu_Click(object sender, RoutedEventArgs e)
        {
            ThemSuaMucTieu win = new ThemSuaMucTieu();
            win.ShowDialog();
        }

        private void btnCapNhatTienDo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnSuaMucTieu_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnXoaMucTieu_Click(object sender, RoutedEventArgs e)
        {

        }

        private void cboTrangThai_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void txtTimKiemMucTieu_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void dgvMucTieu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
