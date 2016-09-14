using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using SendGrid;

namespace MTDB.Core.Services.Common
{
    public class SendGridMessageService : IIdentityMessageService
    {
        public async Task SendAsync(IdentityMessage message)
        {
            var myMessage = new SendGridMessage();

            myMessage.AddTo(message.Destination);
            myMessage.From = new MailAddress("noreply@mtdb.com", "MTDB.com");
            myMessage.Subject = message.Subject;
            myMessage.Html = message.Body;

            var username = ConfigurationManager.AppSettings["smtp:Username"];
            var password = ConfigurationManager.AppSettings["smtp:Password"];
            var credentials = new NetworkCredential(username, password);
            var transportWeb = new Web(credentials);

            await transportWeb.DeliverAsync(myMessage);
        }
    }
}
