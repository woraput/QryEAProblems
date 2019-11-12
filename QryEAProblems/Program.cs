using System;
using System.IO;
using MongoDB.Driver;
using System.Linq;
using QryEAProblems.Models;
using System.Collections.Generic;
using Newtonsoft.Json;

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
            //Process();
            //Process1();
            //Process2();
            Process3();
        }

        private static void Process3()
        {
            var listEAProblems = new List<string>();
            var listEaAfterRun = new List<object>();
            var path = $@"D:\work diamond\โครงการน้ำ\QryEAProblems\listEAProblems.txt";
            var logFile = File.ReadAllText(path);
            var deser = JsonConvert.DeserializeObject<List<string>>(logFile);
            foreach (var s in deser) listEAProblems.Add(s);

            try
            {
                //var fiMoneys = fiMoney.Find(it => it.EaCode == "11001021000008")
                var fiMoneys = fiMoney.Find(it => listEAProblems.Contains(it.EaCode))
                    .Project(it => new
                    {
                        EaCode = it.EaCode,
                        _id = it.FiId,
                        Building = it.BuildingDoneAll + it.BuildingSad + it.BuildingMicOff + it.BuildingEyeOff,
                        Unit = it.HouseholdComplete + it.HouseholdMicOff + it.HouseholdEyeOff + it.HouseholdPause + it.HouseholdRefresh + it.HouseholdSad,
                        ComunityComplete = it.ComunityComplete,
                    })
                    .ToListAsync().GetAwaiter().GetResult()
                    //.Take(50)
                    .GroupBy(it => it.EaCode,
                        (ea, items) => new
                        {
                            EaCode = ea,
                            Building = items.Sum(it => it.Building),
                            Unit = items.Sum(it => it.Unit),
                            ComunityComplete = items.Sum(it => it.ComunityComplete)
                        });
                var eaFi = fiMoneys.Select(it => it.EaCode).ToList();
                var surveys = surveyData.Find(it => eaFi.Contains(it.EA) && it.Enlisted == true)
                  .Project(it => new
                  {
                      EA = it.EA,
                      SampleType = it.SampleType,
                      Status = it.Status,
                      UserID = it.UserId
                  })
                  .ToListAsync()
                  .GetAwaiter()
                  .GetResult();

                var count = 0;
                var all = fiMoneys.Count();
                var pathAfterRun = $@"D:\work diamond\โครงการน้ำ\QryEAProblems\findEAProblem2.txt";
                //var logFileAfter = File.ReadAllText(pathAfterRun);
                //var deserAfter = JsonConvert.DeserializeObject<List<string>>(logFileAfter);
                //foreach (var s in deserAfter) listEaAfterRun.Add(s);
                foreach (var item in fiMoneys)
                {
                    try
                    {
                        var survey = surveys?.Where(it => it.EA == item.EaCode).ToList();
                        var building = survey?.Where(it => it.SampleType == "b").Count() ?? 0;
                        var unit = survey?.Where(it => it.SampleType == "u").Count() ?? 0;
                        var com = survey?.Where(it => it.SampleType == "c" && it.Status == "done-all").Count() ?? 0;
                        var ea = item?.EaCode ?? null;
                        var equal = item.Building.Equals(building)
                                    && item.Unit.Equals(unit)
                                   && item.ComunityComplete.Equals(com);
                        var userId = survey.Select(it => it.UserID).Distinct().ToList();
                        var isValid = false;
                        if (!equal)
                        {
                            if (listEaAfterRun.Contains(ea) && isValid == true)
                            {
                                count++;
                                Console.WriteLine($"{count} / {all} Pass!");
                                continue;
                            }
                            else
                            {
                                foreach (var user in userId)
                                {
                                    listEaAfterRun.Add(new
                                    {
                                        EA = item.EaCode,
                                        UserID = user,
                                    });
                                }
                                string json = JsonConvert.SerializeObject(listEaAfterRun);
                                using (var writer = new StreamWriter(pathAfterRun))
                                    writer.WriteLine(json);
                            }
                        }
                        count++;
                        if (count != all)
                        {
                            Console.WriteLine($"{count} / {all} not Done!");
                        }
                        else
                        {
                            Console.WriteLine($"{count} / {all} Done!");
                        }

                    }
                    catch (Exception err)
                    {
                        throw err;
                    }
                }
            }
            catch (Exception err)
            {
                throw err;

            }
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
                //var fiMoneys = fiMoney.Find(it => it.EaCode == "11001021000008")
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
                    //.Take(50)
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
                var path = $@"D:\work diamond\โครงการน้ำ\QryEAProblems\listEAProblems.txt";
                var logFile = File.ReadAllText(path);
                var deser = JsonConvert.DeserializeObject<List<string>>(logFile);
                foreach (var s in deser) listEAProblems.Add(s);

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
                            if (listEAProblems.Contains(ea))
                            {
                                count++;
                                Console.WriteLine($"{count} / {all} Pass!");
                                continue;
                            }
                            else
                            {
                                listEAProblems.Add(ea);
                                string json = JsonConvert.SerializeObject(listEAProblems);
                                using (var writer = new StreamWriter(path))
                                    writer.WriteLine(json);
                            }
                        }
                        count++;
                        if (count != all)
                        {
                            Console.WriteLine($"{count} / {all} not Done!");
                        }
                        else
                        {
                            Console.WriteLine($"{count} / {all} Done!");
                        }

                    }
                    catch (Exception err)
                    {
                        throw err;
                    }
                }
            }
            catch (Exception err)
            {
                throw err;

            }
        }

        private static void Process1()
        {
            var listFIidOfEAProblems = new List<object>();
            var pathListEa = $@"D:\work diamond\โครงการน้ำ\QryEAProblems\listEAProblems.txt";
            var pathListUserID = $@"D:\work diamond\โครงการน้ำ\QryEAProblems\listUserID.txt";
            var logFile = File.ReadAllText(pathListEa);
            //var logFileID = File.ReadAllText(pathListUserID);
            var deser = JsonConvert.DeserializeObject<List<string>>(logFile);
            //var deser = JsonConvert.DeserializeObject<List<object>>(logFileID);
            var fiMoneys = fiMoney.Find(it => true)
               .Project(it => new
               {
                   EA = it.EaCode,
                   IdUser = it.FiId,
               })
               .ToList();
            var count = 0;
            var all = deser.Count();

            try
            {
                foreach (var s in deser)
                {
                    var userId = fiMoneys.Where(it => it.EA == s).ToList();
                    foreach (var i in userId)
                    {
                        listFIidOfEAProblems.Add(new
                        {
                            EA = i.EA,
                            User = i.IdUser,
                        });
                    }
                    count++;
                    if (count != all)
                    {
                        Console.WriteLine($"{count} / {all} not Done!");
                    }
                    else
                    {
                        Console.WriteLine($"{count} / {all} Done!");
                    }
                }

                string json = JsonConvert.SerializeObject(listFIidOfEAProblems);
                using (var writer = new StreamWriter(pathListUserID))
                    writer.WriteLine(json);
            }
            catch (Exception err)
            {

                throw err;
            }

        }
        private static void Process2()
        {
            var pathListUserID = $@"D:\work diamond\โครงการน้ำ\QryEAProblems\listUserID.txt";
            var pathListUserIDOnly = $@"D:\work diamond\โครงการน้ำ\QryEAProblems\listUserIDOnly.txt";
            var logFileID = File.ReadAllText(pathListUserID);
            var deser = JsonConvert.DeserializeObject<List<UserID>>(logFileID);
            var user = deser.Select(it => it.User).Distinct().ToList();

            string json = JsonConvert.SerializeObject(user);
            using (var writer = new StreamWriter(pathListUserIDOnly))
                writer.WriteLine(json);
        }

        private class UserID
        {
            public string EA { get; set; }
            public string User { get; set; }
        }
    }
}
