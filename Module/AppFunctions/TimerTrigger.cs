using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bygdrift.DataLakeTools;
using Bygdrift.Warehouse;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.ContextImplementations;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.Options;
using Microsoft.Extensions.Logging;
using Module.Raw;
using Module.Refines;
using Module.Refines.Models;
using Module.Services;

namespace Module.AppFunctions
{
    public class TimerTrigger
    {
        public TimerTrigger(ILogger<TimerTrigger> logger, IDurableClientFactory clientFactory)
        {
            if (App == null)
            {
                App = new AppBase<Settings>(logger);
                App.Log.LogInformation("Created a new Appbase at: {Now}", App.LoadedLocal);
            }

            if (Client == null)
                Client = clientFactory.CreateClient(new DurableClientOptions { TaskHub = App.ModuleName });

            MeteringsToLoad = int.MaxValue;
        }

        public static AppBase<Settings> App { get; private set; }
        public static IDurableClient Client { get; private set; }
        public static int MeteringsToLoad { get; set; }

        [FunctionName(nameof(Starter))]
        public async Task Starter([TimerTrigger("%StarterScheduleExpression%"
#if DEBUG
            ,RunOnStartup = true
#endif
            )] TimerInfo timerInfo)
        {
            if (await Basic.IsRunning(App, Client)) return;
            App.LoadedUtc = DateTime.UtcNow;
            App.Config["QualifiedInstanceId"] = await Client.StartNewAsync(nameof(RunOrchestrator));
        }

        [FunctionName(nameof(RunOrchestrator))]
        public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            if (!Basic.IsQualifiedInstance(App, context.InstanceId)) return;
            if (!context.IsReplaying)
                App.Log.LogInformation("Has started instance {Instance}", context.InstanceId);


            var reads = await context.CallActivityAsync<List<ReadPartition>>(nameof(GetMeteringsAndSaveToDatabaseAsync), null);
            if (reads == null)
            {
                App.Log.LogWarning("No meteringpoints where found");
                return;
            }

            for (int i = 0; i < 50; i++)  //A high number 
            {
                if (!context.IsReplaying)
                    App.Log.LogInformation($"{reads.Count(o => o.LoadedToDataLake && o.TimesTriedLoadingFromService < 3)} of {reads.Count} meterings per hour, day and month loaded");

                reads = await context.CallActivityAsync<List<ReadPartition>>(nameof(GetReadingsAndSaveToDataLakeAsync), reads);

                if (i == 49)
                    App.Log.LogError("When loading all readings to dataLake, the for-loop came up to 49 loops and thats way to much. A developer should look at it.");

                if (reads.All(o => o.LoadedToDataLake || o.TimesTriedLoadingFromService > 2))
                    break;
            }

            foreach (var item in reads.Where(o => !o.LoadedToDataLake))
                App.Log.LogError("MeteringId {Id} per {Aggregation} has been tried loading {Times} times without luck.", item.Id, item.Aggregation, item.TimesTriedLoadingFromService);

            if (!context.IsReplaying)
                App.Log.LogInformation($"Importing readingRefines...");

            for (int i = 0; i < 50; i++)  //A high number 
            {
                reads = await context.CallActivityAsync<List<ReadPartition>>(nameof(RefineReadingsAndImport), reads);

                if (i == 49)
                    App.Log.LogError("When loading all readings to database, the for-loop came up to 49 loops and thats way to much. A developer should look at it.");

                if (reads.All(o => o.LoadedToDatabase == true))
                    break;
            }

            await context.CallActivityAsync(nameof(CleanDatabase), null);
            App.Log.LogInformation($"Finished reading in {context.CurrentUtcDateTime.Subtract(App.LoadedUtc).TotalSeconds / 60} minutes.");
        }

        [FunctionName(nameof(GetMeteringsAndSaveToDatabaseAsync))]
        public async Task<List<ReadPartition>> GetMeteringsAndSaveToDatabaseAsync([ActivityTrigger] IDurableActivityContext context)
        {
            if (!Basic.IsQualifiedInstance(App, context.InstanceId)) return default;
            var meteringsFromDb = App.Mssql.GetAsCsv("Meterings");
            var service = new WebService(App.Settings.Token, App.Log);
            if (!App.DataLake.GetJson("Raw", "MeteringIds.json", FolderStructure.DatePath, out string[] meteringIds))
            {
                meteringIds = await service.GetMeteringPointIdsAsync();
                if (MeteringsToLoad > 0 && MeteringsToLoad < meteringIds.Length)
                    meteringIds = meteringIds.Take(MeteringsToLoad).ToArray();

                await App.DataLake.SaveObjectAsync(meteringIds, "Raw", "MeteringIds.json", FolderStructure.DatePath);
            }

            if (!App.DataLake.GetJson("Raw", "MeteringDetails.json", FolderStructure.DatePath, out IEnumerable<MeteringDetail> meteringDetails))
            {
                meteringDetails = await service.GetMeteringPointsDetailsAsync(null, meteringIds);
                await App.DataLake.SaveObjectAsync(meteringDetails, "Raw", "MeteringDetails.json", FolderStructure.DatePath);
            }

            var readPartitionsFromDb = new MeteringsRefine(App, meteringsFromDb, meteringDetails);
            await App.DataLake.SaveObjectAsync(readPartitionsFromDb.ReadPartitionsFromDb, "Raw", "ReadPartitionsFromDb.json", FolderStructure.DatePath);
            return readPartitionsFromDb.ReadPartitionsFromDb;
        }

