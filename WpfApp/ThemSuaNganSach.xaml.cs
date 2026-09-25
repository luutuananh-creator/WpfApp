using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Data;


namespace WpfApp
{
    public partial class ThemSuaNganSach : Window
    {
        private int _maNganSach = 0;

        public ThemSuaNganSach(int maNganSach = 0)
        {
            InitializeComponent();
            _maNganSach = maNganSach;
            this.Loaded += ThemSuaNganSach_Loaded;
        }

        private void txtHanMuc_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, @"^[0-9]+$");
        }

        private void ThemSuaNganSach_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDanhMuc();
            InitThangNam();

            if (_maNganSach > 0)
            {
                txtTieuDe.Text = "CHỈNH SỬA NGÂN SÁCH";
                txtTieuDe.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DarkOrange);
                LoadDataChiTiet(); // Gọi dữ liệu cũ lên
            }
        }

        private void InitThangNam()
        {
            for (int i = 1; i <= 12; i++) cboThang.Items.Add($"Tháng {i}");
            cboThang.SelectedIndex = DateTime.Now.Month - 1;
            txtNam.Text = DateTime.Now.Year.ToString();
        }

        private void LoadDanhMuc()
        {
            try
            {
                
                string query = $"SELECT MaDanhMuc, TenDanhMuc FROM DanhMuc WHERE MaNguoiDung = {DangNhap.MaNguoiDungHienTai}";
                DataTable dt = DatabaseHelper.GetData(query);

                cboDanhMuc.ItemsSource = dt.DefaultView;
                cboDanhMuc.DisplayMemberPath = "TenDanhMuc";
                cboDanhMuc.SelectedValuePath = "MaDanhMuc";

                if (_maNganSach == 0 && dt.Rows.Count > 0) cboDanhMuc.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi load danh mục: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDataChiTiet()
        {
            try
            {
                string query = $"SELECT * FROM NganSach WHERE MaNganSach = {_maNganSach}";
                DataTable dt = DatabaseHelper.GetData(query);

                if (dt.Rows.Count > 0)
                {
                    cboDanhMuc.SelectedValue = dt.Rows[0]["MaDanhMuc"];
                    cboThang.SelectedIndex = Convert.ToInt32(dt.Rows[0]["Thang"]) - 1;
                    txtNam.Text = dt.Rows[0]["Nam"].ToString();
                    txtHanMuc.Text = Convert.ToDecimal(dt.Rows[0]["HanMuc"]).ToString("0"); // Đổ hạn mức cũ ra
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lấy chi tiết ngân sách: " + ex.Message);
            }
        }

        private void btnLuu_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(txtHanMuc.Text, out decimal hanMuc) || hanMuc <= 0)
            {
                MessageBox.Show("Vui lòng nhập hạn mức hợp lệ!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cboDanhMuc.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn danh mục!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int maDanhMuc = Convert.ToInt32(cboDanhMuc.SelectedValue);
            int thang = cboThang.SelectedIndex + 1;
            int nam = Convert.ToInt32(txtNam.Text);

            try
            {
                string query = "";
                if (_maNganSach == 0) // LỆNH INSERT
                {
                    query = $@"INSERT INTO NganSach (MaNguoiDung, MaDanhMuc, Thang, Nam, HanMuc) 
                               VALUES ({DangNhap.MaNguoiDungHienTai}, {maDanhMuc}, {thang}, {nam}, {hanMuc})";
                }
                else // LỆNH UPDATE
                {
                    query = $@"UPDATE NganSach 
                               SET MaDanhMuc = {maDanhMuc}, Thang = {thang}, Nam = {nam}, HanMuc = {hanMuc} 
                               WHERE MaNganSach = {_maNganSach}";
                }

                DatabaseHelper.ExecuteQuery(query);
                MessageBox.Show("Lưu thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true; // Trả về true để màn hình chính biết và tải lại lưới DataGrid
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lưu dữ liệu: " + ex.Message, "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnHuy_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}