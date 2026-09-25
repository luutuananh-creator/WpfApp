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
                DateTime ngayGD = dpNgay.SelectedDate.Value;
                string ngay = ngayGD.ToString("yyyy-MM-dd");
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

                // ⭐ KIỂM TRA NGÂN SÁCH SAU KHI LƯU (chỉ khi là Chi tiêu)
                if (radChiTieu.IsChecked == true)
                {
                    KiemTraNganSachSauKhiLuu(maDanhMuc, soTien, ngayGD);
                }

                // ⭐ Set DialogResult để QuetHoaDonAI biết đã lưu thành công
                this.DialogResult = true;
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


        // ============================================================
        // ⭐ KIỂM TRA NGÂN SÁCH SAU KHI LƯU GIAO DỊCH
        // ============================================================
        private void KiemTraNganSachSauKhiLuu(int maDanhMuc, decimal soTien, DateTime ngayGD)
        {
            try
            {
                // 1. Lấy tên danh mục
                string sqlDM = "SELECT TenDanhMuc FROM DanhMuc WHERE MaDanhMuc = @MaDanhMuc";
                var dtDM = DatabaseHelper.GetData(sqlDM, new System.Data.SqlClient.SqlParameter[] {
            new System.Data.SqlClient.SqlParameter("@MaDanhMuc", maDanhMuc)
        });

                if (dtDM.Rows.Count == 0) return;
                string tenDanhMuc = dtDM.Rows[0]["TenDanhMuc"].ToString();

                // 2. Lấy ngân sách của danh mục cho tháng đó
                string sqlNS = @"
            SELECT HanMuc FROM NganSach
            WHERE MaNguoiDung = @MaNguoiDung
              AND MaDanhMuc = @MaDanhMuc
              AND Thang = @Thang AND Nam = @Nam";

                var dtNS = DatabaseHelper.GetData(sqlNS, new System.Data.SqlClient.SqlParameter[] {
            new System.Data.SqlClient.SqlParameter("@MaNguoiDung", DangNhap.MaNguoiDungHienTai),
            new System.Data.SqlClient.SqlParameter("@MaDanhMuc", maDanhMuc),
            new System.Data.SqlClient.SqlParameter("@Thang", ngayGD.Month),
            new System.Data.SqlClient.SqlParameter("@Nam", ngayGD.Year)
        });

                // TRƯỜNG HỢP A: CHƯA CÓ NGÂN SÁCH → Hỏi user
                if (dtNS.Rows.Count == 0)
                {
                    var result = MessageBox.Show(
                        $"📊 Danh mục \"{tenDanhMuc}\" CHƯA có ngân sách cho tháng {ngayGD.Month}/{ngayGD.Year}.\n\n" +
                        $"Giao dịch vừa lưu: {soTien:N0} đ\n\n" +
                        $"Bạn có muốn đặt ngân sách cho danh mục này không?",
                        "Chưa có ngân sách",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        ThemSuaNganSach popup = new ThemSuaNganSach(0);
                        popup.ShowDialog();
                    }
                    return;
                }

                // TRƯỜNG HỢP B: CÓ NGÂN SÁCH → Kiểm tra vượt
                decimal hanMuc = Convert.ToDecimal(dtNS.Rows[0]["HanMuc"]);

                string sqlDaChi = @"
            SELECT ISNULL(SUM(SoTien), 0) AS DaChi FROM GiaoDich
            WHERE MaNguoiDung = @MaNguoiDung
              AND MaDanhMuc = @MaDanhMuc
              AND MONTH(NgayGiaoDich) = @Thang
              AND YEAR(NgayGiaoDich) = @Nam";

                var dtDC = DatabaseHelper.GetData(sqlDaChi, new System.Data.SqlClient.SqlParameter[] {
            new System.Data.SqlClient.SqlParameter("@MaNguoiDung", DangNhap.MaNguoiDungHienTai),
            new System.Data.SqlClient.SqlParameter("@MaDanhMuc", maDanhMuc),
            new System.Data.SqlClient.SqlParameter("@Thang", ngayGD.Month),
            new System.Data.SqlClient.SqlParameter("@Nam", ngayGD.Year)
        });

                decimal daChi = Convert.ToDecimal(dtDC.Rows[0]["DaChi"]);
                double phanTram = hanMuc > 0 ? (double)(daChi / hanMuc * 100) : 0;

                if (daChi > hanMuc)
                {
                    decimal vuot = daChi - hanMuc;
                    MessageBox.Show(
                        $"🚨 CẢNH BÁO VƯỢT NGÂN SÁCH!\n\n" +
                        $"Danh mục: {tenDanhMuc}\n" +
                        $"Hạn mức tháng: {hanMuc:N0} đ\n" +
                        $"Đã chi: {daChi:N0} đ\n" +
                        $"VƯỢT: {vuot:N0} đ ({phanTram:F0}%)",
                        "Vượt ngân sách",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                else if (phanTram >= 90)
                {
                    MessageBox.Show(
                        $"🔴 SẮP HẾT NGÂN SÁCH!\n\n" +
                        $"Danh mục: {tenDanhMuc}\n" +
                        $"Đã chi: {daChi:N0}/{hanMuc:N0} đ ({phanTram:F0}%)\n" +
                        $"Còn lại: {(hanMuc - daChi):N0} đ",
                        "Sắp hết ngân sách",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                else if (phanTram >= 70)
                {
                    MessageBox.Show(
                        $"⚠️ CHÚ Ý NGÂN SÁCH\n\n" +
                        $"Danh mục: {tenDanhMuc}\n" +
                        $"Đã dùng: {phanTram:F0}% ({daChi:N0}/{hanMuc:N0} đ)",
                        "Chú ý ngân sách",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi kiểm tra NS sau lưu: " + ex.Message);
            }
        }

        // ============================================================
        // ⭐ THÊM MỚI: PreFillData — Điền dữ liệu từ AI quét hóa đơn
        // ============================================================
        /// <summary>
        /// Điền sẵn dữ liệu từ AI quét hóa đơn vào form.
        /// Được gọi từ QuetHoaDonAI.
        /// </summary>
        public void PreFillData(string tenCuaHang, DateTime ngay, decimal soTien,
                                 string danhMucGoiY, string ghiChu, string anhHoaDon)
        {
            try
            {
                // 1. Số tiền
                if (txtSoTien != null)
                    txtSoTien.Text = soTien.ToString("0");

                // 2. Ngày
                if (dpNgay != null)
                    dpNgay.SelectedDate = ngay;

                // 3. Ghi chú (gộp tên cửa hàng + mô tả)
                if (txtGhiChu != null)
                {
                    string noiDung = string.IsNullOrWhiteSpace(ghiChu)
                        ? tenCuaHang
                        : tenCuaHang + " - " + ghiChu;
                    txtGhiChu.Text = noiDung;
                }

                // 4. Set radio "Chi tiêu"
                if (radChiTieu != null)
                    radChiTieu.IsChecked = true;

                // 5. Load danh mục "Chi tiêu"
                LoadDanhMucLenComboBox();

                // 6. Chọn danh mục khớp với gợi ý AI
                if (cboDanhMuc != null && cboDanhMuc.Items.Count > 0 &&
                    !string.IsNullOrWhiteSpace(danhMucGoiY))
                {
                    bool found = false;
                    foreach (var item in cboDanhMuc.Items)
                    {
                        if (item is System.Data.DataRowView rowView)
                        {
                            string ten = rowView["TenDanhMuc"].ToString();
                            if (ten.ToLower().Contains(danhMucGoiY.ToLower()) ||
                                danhMucGoiY.ToLower().Contains(ten.ToLower()))
                            {
                                cboDanhMuc.SelectedItem = item;
                                found = true;
                                break;
                            }
                        }
                    }
                    if (!found)
                        cboDanhMuc.SelectedIndex = 0;
                }

                // 7. Phương thức thanh toán mặc định
                if (cboPhuongThuc != null && cboPhuongThuc.Items.Count > 0)
                    cboPhuongThuc.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi điền dữ liệu: " + ex.Message);
            }
        }
    }
}