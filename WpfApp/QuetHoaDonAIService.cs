using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WpfApp
{
    /// <summary>
    /// Service gọi Gemini API để đọc hóa đơn từ ảnh.
    /// Đặt tên riêng để không trùng với các module AI khác trong nhóm.
    /// Yêu cầu: GEMINI_API_KEY trong biến môi trường.
    /// </summary>
    public class QuetHoaDonAIService
    {
        // Đọc key từ biến môi trường (KHÔNG hardcode)
        private static readonly string ApiKey =
            Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";

        // Model Gemini — flash cho nhanh + free tier hào phóng

        // BỎ "models/" — chỉ giữ tên model
        private const string MODEL = "gemini-3-flash-preview";

        /// <summary>
        /// Gửi ảnh hóa đơn lên Gemini để bóc tách.
        /// Trả về JSON string với: TenCuaHang, Ngay, TongTien, DanhMucGoiY, GhiChu
        /// </summary>
        public static async Task<string> DocHoaDon(string imagePath)
        {
            // ============================================================
            // VALIDATE
            // ============================================================
            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                throw new InvalidOperationException(
                    "Chưa cấu hình GEMINI_API_KEY.\n\n" +
                    "Hướng dẫn:\n" +
                    "1. Vào https://aistudio.google.com/app/apikey\n" +
                    "2. Tạo API key\n" +
                    "3. Windows + R → sysdm.cpl → Environment Variables\n" +
                    "4. Thêm biến GEMINI_API_KEY = <key>\n" +
                    "5. RESTART Visual Studio");
            }

            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException("Không tìm thấy file ảnh: " + imagePath);
            }

            // ============================================================
            // ĐỌC ẢNH → BASE64
            // ============================================================
            byte[] imageBytes = File.ReadAllBytes(imagePath);
            string base64Image = Convert.ToBase64String(imageBytes);

            // Xác định MIME type
            string ext = Path.GetExtension(imagePath).ToLower();
            string mimeType;

            if (ext == ".jpg" || ext == ".jpeg")
                mimeType = "image/jpeg";
            else if (ext == ".png")
                mimeType = "image/png";
            else if (ext == ".webp")
                mimeType = "image/webp";
            else if (ext == ".gif")
                mimeType = "image/gif";
            else if (ext == ".bmp")
                mimeType = "image/bmp";
            else
                mimeType = "image/jpeg";

            // ============================================================
            // PROMPT CHO AI
            // ============================================================
            string prompt = @"Bạn là chuyên gia đọc hóa đơn. Hãy phân tích ảnh hóa đơn và trả về JSON với cấu trúc CHÍNH XÁC sau:

{
  ""TenCuaHang"": ""Tên cửa hàng/quán/nhà hàng"",
  ""Ngay"": ""yyyy-MM-dd"",
  ""TongTien"": 100000,
  ""DanhMucGoiY"": ""Ăn uống"",
  ""GhiChu"": ""Mô tả ngắn gọn nội dung hóa đơn""
}

QUY TẮC:
- TenCuaHang: Tên cửa hàng. Nếu không rõ → ghi ""Không xác định""
- Ngay: Định dạng yyyy-MM-dd. Nếu không rõ → dùng ngày hôm nay
- TongTien: Số tiền (chỉ số, không đơn vị, không dấu chấm). Nếu không rõ → 0
- DanhMucGoiY: Chọn 1 trong các danh mục sau:
  * ""Ăn uống"" — nhà hàng, quán ăn, cà phê, trà sữa
  * ""Đi lại"" — xăng, taxi, grab, vé xe
  * ""Mua sắm"" — siêu thị, quần áo, đồ gia dụng
  * ""Giải trí"" — phim, karaoke, game
  * ""Hóa đơn"" — điện, nước, internet
  * ""Khác"" — không thuộc nhóm nào trên
- GhiChu: Mô tả ngắn (VD: ""Ăn tối nhà hàng"", ""Đổ xăng"")

CHỈ TRẢ VỀ JSON, KHÔNG CÓ TEXT KHÁC.";

            // ============================================================
            // TẠO REQUEST BODY
            // ============================================================
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = prompt },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = mimeType,
                                    data = base64Image
                                }
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.1,
                    responseMimeType = "application/json"
                }
            };

            string jsonBody = JsonConvert.SerializeObject(requestBody);
            string url = "https://generativelanguage.googleapis.com/v1beta/models/" + MODEL + ":generateContent?key=" + ApiKey;

            // ============================================================
            // GỌI API
            // ============================================================
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(60);

                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content);
                string responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(
                        "Gemini API lỗi (" + response.StatusCode + "):\n" + responseString);
                }

                var json = JObject.Parse(responseString);
                string resultText = json["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();

                if (string.IsNullOrWhiteSpace(resultText))
                {
                    throw new Exception("Gemini không trả về kết quả.\nResponse: " + responseString);
                }

                // Xóa markdown code block nếu AI tự thêm vào
                return resultText
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();
            }
        }
    }
}