using System;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfApp
{
    public partial class TroLyAI : Page
    {
        public TroLyAI()
        {
            InitializeComponent();
        }

        // 1. LẤY DỮ LIỆU & HÀNH VI CHI TIÊU TỪ DB
        
        private string LayDuLieuThucTeTuDB()
        {
            try
            {
                int thang = DateTime.Now.Month;
                int nam = DateTime.Now.Year;

                // Lấy Tổng Thu, Tổng Chi
                string queryTong = $@"
                    SELECT 
                        ISNULL(SUM(CASE WHEN dm.LoaiDanhMuc = N'Thu nhập' THEN gd.SoTien ELSE 0 END), 0) AS TongThu,
                        ISNULL(SUM(CASE WHEN dm.LoaiDanhMuc = N'Chi tiêu' THEN gd.SoTien ELSE 0 END), 0) AS TongChi
                    FROM GiaoDich gd
                    INNER JOIN DanhMuc dm ON gd.MaDanhMuc = dm.MaDanhMuc
                    WHERE gd.MaNguoiDung = {DangNhap.MaNguoiDungHienTai}
                      AND MONTH(gd.NgayGiaoDich) = {thang} AND YEAR(gd.NgayGiaoDich) = {nam}";

                // Lấy Top 3 Danh mục tiêu xài nhiều nhất để AI phân tích hành vi
                string queryHanhVi = $@"
                    SELECT TOP 3 dm.TenDanhMuc, SUM(gd.SoTien) AS TongTien
                    FROM GiaoDich gd
                    INNER JOIN DanhMuc dm ON gd.MaDanhMuc = dm.MaDanhMuc
                    WHERE gd.MaNguoiDung = {DangNhap.MaNguoiDungHienTai}
                      AND MONTH(gd.NgayGiaoDich) = {thang} AND YEAR(gd.NgayGiaoDich) = {nam}
                      AND dm.LoaiDanhMuc = N'Chi tiêu'
                    GROUP BY dm.TenDanhMuc
                    ORDER BY TongTien DESC";

                DataTable dtTong = DatabaseHelper.GetData(queryTong);
                DataTable dtHanhVi = DatabaseHelper.GetData(queryHanhVi);

                if (dtTong != null && dtTong.Rows.Count > 0)
                {
                    decimal tongThu = Convert.ToDecimal(dtTong.Rows[0]["TongThu"]);
                    decimal tongChi = Convert.ToDecimal(dtTong.Rows[0]["TongChi"]);
                    decimal soDu = tongThu - tongChi;

                    string duLieu = $"- Tổng thu nhập tháng {thang}: {tongThu:N0} đ\n" +
                                    $"- Tổng chi tiêu tháng {thang}: {tongChi:N0} đ\n" +
                                    $"- Số dư hiện tại: {soDu:N0} đ\n\n" +
                                    $"Thói quen chi tiêu cao nhất tháng này:\n";

                    if (dtHanhVi != null && dtHanhVi.Rows.Count > 0)
                    {
                        foreach (DataRow row in dtHanhVi.Rows)
                        {
                            duLieu += $"- {row["TenDanhMuc"]}: {Convert.ToDecimal(row["TongTien"]):N0} đ\n";
                        }
                    }
                    else
                    {
                        duLieu += "- Chưa có khoản chi tiêu nào đáng kể.\n";
                    }

                    return duLieu;
                }
                return "Chưa có dữ liệu giao dịch trong tháng này.";
            }
            catch
            {
                return "Hệ thống đang lỗi, không thể đọc dữ liệu tài chính.";
            }
        }

        
        // 2. SỰ KIỆN: NÚT GỬI TIN NHẮN
        
        private async void btnGuiTinNhan_Click(object sender, RoutedEventArgs e)
        {
            string cauHoi = txtChatInput.Text.Trim();
            if (string.IsNullOrEmpty(cauHoi) || cauHoi == "Nhập yêu cầu của bạn vào đây...") return;

            ThemTinNhanLenManHinh(cauHoi, true);
            txtChatInput.Clear();
            btnGuiTinNhan.IsEnabled = false;

            try
            {
                string duLieuDB = LayDuLieuThucTeTuDB();

                //  THÔNG MINH, LINH HOẠT VÀ HIỂU ĐÚNG NGỮ CẢNH
                string prompt = $@"
Bạn là trợ lý tài chính cá nhân xuất sắc. Đây là hồ sơ tài chính tháng này của tôi:
{duLieuDB}

QUY TẮC PHÂN TÍCH VÀ TƯ VẤN:
1. NẾU TÔI NHỜ TƯ VẤN (VD: Lên thực đơn, kế hoạch đi chơi, mẹo tiết kiệm): Hãy thoải mái sáng tạo, tự do lên danh sách, bảng chi tiết cụ thể hoặc đưa ra các lựa chọn giả định cho tôi. Bạn được phép chia nhỏ số tiền để gợi ý.
2. NẾU TÔI HỎI VỀ SỐ LIỆU ĐÃ LƯU: CHỈ dùng dữ liệu tôi cung cấp ở trên. Nếu tôi hỏi lịch sử chi tiết (VD: 'hôm qua mua gì', 'chi tiết tiền đi chợ') mà dữ liệu trên không có, HÃY ĐÁP: 'Hệ thống hiện tại chỉ tổng hợp theo danh mục, chưa xem được chi tiết từng món hàng'.
3. PHÂN TÍCH HÀNH VI: Dựa vào 'Thói quen chi tiêu cao nhất', hãy linh hoạt đưa ra lời khuyên (VD: Thấy tiêu nhiều tiền Ăn uống thì khuyên nấu ăn ở nhà).
4. Không dùng định dạng bảng Markdown (|||), hãy dùng các gạch đầu dòng hoặc đánh số thứ tự để liệt kê chi tiết cho dễ nhìn trên ứng dụng.

Yêu cầu/Câu hỏi của tôi: {cauHoi}";

                // Gọi File GroqHelper 
                string cauTraLoiAI = await AIPhanTich.GuiYeuCau(prompt);

                ThemTinNhanLenManHinh(cauTraLoiAI, false);
            }
            catch (Exception ex)
            {
                ThemTinNhanLenManHinh("Lỗi kết nối AI: " + ex.Message, false);
            }
            finally
            {
                btnGuiTinNhan.IsEnabled = true;
                txtChatInput.Focus();
            }
        }

     
        // 3. HÀM VẼ BONG BÓNG CHAT VÀ CÁC SỰ KIỆN NÚT BẤM
        
        private void ThemTinNhanLenManHinh(string noiDung, bool laNguoiDung)
        {
            var txt = new TextBlock
            {
                Text = noiDung,
                TextWrapping = TextWrapping.Wrap,
                Foreground = laNguoiDung ? Brushes.White : Brushes.Black,
                FontSize = 14
            };

            var border = new Border
            {
                Background = laNguoiDung ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"))
                                         : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")),
                Padding = new Thickness(15),
                CornerRadius = laNguoiDung ? new CornerRadius(8, 8, 0, 8) : new CornerRadius(8, 8, 8, 0),
                HorizontalAlignment = laNguoiDung ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 350,
                Margin = new Thickness(0, 0, 0, 15),
                Child = txt
            };

            chatMessages.Children.Add(border);
            if (chatScroll != null) chatScroll.ScrollToEnd();
        }

        private void txtChatInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
            {
                e.Handled = true;
                btnGuiTinNhan_Click(null, null);
            }
        }

        private void btnGoiY_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null)
            {
                txtChatInput.Text = btn.Content.ToString();
                txtChatInput.Focus();
                txtChatInput.CaretIndex = txtChatInput.Text.Length;
            }
        }

        private void btnDung_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Groq phản hồi rất nhanh nên tính năng Dừng không còn cần thiết.", "Thông báo");
        }

        private void btnQuetHoaDon_Click(object sender, RoutedEventArgs e)
        {
            QuetHoaDonAI win = new QuetHoaDonAI();
            win.ShowDialog();
        }

        private void btnNhapGiaoDich_Click(object sender, RoutedEventArgs e)
        {
            NhapGiaoDichAI win = new NhapGiaoDichAI();
            win.ShowDialog();
        }

        private void btnPhanTich_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Tính năng Phân tích chi tiết đang hoàn thiện.");
        }
    }
}