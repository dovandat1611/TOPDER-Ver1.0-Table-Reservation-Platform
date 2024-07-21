using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Text.Unicode;
using System.Threading.Tasks;
using Humanizer;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Org.BouncyCastle.Utilities;
using Topder.Models;
using static System.Net.Mime.MediaTypeNames;

namespace PROJECT_PRN221.Utils
{
    public class Mail
    {
        public class MailSettings
        {
            public string Mail { get; set; }
            public string DisplayName { get; set; }
            public string Password { get; set; }
            public string Host { get; set; }
            public int Port { get; set; }

        }

        public interface IEmailSender
        {
            Task SendEmailAsync(string email, string subject, string htmlMessage);
        }

        public class SendMailService : IEmailSender
        {
            private readonly MailSettings mailSettings;

            private readonly ILogger<SendMailService> logger;

            public SendMailService(IOptions<MailSettings> _mailSettings, ILogger<SendMailService> _logger)
            {
                mailSettings = _mailSettings.Value;
                logger = _logger;
                logger.LogInformation("Create SendMailService");
            }

            public async Task SendEmailAsync(string email, string subject, string htmlMessage)
            {
                var message = new MimeMessage();
                message.Sender = new MailboxAddress(mailSettings.DisplayName, mailSettings.Mail);
                message.From.Add(new MailboxAddress(mailSettings.DisplayName, mailSettings.Mail));
                message.To.Add(MailboxAddress.Parse(email));
                message.Subject = subject;

                var builder = new BodyBuilder();

                string image = "";
                if (subject.Equals("OTP"))
                {
                    image = "https://sp-ao.shortpixel.ai/client/to_auto,q_glossy,ret_img,w_768,h_570/https://vbee.vn/blog/wp-content/uploads/2020/10/cong-nghe-xac-thuc-sms-otp-1-768x570.jpg";

                }
                if(subject.Equals("Contact"))
                {
                    image = "https://blog.vantagecircle.com/content/images/2023/05/trust-in-the-workplace.png";

                }

                if (subject.Equals("Register"))
                {
                    builder.HtmlBody = $@"<!DOCTYPE html>
                        <html lang=""en"">
                        <head>
                            <meta charset=""UTF-8"">
                            <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
                            <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                            <title>Topder</title>
                        </head>
                        <body>
                            <div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;"">
                                <h2>{subject}</h2>
                                <hr>
                                <p>{htmlMessage}</p>
                                <hr>
                                <p>Regards,<br>Topder</p>
                            </div>
                        </body>
                        </html>";
                }

                if (subject.Equals("OTP") || subject.Equals("Contact"))
                {
                    builder.HtmlBody = $@"<!DOCTYPE html>
                        <html lang=""en"">
                        <head>
                            <meta charset=""UTF-8"">
                            <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
                            <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                            <title>Topder</title>
                        </head>
                        <body>
                            <div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;"">
                                <h2>{subject}</h2>
                                <hr>
                                <p>{htmlMessage}</p>
                                <img src=""{image}"" alt=""Image"" style=""max-width: 100%;"">
                                <hr>
                                <p>Regards,<br>Topder</p>
                            </div>
                        </body>
                        </html>";
                }

                if (subject.Equals("UpdateOrderStatusRestaurant"))
                {
                    int orderID = Convert.ToInt32(htmlMessage);
                    using (TopderContext context = new TopderContext())
                    {
                        Order order = context.Orders.Include(x => x.Restaurant).FirstOrDefault(o => o.OrderId == orderID);

                        string htmlBody = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset=""UTF-8"">
            <title>Cập Nhật Trạng Thái Đơn Hàng</title>
            <style>
                body {{
                    font-family: Arial, sans-serif;
                    background-color: #f4f4f4;
                    margin: 0;
                    padding: 0;
                }}
                .container {{
                    max-width: 600px;
                    margin: 0 auto;
                    padding: 20px;
                    background-color: white;
                    border-radius: 5px;
                    box-shadow: 0 0 10px rgba(0,0,0,0.1);
                }}
                h1 {{
                    color: #333;
                    text-align: center;
                }}
                .order-info {{
                    margin-top: 30px;
                }}
                .order-info p {{
                    margin: 10px 0;
                }}
                .order-image {{
                    text-align: center;
                    margin-top: 20px;
                }}
                .order-image img {{
                    max-width: 100%;
                    height: auto;
                }}
            </style>
        </head>
        <body>
            <div class=""container"">
                <h1>Cập Nhật Trạng Thái Đơn Hàng #{order.OrderId}</h1>
                <div class=""order-info"">
                    <p>Kính gửi {order.Restaurant.NameRes},</p>
                    <p>Chúng tôi muốn cung cấp cho bạn thông tin cập nhật về đơn hàng của nhà hàng bạn.</p>
                    <p>Mã Đơn Hàng: #{order.OrderId}</p>
                    <p>Đơn hàng của quý khách hiện đang ở giai đoạn <strong>Hủy</strong>.</p>
                    <p>Trân trọng,</p>
                    <p>Topder</p>
                </div>
                <div class=""order-image"">
                    <img src=""https://outviocmsassets.s3.eu-central-1.amazonaws.com/cl3oal8w6000c79570tgf4k1k.jpg""/>
                </div>
            </div>
        </body>
        </html>";
                        builder.HtmlBody = htmlBody;
                    }
                }

                if (subject.Equals("UpdateOrderStatus"))
                {
                    int orderID = Convert.ToInt32(htmlMessage);
                    using (TopderContext context = new TopderContext())
                    {
                        Order order = context.Orders.Include(x => x.Restaurant).FirstOrDefault(o => o.OrderId == orderID);
                        string statusText = order.Statusorder switch
                        {
                            "Wait" => "Đang Chờ",
                            "Accept" => "Chấp Nhận",
                            "Process" => "Đã Nhận Bàn",
                            "Done" => "Hoàn Thành",
                            "Cancel" => "Hủy",
                            _ => ""
                        };

                        string htmlBody = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset=""UTF-8"">
            <title>Cập Nhật Trạng Thái Đơn Hàng</title>
            <style>
                body {{
                    font-family: Arial, sans-serif;
                    background-color: #f4f4f4;
                    margin: 0;
                    padding: 0;
                }}
                .container {{
                    max-width: 600px;
                    margin: 0 auto;
                    padding: 20px;
                    background-color: white;
                    border-radius: 5px;
                    box-shadow: 0 0 10px rgba(0,0,0,0.1);
                }}
                h1 {{
                    color: #333;
                    text-align: center;
                }}
                .order-info {{
                    margin-top: 30px;
                }}
                .order-info p {{
                    margin: 10px 0;
                }}
                .order-image {{
                    text-align: center;
                    margin-top: 20px;
                }}
                .order-image img {{
                    max-width: 100%;
                    height: auto;
                }}
            </style>
        </head>
        <body>
            <div class=""container"">
                <h1>Cập Nhật Trạng Thái Đơn Hàng</h1>
                <div class=""order-info"">
                    <p>Kính gửi Quý Khách,</p>
                    <p>Chúng tôi muốn cung cấp cho quý khách thông tin cập nhật về đơn hàng của quý khách.</p>
                    <p>Mã Đơn Hàng: #{order.OrderId}</p>
                    <p>Đơn hàng của quý khách hiện đang ở giai đoạn <strong>{statusText}</strong>.</p>
                    <p>Cảm ơn quý khách đã kiên nhẫn và ủng hộ chúng tôi.</p>
                    <p>Trân trọng,</p>
                    <p>Topder</p>
                </div>
                <div class=""order-image"">
                    <img src=""https://outviocmsassets.s3.eu-central-1.amazonaws.com/cl3oal8w6000c79570tgf4k1k.jpg"" alt=""Hình Ảnh Sản Phẩm"">
                </div>
            </div>
        </body>
        </html>";
                        builder.HtmlBody = htmlBody;
                    }
                }


                if (subject.Equals("Order"))
                {
                    int orderID = Convert.ToInt32(htmlMessage);
                    TopderContext context = new TopderContext();
                    Order order = context.Orders.Include(x => x.Restaurant).FirstOrDefault(o => o.OrderId == orderID);

                    builder.HtmlBody = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <title>Order Confirmation</title>
            <style>
                body {{
                    font-family: Arial, sans-serif;
                    margin: 0;
                    padding: 20px;
                }}

                h1 {{
                    color: #333;
                    font-size: 24px;
                }}

                table {{
                    width: 100%;
                    border-collapse: collapse;
                    margin-top: 20px;
                }}

                th, td {{
                    padding: 10px;
                    border-bottom: 1px solid #ddd;
                }}

                th {{
                    background-color: #f5f5f5;
                }}

                img {{
                    max-width: 100px;
                    height: auto;
                }}
            </style>
        </head>
        <body>
            <h1>Thông Tin Đặt Bàn</h1>
            <img src=""https://outviocmsassets.s3.eu-central-1.amazonaws.com/cl3oal8w6000c79570tgf4k1k.jpg"" alt=""Image"" style=""max-width: 50%;"">
            <table>
                <thead>
                    <tr>
                        <th>Mã Đặt Hàng</th>
                        <th>Nhà Hàng</th>
                        <th>Ngày</th>
                        <th>Thời Gian</th>
                        <th>Số Người Lớn</th>
                        <th>Số Trẻ Em</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td>{order.OrderId}</td>
                        <td>{order.Restaurant.NameRes}</td>
                        <td>{order.Date}</td>
                        <td>{order.Time}</td>
                        <td>{order.NumberPerson}</td>
                        <td>{order.NumberChild}</td>
                    </tr>
                </tbody>
            </table>
            <p>
                Cảm ơn bạn đã đặt hàng! Nếu bạn có bất kỳ câu hỏi nào, hãy thoải mái liên hệ với chúng tôi.
            </p>
        </body>
        </html>";
                }
                if (subject.Equals("Active Account"))
                {
                    builder.HtmlBody = $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Chào Mừng Đến với Topder!</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            background-color: #f4f4f4;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 600px;
            margin: 20px auto;
            padding: 20px;
            background-color: #fff;
            border-radius: 5px;
            box-shadow: 0 0 10px rgba(0,0,0,0.1);
        }}
        h1 {{
            color: #333;
            text-align: center;
        }}
        p {{
            margin: 10px 0;
        }}
        .button {{
            display: inline-block;
            padding: 10px 20px;
            background-color: #007bff;
            color: #fff;
            text-decoration: none;
            border-radius: 5px;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <h1>Chào Mừng Đến với Topder!</h1>
        <p>Kính gửi {htmlMessage},</p>
        <p>Chúng tôi xin chân thành cảm ơn bạn đã đăng ký tài khoản của mình trên Topder!</p>
        <p>Tài khoản của bạn đã được kích hoạt thành công. Bây giờ, bạn có thể bắt đầu tận hưởng các lợi ích của việc sử dụng nền tảng Topder để quản lý và quảng bá nhà hàng của mình.</p>
        <p>Nếu bạn cần bất kỳ hỗ trợ hoặc có câu hỏi nào, đừng ngần ngại liên hệ với chúng tôi qua email hoặc số điện thoại được cung cấp dưới đây.</p>
        <p>Cảm ơn bạn đã lựa chọn Topder!</p>
        <p>Trân trọng,</p>
        <p>Topder</p>
        <p>Email: topder.vn@gmail.com</p>
        <p>Điện thoại: 0828 290 092 hoặc 0931 589 123</p>
    </div>
</body>
</html>
";
                }

                    if (subject.Equals("New Order"))
                {
                    int orderID = Convert.ToInt32(htmlMessage);
                    TopderContext context = new TopderContext();
                    Order order = context.Orders.Include(x => x.Restaurant).FirstOrDefault(o => o.OrderId == orderID);

                    builder.HtmlBody = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <title>Order Confirmation</title>
            <style>
                body {{
                    font-family: Arial, sans-serif;
                    margin: 0;
                    padding: 20px;
                }}

                h1 {{
                    color: #333;
                    font-size: 24px;
                }}

                table {{
                    width: 100%;
                    border-collapse: collapse;
                    margin-top: 20px;
                }}

                th, td {{
                    padding: 10px;
                    border-bottom: 1px solid #ddd;
                }}

                th {{
                    background-color: #f5f5f5;
                }}

                img {{
                    max-width: 100px;
                    height: auto;
                }}
            </style>
        </head>
        <body>
            <h1>Bạn Có Order Mới!</h1>
            <img src=""https://outviocmsassets.s3.eu-central-1.amazonaws.com/cl3oal8w6000c79570tgf4k1k.jpg"" alt=""Image"" style=""max-width: 50%;"">
            <table>
                <thead>
                    <tr>
                        <th>Mã Đặt Hàng</th>
                        <th>Nhà Hàng</th>
                        <th>Ngày</th>
                        <th>Thời Gian</th>
                        <th>Số Người Lớn</th>
                        <th>Số Trẻ Em</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td>{order.OrderId}</td>
                        <td>{order.Restaurant.NameRes}</td>
                        <td>{order.Date}</td>
                        <td>{order.Time}</td>
                        <td>{order.NumberPerson}</td>
                        <td>{order.NumberChild}</td>
                    </tr>
                </tbody>
            </table>
            <p>
                Hãy vào Topder để xem chi tiết đơn hàng của nhà hàng mình hơn!
            </p>
        </body>
        </html>";
                }

                message.Body = builder.ToMessageBody();

                using var smtp = new MailKit.Net.Smtp.SmtpClient();

                try
                {
                    smtp.Connect(mailSettings.Host, mailSettings.Port, SecureSocketOptions.StartTls);
                    smtp.Authenticate(mailSettings.Mail, mailSettings.Password);
                    await smtp.SendAsync(message);
                }
                catch (Exception ex)
                {
                    System.IO.Directory.CreateDirectory("mailssave");
                    var emailsavefile = string.Format(@"mailssave/{0}.eml", Guid.NewGuid());
                    await message.WriteToAsync(emailsavefile);

                    logger.LogInformation("Lỗi gửi mail, lưu tại - " + emailsavefile);
                    logger.LogError(ex.Message);
                }

                smtp.Disconnect(true);

                logger.LogInformation("send mail to: " + email);
            }
        }
    }
}
