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
using Newtonsoft.Json;
using System.Data;


namespace WpfApp 
{
    public partial class NhapGiaoDichAI : Window
    {
        // Danh sách để chứa các kết quả do AI bóc tách ra
        List<KetQuaAIModel> danhSachKetQua = new List<KetQuaAIModel>();

        public NhapGiaoDichAI()
        {
            InitializeComponent();
        }

        // 1. HÀM TÌM MÃ DANH MỤC THỰC TẾ TRONG DATABASE
        
        private int TimMaDanhMucTuDatabase(string tenDanhMucCanTim)
        {
            // Tìm danh mục của người dùng hiện tại có tên gần giống với từ khóa
            string query = $"SELECT TOP 1 MaDanhMuc FROM DanhMuc WHERE MaNguoiDung = {DangNhap.MaNguoiDungHienTai} AND TenDanhMuc LIKE N'%{tenDanhMucCanTim}%'";
            DataTable dt = DatabaseHelper.GetData(query);

            if (dt.Rows.Count > 0)
            {
                return Convert.ToInt32(dt.Rows[0]["MaDanhMuc"]);
            }

            // Nếu không tìm thấy (VD: AI nói "Mua sắm" nhưng user chưa tạo danh mục này),
            // Ta sẽ lấy đại 1 danh mục bất kỳ của user đó để tránh lỗi văng app, người dùng có thể tự sửa lại sau.
            string queryFallback = $"SELECT TOP 1 MaDanhMuc FROM DanhMuc WHERE MaNguoiDung = {DangNhap.MaNguoiDungHienTai}";
            DataTable dtFallback = DatabaseHelper.GetData(queryFallback);

            if (dtFallback.Rows.Count > 0) return Convert.ToInt32(dtFallback.Rows[0]["MaDanhMuc"]);

            return -1; // Trả về -1 nếu user hoàn toàn chưa tạo bất kỳ danh mục nào
        }

       
        // 2. NÚT PHÂN TÍCH (GỌI AI)
       
