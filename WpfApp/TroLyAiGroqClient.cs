using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WpfApp.AI
{
    // Separate from GroqHelper used by other members of the project.
    public sealed class TroLyAiGroqClient
    {
        private static readonly HttpClient Client = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
        private const string Endpoint = "https://api.groq.com/openai/v1/chat/completions";

        public static bool HasKey
        {
            get { return !String.IsNullOrWhiteSpace(ReadKey()); }
        }

        private static string ReadKey()
        {
            // Prefer a feature-specific key, then the conventional Groq variable.
            return ReadSetting("MYFINANCE_GROQ_API_KEY") ?? ReadSetting("GROQ_API_KEY");
        }

        private static string ReadSetting(string name)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (String.IsNullOrWhiteSpace(value))
                value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);
            return String.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        public async Task<string> CompleteAsync(List<object> messages, JObject schema, CancellationToken token)
        {
            string key = ReadKey();
            if (String.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("Chưa cấu hình API key. Xem HUONG_DAN.md: biến MYFINANCE_GROQ_API_KEY.");
            string model = ReadSetting("MYFINANCE_GROQ_MODEL") ?? "openai/gpt-oss-120b";
            var body = new JObject
            {
                ["model"] = model,
                ["messages"] = JArray.FromObject(messages),
                ["temperature"] = 0.2,
                ["max_completion_tokens"] = 4096
            };
            if (model == "openai/gpt-oss-120b" || model == "openai/gpt-oss-20b")
                body["reasoning_effort"] = "low";
            if (schema != null)
                body["response_format"] = new JObject
                {
                    ["type"] = "json_schema",
                    ["json_schema"] = new JObject
                    {
                        ["name"] = "finance_query_plan",
                        ["strict"] = true,
                        ["schema"] = schema
                    }
                };

            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, Endpoint))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                    request.Content = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json");
                    using (var response = await Client.SendAsync(request, token).ConfigureAwait(false))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            // Never echo remote error bodies, request payloads or credentials into chat.
                            int status = (int)response.StatusCode;
                            if (status == 401 || status == 403)
                                throw new InvalidOperationException("Groq từ chối truy cập. Kiểm tra API key và quyền sử dụng model.");
                            if (status == 429)
                                throw new InvalidOperationException("Đã chạm giới hạn yêu cầu của Groq. Hãy chờ một lúc rồi thử lại.");
                            if (status == 400 || status == 404)
                                throw new InvalidOperationException("Groq chưa chấp nhận model hoặc định dạng yêu cầu. Kiểm tra MYFINANCE_GROQ_MODEL và khả năng hỗ trợ Structured Outputs.");
                            throw new InvalidOperationException("Dịch vụ AI đang gặp lỗi (HTTP " + status + "). Hãy thử lại sau.");
                        }
                        string raw = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        token.ThrowIfCancellationRequested();
                        JObject result = JObject.Parse(raw);
                        var choice = result["choices"]?[0];
                        if (choice == null || Convert.ToString(choice["finish_reason"]) != "stop")
                            throw new InvalidOperationException("AI chưa trả lời đầy đủ. Hãy thử câu hỏi ngắn hơn.");
                        string refusal = Convert.ToString(choice["message"]?["refusal"]);
                        if (!String.IsNullOrWhiteSpace(refusal))
                            throw new InvalidOperationException("AI chưa thể xử lý yêu cầu này. Bạn hãy diễn đạt lại.");
                        string content = Convert.ToString(choice["message"]?["content"]);
                        if (String.IsNullOrWhiteSpace(content) || content.Length > 24000)
                            throw new InvalidOperationException("Phản hồi AI trống hoặc quá dài. Hãy thu hẹp câu hỏi.");
                        return content.Trim();
                    }
                }
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                throw new InvalidOperationException("AI phản hồi quá lâu. Bạn hãy thử lại.");
            }
            catch (HttpRequestException)
            {
                throw new InvalidOperationException("Không kết nối được Groq. Hãy kiểm tra Internet.");
            }
            catch (JsonException)
            {
                throw new InvalidOperationException("Dịch vụ AI trả về dữ liệu không đọc được. Hãy thử lại.");
            }
        }
    }
}
