using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            Dictionary<DateTime, List<TimeBlocks>> PcOnOff = new Dictionary<DateTime, List<TimeBlocks>>();
            //PcOnOff = null;
            EventLog logs = new EventLog();
            logs.Log = "System";
            logs.MachineName = ".";
            var entries = logs.Entries.Cast<EventLogEntry>();
            var on = from e in entries
                     where e.TimeGenerated >= DateTime.Now.AddDays(-10) && e.InstanceId == 2147489653 && e.Source.Contains("EventLog") && e.EntryType == EventLogEntryType.Information //&& e.TimeGenerated >= DateTime.Now.AddDays(-10)
                     select new OnOffEntry { EntryDate = e.TimeGenerated.Date, TimeGenerated = e.TimeGenerated, Type = "On" }; ;
            var off = from e in entries
                      where e.TimeGenerated >= DateTime.Now.AddDays(-10) && e.InstanceId == 2147489654 && e.Source.Contains("EventLog") && e.EntryType == EventLogEntryType.Information //&& e.TimeGenerated >= DateTime.Now.AddDays(-10)
                      select new OnOffEntry { EntryDate = e.TimeGenerated.Date, TimeGenerated = e.TimeGenerated, Type = "Off" };
            var sleep = from e in entries
                        where e.TimeGenerated >= DateTime.Now.AddDays(-10) && e.InstanceId == 42 && e.Source.Contains("Kernel-Power") && e.EntryType == EventLogEntryType.Information //&& e.TimeGenerated >= DateTime.Now.AddDays(-10)
                        select new OnOffEntry { EntryDate = e.TimeGenerated.Date, TimeGenerated = e.TimeGenerated, Type = "Sleep" };
            var awake = from e in entries
                        where e.TimeGenerated >= DateTime.Now.AddDays(-10) && e.InstanceId == 1 && e.Source.Contains("Kernel-General") && e.EntryType == EventLogEntryType.Information && e.UserName == null
                        select new OnOffEntry { EntryDate = e.TimeGenerated.Date, TimeGenerated = e.TimeGenerated, Type = "Awake" };
            List<OnOffEntry> temp = new List<OnOffEntry>();
            on.ToList().ForEach(x =>
            {
                if (temp.FirstOrDefault(y => y.TimeGenerated == x.TimeGenerated) == null)
                    temp.Add(x);
            });
            off.ToList().ForEach(x =>
            {
                if (temp.FirstOrDefault(y => y.TimeGenerated == x.TimeGenerated) == null)
                    temp.Add(x);
            });
            sleep.ToList().ForEach(x =>
            {
                if (temp.FirstOrDefault(y => y.TimeGenerated == x.TimeGenerated) == null)
                    temp.Add(x);
            });

            awake.ToList().ForEach(x =>
            {
                if (temp.FirstOrDefault(y => y.TimeGenerated == x.TimeGenerated) == null)
                    temp.Add(x);
            });
            List<OnOffEntry> result = new List<OnOffEntry>();
            string actionType = "";
            OnOffEntry itemBefore = null;
            foreach (OnOffEntry item in temp.Distinct().OrderBy(x => x.TimeGenerated))
            {
                if (actionType == item.Type)
                    result.Remove(itemBefore);
                result.Add(item);
                actionType = item.Type;
                itemBefore = item;
            }
            if ((result[0].Type == "Sleep" || result[0].Type == "Off"))
            {
                result.Insert(0, new OnOffEntry { EntryDate = result[0].EntryDate, TimeGenerated = result[0].EntryDate, Type = "On" });
            }

            //testing
            List<OnOffEntry> tempresult = new List<OnOffEntry>(result);
            var firstDate = tempresult[0].EntryDate;
            int days = 1;
            DateTime i = firstDate;
            while (i < DateTime.Now.Date)
            {
                var a = tempresult.Where(x => x.EntryDate == i);
                if (a.First().Type == "Off")
                {
                    result.Add(new OnOffEntry { EntryDate = i, TimeGenerated = i, Type = "On" });
                }
                if (a.Last().Type == "On")
                {
                    result.Add(new OnOffEntry { EntryDate = i, TimeGenerated = i.AddSeconds(-1), Type = "Off" });
                }
                foreach (var items in a)
                {
                    Console.WriteLine(items.EntryDate + "\t" + items.TimeGenerated + "\t" + items.Type);
                }
                //days++;
                i.AddDays(days++);
                Console.WriteLine("-------------------------End OF Day----------------------");
            }
            //if( == )
            //var nextDate = result[i].EntryDate;
            //    int days = 1;
            //    if (i == 1 && (tempresult[0].Type == "Off" || tempresult[0].Type == "Sleep"))
            //    {
            //        result.Insert(0, new OnOffEntry { EntryDate = tempresult[0].EntryDate, TimeGenerated = tempresult[0].EntryDate, Type = "On" });
            //    }
            //    while (firstDate.CompareTo(nextDate) == -1)
            //    { 

            //    }

            //    //Console.WriteLine(tempresult.EntryDate + " | " + tempresult.TimeGenerated + " | " + tempresult.Type);
            //}
            //List<OnOffEntry> tempresult = new List<OnOffEntry>();
            //tempresult = result;
            //var firstDate = tempresult[0].EntryDate;
            //for (int i = 1; i < tempresult.Count; i++)
            //{
            //    var nextDate = tempresult[i].EntryDate;
            //    int days = 1;
            //    while (firstDate.AddDays(days).CompareTo(nextDate) == -1)
            //    {
            //        result.Add(new OnOffEntry { EntryDate = firstDate.AddDays(days), Type = null });
            //        days++;
            //    }
            //    firstDate = nextDate;
            //}


            for (int count = 0; count < result.Count - 1; count++)
            {
                int nextCount = count + 1;
                if (!PcOnOff.ContainsKey(result[count].EntryDate))
                {
                    List<TimeBlocks> EachTimeBlocks = new List<TimeBlocks>();
                    PcOnOff.Add(result[count].EntryDate, EachTimeBlocks);
                }

                PcOnOff[result[count].EntryDate].Add(new TimeBlocks
                {
                    EntryStartTime = result[count].TimeGenerated,
                    startAction = result[count].Type,
                    EntryStopTime = result[++count].TimeGenerated,
                    stopAction = result[nextCount].Type
                });
            }
            if (!PcOnOff.ContainsKey(result.Last().EntryDate))
            {
                List<TimeBlocks> EachTimeBlocks = new List<TimeBlocks>();
                PcOnOff.Add(result.Last().EntryDate, EachTimeBlocks);
            }
            PcOnOff[result.Last().EntryDate].Add(new TimeBlocks
            {
                EntryStartTime = result.Last().TimeGenerated,
                startAction = result.Last().Type,
                EntryStopTime = DateTime.Now,
                stopAction = "On"
            });
            //Dictionary<DateTime, List<TimeBlocks>> PcOnOffTime = new Dictionary<DateTime, List<TimeBlocks>>(PcOnOff);
            //var firstKey = PcOnOffTime.ElementAt(0).Key;
            //for (int i = 1; i < PcOnOffTime.Count; i++)
            //{
            //    var nextKey = PcOnOffTime.ElementAt(i).Key;
            //    int days = 1;
            //    while (firstKey.AddDays(days).CompareTo(nextKey) == -1)
            //    {
            //        List<TimeBlocks> EachTimeBlocks = new List<TimeBlocks>();
            //        PcOnOff.Add(firstKey.AddDays(days), EachTimeBlocks);
            //        days++;
            //        //if (PcOnOffTime.ElementAt(i).Value.Last(). == "On" || PcOnOffTime.ElementAt(i).Value.Last() == "Awake")
            //        //{

            //        //}
            //    }
            //    firstKey = nextKey;
            //}

            foreach (var kvp in PcOnOff.OrderBy(x => x.Key))
            {
                Console.WriteLine(kvp.Key);
                foreach (var kvpv in kvp.Value)
                {
                    Console.WriteLine(kvpv.EntryStartTime + "|" + kvpv.startAction + "|" + kvpv.EntryStopTime + "|" + kvpv.stopAction);
                }
                Console.WriteLine("--------------------------------------");
            }
            Console.ReadLine();
        }


    }
}
