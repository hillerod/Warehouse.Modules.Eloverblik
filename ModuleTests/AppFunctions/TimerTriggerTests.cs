using Bygdrift.DataLakeTools;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.ContextImplementations;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.AppFunctions;
using Module.Raw;
using Module.Refines.Models;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ModuleTests.AppFunctions
{
    [TestClass]
    public class TimerTriggerTests
    {
        private readonly Mock<IDurableActivityContext> contextMock = new();
        private readonly Mock<ILogger<TimerTrigger>> loggerMock = new();
        private readonly Mock<IDurableClientFactory> clientFactory = new();
        private readonly string instanceId = "testId";
        private readonly TimerTrigger timerTrigger;
        private readonly DateTime now;

        public TimerTriggerTests()
        {
            contextMock.Setup(o => o.InstanceId).Returns(instanceId);
            timerTrigger = new TimerTrigger(loggerMock.Object, clientFactory.Object);
            TimerTrigger.App.Config["QualifiedInstanceId"] = instanceId;
            now = DateTime.Now;
            TimerTrigger.MeteringsToLoad = 40;
        }

        public List<string> Errors { get { return TimerTrigger.App.Log.GetErrors().ToList(); } }

        [TestMethod]
        public async Task PureRestart()
        {
            TimerTrigger.App.Mssql.DeleteTable("Meterings");
            TimerTrigger.App.Mssql.DeleteTable("DataPerHour");
            TimerTrigger.App.Mssql.DeleteTable("DataPerDay");
            TimerTrigger.App.Mssql.DeleteTable("DataPerMonth");
            await TimerTrigger.App.DataLake.DeleteDirectoryAsync("Raw", FolderStructure.DatePath);
        }

        [TestMethod]
        public async Task GoTwoDaysBack()
        {
            var param = new { From = TimerTrigger.App.Now.AddYears(-10).Date, To = TimerTrigger.App.Now.AddDays(-2).Date };
            var sql = $"UPDATE [{TimerTrigger.App.ModuleName}].[Meterings] SET" +
                " dataPerDayFrom = @From, dataPerDayTo = @To," +
                " dataPerMonthFrom = @From, dataPerMonthTo = @To," +
                " dataPerHourFrom = @From, dataPerHourTo = @To";
            TimerTrigger.App.Mssql.ExecuteNonQuery(sql, param);
            await TimerTrigger.App.DataLake.DeleteFileAsync("Raw", "ReadPartitions.json", FolderStructure.DatePath);
            await TimerTrigger.App.DataLake.DeleteFileAsync("Raw", "ReadPartitionsFromDb.json", FolderStructure.DatePath);
        }

        //[TestMethod]
        //public async Task CallOrchestrator()
        //{
        //    var mockContext = new Mock<IDurableOrchestrationContext>();
        //    mockContext.Setup(x => x.CallActivityAsync<string>("E1_SayHello", "Tokyo")).ReturnsAsync("Hello Tokyo!");

        //    var result = await HelloSequence.Run(mockContext.Object);

        //    Assert.Equal(3, result.Count);
        //    Assert.Equal("Hello Tokyo!", result[0]);
        //}

        [TestMethod]
        public async Task GetMeteringsAndSaveToDatabaseAsync()
        {
            contextMock.Setup(o => o.GetInput<DateTime>()).Returns(now);
            var readsmetering = await timerTrigger.GetMeteringsAndSaveToDatabaseAsync(contextMock.Object);
            Assert.IsFalse(Errors.Any());
        }

        [TestMethod]
        public async Task GetReadingsAndSaveToDataLakeAsync()
        {
            if (!TimerTrigger.App.DataLake.GetJson("Raw", "ReadPartitionsFromDb.json", FolderStructure.DatePath, out List<ReadPartition> readPartitionsFromDb))
                throw new Exception("Run GetMeteringsAndSaveToDatabaseAsync() first");

            contextMock.Setup(o => o.GetInput<List<ReadPartition>>()).Returns(readPartitionsFromDb);
            var readsDataLake = await timerTrigger.GetReadingsAndSaveToDataLakeAsync(contextMock.Object);
            await new ReadingsRaw(TimerTrigger.App).SaveLoadedReadPartitionToDataLake(readsDataLake);
            Assert.IsFalse(Errors.Any());
        }

        [TestMethod]
        public void RefineReadingsAndImport()
        {
            Assert.IsTrue(new ReadingsRaw(TimerTrigger.App).TryGetAlreadyLoadedReads(out List<ReadPartition> readsDataLake));

            for (int i = 0; i < 50; i++)  //A high number 
            {
                TimerTrigger.App.Log.LogInformation($"DatalakePaths left: {readsDataLake.Where(o => o.LoadedToDatabase == null).GroupBy(o => o.DataLakePath).Count()}.");
                contextMock.Setup(o => o.GetInput<List<ReadPartition>>()).Returns(readsDataLake);
                readsDataLake = timerTrigger.RefineReadingsAndImport(contextMock.Object);

                if (i == 49)
                    TimerTrigger.App.Log.LogError("When loading all readings to database, the for-loop came up to 49 loops and thats way to much. A developer should look at it.");

                if (readsDataLake.All(o => o.LoadedToDatabase == true))
                    break;
            }
            Assert.IsFalse(Errors.Any());
        }

        [TestMethod]
        public void CleanDatabase()
        {
            timerTrigger.CleanDatabase(contextMock.Object);
            Assert.IsFalse(Errors.Any());
        }
    }
}
