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

        private string LayDuLieuThucTeTuDB()
        {
            try
            {
                int thang = DateTime.Now.Month;
                int nam = DateTime.Now.Year;

                string query = $@"
                    SELECT 
                        ISNULL(SUM(CASE WHEN dm.LoaiDanhMuc = N'Thu nhập' THEN gd.SoTien ELSE 0 END), 0) AS TongThu,
                        ISNULL(SUM(CASE WHEN dm.LoaiDanhMuc = N'Chi tiêu' THEN gd.SoTien ELSE 0 END), 0) AS TongChi
                    FROM GiaoDich gd
                    INNER JOIN DanhMuc dm ON gd.MaDanhMuc = dm.MaDanhMuc
                    WHERE gd.MaNguoiDung = {DangNhap.MaNguoiDungHienTai}
                      AND MONTH(gd.NgayGiaoDich) = {thang} AND YEAR(gd.NgayGiaoDich) = {nam}";

                DataTable dt = DatabaseHelper.GetData(query);

                if (dt != null && dt.Rows.Count > 0)
                {
                    decimal tongThu = Convert.ToDecimal(dt.Rows[0]["TongThu"]);
                    decimal tongChi = Convert.ToDecimal(dt.Rows[0]["TongChi"]);
                    decimal soDu = tongThu - tongChi;

                    return $"- Tổng thu nhập tháng {thang}/{nam}: {tongThu:N0} đ\n" +
                           $"- Tổng chi tiêu tháng {thang}/{nam}: {tongChi:N0} đ\n" +
                           $"- Số dư hiện tại: {soDu:N0} đ\n";
                }
                return "Chưa có dữ liệu giao dịch trong tháng này.";
            }
            catch
            {
                return "Hệ thống đang lỗi, không thể đọc dữ liệu tài chính.";
            }
        }

        
        // SỰ KIỆN: NÚT GỬI TIN NHẮN
        
        private async void btnGuiTinNhan_Click(object sender, RoutedEventArgs e)
        {
            string cauHoi = txtChatInput.Text.Trim();
            if (string.IsNullOrEmpty(cauHoi) || cauHoi == "Nhập yêu cầu của bạn vào đây...") return;

            // 1. In câu hỏi của người dùng lên màn hình
            ThemTinNhanLenManHinh(cauHoi, true);
            txtChatInput.Clear();
            btnGuiTinNhan.IsEnabled = false; // Khóa nút lúc chờ AI phản hồi

            try
            {
                // 2. Lấy dữ liệu thật từ SQL
                string duLieuDB = LayDuLieuThucTeTuDB();

                // 3. Ghép thành câu lệnh (Prompt) siêu chặt chẽ
                string prompt = $@"
                Bạn là trợ lý tài chính cá nhân. Dưới đây là số liệu tài chính tháng này của tôi:
                {duLieuDB}

                QUY TẮC CỦA BẠN:
                1. Nếu tôi hỏi về số tiền, thu chi, ngân sách: CHỈ ĐƯỢC DÙNG dữ liệu ở trên để trả lời. TUYỆT ĐỐI KHÔNG tự bịa ra số liệu. Nếu dữ liệu trên không có (ví dụ hỏi chi tiết tiền đi chợ), hãy nói: 'Hệ thống hiện tại chỉ có tổng thu chi, tôi chưa xem được chi tiết khoản này'.
                2. Nếu tôi hỏi các kiến thức ngoài lề (cách tiết kiệm, lạm phát, chứng khoán...): Hãy trả lời bình thường bằng kiến thức của bạn.
                3. Trả lời ngắn gọn, thân thiện bằng tiếng Việt. Không dùng định dạng bảng Markdown.

                Câu hỏi của tôi: {cauHoi}";

                // 4. Gọi File AIPhanTich để liên kết với Groq AI
                string cauTraLoiAI = await AIPhanTich.GuiYeuCau(prompt);

                // 5. In câu trả lời lên màn hình
                ThemTinNhanLenManHinh(cauTraLoiAI, false);
            }
            catch (Exception ex)
            {
                ThemTinNhanLenManHinh("Lỗi kết nối AI: " + ex.Message, false);
            }
            finally
            {
                btnGuiTinNhan.IsEnabled = true; // Mở khóa nút
                txtChatInput.Focus(); // Tự động focus lại vào ô nhập chữ
            }
        }

        
        // HÀM: VẼ BONG BÓNG CHAT
        
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
            if (chatScroll != null) chatScroll.ScrollToEnd(); // Tự cuộn xuống dưới
        }

        

        // 1. Nhấn Enter để gửi (thay vì phải click chuột vào nút Gửi)
        private void txtChatInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
            {
                e.Handled = true;
                btnGuiTinNhan_Click(null, null);
            }
        }

        // 2. Nhấn vào các nút Gợi ý câu hỏi
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

        // 3. Nhấn vào nút Dừng
        private void btnDung_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Gemini phản hồi rất nhanh nên tính năng Dừng không còn cần thiết.", "Thông báo");
        }

        
        // CÁC NÚT LIÊN KẾT GIAO DIỆN KHÁC
        
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