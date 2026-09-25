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
        private int _maGiaoDich = 0;

        public ThemSuaGiaoDich(int maGiaoDich = 0)
        {
            InitializeComponent();
            _maGiaoDich = maGiaoDich;

            if (_maGiaoDich > 0)
            {
                this.Title = "Sửa Giao Dịch";
                TaiDuLieuCu(); // Lấy dữ liệu cũ đổ lên form
            }
            else
            {
                this.Title = "Thêm Giao Dịch Mới";
                dpNgay.SelectedDate = DateTime.Now;
                LoadDanhMucLenComboBox();
            }
        }

        private void TaiDuLieuCu()
        {
            try
            {
                string query = $"SELECT * FROM GiaoDich WHERE MaGiaoDich = {_maGiaoDich}";
                DataTable dt = DatabaseHelper.GetData(query);

                if (dt.Rows.Count > 0)
                {
                    txtSoTien.Text = Convert.ToDecimal(dt.Rows[0]["SoTien"]).ToString("0");
                    dpNgay.SelectedDate = Convert.ToDateTime(dt.Rows[0]["NgayGiaoDich"]);
                    txtGhiChu.Text = dt.Rows[0]["GhiChu"].ToString();

                    // Xác định Phương thức thanh toán
                    string phuongThuc = dt.Rows[0]["PhuongThucThanhToan"].ToString();
                    foreach (ComboBoxItem item in cboPhuongThuc.Items)
                    {
                        if (item.Content.ToString() == phuongThuc)
                        {
                            cboPhuongThuc.SelectedItem = item;
                            break;
                        }
                    }

                    // Truy vấn ngược bảng DanhMuc để biết đây là Thu hay Chi
                    int maDM = Convert.ToInt32(dt.Rows[0]["MaDanhMuc"]);
                    string queryDM = $"SELECT LoaiDanhMuc FROM DanhMuc WHERE MaDanhMuc = {maDM}";
                    DataTable dtDM = DatabaseHelper.GetData(queryDM);

                    if (dtDM.Rows.Count > 0)
                    {
                        if (dtDM.Rows[0]["LoaiDanhMuc"].ToString() == "Thu nhập")
                            radThuNhap.IsChecked = true;
                        else
                            radChiTieu.IsChecked = true;
                    }

                    LoadDanhMucLenComboBox();
                    cboDanhMuc.SelectedValue = maDM; // Chọn đúng danh mục cũ
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu cũ: " + ex.Message);
            }
        }

        private void LoadDanhMucLenComboBox()
        {
            try
            {
                string loai = radChiTieu.IsChecked == true ? "Chi tiêu" : "Thu nhập";
                string query = $"SELECT MaDanhMuc, TenDanhMuc FROM DanhMuc WHERE MaNguoiDung = {DangNhap.MaNguoiDungHienTai} AND LoaiDanhMuc = N'{loai}'";

                DataTable dt = DatabaseHelper.GetData(query);
                cboDanhMuc.ItemsSource = dt.DefaultView;
                cboDanhMuc.DisplayMemberPath = "TenDanhMuc";
                cboDanhMuc.SelectedValuePath = "MaDanhMuc";

                if (_maGiaoDich == 0 && dt.Rows.Count > 0)
                    cboDanhMuc.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải danh mục: " + ex.Message);
            }
        }

        private void radChiTieu_Checked(object sender, RoutedEventArgs e) { if (this.IsLoaded) LoadDanhMucLenComboBox(); }
        private void radThuNhap_Checked(object sender, RoutedEventArgs e) { if (this.IsLoaded) LoadDanhMucLenComboBox(); }

        private void btnLuu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cboDanhMuc.SelectedValue == null)
                {
                    MessageBox.Show("Vui lòng chọn danh mục!");
                    return;
                }

                decimal soTien = Convert.ToDecimal(txtSoTien.Text);
                int maDanhMuc = Convert.ToInt32(cboDanhMuc.SelectedValue);
                string ngay = dpNgay.SelectedDate.Value.ToString("yyyy-MM-dd");
                string phuongThuc = ((ComboBoxItem)cboPhuongThuc.SelectedItem).Content.ToString();
                string ghiChu = (txtGhiChu.Text == "Nhập mô tả cho giao dịch này...") ? "" : txtGhiChu.Text.Trim();

                string query = "";
                if (_maGiaoDich == 0)
                {
                    query = $@"INSERT INTO GiaoDich (MaNguoiDung, MaDanhMuc, SoTien, NgayGiaoDich, PhuongThucThanhToan, GhiChu) 
                               VALUES ({DangNhap.MaNguoiDungHienTai}, {maDanhMuc}, {soTien}, '{ngay}', N'{phuongThuc}', N'{ghiChu}')";
                }
                else
                {
                    query = $@"UPDATE GiaoDich 
                               SET MaDanhMuc = {maDanhMuc}, SoTien = {soTien}, NgayGiaoDich = '{ngay}', PhuongThucThanhToan = N'{phuongThuc}', GhiChu = N'{ghiChu}'
                               WHERE MaGiaoDich = {_maGiaoDich}";
                }

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
            LoadDanhMucLenComboBox();
        }

        private void txtSoTien_TextChanged(object sender, TextChangedEventArgs e) { }
        private void cboDanhMuc_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void cboPhuongThuc_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void txtGhiChu_TextChanged(object sender, TextChangedEventArgs e) { }
    }
}