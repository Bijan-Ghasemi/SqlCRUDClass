using System;
using System.Collections.Generic;
using System.Text;
using Repository.Domain;

namespace TestWebAPI.Domain.Model
{
    [TableInfo("Web_API_Response_Log", "ID",true)]
    public class Web_API_Response_Log
    {
        public string ID { get; set; }
        public string LogDate { get; set; }
        public string UserName { get; set; }
        public string API_Controller { get; set; }
        public string API_Action { get; set; }
        public string Input { get; set; }
        public string Output { get; set; }
        public string ClientAddress { get; set; }
        public string Duration { get; internal set; }
    }
}
