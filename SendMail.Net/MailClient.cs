using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;

namespace SendMail.Net
{
    public class MailClient
    {
        public MailClient()
        {
        }

        public void Send(MailMessage mailMessage)
        {
            if (mailMessage.To != null && mailMessage.To.Count > 0)
            {
                foreach (MailAddress to in mailMessage.To)
                {
                    MessageSender sender = new MessageSender(mailMessage.From,
                                                            mailMessage.To,
                                                            to,
                                                            mailMessage.CC,
                                                            mailMessage.Subject,
                                                            mailMessage.Body,
                                                            mailMessage.Attachments,
                                                            mailMessage.IsBodyHtml);
                    sender.Send();
                }
            }
        }
    }
}
