using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp
{
    public partial class ThemSuaMucTieu : Window
    {
        // = 0 khi THÊM MỚI, > 0 khi SỬA
        private int _maMucTieuDangSua = 0;

        // ============================================================
        // CONSTRUCTOR 1 — THÊM MỚI
        // ============================================================
        public ThemSuaMucTieu()
        {
            InitializeComponent();
            Title = "Thêm mục tiêu tiết kiệm";
            _maMucTieuDangSua = 0;

            if (dpNgayHoanThanh != null)
                dpNgayHoanThanh.SelectedDate = DateTime.Today.AddMonths(1);
        }

        // ============================================================
        // CONSTRUCTOR 2 — SỬA
        // ============================================================
        public ThemSuaMucTieu(int maMucTieu)
        {
            InitializeComponent();
            Title = "Sửa mục tiêu tiết kiệm";
            _maMucTieuDangSua = maMucTieu;
            LoadDuLieuLenForm();
        }

        // ============================================================
        // LOAD DỮ LIỆU KHI SỬA
        // ============================================================
        private void LoadDuLieuLenForm()
        {
            try
            {
                string sql = @"
                    SELECT TenMucTieu, SoTienMucTieu, DaTichLuy, NgayHoanThanh
                    FROM MucTieuTietKiem
                    WHERE MaMucTieu = @MaMucTieu AND MaNguoiDung = @MaNguoiDung";

                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@MaMucTieu", _maMucTieuDangSua),
                    new SqlParameter("@MaNguoiDung", Session.MaNguoiDung)
                };

                var dt = DatabaseHelper.GetData(sql, parameters);

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    txtTenMucTieu.Text = row["TenMucTieu"].ToString();
                    txtSoTienMucTieu.Text = Convert.ToDecimal(row["SoTienMucTieu"]).ToString("0");
                    txtDaTichLuy.Text = Convert.ToDecimal(row["DaTichLuy"]).ToString("0");

                    if (row["NgayHoanThanh"] != DBNull.Value)
                        dpNgayHoanThanh.SelectedDate = Convert.ToDateTime(row["NgayHoanThanh"]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message,
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ============================================================
        // EVENT TextChanged — rỗng, chỉ để XAML hết lỗi
        // ============================================================
        private void txtTenMucTieu_TextChanged(object sender, TextChangedEventArgs e) { }
        private void txtSoTienMucTieu_TextChanged(object sender, TextChangedEventArgs e) { }
        private void txtDaTichLuy_TextChanged(object sender, TextChangedEventArgs e) { }

        // ============================================================
        // NÚT: LƯU — ĐÃ THÊM VALIDATE DaTichLuy <= SoTienMucTieu
        // ============================================================
        private void btnLuu_Click(object sender, RoutedEventArgs e)
        {
            // -------- 1. VALIDATE TÊN --------
            string tenMucTieu = txtTenMucTieu.Text?.Trim();

            if (string.IsNullOrWhiteSpace(tenMucTieu))
            {
                MessageBox.Show("Vui lòng nhập tên mục tiêu.",
                    "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtTenMucTieu.Focus();
                return;
            }

            // -------- 2. VALIDATE SỐ TIỀN MỤC TIÊU --------
            if (!decimal.TryParse(txtSoTienMucTieu.Text, out decimal soTien) || soTien <= 0)
            {
                MessageBox.Show("Số tiền mục tiêu phải là số dương.",
                    "Sai định dạng", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtSoTienMucTieu.Focus();
                return;
            }

            // -------- 3. VALIDATE SỐ TIỀN ĐÃ TÍCH LŨY --------
            decimal daTichLuy = 0;
            if (!string.IsNullOrWhiteSpace(txtDaTichLuy.Text))
            {
                if (!decimal.TryParse(txtDaTichLuy.Text, out daTichLuy) || daTichLuy < 0)
                {
                    MessageBox.Show("Số tiền đã có sẵn không hợp lệ.",
                        "Sai định dạng", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtDaTichLuy.Focus();
                    return;
                }
            }

            // ============================================================
            // 4. ⚠️ LOGIC MỚI: DaTichLuy KHÔNG ĐƯỢC VƯỢT SoTienMucTieu
            // ============================================================
            if (daTichLuy > soTien)
            {
                MessageBox.Show(
                    $"Số tiền đã có ({daTichLuy:N0} đ) không được lớn hơn " +
                    $"số tiền mục tiêu ({soTien:N0} đ).\n\n" +
                    $"Vui lòng nhập lại.",
                    "Logic không hợp lệ",
                    MessageBoxButton.OK, MessageBoxImage.Warning);

                txtDaTichLuy.Focus();
                txtDaTichLuy.SelectAll();
                return;
            }

            // -------- 5. XÁC ĐỊNH TRẠNG THÁI --------
            DateTime? ngayHoanThanh = dpNgayHoanThanh.SelectedDate;

            string trangThai;
            if (daTichLuy >= soTien && soTien > 0)
                trangThai = "Đã hoàn thành";
            else
                trangThai = "Đang thực hiện";

            // -------- 6. LƯU VÀO DB --------
            try
            {
                string sql;
                SqlParameter[] parameters;

                if (_maMucTieuDangSua == 0)
                {
                    // === INSERT ===
                    sql = @"
                        INSERT INTO MucTieuTietKiem
                            (MaNguoiDung, TenMucTieu, SoTienMucTieu, DaTichLuy,
                             NgayHoanThanh, TrangThai)
                        VALUES
                            (@MaNguoiDung, @TenMucTieu, @SoTienMucTieu, @DaTichLuy,
                             @NgayHoanThanh, @TrangThai)";

                    parameters = new SqlParameter[]
                    {
                        new SqlParameter("@MaNguoiDung", Session.MaNguoiDung),
                        new SqlParameter("@TenMucTieu", tenMucTieu),
                        new SqlParameter("@SoTienMucTieu", soTien),
                        new SqlParameter("@DaTichLuy", daTichLuy),
                        new SqlParameter("@NgayHoanThanh", (object)ngayHoanThanh ?? DBNull.Value),
                        new SqlParameter("@TrangThai", trangThai)
                    };
                }
                else
                {
                    // === UPDATE ===
                    sql = @"
                        UPDATE MucTieuTietKiem
                        SET TenMucTieu     = @TenMucTieu,
                            SoTienMucTieu  = @SoTienMucTieu,
                            DaTichLuy      = @DaTichLuy,
                            NgayHoanThanh  = @NgayHoanThanh,
                            TrangThai      = @TrangThai
                        WHERE MaMucTieu = @MaMucTieu
                          AND MaNguoiDung = @MaNguoiDung";

                    parameters = new SqlParameter[]
                    {
                        new SqlParameter("@TenMucTieu", tenMucTieu),
                        new SqlParameter("@SoTienMucTieu", soTien),
                        new SqlParameter("@DaTichLuy", daTichLuy),
                        new SqlParameter("@NgayHoanThanh", (object)ngayHoanThanh ?? DBNull.Value),
                        new SqlParameter("@TrangThai", trangThai),
                        new SqlParameter("@MaMucTieu", _maMucTieuDangSua),
                        new SqlParameter("@MaNguoiDung", Session.MaNguoiDung)
                    };
                }

                int rows = DatabaseHelper.ExecuteQuery(sql, parameters);

                if (rows > 0)
                {
                    MessageBox.Show(
                        _maMucTieuDangSua == 0
                            ? "Đã thêm mục tiêu thành công!"
                            : "Đã cập nhật mục tiêu thành công!",
                        "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Không có dòng nào được lưu.",
                        "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu mục tiêu:\n" + ex.Message,
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ============================================================
        // NÚT: HỦY
        // ============================================================
        private void btnHuy_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}