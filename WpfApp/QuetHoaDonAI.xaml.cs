using System;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;

namespace WpfApp
{
    public partial class QuetHoaDonAI : Window
    {
        // Dữ liệu AI bóc tách
        private string _imagePath = "";
        private string _tenCuaHang = "";
        private DateTime _ngayGiaoDich = DateTime.Today;
        private decimal _tongTien = 0;
        private string _danhMucGoiY = "";
        private string _ghiChu = "";

        public QuetHoaDonAI()
        {
            InitializeComponent();
        }

        // ============================================================
        // BƯỚC 1: CHỌN ẢNH
        // ============================================================
        private void btnChonAnh_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Chọn ảnh hóa đơn",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.webp;*.bmp|All Files|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                _imagePath = dialog.FileName;

                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_imagePath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    imgPreview.Source = bitmap;
                    imgPreview.Visibility = Visibility.Visible;
                    spChonAnhPlaceholder.Visibility = Visibility.Collapsed;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi hiển thị ảnh: " + ex.Message);
                }
            }
        }

        // ============================================================
        // BƯỚC 2: GỌI AI PHÂN TÍCH
        // ============================================================
        private async void btnPhanTichHoaDon_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_imagePath) || !File.Exists(_imagePath))
            {
                MessageBox.Show("Vui lòng chọn ảnh hóa đơn trước!",
                    "Thiếu ảnh", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnPhanTichHoaDon.IsEnabled = false;
            btnPhanTichHoaDon.Content = "⏳ Đang phân tích...";
            txtTrangThai.Visibility = Visibility.Visible;

            try
            {
                // Gọi Gemini qua Service
                string jsonResult = await QuetHoaDonAIService.DocHoaDon(_imagePath);

                // Parse JSON
                var json = JObject.Parse(jsonResult);

                _tenCuaHang = json["TenCuaHang"]?.ToString() ?? "Không xác định";
                string ngayStr = json["Ngay"]?.ToString() ?? "";
                _tongTien = json["TongTien"]?.ToObject<decimal>() ?? 0;
                _danhMucGoiY = json["DanhMucGoiY"]?.ToString() ?? "Khác";
                _ghiChu = json["GhiChu"]?.ToString() ?? "";

                if (DateTime.TryParse(ngayStr, out DateTime parsedDate))
                    _ngayGiaoDich = parsedDate;

                // Hiển thị
                txtCuaHang.Text = _tenCuaHang;
                txtNgay.Text = _ngayGiaoDich.ToString("dd/MM/yyyy");
                txtTongTien.Text = _tongTien.ToString("N0") + " đ";

                // KIỂM TRA NGÂN SÁCH
                KiemTraNganSach();

                MessageBox.Show(
                    "✅ Đã bóc tách hóa đơn thành công!\n\n" +
                    "• Cửa hàng: " + _tenCuaHang + "\n" +
                    "• Ngày: " + _ngayGiaoDich.ToString("dd/MM/yyyy") + "\n" +
                    "• Tổng tiền: " + _tongTien.ToString("N0") + " đ\n" +
                    "• Danh mục gợi ý: " + _danhMucGoiY + "\n" +
                    "• Ghi chú: " + _ghiChu,
                    "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi phân tích hóa đơn:\n\n" + ex.Message,
                    "Lỗi AI", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnPhanTichHoaDon.IsEnabled = true;
                btnPhanTichHoaDon.Content = "🪄 Phân tích hóa đơn";
                txtTrangThai.Visibility = Visibility.Collapsed;
            }
        }

        // ============================================================
        // KIỂM TRA NGÂN SÁCH
        // ============================================================
        private void KiemTraNganSach()
        {
            if (_tongTien <= 0) return;

            try
            {
                // 1. Tìm danh mục khớp với gợi ý AI
                string sqlFindDanhMuc = @"
                    SELECT TOP 1 MaDanhMuc, TenDanhMuc
                    FROM DanhMuc
                    WHERE MaNguoiDung = @MaNguoiDung
                      AND LoaiDanhMuc = N'Chi tiêu'
                      AND TenDanhMuc LIKE N'%' + @Keyword + N'%'
                    ORDER BY MaDanhMuc";

                SqlParameter[] pFind = {
                    new SqlParameter("@MaNguoiDung", DangNhap.MaNguoiDungHienTai),
                    new SqlParameter("@Keyword", _danhMucGoiY)
                };

                var dtDanhMuc = DatabaseHelper.GetData(sqlFindDanhMuc, pFind);

                if (dtDanhMuc.Rows.Count == 0) return;

                int maDanhMuc = Convert.ToInt32(dtDanhMuc.Rows[0]["MaDanhMuc"]);
                string tenDanhMuc = dtDanhMuc.Rows[0]["TenDanhMuc"].ToString();

                // 2. Lấy hạn mức ngân sách tháng
                string sqlNganSach = @"
                    SELECT HanMuc FROM NganSach
                    WHERE MaNguoiDung = @MaNguoiDung
                      AND MaDanhMuc = @MaDanhMuc
                      AND Thang = @Thang AND Nam = @Nam";

                SqlParameter[] pNS = {
                    new SqlParameter("@MaNguoiDung", DangNhap.MaNguoiDungHienTai),
                    new SqlParameter("@MaDanhMuc", maDanhMuc),
                    new SqlParameter("@Thang", _ngayGiaoDich.Month),
                    new SqlParameter("@Nam", _ngayGiaoDich.Year)
                };

                var dtNS = DatabaseHelper.GetData(sqlNganSach, pNS);
                if (dtNS.Rows.Count == 0) return;

                decimal hanMuc = Convert.ToDecimal(dtNS.Rows[0]["HanMuc"]);

                // 3. Tính đã chi trong tháng
                string sqlDaChi = @"
                    SELECT ISNULL(SUM(SoTien), 0) AS DaChi FROM GiaoDich
                    WHERE MaNguoiDung = @MaNguoiDung
                      AND MaDanhMuc = @MaDanhMuc
                      AND MONTH(NgayGiaoDich) = @Thang
                      AND YEAR(NgayGiaoDich) = @Nam";

                SqlParameter[] pDC = {
                    new SqlParameter("@MaNguoiDung", DangNhap.MaNguoiDungHienTai),
                    new SqlParameter("@MaDanhMuc", maDanhMuc),
                    new SqlParameter("@Thang", _ngayGiaoDich.Month),
                    new SqlParameter("@Nam", _ngayGiaoDich.Year)
                };

                var dtDC = DatabaseHelper.GetData(sqlDaChi, pDC);
                decimal daChi = Convert.ToDecimal(dtDC.Rows[0]["DaChi"]);
                decimal daChiSau = daChi + _tongTien;
                double phanTram = hanMuc > 0 ? (double)(daChiSau / hanMuc * 100) : 0;

                // 4. Cảnh báo theo mức (đồng bộ với NganSach.xaml.cs)
                if (daChiSau > hanMuc)
                {
                    decimal vuot = daChiSau - hanMuc;
                    MessageBox.Show(
                        "🚨 CẢNH BÁO VƯỢT NGÂN SÁCH!\n\n" +
                        "Danh mục: " + tenDanhMuc + "\n" +
                        "Hạn mức tháng: " + hanMuc.ToString("N0") + " đ\n" +
                        "Đã chi trước đó: " + daChi.ToString("N0") + " đ\n" +
                        "Hóa đơn này: +" + _tongTien.ToString("N0") + " đ\n" +
                        "─────────────────────\n" +
                        "Tổng sau khi thêm: " + daChiSau.ToString("N0") + " đ\n" +
                        "VƯỢT: " + vuot.ToString("N0") + " đ (" + phanTram.ToString("F0") + "%)",
                        "Vượt ngân sách", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else if (phanTram >= 80)
                {
                    MessageBox.Show(
                        "🔴 SẮP HẾT NGÂN SÁCH!\n\n" +
                        "Danh mục: " + tenDanhMuc + "\n" +
                        "Sau khi thêm: " + daChiSau.ToString("N0") + "/" + hanMuc.ToString("N0") + " đ (" + phanTram.ToString("F0") + "%)\n" +
                        "Còn lại: " + (hanMuc - daChiSau).ToString("N0") + " đ",
                        "Sắp hết", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else if (phanTram >= 50)
                {
                    MessageBox.Show(
                        "⚠️ CHÚ Ý NGÂN SÁCH\n\n" +
                        "Danh mục: " + tenDanhMuc + "\n" +
                        "Đã dùng: " + phanTram.ToString("F0") + "% (" + daChiSau.ToString("N0") + "/" + hanMuc.ToString("N0") + " đ)",
                        "Chú ý", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi kiểm tra NS: " + ex.Message);
            }
        }

        // ============================================================
        // BƯỚC 3: XÁC NHẬN — CHUYỂN SANG FORM THÊM GIAO DỊCH
        // ============================================================
        private void btnXacNhanLuu_Click(object sender, RoutedEventArgs e)
        {
            if (_tongTien <= 0)
            {
                MessageBox.Show("Chưa có dữ liệu hóa đơn. Vui lòng phân tích ảnh trước!",
                    "Thiếu dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var win = new ThemSuaGiaoDich();
            win.PreFillData(
                tenCuaHang: _tenCuaHang,
                ngay: _ngayGiaoDich,
                soTien: _tongTien,
                danhMucGoiY: _danhMucGoiY,
                ghiChu: _ghiChu,
                anhHoaDon: _imagePath
            );

            // ⭐ Form sẽ tự kiểm tra ngân sách sau khi lưu (trong btnLuu_Click)
            if (win.ShowDialog() == true)
            {
                DialogResult = true;
                Close();
            }
        }

        // ============================================================
        // NÚT HỦY
        // ============================================================
        private void btnHuy_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Event rỗng (XAML gắn sẵn)
        private void txtCuaHang_TextChanged(object sender, TextChangedEventArgs e) { }
        private void txtNgay_TextChanged(object sender, TextChangedEventArgs e) { }
        private void txtTongTien_TextChanged(object sender, TextChangedEventArgs e) { }
    }
}