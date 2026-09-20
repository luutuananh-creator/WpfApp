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
    /// Interaction logic for TroLyAI.xaml
    /// </summary>
    public partial class TroLyAI : Page
    {
        public TroLyAI()
        {
            InitializeComponent();
        }
        private void btnQuetHoaDon_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            QuetHoaDonAI win = new QuetHoaDonAI();
            win.ShowDialog();
        }
        private void btnNhapGiaoDich_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            NhapGiaoDichAI win = new NhapGiaoDichAI();
            win.ShowDialog();
        }
        private void btnPhanTich_Click(object sender, System.Windows.RoutedEventArgs e)
        {

        }
        private void btnGuiTinNhan_Click(object sender, System.Windows.RoutedEventArgs e)
        {

        }
    }

}
