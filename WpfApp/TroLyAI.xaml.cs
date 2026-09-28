using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfApp.AI;
using System.Text.RegularExpressions;
using System.Windows.Documents;
namespace WpfApp
{
    public partial class TroLyAI : Page
    {
        private readonly List<ChatTurn> _history = new List<ChatTurn>();
        private TroLyAiRepository _repository;
        private TroLyAiService _service;
        private CancellationTokenSource _lifetime;
        private CancellationTokenSource _request;
        private int _userId;
        private int _generation;
        private bool _busy;

        public TroLyAI()
        {
            InitializeComponent();
            dpThangPhanTich.SelectedDate = AiClock.Today;
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
        }

        private bool Current(int generation)
        {
            return IsLoaded && generation == _generation && _userId > 0 && Session.MaNguoiDung == _userId;
        }

        private bool CheckAccount()
        {
            if (Current(_generation)) return true;
            if (_request != null) _request.Cancel();
            chatMessages.Children.Clear();
            _history.Clear();
            txtChatInput.Clear();
            txtTrangThai.Text = "Phiên đăng nhập đã thay đổi. Hãy mở lại trang Trợ lý AI.";
            return false;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            int generation = ++_generation;
            if (_lifetime != null) { _lifetime.Cancel(); _lifetime.Dispose(); }
            _lifetime = new CancellationTokenSource();
            CancellationToken token = _lifetime.Token;
            _userId = Session.MaNguoiDung;
            _history.Clear();
            chatMessages.Children.Clear();
            txtChatInput.Clear();
            SetBusy(true);
            btnDung.IsEnabled = false;
            if (_userId <= 0)
            {
                txtTrangThai.Text = "Bạn cần đăng nhập để dùng trợ lý AI.";
                SetBusy(false);
                return;
            }
            var repository = new TroLyAiRepository(_userId);
            _repository = repository;
            _service = new TroLyAiService(repository);
            txtTrangThai.Text = "Đang tải hội thoại...";
            try
            {
                var turns = await Task.Run(() => repository.LoadHistory(), token);
                if (!Current(generation) || token.IsCancellationRequested) return;
                _history.AddRange(turns);
                foreach (var turn in turns)
                {
                    AddMessage(turn.User, true);
                    AddMessage(turn.Assistant, false);
                }
                if (turns.Count == 0)
                    AddMessage("Chào bạn! Mình có thể giúp xem thu chi, ngân sách và tiến độ tiết kiệm. Ví dụ: “Tháng này tôi chi nhiều nhất vào đâu?”", false);
                txtTrangThai.Text = TroLyAiGroqClient.HasKey
                    ? "Sẵn sàng · hiển thị tối đa 20 lượt gần nhất."
                    : "Chưa có API key. Xem hướng dẫn cấu hình MYFINANCE_GROQ_API_KEY.";
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (Current(generation)) txtTrangThai.Text = "Chưa tải được lịch sử. " + FriendlyError(ex);
            }
            finally
            {
                if (Current(generation)) { SetBusy(false); txtChatInput.Focus(); }
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            ++_generation;
            if (_lifetime != null) _lifetime.Cancel();
            if (_request != null) _request.Cancel();
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            bool available = !busy && _userId > 0 && Session.MaNguoiDung == _userId;
            btnGuiTinNhan.IsEnabled = available;
            btnPhanTich.IsEnabled = available;
            btnNhapGiaoDich.IsEnabled = available;
            btnQuetHoaDon.IsEnabled = available;
            btnGoiYThuChi.IsEnabled = available;
            btnGoiYNganSach.IsEnabled = available;
            btnGoiYMucTieu.IsEnabled = available;
            dpThangPhanTich.IsEnabled = available;
            btnDung.IsEnabled = busy && _request != null;
        }

        private async Task SendAsync(string question, DateTime? reportMonth = null)
        {
            if (_busy || !CheckAccount()) return;
            question = (question ?? "").Trim();
            if (question.Length == 0) { txtChatInput.Focus(); return; }
            if (question.Length > 2000) { txtTrangThai.Text = "Mỗi câu hỏi tối đa 2.000 ký tự."; return; }
            if (!TroLyAiGroqClient.HasKey)
            {
                txtTrangThai.Text = "Bạn cần cấu hình MYFINANCE_GROQ_API_KEY theo HUONG_DAN.md trước khi gửi.";
                return;
            }

            int generation = _generation;
            var repository = _repository;
            var service = _service;
            var context = new List<ChatTurn>(_history);
            var request = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            _request = request;
            SetBusy(true);
            if (!reportMonth.HasValue) txtChatInput.Clear();
            AddMessage(question, true);
            var progress = new Progress<string>(text => {
                if (Current(generation) && _request == request && !request.IsCancellationRequested)
                    txtTrangThai.Text = text;
            });
            try
            {
                var reply = await service.AnswerAsync(question, context, reportMonth, progress, request.Token);
                request.Token.ThrowIfCancellationRequested();
                if (!Current(generation)) return;
                AddMessage(reply.Text, false);
                _history.Add(new ChatTurn { User = question, Assistant = reply.Text });
                if (_history.Count > 20) _history.RemoveAt(0);
                btnDung.IsEnabled = false;
                txtTrangThai.Text = "Đang lưu hội thoại...";
                // Capture this repository/user. Never look up Session from inside a background operation.
                var warnings = await Task.Run(() => {
                    var list = new List<string>();
                    try { repository.SaveTurn(question, reply.Text); }
                    catch (Exception) { list.Add("Chưa lưu được lượt chat; nội dung hiện chỉ có trong phiên này."); }
                    if (reply.ReportMonth.HasValue && !String.IsNullOrWhiteSpace(reply.ReportBody))
                    {
                        try { repository.SaveReport(reply.ReportMonth.Value, reply.ReportBody); }
                        catch (Exception) { list.Add("Chưa lưu được báo cáo vào database."); }
                    }
                    return list;
                });
                if (!Current(generation)) return;
                txtTrangThai.Text = warnings.Count > 0 ? String.Join(" ", warnings)
                    : reply.ReportMonth.HasValue ? "Đã lưu hội thoại và báo cáo phân tích." : "Đã lưu hội thoại.";
            }
            catch (OperationCanceledException)
            {
                if (Current(generation))
                {
                    txtTrangThai.Text = "Đã dừng yêu cầu; lượt này chưa được lưu.";
                    if (String.IsNullOrWhiteSpace(txtChatInput.Text)) txtChatInput.Text = question;
                }
            }
            catch (Exception ex)
            {
                if (Current(generation))
                {
                    txtTrangThai.Text = FriendlyError(ex);
                    if (String.IsNullOrWhiteSpace(txtChatInput.Text)) txtChatInput.Text = question;
                }
            }
            finally
            {
                if (_request == request) _request = null;
                request.Dispose();
                if (Current(generation)) { SetBusy(false); txtChatInput.Focus(); }
            }
        }

        private static string FriendlyError(Exception ex)
        {
            if (ex is SqlException) return "Không truy cập được database. Kiểm tra LocalDB, kết nối và các bảng AI theo hướng dẫn.";
            if (ex is InvalidOperationException || ex is ArgumentException) return ex.Message;
            return "Có lỗi khi xử lý yêu cầu. Hãy thử lại hoặc kiểm tra cấu hình dự án.";
        }

        // Hiển thị một tin nhắn lên khung chat.
        private void AddMessage(string text, bool fromUser)
        {
            var noiDung = new StackPanel();

            // Nhãn nhỏ giúp phân biệt người dùng và trợ lý.
            noiDung.Children.Add(new TextBlock
            {
                Text = fromUser ? "BẠN" : "TRỢ LÝ AI",
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = fromUser
                    ? Brushes.White
                    : new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                Opacity = 0.8,
                Margin = new Thickness(0, 0, 0, 8)
            });

            if (fromUser)
            {
                // Tin nhắn người dùng hiển thị nguyên văn.
                noiDung.Children.Add(new TextBlock
                {
                    Text = text,
                    FontSize = 14,
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 23
                });
            }
            else
            {
                // Tin nhắn AI được xử lý in đậm và xuống dòng.
                noiDung.Children.Add(TaoNoiDungAI(text));
            }

            var khung = new Border
            {
                Child = noiDung,

                Padding = new Thickness(18, 14, 18, 14),
                Margin = new Thickness(0, 0, 0, 18),
                MaxWidth = 720,

                HorizontalAlignment = fromUser
                    ? HorizontalAlignment.Right
                    : HorizontalAlignment.Left,

                CornerRadius = fromUser
                    ? new CornerRadius(14, 14, 4, 14)
                    : new CornerRadius(14, 14, 14, 4),

                Background = fromUser
                    ? new SolidColorBrush(Color.FromRgb(37, 99, 235))
                    : new SolidColorBrush(Color.FromRgb(248, 250, 252)),

                BorderBrush = fromUser
                    ? Brushes.Transparent
                    : new SolidColorBrush(Color.FromRgb(226, 232, 240)),

                BorderThickness = new Thickness(1)
            };

            chatMessages.Children.Add(khung);

            // Giới hạn số tin nhắn đang hiển thị để giao diện nhẹ hơn.
            // Không xóa lịch sử trong database.
            while (chatMessages.Children.Count > 80)
            {
                chatMessages.Children.RemoveAt(0);
            }

            chatScroll.UpdateLayout();
            chatScroll.ScrollToEnd();
        }


        // Chia câu trả lời AI thành từng dòng có khoảng cách.
        private StackPanel TaoNoiDungAI(string text)
        {
            var panel = new StackPanel();

            string[] cacDong = (text ?? "")
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Split('\n');

            bool cachDoan = false;

            foreach (string dongGoc in cacDong)
            {
                string dong = dongGoc.Trim();

                if (string.IsNullOrWhiteSpace(dong))
                {
                    cachDoan = true;
                    continue;
                }

                // Nhận diện tiêu đề Markdown: #, ## hoặc ###.
                bool laTieuDe = Regex.IsMatch(dong, @"^#{1,3}\s+");

                if (laTieuDe)
                {
                    dong = Regex.Replace(dong, @"^#{1,3}\s+", "");
                }

                // Dòng chỉ chứa một đoạn **in đậm** cũng được xem là tiêu đề.
                bool caDongInDam = Regex.IsMatch(
                    dong,
                    @"^\*\*[^*]+\*\*$"
                );

                // Đổi "- nội dung" hoặc "* nội dung" thành dấu đầu dòng.
                dong = Regex.Replace(dong, @"^[-*]\s+", "• ");

                var textBlock = new TextBlock
                {
                    FontFamily = new FontFamily("Segoe UI"),

                    FontSize = laTieuDe || caDongInDam ? 15 : 14,

                    FontWeight = laTieuDe
                        ? FontWeights.SemiBold
                        : FontWeights.Normal,

                    Foreground = new SolidColorBrush(
                        Color.FromRgb(30, 41, 59)
                    ),

                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 24,

                    Margin = new Thickness(
                        0,
                        cachDoan && panel.Children.Count > 0 ? 8 : 0,
                        0,
                        5
                    )
                };

                ThemChuInDam(textBlock, dong);

                panel.Children.Add(textBlock);
                cachDoan = false;
            }

            return panel;
        }


        // Chuyển **nội dung** thành chữ in đậm thực sự.
        private void ThemChuInDam(TextBlock textBlock, string noiDung)
        {
            MatchCollection cacDoanInDam = Regex.Matches(
                noiDung,
                @"\*\*(.+?)\*\*"
            );

            int viTriHienTai = 0;

            foreach (Match match in cacDoanInDam)
            {
                // Thêm phần chữ thường trước đoạn in đậm.
                if (match.Index > viTriHienTai)
                {
                    string chuThuong = noiDung.Substring(
                        viTriHienTai,
                        match.Index - viTriHienTai
                    );

                    textBlock.Inlines.Add(new Run(chuThuong));
                }

                // Thêm phần in đậm, bỏ hai cặp dấu **.
                textBlock.Inlines.Add(
                    new Bold(new Run(match.Groups[1].Value))
                );

                viTriHienTai = match.Index + match.Length;
            }

            // Thêm phần chữ thường còn lại.
            if (viTriHienTai < noiDung.Length)
            {
                textBlock.Inlines.Add(
                    new Run(noiDung.Substring(viTriHienTai))
                );
            }
        }

        private async void btnGuiTinNhan_Click(object sender, RoutedEventArgs e)
        {
            await SendAsync(txtChatInput.Text);
        }

        private async void txtChatInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
            {
                e.Handled = true;
                await SendAsync(txtChatInput.Text);
            }
        }

