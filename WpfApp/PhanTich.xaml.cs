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


namespace WpfApp/// <summary>
/// Interaction logic for PhanTich.xaml
/// </summary> // Lưu ý: Nếu Project của bạn tên khác, hãy giữ nguyên chữ "namespace Tên_Project_Của_Bạn" cũ nhé
{
    public partial class PhanTich : Page
    {
        public PhanTich()
        {
            InitializeComponent();
        }

        // Hàm xử lý khi bấm nút "Lọc dữ liệu"
        private void btnApDungLoc_Click(object sender, RoutedEventArgs e)
        {
            // Code lọc dữ liệu biểu đồ sau này viết ở đây
        }

        // Hàm xử lý khi bấm nút "Xem Giao dịch chi tiết"
        private void btnXemGiaoDich_Click(object sender, RoutedEventArgs e)
        {
            // Code chuyển sang trang Giao dịch sau này viết ở đây
        }

        // Hàm xử lý khi bấm nút "Phân tích bằng AI"
        private void btnPhanTichAI_Click(object sender, RoutedEventArgs e)
        {
            // Code chuyển sang trang Trợ lý AI sau này viết ở đây
        }

        private void cboLoaiBieuDo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        private void cboLocThoiGian_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Code xử lý khi người dùng đổi bộ lọc thời gian
        }
    }
}