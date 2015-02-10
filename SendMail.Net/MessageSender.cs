using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SendMail.Net
{
    public class MessageSender
    {
        private MailAddress _From = null;
        private MailAddressCollection _To = null;
        private MailAddress _RealTo = null;
        private MailAddressCollection _CC = null;
        private string _Subject = string.Empty;
        private string _Body = string.Empty;
        private AttachmentCollection _Attachments = null;
        private bool _IsHtmlBody = false;
        private byte[] _Buffer = new byte[4096];

        public MessageSender(MailAddress from,
            MailAddressCollection to,
            MailAddress realTo,
            MailAddressCollection cc,
            string subject,
            string body,
            AttachmentCollection attachments,
            bool isHtmlBody)
        {
            this._From = from;
            this._RealTo = realTo;
            this._To = to;
            this._CC = cc;
            this._Subject = subject;
            this._Body = body;
            this._Attachments = attachments;
            this._IsHtmlBody = isHtmlBody;
        }

        public bool Send()
        {
            bool result = false;

            //解析MX服务器地址
            string to = this._RealTo.Address;
            string toDomain = to.Substring(to.IndexOf('@') + 1);
            string[] mxDomains = DnsMx.GetMXRecords(toDomain);
            string mxDomain = mxDomains[0];
            IPHostEntry ip = Dns.GetHostEntry(IPAddress.Parse(mxDomain));
            IPEndPoint ipe = new IPEndPoint(ip.AddressList[0], 25);

            //连接服务器
            using (Socket socket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Connect(ipe);

                //连接
                if (this.ReceiveData(socket) != ESMTPResponse.CONNECT_SUCCESS)
                    return result;
                this.SendData(socket, string.Format("HELO {0}\r\n", Dns.GetHostName()));
                if (this.ReceiveData(socket) != ESMTPResponse.GENERIC_SUCCESS)
                    return result;

                //TO
                foreach (MailAddress mailAddress in this._To)
                {
                    this.SendData(socket, string.Format("RCPT TO: {0}\r\n", mailAddress.Address));
                    if (this.ReceiveData(socket) != ESMTPResponse.GENERIC_SUCCESS)
                        return result;
                }

                //CC
                if (this._CC != null && this._CC.Count > 0)
                {
                    foreach (MailAddress mailAddress in this._CC)
                    {
                        this.SendData(socket, string.Format("RCPT TO: {0}\r\n", mailAddress.Address));
                        if (this.ReceiveData(socket) != ESMTPResponse.GENERIC_SUCCESS)
                            return result;
                    }
                }

                //准备发送数据
                this.SendData(socket, "DATA\r\n");
                if (this.ReceiveData(socket) != ESMTPResponse.DATA_SUCCESS)
                    return result;

                //发送数据
                this.SendBody(socket);

                //发送完成
                if (this.ReceiveData(socket) != ESMTPResponse.GENERIC_SUCCESS)
                    return result;

                //退出
                this.SendData(socket, "QUIT\r\n");

                if (this.ReceiveData(socket) != ESMTPResponse.QUIT_SUCCESS)
                    return result;
                result = true;
            }

            return result;
        }

        private void SendBody(Socket socket)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format("From: {0} \r\n", this._From.Address));
            sb.Append(string.Format("To: {0} \r\n", this.GetMailLists(this._To)));
            if (this._CC != null && this._CC.Count > 0)
                sb.Append(string.Format("Cc: {0}\r\n", this.GetMailLists(this._CC)));
            sb.Append(string.Format("Date {0}\r\n", DateTime.Now.ToString("ddd, d M y H:m:s z")));
            sb.Append(string.Format("Subject: {0}\r\n", this._Subject));
            sb.Append("X-Mailer: SendMail.Net v1\r\n");
            if (!this._Body.EndsWith("\r\n"))
                this._Body += "\r\n";

            sb.Append("\r\n");
            sb.Append(this._Body);
            sb.Append(".\r\n\r\n\r\n");
            this.SendData(socket, sb.ToString());
        }



        private ESMTPResponse ReceiveData(Socket socket)
        {
            while (socket.Available == 0)
            {
                Thread.Sleep(100);
            }
            byte[] buffer = new byte[1024];
            int size = socket.Receive(buffer, 0, socket.Available, SocketFlags.None);
            string response = Encoding.UTF8.GetString(buffer, 0, size);
            return (ESMTPResponse)((int)Convert.ToInt32(response.Substring(0, 3)));
        }

        private void SendData(Socket socket, string msg)
        {
            byte[] data = Encoding.UTF8.GetBytes(msg);
            socket.Send(data, 0, data.Length, SocketFlags.None);
        }

        private void SendData(Socket socket, byte[] buffer, int offset, int size)
        {
            socket.Send(buffer, offset, size, SocketFlags.None);
        }

        private string GetMailLists(MailAddressCollection collection)
        {
            string result = string.Empty;
            if (collection != null && collection.Count > 0)
            {
                for (int i = 0; i < collection.Count; i++)
                {
                    if (i == collection.Count - 1)
                    {
                        result = string.Concat(result, collection[i].Address);
                    }
                    else
                    {
                        result = string.Concat(result, collection[i].Address, ",");
                    }
                }
            }
            return result;
        }
    }
}
