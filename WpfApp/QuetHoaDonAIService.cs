using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WpfApp
{
    public class QuetHoaDonAIService
    {
        // Nhớ dán API Key của Groq vào đây (bắt đầu bằng gsk_...)
        private static readonly string ApiKey =
            Environment.GetEnvironmentVariable("GROQ_API_KEY") ??  "";

        // Sử dụng model Vision bản 11B (chạy ổn định và không bị lỗi content string)
        private const string MODEL = "llama-3.2-11b-vision-preview";

        public static async Task<string> DocHoaDon(string imagePath)
        {
            // 1. Kiểm tra API Key và File ảnh
            if (string.IsNullOrWhiteSpace(ApiKey) || ApiKey.Contains("DÁN_API_KEY"))
            {
                throw new InvalidOperationException("Chưa cấu hình API Key của Groq.");
            }
            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException("Không tìm thấy file ảnh: " + imagePath);
            }

            // 2. Chuyển ảnh sang Base64 và lấy MimeType
            byte[] imageBytes = File.ReadAllBytes(imagePath);
            string base64Image = Convert.ToBase64String(imageBytes);

            string ext = Path.GetExtension(imagePath).ToLower();
            string mimeType = "image/jpeg"; // Mặc định là jpeg

            // ĐÃ SỬA: Dùng if-else truyền thống để không bị báo lỗi đỏ ở các bản C# cũ
            if (ext == ".png")
            {
                mimeType = "image/png";
            }
            else if (ext == ".webp")
            {
                mimeType = "image/webp";
            }
            else if (ext == ".gif")
            {
                mimeType = "image/gif";
            }

            // 3. Chuẩn bị câu lệnh (Prompt)
            string homNay = DateTime.Now.ToString("yyyy-MM-dd");
            string prompt = $@"Bạn là chuyên gia đọc hóa đơn. Hãy phân tích ảnh hóa đơn và trả về JSON với cấu trúc CHÍNH XÁC sau:{{
  ""TenCuaHang"": ""Tên cửa hàng/quán/nhà hàng"",
  ""Ngay"": ""yyyy-MM-dd"",
  ""TongTien"": 100000,
  ""DanhMucGoiY"": ""Ăn uống"",
  ""GhiChu"": ""Mô tả ngắn gọn nội dung hóa đơn""}}

QUY TẮC:
- TenCuaHang: Nếu không rõ → ghi ""Không xác định""
- Ngay: Định dạng yyyy-MM-dd. Nếu trên hóa đơn không ghi ngày → dùng ngày hôm nay ({homNay})
- TongTien: Chỉ lấy số tiền thanh toán cuối cùng (chỉ xuất số, KHÔNG kèm chữ VNĐ, KHÔNG có dấu chấm phẩy). Nếu không rõ → 0
- DanhMucGoiY: Bắt buộc chọn 1 trong các danh mục: ""Ăn uống"", ""Đi lại"", ""Mua sắm"", ""Giải trí"", ""Hóa đơn"", ""Khác""
- GhiChu: Mô tả ngắn (VD: ""Ăn tối nhà hàng"")

CHỈ TRẢ VỀ JSON, KHÔNG GIẢI THÍCH GÌ THÊM.";

            // 4. XÂY DỰNG JSON THỦ CÔNG (Đảm bảo Groq không bị nhầm lẫn kiểu dữ liệu)
            var textPart = new JObject
            {
                ["type"] = "text",
                ["text"] = prompt
            };

            var imagePart = new JObject
            {
                ["type"] = "image_url",
                ["image_url"] = new JObject
                {
                    ["url"] = $"data:{mimeType};base64,{base64Image}"
                }
            };

            var contentArray = new JArray { textPart, imagePart };

            var messageObj = new JObject
            {
                ["role"] = "user",
                ["content"] = contentArray
            };

            var requestBody = new JObject
            {
                ["model"] = MODEL,
                ["messages"] = new JArray { messageObj },
                ["temperature"] = 0.1
            };

            string jsonBody = requestBody.ToString();
            string url = "https://api.groq.com/openai/v1/chat/completions";

            // 5. Gọi API Groq
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(60);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content);
                string responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Groq API lỗi ({response.StatusCode}):\n{responseString}");
                }

                // 6. Bóc tách kết quả trả về
                var json = JObject.Parse(responseString);
                string resultText = json["choices"]?[0]?["message"]?["content"]?.ToString();

                if (string.IsNullOrWhiteSpace(resultText))
                {
                    throw new Exception("Groq không phân tích được ảnh này.");
                }

                // Xóa các ký tự thừa markdown
                return resultText.Replace("```json", "").Replace("```", "").Trim();
            }
        }
    }
}