        [FunctionName(nameof(GetReadingsAndSaveToDataLakeAsync))]
        public async Task<List<ReadPartition>> GetReadingsAndSaveToDataLakeAsync([ActivityTrigger] IDurableActivityContext context)
        {
            if (!Basic.IsQualifiedInstance(App, context.InstanceId)) return default;
            var readsFromDb = context.GetInput<List<ReadPartition>>();
            App.Log.LogInformation("GetReadingsAndSaveToDataLakeAsync. Instance {Instance}", context.InstanceId);

            var readingsRaw = new ReadingsRaw(App);
            if (readingsRaw.TryGetAlreadyLoadedReads(out var readLoaded))
                return readLoaded;

            //var reads = await readingsRaw.GetReads(readsFromDb, 20, 6);
            var reads = await readingsRaw.GetReads(readsFromDb, 20, 3);
            var fetcedReads = reads.Count(o => o.LoadedToDataLake || o.TimesTriedLoadingFromService > 2);
            if (fetcedReads == reads.Count)
                await readingsRaw.SaveLoadedReadPartitionToDataLake(reads);

            return reads;
        }

        [FunctionName(nameof(RefineReadingsAndImport))]
        public List<ReadPartition> RefineReadingsAndImport([ActivityTrigger] IDurableActivityContext context)
        {
            if (!Basic.IsQualifiedInstance(App, context.InstanceId)) return default;
            var reads = context.GetInput<List<ReadPartition>>();
            var stopTime = DateTime.Now.AddMinutes(3);
            foreach (var group in reads.Where(o => o.LoadedToDatabase == null)?.GroupBy(o => o.DataLakePath))
            {
                if (!string.IsNullOrEmpty(group.Key))
                {
                    var refine = GetReadingRefinesFromDataLakePaths(group.Key);
                    if (refine != null && refine.MeteringPointIds.Any() && refine.Csv.RowLimit.Max > 0)
                    {
                        var itemsLeft = reads.Where(o => o.LoadedToDatabase == null)?.GroupBy(o => o.DataLakePath).Count();
                        App.Log.LogInformation($"Imported readingRefines Per{refine.TimeAggregation}. {itemsLeft} left. {refine.MeteringPointIds.First()} to {refine.MeteringPointIds.Last()}. {refine.MeteringPointIds.Count} items.");
                    }
                }
                foreach (var read in group)
                    read.LoadedToDatabase = true;

                if (DateTime.Now > stopTime)
                    break;
            }

            return reads;
        }

        private ReadingsRefine GetReadingRefinesFromDataLakePaths(string datalakePath)
        {
            if (datalakePath == null)
                return default;

            if (datalakePath.Contains($"ReadingsPer{TimeAggregation.Month}_"))
                return new ReadingsRefine(App, "DataPerMonth", datalakePath, TimeAggregation.Month);

            if (datalakePath.Contains($"ReadingsPer{TimeAggregation.Day}_"))
                return new ReadingsRefine(App, "DataPerDay", datalakePath, TimeAggregation.Day);

            if (datalakePath.Contains($"ReadingsPer{TimeAggregation.Hour}_"))
                return new ReadingsRefine(App, "DataPerHour", datalakePath, TimeAggregation.Hour);

            return null;
        }

        [FunctionName(nameof(CleanDatabase))]
        public void CleanDatabase([ActivityTrigger] IDurableActivityContext context)
        {
            if (!Basic.IsQualifiedInstance(App, context.InstanceId))
                return;

            App.Log.LogInformation("Removing old data from database...");

            new ReadingsRefine(App, "DataPerMonth", null, TimeAggregation.Month).RemoveExpiredRowsFromDatabase();
            new ReadingsRefine(App, "DataPerDay", null, TimeAggregation.Day).RemoveExpiredRowsFromDatabase();
            new ReadingsRefine(App, "DataPerHour", null, TimeAggregation.Hour).RemoveExpiredRowsFromDatabase();
        }
    }
}