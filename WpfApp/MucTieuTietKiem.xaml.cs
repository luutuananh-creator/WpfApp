using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp
{
    public partial class MucTieuTietKiem : Page
    {
        private List<MucTieuRow> _allMucTieu = new List<MucTieuRow>();
        private bool _isInitializing = true;

        public MucTieuTietKiem()
        {
            InitializeComponent();
            Loaded += MucTieuTietKiem_Loaded;
        }

        // ============================================================
        // KHỞI TẠO KHI MỞ TRANG
        // ============================================================
        private void MucTieuTietKiem_Loaded(object sender, RoutedEventArgs e)
        {
            _isInitializing = true;
            try
            {
                // ComboBox mặc định = "Tất cả mục tiêu" (index 2)
                cboTrangThai.SelectedIndex = 2;

                // Load dữ liệu từ SQL
                LoadDanhSachMucTieu();
            }
            finally
            {
                _isInitializing = false;
            }
        }

        // ============================================================
        // LOAD DATA
        // ============================================================
        private void LoadDanhSachMucTieu()
        {
            _allMucTieu.Clear();
            try
            {
                string sql = @"SELECT MaMucTieu, TenMucTieu, SoTienMucTieu, DaTichLuy,
                                      NgayHoanThanh, TrangThai
                               FROM MucTieuTietKiem
                               WHERE MaNguoiDung = @MaNguoiDung
                               ORDER BY NgayTao DESC";

                SqlParameter[] p = {
                    new SqlParameter("@MaNguoiDung", Session.MaNguoiDung)
                };

                DataTable dt = DatabaseHelper.GetData(sql, p);

                foreach (DataRow row in dt.Rows)
                {
                    decimal soTien = Convert.ToDecimal(row["SoTienMucTieu"]);
                    decimal daTichLuy = Convert.ToDecimal(row["DaTichLuy"]);

                    // Kẹp dữ liệu cũ bị sai (nếu có)
                    if (daTichLuy > soTien && soTien > 0)
                        daTichLuy = soTien;

                    _allMucTieu.Add(new MucTieuRow
                    {
                        MaMucTieu = Convert.ToInt32(row["MaMucTieu"]),
                        TenMucTieu = row["TenMucTieu"].ToString(),
                        SoTienMucTieu = soTien,
                        DaTichLuy = daTichLuy,
                        NgayHoanThanh = row["NgayHoanThanh"] == DBNull.Value
                            ? (DateTime?)null
                            : Convert.ToDateTime(row["NgayHoanThanh"]),
                        TrangThai = row["TrangThai"].ToString(),
                        TienDo = soTien > 0
                            ? Math.Min((double)(daTichLuy / soTien) * 100, 100)
                            : 0
                    });
                }

                ApplyFilter();

                if (dgvMucTieu.Items.Count > 0)
                    dgvMucTieu.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải danh sách: " + ex.Message);
            }
        }

        // ============================================================
        // FILTER — ĐÃ SỬA: không còn logic "🔍"
        // ============================================================
        private void ApplyFilter()
        {
            // ⚠️ TRIM: xóa khoảng trắng đầu/cuối
            string keyword = txtTimKiemMucTieu.Text?.Trim().ToLower() ?? "";

            string tt = (cboTrangThai.SelectedItem as ComboBoxItem)?.Content?.ToString()
                        ?? "Tất cả mục tiêu";

            var filtered = _allMucTieu.Where(m =>
            {
                // Trim cả tên trong data để chắc chắn match
                string ten = (m.TenMucTieu ?? "").Trim().ToLower();

                bool matchKeyword = string.IsNullOrEmpty(keyword)
                                    || ten.Contains(keyword);

                bool matchTrangThai = tt == "Tất cả mục tiêu"
                                      || m.TrangThai == tt;

                return matchKeyword && matchTrangThai;
            }).ToList();

            dgvMucTieu.ItemsSource = filtered;
        }

        // ============================================================
        // EVENTS — ĐÃ SỬA: bỏ logic gán icon 🔍
        // ============================================================
        private void txtTimKiemMucTieu_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing) return;
            ApplyFilter();
        }

        private void cboTrangThai_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            ApplyFilter();
        }

        private void dgvMucTieu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool has = dgvMucTieu.SelectedItem != null;
            btnSuaMucTieu.IsEnabled = has;
            btnXoaMucTieu.IsEnabled = has;
            btnCapNhatTienDo.IsEnabled = has;
        }

        // ============================================================
        // NÚT THÊM
        // ============================================================
        private void btnThemMucTieu_Click(object sender, RoutedEventArgs e)
        {
            ThemSuaMucTieu win = new ThemSuaMucTieu();
            if (win.ShowDialog() == true)
                LoadDanhSachMucTieu();
        }

        // ============================================================
        // NÚT SỬA
        // ============================================================
        private void btnSuaMucTieu_Click(object sender, RoutedEventArgs e)
        {
            var selected = dgvMucTieu.SelectedItem as MucTieuRow;
            if (selected == null)
            {
                MessageBox.Show("Vui lòng chọn một mục tiêu để sửa.");
                return;
            }

            ThemSuaMucTieu win = new ThemSuaMucTieu(selected.MaMucTieu);
            if (win.ShowDialog() == true)
                LoadDanhSachMucTieu();
        }

        // ============================================================
        // NÚT XÓA
        // ============================================================
        private void btnXoaMucTieu_Click(object sender, RoutedEventArgs e)
        {
            var selected = dgvMucTieu.SelectedItem as MucTieuRow;
            if (selected == null)
            {
                MessageBox.Show("Vui lòng chọn một mục tiêu để xóa.");
                return;
            }

            var confirm = MessageBox.Show(
                $"Bạn có chắc muốn xóa mục tiêu \"{selected.TenMucTieu}\"?",
                "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                string sql = @"DELETE FROM MucTieuTietKiem 
                               WHERE MaMucTieu = @MaMucTieu AND MaNguoiDung = @MaNguoiDung";
                SqlParameter[] p = {
                    new SqlParameter("@MaMucTieu", selected.MaMucTieu),
                    new SqlParameter("@MaNguoiDung", Session.MaNguoiDung)
                };
                DatabaseHelper.ExecuteQuery(sql, p);

                MessageBox.Show("Đã xóa thành công!");
                LoadDanhSachMucTieu();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi xóa: " + ex.Message);
            }
        }

        // ============================================================
        // NÚT CẬP NHẬT TIẾN ĐỘ
        // ============================================================
        private void btnCapNhatTienDo_Click(object sender, RoutedEventArgs e)
        {
            var selected = dgvMucTieu.SelectedItem as MucTieuRow;
            if (selected == null)
            {
                MessageBox.Show("Vui lòng chọn một mục tiêu.");
                return;
            }

            // Kiểm tra 1: Đã đạt 100% thì không cho góp
            if (selected.DaTichLuy >= selected.SoTienMucTieu)
            {
                MessageBox.Show(
                    $"Mục tiêu \"{selected.TenMucTieu}\" đã hoàn thành 100%!\n\n" +
                    $"Không thể góp thêm.",
                    "Đã hoàn thành",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            decimal conThieu = selected.SoTienMucTieu - selected.DaTichLuy;

            string input = Microsoft.VisualBasic.Interaction.InputBox(
                $"Đã tích lũy: {selected.DaTichLuy:N0} / {selected.SoTienMucTieu:N0} đ\n" +
                $"Còn thiếu:   {conThieu:N0} đ\n\n" +
                $"Nhập số tiền muốn góp thêm (tối đa {conThieu:N0} đ):",
                $"Cập nhật tiến độ: {selected.TenMucTieu}",
                "0");

            if (string.IsNullOrWhiteSpace(input)) return;

            if (!decimal.TryParse(input, out decimal soTienGop) || soTienGop <= 0)
            {
                MessageBox.Show("Số tiền không hợp lệ.",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Kiểm tra 2: Số góp không vượt số còn thiếu
            if (soTienGop > conThieu)
            {
                MessageBox.Show(
                    $"Số tiền góp ({soTienGop:N0} đ) vượt quá số tiền còn thiếu ({conThieu:N0} đ).\n\n" +
                    $"Vui lòng nhập số tiền ≤ {conThieu:N0} đ.",
                    "Vượt hạn mức",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                decimal moi = selected.DaTichLuy + soTienGop;
                string ttMoi = moi >= selected.SoTienMucTieu
                    ? "Đã hoàn thành"
                    : "Đang thực hiện";

                string sql = @"UPDATE MucTieuTietKiem 
                               SET DaTichLuy = @DaTichLuy, TrangThai = @TrangThai
                               WHERE MaMucTieu = @MaMucTieu AND MaNguoiDung = @MaNguoiDung";

                SqlParameter[] p = {
                    new SqlParameter("@DaTichLuy", moi),
                    new SqlParameter("@TrangThai", ttMoi),
                    new SqlParameter("@MaMucTieu", selected.MaMucTieu),
                    new SqlParameter("@MaNguoiDung", Session.MaNguoiDung)
                };

                DatabaseHelper.ExecuteQuery(sql, p);

                MessageBox.Show(
                    $"Đã cập nhật tiến độ!\n\n" +
                    $"Đã góp:    +{soTienGop:N0} đ\n" +
                    $"Tổng cộng: {moi:N0} / {selected.SoTienMucTieu:N0} đ\n" +
                    $"Tiến độ:   {(moi / selected.SoTienMucTieu * 100):F0}%",
                    "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadDanhSachMucTieu();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }

        // ============================================================
        // CLASS NỘI BỘ
        // ============================================================
        public class MucTieuRow
        {
            public int MaMucTieu { get; set; }
            public string TenMucTieu { get; set; }
            public decimal SoTienMucTieu { get; set; }
            public decimal DaTichLuy { get; set; }
            public DateTime? NgayHoanThanh { get; set; }
            public string TrangThai { get; set; }
            public double TienDo { get; set; }
        }
    }
}