        private async void btnPhanTich_Click(object sender, RoutedEventArgs e)
        {
            string noiDungGoc = txtNoiDung.Text.Trim();
            if (string.IsNullOrEmpty(noiDungGoc) || noiDungGoc.Contains("Sáng nay uống cafe"))
            {
                MessageBox.Show("Vui lòng nhập câu lệnh thực tế của bạn!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            txtTrangThai.Visibility = Visibility.Visible;
            txtTrangThai.Text = "AI đang suy nghĩ và đọc CSDL...";
            btnPhanTich.IsEnabled = false; // Khóa nút tránh user bấm nhiều lần
            danhSachKetQua.Clear();

            try
            {
                // 1. LẤY TOÀN BỘ DANH MỤC THỰC TẾ TRONG DATABASE CỦA USER
                string queryDanhMuc = $"SELECT TenDanhMuc FROM DanhMuc WHERE MaNguoiDung = {DangNhap.MaNguoiDungHienTai}";
                DataTable dt = DatabaseHelper.GetData(queryDanhMuc);

                List<string> dsDanhMucDB = new List<string>();
                foreach (DataRow row in dt.Rows)
                {
                    dsDanhMucDB.Add(row["TenDanhMuc"].ToString());
                }
                string chuoiDanhMuc = string.Join(", ", dsDanhMucDB);

                // 2. TẠO CÂU LỆNH (PROMPT) THÔNG MINH ÉP GEMINI TRẢ VỀ DỮ LIỆU CHUẨN
                string prompt = $@"
                Bạn là trợ lý tài chính. Người dùng vừa nhập câu sau: '{noiDungGoc}'.
                Hãy trích xuất các giao dịch tài chính từ câu trên.
                Bạn BẮT BUỘC phải phân loại giao dịch vào một trong các Danh mục sau đây (không được tự bịa ra danh mục khác): [{chuoiDanhMuc}]. 
                Nếu không có danh mục nào khớp, hãy chọn danh mục gần đúng nhất.
        
                Trả về kết quả DƯỚI DẠNG MẢNG JSON theo đúng cấu trúc sau (không kèm theo bất kỳ chữ nào khác):
                [
                {{ ""DanhMuc"": ""Tên danh mục bạn chọn"", ""GiaTriSoTien"": 50000, ""GhiChu"": ""Đổ xăng"" }}
                ]";

                // 3. GỌI API  (Chờ vài giây)
                string jsonKetQua = await GroqHelper.GuiYeuCauPhanTich(prompt);

                // 4. BIẾN ĐỔI JSON THÀNH LIST KẾT QUẢ ĐỂ HIỂN THỊ
                // Sử dụng thư viện Newtonsoft.Json để ép chuỗi Text thành Object C#
                var dataTraVe = JsonConvert.DeserializeObject<List<KetQuaAIModel>>(jsonKetQua);

                if (dataTraVe != null)
                {
                    foreach (var item in dataTraVe)
                    {
                        // Tìm lại MaDanhMuc dưới Database dựa trên cái Tên mà AI vừa khớp được
                        item.MaDanhMuc = TimMaDanhMucTuDatabase(item.DanhMuc);
                        item.Ngay = DateTime.Now.ToString("yyyy-MM-dd");
                        item.SoTien = item.GiaTriSoTien.ToString("N0") + " đ";
                        danhSachKetQua.Add(item);
                    }
                }

                dgvKetQuaAI.ItemsSource = null;
                dgvKetQuaAI.ItemsSource = danhSachKetQua;
                txtTrangThai.Text = "Nhận diện thành công!";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi phân tích AI: " + ex.Message, "Lỗi");
                txtTrangThai.Text = "Phân tích thất bại.";
            }
            finally
            {
                btnPhanTich.IsEnabled = true; // Mở khóa nút
            }
        }

        
        // 3. NÚT XÁC NHẬN VÀ LƯU VÀO DATABASE
        
        private void btnXacNhanLuu_Click(object sender, RoutedEventArgs e)
        {
            if (danhSachKetQua.Count == 0)
            {
                MessageBox.Show("Chưa có dữ liệu để lưu!", "Cảnh báo");
                return;
            }

            try
            {
                int soLuongDaLuu = 0;
                string cauLenhGoc = txtNoiDung.Text.Replace("'", "''"); // Tránh lỗi dấu nháy đơn trong SQL

                // Duyệt qua từng dòng kết quả trên DataGrid để lưu xuống Database
                foreach (var item in danhSachKetQua)
                {
                    // Chú ý: Cột DuocTaoTuAI = 1 và lưu lại CauLenhAIGoc
                    string query = $@"
                        INSERT INTO GiaoDich (MaNguoiDung, MaDanhMuc, SoTien, NgayGiaoDich, PhuongThucThanhToan, GhiChu, DuocTaoTuAI, CauLenhAIGoc) 
                        VALUES (
                            {DangNhap.MaNguoiDungHienTai}, 
                            {item.MaDanhMuc}, 
                            {item.GiaTriSoTien}, 
                            '{item.Ngay}', 
                            N'Tiền mặt', 
                            N'{item.GhiChu}', 
                            1, 
                            N'{cauLenhGoc}'
                        )";

                    DatabaseHelper.ExecuteQuery(query);
                    soLuongDaLuu++;
                }

                MessageBox.Show($"Đã lưu thành công {soLuongDaLuu} giao dịch vào hệ thống!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close(); // Đóng cửa sổ, màn hình GiaoDich ở ngoài sẽ tự động refresh
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu dữ liệu AI: " + ex.Message, "Lỗi Database");
            }
        }

        // Nút Hủy
        private void btnHuy_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Các sự kiện rỗng không cần thiết
        private void txtNoiDung_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) { }
        private void dgvKetQuaAI_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { }
    }

    // ==========================================
    // CLASS MODEL ÁNH XẠ XAML
    // ==========================================
    public class KetQuaAIModel
    {
        public int MaDanhMuc { get; set; } // Ẩn, dùng để lưu DB
        public decimal GiaTriSoTien { get; set; } // Ẩn, số tiền dạng số học để lưu DB

        // 4 biến dưới đây khớp y hệt với thuộc tính Binding trong XAML của dgvKetQuaAI
        public string DanhMuc { get; set; }
        public string SoTien { get; set; }
        public string GhiChu { get; set; }
        public string Ngay { get; set; }
    }
}
