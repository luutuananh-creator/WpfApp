using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WpfApp
{
    public class AIPhanTich
    {
        // Nhớ dán API Key của Groq vào đây (bắt đầu bằng gsk_...)
        private static readonly string ApiKey = "";
        private static readonly string ApiUrl = "https://api.groq.com/openai/v1/chat/completions";
        private const string MODEL = "openai/gpt-oss-120b";

        public static async Task<string> GuiYeuCau(string prompt)
        {
            using (HttpClient client = new HttpClient())
            {
                // Groq bắt buộc truyền API Key qua Header
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

                var requestBody = new
                {
                    model = MODEL,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.2 // Giảm sự "sáng tạo", ép AI nói thật dựa trên dữ liệu
                };

                string jsonBody = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(ApiUrl, content);
                string responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    JObject json = JObject.Parse(responseString);
                    string resultText = json["choices"]?[0]?["message"]?["content"]?.ToString();
                    return resultText?.Replace("```json", "").Replace("```", "").Trim();
                }
                else
                {
                    throw new Exception("Lỗi liên kết Groq API: " + responseString);
                }
            }
        }
    }
}