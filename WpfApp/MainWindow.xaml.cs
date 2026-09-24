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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new TongQuan());
        }
        private void btnMenuTongQuan_Click(object sender, RoutedEventArgs e) { MainFrame.Navigate(new TongQuan()); }
        private void btnMenuGiaoDich_Click(object sender, RoutedEventArgs e) { MainFrame.Navigate(new GiaoDich()); }
        private void btnMenuDanhMuc_Click(object sender, RoutedEventArgs e) { MainFrame.Navigate(new DanhMuc()); }
        private void btnMenuNganSach_Click(object sender, RoutedEventArgs e) { MainFrame.Navigate(new NganSach()); }
        private void btnMenuMucTieu_Click(object sender, RoutedEventArgs e) { MainFrame.Navigate(new MucTieuTietKiem()); }
        private void btnMenuPhanTich_Click(object sender, RoutedEventArgs e) { MainFrame.Navigate(new PhanTich()); }
        private void btnMenuTroLyAI_Click(object sender, RoutedEventArgs e) { MainFrame.Navigate(new TroLyAI()); }
        private void btnMenuCaiDat_Click(object sender, RoutedEventArgs e) { MainFrame.Navigate(new CaiDat()); }
    }
}
