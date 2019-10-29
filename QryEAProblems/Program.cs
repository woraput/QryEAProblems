using System;
using MongoDB.Driver;
using System.Linq;
using QryEAProblems.Models;
using System.Collections.Generic;

namespace QryEAProblems
{
    class Program
    {
        private static IMongoCollection<SurveyData> surveyData { get; set; }
        private static IMongoCollection<ReportEaInfo> reportEAInfo { get; set; }
        private static IMongoCollection<FiMoney> fiMoney { get; set; }
        private static IMongoCollection<UploadLog> uploadLog { get; set; }
        private static IMongoCollection<SwapSurveyEaLog> swapSurveyEaLog { get; set; }
        static void Main(string[] args)
        {
            var client = new MongoClient("mongodb://dbagent:Nso4Passw0rd5@mongodbproykgte5e7lvm7y-vm0.southeastasia.cloudapp.azure.com/nso");
            var database = client.GetDatabase("nso");
            reportEAInfo = database.GetCollection<ReportEaInfo>("reporteainfo");
            surveyData = database.GetCollection<SurveyData>("survey");
            fiMoney = database.GetCollection<FiMoney>("fimoney");
            uploadLog = database.GetCollection<UploadLog>("uploadlog");
            swapSurveyEaLog = database.GetCollection<SwapSurveyEaLog>("SwapSurveyEaLog");
            Process();
        }

        private static void Process()
        {
            var listEAProblems = new List<string>();
            var swapList = swapSurveyEaLog.Aggregate()
                .Match(it => true)
                .Project(it => new
                {
                    ea1 = it.Ea1,
                    ea2 = it.Ea2,
                })
                .ToListAsync()
                .GetAwaiter()
                .GetResult();
            var ea1 = swapList.Select(it => it.ea1).Distinct();
            var ea2 = swapList.Select(it => it.ea2).Distinct();
            var eaCodeSwaps = ea1.Union(ea2).ToList();
            try
            {

                var fiMoneys = fiMoney.Find(it => !eaCodeSwaps.Contains(it.EaCode))
                    .Project(it => new
                    {
                        EaCode = it.EaCode,
                        _id = it.FiId,
                        Building = it.BuildingDoneAll + it.BuildingSad + it.BuildingMicOff + it.BuildingEyeOff,
                        Unit = it.HouseholdComplete + it.HouseholdMicOff + it.HouseholdEyeOff + it.HouseholdPause + it.HouseholdRefresh + it.HouseholdSad,
                        ComunityComplete = it.ComunityComplete,
                    })
                    .ToList()
                    .GroupBy(it => it.EaCode,
                        (ea, items) => new
                        {
                            EaCode = ea,
                            Building = items.Sum(it => it.Building),
                            Unit = items.Sum(it => it.Unit),
                            ComunityComplete = items.Sum(it => it.ComunityComplete)
                        });

                var count = 0;
                var all = fiMoneys.Count();
                foreach (var item in fiMoneys)
                {
                    try
                    {
                        var surveys = surveyData.Find(it => it.EA == item.EaCode && it.Enlisted == true)
                  .Project(it => new
                  {
                      EA = it.EA,
                      SampleType = it.SampleType,
                      Status = it.Status
                  })
                  .ToListAsync()
                  .GetAwaiter()
                  .GetResult();
                        var building = surveys?.Where(it => it.SampleType == "b").Count() ?? 0;
                        var unit = surveys?.Where(it => it.SampleType == "u").Count() ?? 0;
                        var com = surveys?.Where(it => it.SampleType == "c" && it.Status == "done-all").Count() ?? 0;
                        var ea = item?.EaCode ?? null;
                        var equal = item.Building.Equals(building)
                                    && item.Unit.Equals(unit)
                                   && item.ComunityComplete.Equals(com);

                        if (!equal)
                        {
                            listEAProblems.Add(ea);
                        }
                        count++;
                        if (count != all)
                        {
                            Console.WriteLine($"{count} / {all}");

                        }
                        else
                        {
                            Console.WriteLine("Done!");

                        }

                    }
                    catch (Exception err)
                    {
                        throw err;
                    }
                }
                //var fiMoneys = fiMoney.Find(it => it.EaCode == "11001081000001")
                //    .Project(it => new
                //    {
                //        EaCode = it.EaCode,
                //        //BuildingDoneAll = it.BuildingDoneAll,
                //        //BuildingSad = it.BuildingSad,
                //        //BuildingMicOff = it.BuildingMicOff,
                //        //BuildingEyeOff = it.BuildingEyeOff,
                //        //BuildingCheckMark = it.BuildingCheckMark,
                //        //BuildingInformation = it.BuildingInformation,
                //        //HouseholdComplete = it.HouseholdComplete,
                //        //HouseholdMicOff = it.HouseholdMicOff,
                //        //HouseholdEyeOff = it.HouseholdEyeOff,
                //        //HouseholdPause = it.HouseholdPause,
                //        //HouseholdRefresh = it.HouseholdRefresh,
                //        //HouseholdSad = it.HouseholdSad
                //        Building = it.BuildingDoneAll + it.BuildingSad + it.BuildingMicOff + it.BuildingEyeOff,
                //        Unit = it.HouseholdComplete + it.HouseholdMicOff + it.HouseholdEyeOff + it.HouseholdPause + it.HouseholdRefresh + it.HouseholdSad,
                //        ComunityComplete = it.ComunityComplete,
                //    })
                //    .ToListAsync()
                //    .GetAwaiter()
                //    .GetResult();


                //var reports = reportEAInfo.Find(it => it.DateApprovePs != null || it.DateApproveFs != null)
                //    .Project(it => it._id)
                //    .ToListAsync()
                //    .GetAwaiter()
                //    .GetResult();

                //var eaAppovedInSurveys = surveys.Where(it => it.EA == it.SrcEA).Select(it => it.EA).Distinct();
                //var listEAProblems = new List<string>();


                //foreach (var ea in eaAppovedInSurveys)
                //{
                //    var reportInfo = reports.Where(it => it._id == ea)
                //    .Select(it => new
                //    {
                //        ProgressBuilding = it.ProgressBuilding,
                //        ProgressUnit = it.ProgressUnit
                //    });

                //    var building = surveys.Where(it => it.EA == ea && it.SampleType == "b" && it.Enlisted == true).Count();
                //    var unit = surveys.Where(it => it.EA == ea && it.SampleType == "u" && it.Enlisted == true).Count();
                //    var dataEqual = reportInfo.FirstOrDefault().ProgressBuilding == building && reportInfo.FirstOrDefault().ProgressUnit == unit;
                //    if (!dataEqual)
                //    {
                //        listEAProblems.Add(ea);
                //    }
                //}
                //return listEAProblems;
            }
            catch (Exception err)
            {
                throw err;
            }
        }
    }
}
