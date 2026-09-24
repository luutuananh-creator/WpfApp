using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;


namespace WpfApp
{
    public partial class DanhMuc : Page
    {
        List<DanhMucModel> danhSach;

        public DanhMuc()
        {
            InitializeComponent();
            LoadData();
        }

        // Tải dữ liệu thật từ SQL Server
        private void LoadData()
        {
            try
            {
                // Chỉ lấy danh mục của tài khoản đang đăng nhập
                string query = $"SELECT MaDanhMuc, TenDanhMuc, LoaiDanhMuc FROM DanhMuc WHERE MaNguoiDung = {DangNhap.MaNguoiDungHienTai}";
                DataTable dt = DatabaseHelper.GetData(query);
                danhSach = new List<DanhMucModel>();

                int stt = 1;
                foreach (DataRow row in dt.Rows)
                {
                    danhSach.Add(new DanhMucModel
                    {
                        STT = stt++,
                        MaDanhMuc = Convert.ToInt32(row["MaDanhMuc"]),
                        TenDanhMuc = row["TenDanhMuc"].ToString(),
                        LoaiDanhMuc = row["LoaiDanhMuc"].ToString()
                    });
                }
                dataGridDanhMuc.ItemsSource = danhSach;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải danh mục: " + ex.Message);
            }
        }

        private void btnThem_Click(object sender, RoutedEventArgs e)
        {
            // Truyền 0 hoặc không truyền gì nghĩa là THÊM MỚI
            ThemSuaDanhMuc win = new ThemSuaDanhMuc(0);
            win.ShowDialog();
            LoadData(); // Load lại bảng sau khi thêm xong
        }

        private void btnSua_Click(object sender, RoutedEventArgs e)
        {
            // 1. Kiểm tra xem người dùng đã chọn dòng nào trên DataGrid chưa
            if (dataGridDanhMuc.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn danh mục cần sửa!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Lấy dữ liệu của dòng đang chọn
            var item = dataGridDanhMuc.SelectedItem as DanhMucModel;

            // 3. Mở cửa sổ ThemSuaDanhMuc và TRUYỀN ID CỦA DANH MỤC SANG ĐỂ SỬA
            ThemSuaDanhMuc win = new ThemSuaDanhMuc(item.MaDanhMuc);
            win.ShowDialog();

            // 4. Load lại dữ liệu sau khi cửa sổ sửa đóng lại
            LoadData();
        }

        private void btnXoa_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridDanhMuc.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn danh mục cần xóa!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var item = dataGridDanhMuc.SelectedItem as DanhMucModel;
            var result = MessageBox.Show($"Bạn có chắc muốn xóa danh mục \"{item.TenDanhMuc}\"?\nLưu ý: Các giao dịch liên quan cũng sẽ tự động bị xóa!",
                                         "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Thực thi lệnh xóa dưới Database
                    string query = $"DELETE FROM DanhMuc WHERE MaDanhMuc = {item.MaDanhMuc}";
                    DatabaseHelper.ExecuteQuery(query);

                    MessageBox.Show("Đã xóa danh mục thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData(); // Tải lại bảng
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi xóa dữ liệu: " + ex.Message, "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnLamMoi_Click(object sender, RoutedEventArgs e)
        {
            txtTimKiem.Clear();
            LoadData();
        }

        private void txtTimKiem_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (danhSach == null) return;
            string keyword = txtTimKiem.Text.ToLower().Replace("🔍 ", "").Trim();

            var ketQua = danhSach.Where(x => x.TenDanhMuc.ToLower().Contains(keyword)).ToList();
            dataGridDanhMuc.ItemsSource = ketQua;
        }
    }

    public class DanhMucModel
    {
        public int STT { get; set; }
        public int MaDanhMuc { get; set; }
        public string TenDanhMuc { get; set; }
        public string LoaiDanhMuc { get; set; }
    }
}