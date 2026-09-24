using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp
{
    /// <summary>
    /// Cửa sổ Cài đặt chung.
    /// Chỉ có 1 chức năng: đổi đơn vị tiền tệ mặc định.
    /// Lưu vào cột TienTeMacDinh của bảng NguoiDung.
    /// </summary>
    public partial class CaiDatChung : Window
    {
        public CaiDatChung()
        {
            InitializeComponent();
            LoadDuLieu();
        }

        // ============================================================
        // LOAD DỮ LIỆU TỪ DATABASE
        // ============================================================
        private void LoadDuLieu()
        {
            try
            {
                string sql = @"
                    SELECT TienTeMacDinh
                    FROM NguoiDung
                    WHERE MaNguoiDung = @MaNguoiDung";

                SqlParameter[] p = {
                    new SqlParameter("@MaNguoiDung", Session.MaNguoiDung)
                };

                var dt = DatabaseHelper.GetData(sql, p);

                if (dt.Rows.Count > 0)
                {
                    string tienTe = dt.Rows[0]["TienTeMacDinh"]?.ToString()?.Trim() ?? "VND";

                    // Tìm item khớp với mã tiền tệ (VND/USD/EUR)
                    bool found = false;
                    foreach (ComboBoxItem item in cboTienTe.Items)
                    {
                        string content = item.Content.ToString();
                        if (content.StartsWith(tienTe, StringComparison.OrdinalIgnoreCase))
                        {
                            cboTienTe.SelectedItem = item;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        cboTienTe.SelectedIndex = 0;   // Fallback VND
                }
                else
                {
                    cboTienTe.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải cài đặt: " + ex.Message,
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                cboTienTe.SelectedIndex = 0;
            }
        }

        // ============================================================
        // NÚT: LƯU
        // ============================================================
        private void btnLuu_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra đã chọn chưa
            if (!(cboTienTe.SelectedItem is ComboBoxItem item))
            {
                MessageBox.Show("Vui lòng chọn đơn vị tiền tệ.",
                    "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Content dạng: "VND — Việt Nam Đồng (₫)" → lấy 3 ký tự đầu = "VND"
            string tienTe = item.Content.ToString().Substring(0, 3).ToUpper();

            try
            {
                string sql = @"
                    UPDATE NguoiDung
                    SET TienTeMacDinh = @TienTe
                    WHERE MaNguoiDung = @MaNguoiDung";

                SqlParameter[] p = {
                    new SqlParameter("@TienTe", tienTe),
                    new SqlParameter("@MaNguoiDung", Session.MaNguoiDung)
                };

                int rows = DatabaseHelper.ExecuteQuery(sql, p);

                if (rows > 0)
                {
                    // Cập nhật Session để các trang khác dùng ngay
                    Session.TienTe = tienTe;

                    MessageBox.Show(
                        $"Đã lưu cài đặt thành công!\n\n" +
                        $"Đơn vị tiền tệ: {tienTe}",
                        "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Không có dòng nào được cập nhật.",
                        "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lưu cài đặt: " + ex.Message,
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