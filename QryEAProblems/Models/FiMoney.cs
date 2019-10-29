using System;
using System.Collections.Generic;
using System.Text;

namespace QryEAProblems.Models
{
    public class FiMoney
    {
        public string _id { get; set; }//EA + FI ID
        public string FiId { get; set; }
        public string FiName { get; set; }
        public string FsId { get; set; }
        public string PsId { get; set; }
        public string EaCode { get; set; }
        public DateTime? PaidFs { get; set; }
        public string PaidFsRefId { get; set; }// _id ของ Reccord FsMoney
        public int BuildingDoneAll { get; set; }
        public int BuildingSad { get; set; }
        public int BuildingMicOff { get; set; }     //new
        public int BuildingEyeOff { get; set; }
        public int BuildingCheckMark { get; set; }
        public int BuildingInformation { get; set; }
        public int HouseholdComplete { get; set; }
        public int HouseholdMicOff { get; set; }
        public int HouseholdEyeOff { get; set; }
        public int HouseholdPause { get; set; }     //new
        public int HouseholdRefresh { get; set; }   //new
        public int HouseholdSad { get; set; }   //new
        public int ComunityComplete { get; set; }
        public double Amount { get; set; }
        public DateTime? CreationDateTime { get; set; }
    }
}
