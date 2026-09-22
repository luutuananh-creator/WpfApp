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
            ThemSuaDanhMuc win = new ThemSuaDanhMuc();
            win.ShowDialog();
            LoadData(); // Load lại bảng sau khi thêm xong
        }

        private void btnSua_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Chức năng sửa đang hoàn thiện.");
        }

        private void btnXoa_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridDanhMuc.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn danh mục cần xóa!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var item = dataGridDanhMuc.SelectedItem as DanhMucModel;
            var result = MessageBox.Show($"Bạn có chắc muốn xóa danh mục \"{item.TenDanhMuc}\"?\nLưu ý: Các giao dịch liên quan cũng sẽ bị xóa!",
                                         "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                string query = $"DELETE FROM DanhMuc WHERE MaDanhMuc = {item.MaDanhMuc}";
                DatabaseHelper.ExecuteQuery(query);
                LoadData();
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