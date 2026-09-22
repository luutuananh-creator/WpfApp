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
    public partial class ThemSuaDanhMuc : Window
    {
        public ThemSuaDanhMuc()
        {
            InitializeComponent();
        }

        private void btnLuu_Click(object sender, RoutedEventArgs e)
        {
            string ten = txtTenDanhMuc.Text.Trim();
            // Nếu bạn có dùng RadioButton Thu/Chi trong XAML này, hãy đổi code cho phù hợp, ở đây giả sử mặc định
            string loai = "Chi tiêu"; // Tạm fix nếu chưa có radChiTieu bên XAML này

            if (string.IsNullOrEmpty(ten))
            {
                MessageBox.Show("Vui lòng nhập tên danh mục!");
                return;
            }

            try
            {
                string query = $@"INSERT INTO DanhMuc (MaNguoiDung, TenDanhMuc, LoaiDanhMuc) 
                                  VALUES ({DangNhap.MaNguoiDungHienTai}, N'{ten}', N'{loai}')";
                DatabaseHelper.ExecuteQuery(query);
                MessageBox.Show("Thêm danh mục thành công!");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }

        private void btnHuy_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Các hàm TextChanged khác để trống
        private void txtTenDanhMuc_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) { }
        private void cboIcon_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { }
        private void cboMauSac_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { }
    }
}