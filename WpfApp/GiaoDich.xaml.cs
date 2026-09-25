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
using System.Data;


namespace WpfApp 
{
    public partial class GiaoDich : Page
    {
        // Biến toàn cục lưu danh sách gốc lấy từ DB để phục vụ việc lọc mà không cần gọi DB liên tục
        List<GiaoDichModel> danhSachGoc = new List<GiaoDichModel>();

        public GiaoDich()
        {
            InitializeComponent();
            LoadData();
        }

       
        private void LoadData()
        {
            try
            {
                // CẬP NHẬT: Select thêm g.MaGiaoDich
                string query = $@"
                    SELECT g.MaGiaoDich, g.NgayGiaoDich, d.LoaiDanhMuc, d.TenDanhMuc, g.GhiChu, g.SoTien 
                    FROM GiaoDich g
                    INNER JOIN DanhMuc d ON g.MaDanhMuc = d.MaDanhMuc
                    WHERE g.MaNguoiDung = {DangNhap.MaNguoiDungHienTai}
                    ORDER BY g.NgayGiaoDich DESC";

                DataTable dt = DatabaseHelper.GetData(query);
                danhSachGoc.Clear();

                foreach (DataRow row in dt.Rows)
                {
                    danhSachGoc.Add(new GiaoDichModel
                    {
                        MaGiaoDich = Convert.ToInt32(row["MaGiaoDich"]),
                        NgayGiaoDich = Convert.ToDateTime(row["NgayGiaoDich"]), // Lưu biến Date thật để dễ lọc tháng
                        Ngay = Convert.ToDateTime(row["NgayGiaoDich"]).ToString("dd/MM/yyyy"), // Chuỗi hiển thị lên bảng
                        Loai = row["LoaiDanhMuc"].ToString(),
                        DanhMuc = row["TenDanhMuc"].ToString(),
                        GhiChu = row["GhiChu"].ToString(),
                        SoTien = Convert.ToDecimal(row["SoTien"]).ToString("N0") + " đ"
                    });
                }

                LocDuLieu(); // Gọi hàm lọc thay vì gán thẳng vào DataGrid
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải giao dịch: " + ex.Message);
            }
        }


        private void LocDuLieu()
        {
            if (danhSachGoc == null) return;

            var ketQua = danhSachGoc.AsEnumerable();

            // A. Lọc theo Text tìm kiếm (Ghi chú hoặc Danh mục)
            if (txtTimKiem != null)
            {
                string keyword = txtTimKiem.Text.ToLower().Replace("🔍", "").Trim();
                if (!string.IsNullOrEmpty(keyword) && keyword != "nhập ghi chú...")
                {
                    ketQua = ketQua.Where(x => (x.GhiChu != null && x.GhiChu.ToLower().Contains(keyword)) ||
                                               (x.DanhMuc != null && x.DanhMuc.ToLower().Contains(keyword)));
                }
            }

            // B. Lọc linh hoạt theo Khoảng thời gian (Từ ngày - Đến ngày)
            if (dpTuNgay != null && dpTuNgay.SelectedDate.HasValue)
            {
                DateTime tuNgay = dpTuNgay.SelectedDate.Value.Date; // 00:00:00
                ketQua = ketQua.Where(x => x.NgayGiaoDich.Date >= tuNgay);
            }

            if (dpDenNgay != null && dpDenNgay.SelectedDate.HasValue)
            {
                DateTime denNgay = dpDenNgay.SelectedDate.Value.Date; // 23:59:59
                ketQua = ketQua.Where(x => x.NgayGiaoDich.Date <= denNgay);
            }

            // C. Lọc theo Phân loại (Tất cả / Thu nhập / Chi tiêu)
            if (cboLocDanhMuc != null && cboLocDanhMuc.SelectedItem != null)
            {
                string loai = ((ComboBoxItem)cboLocDanhMuc.SelectedItem).Content.ToString();
                if (loai == "Thu nhập" || loai == "Chi tiêu")
                {
                    ketQua = ketQua.Where(x => x.Loai == loai);
                }
            }

            // Đẩy kết quả đã lọc lên DataGrid
            if (dgvGiaoDich != null)
            {
                dgvGiaoDich.ItemsSource = ketQua.ToList();
            }
        }

