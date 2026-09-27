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

        private void AddMessage(string text, bool fromUser)
        {
            var content = new TextBox
            {
                Text = text,
                IsReadOnly = true,
                IsReadOnlyCaretVisible = false,
                TextWrapping = TextWrapping.Wrap,
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                FontSize = 14,
                Foreground = fromUser ? Brushes.White : new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            var bubble = new Border
            {
                Child = content,
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 12),
                CornerRadius = new CornerRadius(10),
                MaxWidth = 720,
                HorizontalAlignment = fromUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Background = new SolidColorBrush(fromUser ? Color.FromRgb(37, 99, 235) : Color.FromRgb(241, 245, 249))
            };
            chatMessages.Children.Add(bubble);
            // Bound the visual tree in long sessions; persisted history remains in the database.
            while (chatMessages.Children.Count > 80) chatMessages.Children.RemoveAt(0);
            chatScroll.ScrollToEnd();
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