        private void btnDung_Click(object sender, RoutedEventArgs e)
        {
            if (_request != null) _request.Cancel();
        }

        private async void btnPhanTich_Click(object sender, RoutedEventArgs e)
        {
            DateTime? selected = dpThangPhanTich.SelectedDate;
            if (!selected.HasValue || selected.Value.Year < 1901 || selected.Value.Year > 2100)
            {
                txtTrangThai.Text = "Hãy chọn ngày hợp lệ thuộc tháng cần phân tích (1901–2100).";
                return;
            }
            var month = new DateTime(selected.Value.Year, selected.Value.Month, 1);
            await SendAsync("Phân tích thu chi tháng " + month.ToString("MM/yyyy")
                + ", so sánh tháng trước, kiểm tra ngân sách tháng này và tiến độ tiết kiệm hiện tại. Đề xuất 3 việc cụ thể.", month);
        }

        private void btnGoiY_Click(object sender, RoutedEventArgs e)
        {
            if (_busy || !CheckAccount()) return;
            txtChatInput.Text = Convert.ToString(((Button)sender).Content);
            txtChatInput.Focus();
            txtChatInput.CaretIndex = txtChatInput.Text.Length;
        }

        private void btnQuetHoaDon_Click(object sender, RoutedEventArgs e)
        {
            if (_busy || !CheckAccount()) return;
            new QuetHoaDonAI { Owner = Window.GetWindow(this) }.ShowDialog();
        }

        private void btnNhapGiaoDich_Click(object sender, RoutedEventArgs e)
        {
            if (_busy || !CheckAccount()) return;
            new NhapGiaoDichAI { Owner = Window.GetWindow(this) }.ShowDialog();
        }
    }
}