        // ============ SỰ KIỆN LỌC ============
        private void txtTimKiem_TextChanged(object sender, TextChangedEventArgs e) { LocDuLieu(); }
        private void cboLocDanhMuc_SelectionChanged(object sender, SelectionChangedEventArgs e) { LocDuLieu(); }

        // Khi người dùng chọn lại ngày trên DatePicker
        private void dpTuNgay_SelectedDateChanged(object sender, SelectionChangedEventArgs e) { LocDuLieu(); }
        private void dpDenNgay_SelectedDateChanged(object sender, SelectionChangedEventArgs e) { LocDuLieu(); }

        // Sự kiện ComboBox Lọc nhanh (Tự động tính ngày và điền vào DatePicker)
        private void cboLocThoiGian_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboLocThoiGian.SelectedItem == null || dpTuNgay == null || dpDenNgay == null) return;

            string luaChon = ((ComboBoxItem)cboLocThoiGian.SelectedItem).Content.ToString();
            DateTime now = DateTime.Now;

            switch (luaChon)
            {
                case "Tất cả":
                    dpTuNgay.SelectedDate = null;
                    dpDenNgay.SelectedDate = null;
                    break;

                case "Hôm nay":
                    dpTuNgay.SelectedDate = now.Date;
                    dpDenNgay.SelectedDate = now.Date;
                    break;

                case "Tháng này":
                    dpTuNgay.SelectedDate = new DateTime(now.Year, now.Month, 1);
                    dpDenNgay.SelectedDate = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
                    break;

                case "Tháng trước":
                    DateTime thangTruoc = now.AddMonths(-1);
                    dpTuNgay.SelectedDate = new DateTime(thangTruoc.Year, thangTruoc.Month, 1);
                    dpDenNgay.SelectedDate = new DateTime(thangTruoc.Year, thangTruoc.Month, DateTime.DaysInMonth(thangTruoc.Year, thangTruoc.Month));
                    break;
            }
        }

        // Các event kích hoạt hàm lọc
       

        
        private void btnXoaGiaoDich_Click(object sender, RoutedEventArgs e)
        {
            if (dgvGiaoDich.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn giao dịch cần xóa!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var item = dgvGiaoDich.SelectedItem as GiaoDichModel;
            var result = MessageBox.Show($"Bạn có chắc muốn xóa giao dịch \"{item.GhiChu}\" ({item.SoTien}) không?",
                                         "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    string query = $"DELETE FROM GiaoDich WHERE MaGiaoDich = {item.MaGiaoDich}";
                    int row = DatabaseHelper.ExecuteQuery(query);
                    if (row > 0)
                    {
                        LoadData(); 
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi xóa: " + ex.Message, "Lỗi Database");
                }
            }
        }

        
        private void btnSuaGiaoDich_Click(object sender, RoutedEventArgs e)
        {
            if (dgvGiaoDich.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn giao dịch cần sửa!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var item = dgvGiaoDich.SelectedItem as GiaoDichModel;

            MessageBox.Show($"Bạn đang chọn sửa giao dịch Mã số: {item.MaGiaoDich}.\n\n(Để hoàn thiện, ta cần chỉnh sửa lại Constructor của trang ThemSuaGiaoDich để nó nhận dữ liệu truyền sang!)", "Thông báo");
            ThemSuaGiaoDich win = new ThemSuaGiaoDich(item.MaGiaoDich);
            win.ShowDialog();

            // Load lại lưới sau khi sửa xong
            LoadData();
        }

      
        private void btnThemGiaoDich_Click(object sender, RoutedEventArgs e)
        {
            ThemSuaGiaoDich win = new ThemSuaGiaoDich();
            win.ShowDialog();
            LoadData();
        }

        private void btnQuetHoaDon_Click(object sender, RoutedEventArgs e)
        {
            QuetHoaDonAI win = new QuetHoaDonAI();
            win.ShowDialog();
            LoadData();
        }

        private void btnNhapBangAI_Click(object sender, RoutedEventArgs e)
        {
            NhapGiaoDichAI win = new NhapGiaoDichAI();
            win.ShowDialog();
            LoadData();
        }
    }

    
    public class GiaoDichModel
    {
        public int MaGiaoDich { get; set; } 
        public DateTime NgayGiaoDich { get; set; } 
        public string Ngay { get; set; }
        public string Loai { get; set; }
        public string DanhMuc { get; set; }
        public string GhiChu { get; set; }
        public string SoTien { get; set; }
    }
}