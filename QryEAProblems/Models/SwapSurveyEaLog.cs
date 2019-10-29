using System;
using System.Collections.Generic;
using System.Text;

namespace QryEAProblems.Models
{
    public class SwapSurveyEaLog
    {
        public string _id { get; set; }
        public string UserId1 { get; set; }
        public string Ea1 { get; set; }
        public string UserId2 { get; set; }
        public string Ea2 { get; set; }
        public DateTime? CreationDateTime { get; set; }
        public List<string> Survey1Ids { get; set; }
        public List<string> Survey2Ids { get; set; }

    }
}
