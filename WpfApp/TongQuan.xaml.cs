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
    /// Interaction logic for TongQuan.xaml
    /// </summary>
    public partial class TongQuan : Page
    {
        public TongQuan()
        {
            InitializeComponent();
        }

        
        private void btnXemGiaoDich_Click(object sender, RoutedEventArgs e) { NavigationService.Navigate(new GiaoDich()); }
        private void btnXemNganSach_Click(object sender, RoutedEventArgs e) { NavigationService.Navigate(new NganSach()); }
        private void btnXemPhanTich_Click(object sender, RoutedEventArgs e) { NavigationService.Navigate(new PhanTich()); }
    }
}
