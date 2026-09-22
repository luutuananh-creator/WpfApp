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


using System.Data;

namespace WpfApp
{
    public partial class ThemSuaGiaoDich : Window
    {
        public ThemSuaGiaoDich()
        {
            InitializeComponent();
            dpNgay.SelectedDate = DateTime.Now; // Mặc định chọn ngày hôm nay
            LoadDanhMucLenComboBox();
        }

        private void LoadDanhMucLenComboBox()
        {
            try
            {
                // Đọc danh mục dựa theo Radio Button đang chọn (Thu nhập hay Chi tiêu)
                string loai = radChiTieu.IsChecked == true ? "Chi tiêu" : "Thu nhập";
                string query = $"SELECT MaDanhMuc, TenDanhMuc FROM DanhMuc WHERE MaNguoiDung = {DangNhap.MaNguoiDungHienTai} AND LoaiDanhMuc = N'{loai}'";

                DataTable dt = DatabaseHelper.GetData(query);
                cboDanhMuc.ItemsSource = dt.DefaultView;
                if (dt.Rows.Count > 0) cboDanhMuc.SelectedIndex = 0; // Tự động chọn dòng đầu tiên
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải danh mục: " + ex.Message);
            }
        }

        private void radChiTieu_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded) LoadDanhMucLenComboBox();
        }

        private void radThuNhap_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded) LoadDanhMucLenComboBox();
        }

        private void btnLuu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Kiểm tra dữ liệu
                if (cboDanhMuc.SelectedValue == null)
                {
                    MessageBox.Show("Vui lòng chọn danh mục!");
                    return;
                }

                decimal soTien = Convert.ToDecimal(txtSoTien.Text);
                int maDanhMuc = Convert.ToInt32(cboDanhMuc.SelectedValue);
                string ngay = dpNgay.SelectedDate.Value.ToString("yyyy-MM-dd");
                string phuongThuc = ((ComboBoxItem)cboPhuongThuc.SelectedItem).Content.ToString();
                string ghiChu = txtGhiChu.Text == txtGhiChu.Tag.ToString() ? "" : txtGhiChu.Text.Trim();

                string query = $@"INSERT INTO GiaoDich (MaNguoiDung, MaDanhMuc, SoTien, NgayGiaoDich, PhuongThucThanhToan, GhiChu) 
                                  VALUES ({DangNhap.MaNguoiDungHienTai}, {maDanhMuc}, {soTien}, '{ngay}', N'{phuongThuc}', N'{ghiChu}')";

                DatabaseHelper.ExecuteQuery(query);
                MessageBox.Show("Lưu giao dịch thành công!", "Thành công");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi nhập liệu: " + ex.Message);
            }
        }

        private void btnHuy_Click(object sender, RoutedEventArgs e) => this.Close();

        private void btnQuanLyDanhMuc_Click(object sender, RoutedEventArgs e)
        {
            ThemSuaDanhMuc win = new ThemSuaDanhMuc();
            win.ShowDialog();
            LoadDanhMucLenComboBox(); // Thêm xong thì update lại combobox
        }

        // Các hàm rỗng không dùng
        private void txtSoTien_TextChanged(object sender, TextChangedEventArgs e) { }
        private void cboDanhMuc_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void cboPhuongThuc_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private void txtGhiChu_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
