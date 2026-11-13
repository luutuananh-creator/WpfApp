using System;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Data.SqlClient;
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
            if (string.IsNullOrEmpty(cauHoi)) return;

            ThemTinNhanLenManHinh(cauHoi, true);
            txtChatInput.Clear();
            btnGuiTinNhan.IsEnabled = false;

            try
            {
                // 1. KHAI BÁO BIẾN DỮ LIỆU ĐÃ ĐƯỢC LẤY TỪ SQL (Khắc phục lỗi gạch đỏ)
                string duLieuDB = LayDuLieuThucTeTuDB();

                // 2. GOM LỊCH SỬ CHAT GẦN ĐÂY ĐỂ AI HIỂU NGỮ CẢNH (Gom 4 tin nhắn gần nhất)
                string lichSuGanDay = "";
                int count = chatMessages.Children.Count;
                for (int i = Math.Max(0, count - 4); i < count; i++)
                {
                    if (chatMessages.Children[i] is Border b && b.Child is TextBlock tb)
                    {
                        lichSuGanDay += tb.Text + "\n";
                    }
                }

                // 3. TẠO PROMPT GỬI CHO GROQ API
                string prompt = $@"
Bạn là SmartFinance AI.
Dữ liệu tài chính tháng này:
{duLieuDB}

LỊCH SỬ HỘI THOẠI VỪA RỒI:
{lichSuGanDay}

CÂU HỎI MỚI NHẤT CỦA NGƯỜI DÙNG: ""{cauHoi}""

QUY TẮC PHẢN HỒI:
1. ĐỌC KỸ LỊCH SỬ: Nếu người dùng trả lời ngắn như 'có', 'ok', 'được' -> Hãy hiểu họ đang đồng ý với đề xuất ngay phía trên và đưa ra giải pháp NGAY LẬP TỨC.
2. KHÔNG DÙNG DẤU ** HAY MÃ MARKDOWN.
3. NGẮN GỌN & SÚC TÍCH: Chỉ đưa ra tối đa 3-4 ý chính, mỗi ý không quá 2 dòng.
4. NẾU TƯ VẤN MỤC TIÊU LỚN (Như Mua xe, Mua nhà): Hỏi nhẹ nhàng ngân sách dự kiến của người dùng thay vì tự bịa ra con số.";

                // 4. HIỂN THỊ TRẠNG THÁI AI ĐANG XỬ LÝ
                ThemTinNhanLenManHinh("⏳ AI đang suy nghĩ...", false);

                // 5. GỬI YÊU CẦU SANG GROQ
                string cauTraLoiAI = await AIPhanTich.GuiYeuCau(prompt);

                // Xóa tin nhắn chờ
                if (chatMessages.Children.Count > 0)
                {
                    chatMessages.Children.RemoveAt(chatMessages.Children.Count - 1);
                }

                // Hiển thị câu trả lời chính thức
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
            // 1. Tạo TextBlock chứa nội dung tin nhắn
            var txt = new TextBlock
            {
                Text = noiDung,
                TextWrapping = TextWrapping.Wrap,
                Foreground = laNguoiDung ? Brushes.White : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
                FontSize = 13.5,
                LineHeight = 20, // Giúp các dòng chữ thoáng, dễ đọc hơn
                FontFamily = new FontFamily("Segoe UI")
            };

            // 2. Tạo Border bọc tin nhắn (Bong bóng chat)
            var border = new Border
            {
                Background = laNguoiDung
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB")) // Xanh dương hiện đại cho User
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")), // Xám nhạt cao cấp cho AI
                Padding = new Thickness(14, 10, 14, 10),
                CornerRadius = laNguoiDung
                    ? new CornerRadius(16, 16, 2, 16) // Bo góc kiểu tin nhắn Messenger/Telegram
                    : new CornerRadius(16, 16, 16, 2),
                HorizontalAlignment = laNguoiDung ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 420, // Nới rộng độ rộng khung chat
                Margin = new Thickness(0, 0, 0, 12)
            };

            // 3. Thêm hiệu ứng đổ bóng nhẹ cho bong bóng chat của AI
            if (!laNguoiDung)
            {
                border.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 270,
                    ShadowDepth = 1,
                    Opacity = 0.08,
                    BlurRadius = 4
                };
            }

            border.Child = txt;

            // 4. Thêm vào giao diện và tự động cuộn xuống dưới cùng
            chatMessages.Children.Add(border);
            if (chatScroll != null)
            {
                chatScroll.UpdateLayout();
                chatScroll.ScrollToEnd();
            }
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