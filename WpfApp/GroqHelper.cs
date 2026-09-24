using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace WpfApp 
{
    public class GroqHelper
    {
        
        private static readonly string ApiKey = "";
        private static readonly string ApiUrl = "https://api.groq.com/openai/v1/chat/completions";

        public static async Task<string> GuiYeuCauPhanTich(string prompt)
        {
            using (HttpClient client = new HttpClient())
            {
                // Groq yêu cầu xác thực qua Header (Bearer Token)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

                // Đóng gói dữ liệu theo chuẩn OpenAI/Groq
                var requestBody = new
                {
                    model = "openai/gpt-oss-120b", // Sử dụng Llama 3 70B (Model thông minh nhất của Groq)
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.1 // Để thấp giúp AI tập trung trả về JSON chuẩn xác, ít bịa chữ
                };

                string jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                // Bắn API
                HttpResponseMessage response = await client.PostAsync(ApiUrl, content);
                string responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Giải mã kết quả của Groq
                    JObject json = JObject.Parse(responseString);
                    string resultText = json["choices"]?[0]?["message"]?["content"]?.ToString();

                    // Loại bỏ các ký hiệu markdown code block nếu AI cố tình chèn vào
                    return resultText.Replace("```json", "").Replace("```", "").Trim();
                }
                else
                {
                    throw new Exception("Lỗi gọi API Groq: " + responseString);
                }
            }
        }
    }
}