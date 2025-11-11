using System;
using System.Collections.Generic;
using System.Text;

namespace Speaker.Services
{
    public class ServerMessage
    {
        public bool Connected { get; set; }
        public string Type { get; set; }
        public string Action { get; set; }
        public string Reason { get; set; }
        public string channel { get; set; }
        public string status { get; set; }
    }
}
