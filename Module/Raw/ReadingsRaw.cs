using Bygdrift.DataLakeTools;
using Bygdrift.Warehouse;
using Module.Refines.Models;
using Module.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Module.Raw
{
    public class ReadingsRaw
    {
        private readonly WebService service;
        private readonly AppBase app;

        /// <summary>
        /// Loads all reads from Eloverblik and into dataLake
        /// </summary>
        public ReadingsRaw(AppBase app)
        {
            this.app = app;
            service = new WebService(app.Config["EloverblikToken"], app.Log);
        }

        /// <summary>
        /// Only returns the reads if they are completed
        /// </summary>
        public bool TryGetAlreadyLoadedReads(out List<ReadPartition> reads)
        {
            if (app.DataLake.GetJson("Raw", "ReadPartitions.json", FolderStructure.DatePath, out reads))
            {
                var readsLoaded = reads.Count(o => o.LoadedToDataLake || o.TimesTriedLoadingFromService > 2);
                if (readsLoaded == reads.Count)
                    return true;
            }
            return false;
        }

        public async Task<string> SaveLoadedReadPartitionToDataLake(List<ReadPartition> reads)
        {
            return await app.DataLake.SaveObjectAsync(reads, "Raw", "ReadPartitions.json", FolderStructure.DatePath);
        }

        public async Task<List<ReadPartition>> GetReads(List<ReadPartition> reads, int idsPerGroup, int breakMethodAfterMinutes)
        {
            var stopTime = DateTime.Now.AddMinutes(breakMethodAfterMinutes);
            foreach (IGrouping<TimeAggregation, ReadPartition> readGroup in reads.Where(o => o.LoadedToDataLake == false && o.TimesTriedLoadingFromService < 3).GroupBy(o => o.Aggregation))
            {
                app.Log.LogInformation($"Loading {readGroup.Key} ids: {readGroup.First().Id}-{reads.Last().Id}. Count: {readGroup.Count()}");
                var readings = await GetReadsFromServiceAsync(readGroup.Key, readGroup.ToList(), idsPerGroup, new TimeSpan(0, 4, 0), new TimeSpan(0, 5, 0));
                //var succededIds = readings.Where(o => o.Success).SelectMany(o => o.Ids).ToList();
                var savedReadings = await SaveReadingsToDataLakeAsync(readGroup.Key, readings, new TimeSpan(0, 3, 0));

                foreach (var read in readGroup)
                {
                    //if (!readings.Single(o => o.Ids.Contains(read.Id)).Success)
                    //    read.TimesTriedLoadingFromService++;

                    var savedReading = savedReadings.SingleOrDefault(o => o.Ids.Contains(read.Id));
                    if (savedReading.Success)
                    {
                        read.LoadedToDataLake = true;
                        read.DataLakePath = savedReading.Path;
                    }
                    else
                        read.TimesTriedLoadingFromService++;
                }

                if (DateTime.Now > stopTime)
                    break;
            }

            return reads;
        }

        public async Task<List<(bool Success, string Json, string[] Ids)>> GetReadsFromServiceAsync(TimeAggregation aggregation, List<ReadPartition> groups, int idsPerGroup, TimeSpan cancelWebCallAfter, TimeSpan cancelMethodCallAfter)
        {
            var res = new List<(bool Success, string Json, string[] Ids)>();
            if (groups == null || !groups.Any())
                return res;

            var readingGroups = GroupReadPartitions(GetMinLimit(app.Now, aggregation), app.Now, groups, idsPerGroup, GetDaysOverlap(aggregation));

            var tasks = new List<Task<(string Json, string[] Ids)>>();
            foreach (var item in readingGroups)
                if (item.To.Date > item.From.Date)
                    tasks.Add(service.GetReadingTimeSeriesAsync(item.From, item.To, aggregation, cancelWebCallAfter, item.Ids));
                else
                    res.Add((true, null, item.Ids));

            await Task.WhenAny(Task.WhenAll(tasks), Task.Delay(cancelMethodCallAfter));
            var error = app.Log.GetErrors();
            foreach (var item in tasks.Where(t => t.Status == TaskStatus.RanToCompletion).Select(t => t.Result))
                res.Add((true, item.Json, item.Ids));

            return res;
        }

        public async Task<List<(bool Success, string Path, string[] Ids)>> SaveReadingsToDataLakeAsync(TimeAggregation aggregation, List<(bool Success, string Json, string[] Ids)> readings, TimeSpan cancelMethodCallAfter)
        {
            var res = new List<(bool Success, string Path, string[] Ids)>();
            if (readings == null)
                return res;

            var tasks = new List<(string[] Ids, Task<string> Path)>();
            foreach (var item in readings)
            {
                if (item.Json != null)
                {
                    var fileName = $"ReadingsPer{aggregation}_{item.Ids.First()}-{item.Ids.Last()}.json";
                    var basePath = "Raw";
                    var path = app.DataLake.SaveStringAsync(item.Json, basePath, fileName, FolderStructure.DatePath);
                    tasks.Add((item.Ids, path));
                }
                else
                    res.Add((true, null, item.Ids));
            }
            await Task.WhenAny(Task.WhenAll(tasks.Select(o => o.Path)), Task.Delay(cancelMethodCallAfter));
            foreach (var item in tasks.Where(t => t.Path.Status == TaskStatus.RanToCompletion).Select(t => (t.Ids, t.Path.Result)))
                res.Add((true, item.Result, item.Ids));

            return res;
        }

        /// <summary>
        /// Groups a list of readings into packs of meteringIds, that best fits together and therefor can be loaded in groups
        /// </summary>
        /// /// <param name="minLimit">Date span start for the whole period desired to save</param>
        /// <param name="maxLimit">Date span end for the whole period desired to save</param>
        /// <param name="groups">All the meterings that earlier has been loaded</param>
        /// <param name="take">How many to group per pack</param>
        /// <param name="daysOverlap">If 1, then it will subtract a day from the start so there will be an overlap of one day in the loadings</param>
        /// <returns></returns>
        public static List<(string[] Ids, DateTime From, DateTime To)> GroupReadPartitions(DateTime minLimit, DateTime maxLimit, List<ReadPartition> groups, int take, int daysOverlap)
        {
            var res = new List<(string[] Ids, DateTime Min, DateTime Max)>();
            minLimit = minLimit.Date;
            maxLimit = maxLimit.Date;

            foreach (var item in groups)
            {
                item.From = item.From < minLimit ? minLimit : item.From.Date;
                item.To = item.To > maxLimit ? maxLimit : (item.To <= minLimit ? minLimit : item.To.Date);
                var loadFrom = item.From > minLimit ? minLimit : item.To;
                item.DaysSpan = (maxLimit - loadFrom).TotalDays;
            }

            for (int skip = 0; skip < groups.Count; skip += take)
            {
                var resGroup = groups.OrderBy(o => o.DaysSpan).Skip(skip).Take(take).ToList();
                var span = resGroup.Max(o => o.DaysSpan);
                var from = span <= 0 ? maxLimit : maxLimit.AddDays(-span).AddDays(-daysOverlap);
                from = from < minLimit ? minLimit : from;
                res.Add((resGroup.Select(o => o.Id).ToArray(), from, maxLimit));
            }
            return res;
        }

        private int GetMonthsToKeepData(TimeAggregation aggregration) => aggregration switch
        {
            TimeAggregation.Day => int.TryParse(app.Config["MonthsToKeepReadingsPerDay"], out int res) ? res : 60,
            TimeAggregation.Hour => int.TryParse(app.Config["MonthsToKeepReadingsPerHour"], out int res) ? res : 6,
            TimeAggregation.Month => int.TryParse(app.Config["MonthsToKeepReadingsPerMonth"], out int res) ? res : 120,
            TimeAggregation.Quarter => int.TryParse(app.Config["MonthsToKeepReadingsPerMonth"], out int res) ? res : 120,
            TimeAggregation.Year => int.TryParse(app.Config["MonthsToKeepReadingsPerMonth"], out int res) ? res : 120,
            _ => throw new NotImplementedException()
        };

        private DateTime GetMinLimit(DateTime now, TimeAggregation aggregration)
        {
            return now.AddMonths(-GetMonthsToKeepData(aggregration));
        }

        private static int GetDaysOverlap(TimeAggregation aggregration) => aggregration switch
        {
            TimeAggregation.Day => 10,
            TimeAggregation.Hour => 7,
            TimeAggregation.Month => 45,
            TimeAggregation.Quarter => 100,
            TimeAggregation.Year => 400,
            _ => 7,
        };
    }
}
