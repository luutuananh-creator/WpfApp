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
    /// Interaction logic for GiaoDich.xaml
    /// </summary>
    public partial class GiaoDich : Page
    {
        public GiaoDich()
        {
            InitializeComponent();
        }

        private void btnThemGiaoDich_Click(object sender, RoutedEventArgs e)
        {
            ThemSuaGiaoDich win = new ThemSuaGiaoDich();
            win.ShowDialog();
        }

        private void btnQuetHoaDon_Click(object sender, RoutedEventArgs e)
        {
            QuetHoaDonAI win = new QuetHoaDonAI();
            win.ShowDialog();
        }

        private void btnNhapBangAI_Click(object sender, RoutedEventArgs e)
        {
            NhapGiaoDichAI win = new NhapGiaoDichAI();
            win.ShowDialog();
        }

        private void btnXoaGiaoDich_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnSuaGiaoDich_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
