using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace WpfApp.Helpers
{
    public class EmailService
    {
        // Điền Gmail và App Password (Mật khẩu ứng dụng 16 ký tự) của bạn vào đây
        private readonly string _fromEmail = "vdong4419@gmail.com";
        private readonly string _appPassword = "xxxx xxxx xxxx xxxx";

        public async Task<bool> SendOtpEmailAsync(string toEmail, string otpCode, string purpose)
        {
            try
            {
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(_fromEmail, _appPassword),
                    EnableSsl = true,
                };

                string subject = purpose == "Register" ? "[Smart Finance] Mã xác thực đăng ký tài khoản"
                                                      : "[Smart Finance] Mã khôi phục mật khẩu";

                string body = $@"
                    <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #ddd; border-radius: 8px;'>
                        <h2 style='color: #007ACC;'>Mã xác thực từ Smart Finance</h2>
                        <p>Mã OTP của bạn cho thao tác <b>{purpose}</b> là:</p>
                        <h1 style='color: #E91E63; letter-spacing: 5px;'>{otpCode}</h1>
                        <p>Mã này có hiệu lực trong <b>5 phút</b>. Vui lòng không chia sẻ mã này với bất kỳ ai.</p>
                    </div>";

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromEmail, "Smart Finance App"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(toEmail);

                await smtpClient.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}