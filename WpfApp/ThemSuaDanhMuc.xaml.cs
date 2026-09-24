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

using System.Data;

namespace WpfApp 
{
    public partial class ThemSuaDanhMuc : Window
    {
        // Biến toàn cục lưu trữ ID danh mục. Nếu = 0 là Thêm mới, Nếu > 0 là Sửa.
        private int _maDanhMuc = 0;

        // Cập nhật hàm khởi tạo để nhận tham số maDanhMuc
        public ThemSuaDanhMuc(int maDanhMuc = 0)
        {
            InitializeComponent();
            _maDanhMuc = maDanhMuc;

            if (_maDanhMuc > 0)
            {
                // Đổi tiêu đề và tải dữ liệu cũ lên form
                this.Title = "Sửa Thông Tin Danh Mục";
                TaiDuLieuCu();
            }
            else
            {
               
                this.Title = "Thêm Danh Mục Mới";
            }
        }

        private void TaiDuLieuCu()
        {
            try
            {
                // Lấy thông tin của danh mục đang cần sửa từ DB
                string query = $"SELECT TenDanhMuc, LoaiDanhMuc FROM DanhMuc WHERE MaDanhMuc = {_maDanhMuc}";
                DataTable dt = DatabaseHelper.GetData(query);

                if (dt.Rows.Count > 0)
                {
                    // Đổ tên danh mục vào TextBox
                    txtTenDanhMuc.Text = dt.Rows[0]["TenDanhMuc"].ToString();

                    // Tích chọn RadioButton Thu/Chi cho đúng
                    string loai = dt.Rows[0]["LoaiDanhMuc"].ToString();
                    if (loai == "Thu nhập")
                        radThuNhap.IsChecked = true;
                    else
                        radChiTieu.IsChecked = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu cũ: " + ex.Message);
            }
        }

        private void btnLuu_Click(object sender, RoutedEventArgs e)
        {
            string ten = txtTenDanhMuc.Text.Trim();
            string loai = radThuNhap.IsChecked == true ? "Thu nhập" : "Chi tiêu";

            if (string.IsNullOrEmpty(ten))
            {
                MessageBox.Show("Vui lòng nhập tên danh mục!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string query = "";

                if (_maDanhMuc == 0)
                {
                    // THÊM MỚI
                    query = $@"INSERT INTO DanhMuc (MaNguoiDung, TenDanhMuc, LoaiDanhMuc) 
                               VALUES ({DangNhap.MaNguoiDungHienTai}, N'{ten}', N'{loai}')";
                    DatabaseHelper.ExecuteQuery(query);
                    MessageBox.Show("Thêm danh mục thành công!", "Thành công");
                }
                else
                {
                    // CẬP NHẬT (SỬA)
                    query = $@"UPDATE DanhMuc 
                               SET TenDanhMuc = N'{ten}', LoaiDanhMuc = N'{loai}' 
                               WHERE MaDanhMuc = {_maDanhMuc}";
                    DatabaseHelper.ExecuteQuery(query);
                    MessageBox.Show("Cập nhật danh mục thành công!", "Thành công");
                }

                this.Close(); // Lưu xong thì đóng form
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu dữ liệu: " + ex.Message, "Lỗi Database");
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