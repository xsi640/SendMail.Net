using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SendMail.Net
{
    public enum ESMTPResponse : int
    {
        CONNECT_SUCCESS = 220,
        GENERIC_SUCCESS = 250,
        DATA_SUCCESS = 354,
        QUIT_SUCCESS = 221
    }
}
