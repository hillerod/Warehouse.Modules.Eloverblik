using Bygdrift.Warehouse;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ModuleTests.Service
{
    [TestClass]
    public class WebServiceTest
    {
        private WebService _service;
        /// <summary>Path to project base</summary>
        public static readonly string BasePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\"));
        private readonly AppBase app = new();

        public WebService Service { get { return _service ??= new WebService(app.Config["EloverblikToken"], app.Log); } }

        [TestMethod]
        public async Task SendWrongCredentials()
        {
            var service = new WebService("1", app.Log);
            var result = await service.GetMeteringPointsAsync();
            Assert.IsFalse(app.Log.HasErrors());
            Assert.IsTrue(app.Log.GetErrors("Unauthorized").Any());
        }

        [TestMethod]
        public async Task GetMeteringPoints()
        {
            var result = await Service.GetMeteringPointsAsync();
            ToFile(result, "MeteringPoints.json");
        }

        [TestMethod]
        public async Task GetMeteringPointsDetails()
        {
            var meteringPointIds = new string[] { "571313175500079231" };
            var res = await Service.GetMeteringPointsDetailsAsync(null, meteringPointIds);
            var json = JsonConvert.SerializeObject(res);
            ToFile(json, "MeteringPointsDetails_One points extracted.json");
        }

        [TestMethod]
        public async Task GetMeteringPointsDetailsAll()
        {
            var meteringPointIds = await Service.GetMeteringPointIdsAsync();
            var res = await Service.GetMeteringPointsDetailsAsync(null, meteringPointIds);
            var json = JsonConvert.SerializeObject(res);
            ToFile(json, "MeteringPointsDetails.json");
        }

        [TestMethod]
        public async Task GetMeteringPointsCharges()
        {
            var meteringPointIds = new string[] { "571313174115454372", "571313174115454389" };
            var res = await Service.GetMeteringPointsChargesAsync(null, meteringPointIds);
            ToFile(res, "MeteringPointsCharges_Two points extracted.json");
        }

        [TestMethod]
        public async Task GetMeterDataTimeSeriesForAYearForTwoPoints()
        {
            var meteringPointIds = new string[] { "571313174110769501" };
            //var meteringPointIds = new string[] { "571313175562972617" };  //En måler som ikke giver data og altså har en fejl
            var dateFrom = new DateTime(2020, 1, 1);
            var dateTo = new DateTime(2020, 2, 1);

            var res = await Service.GetReadingTimeSeriesAsync(dateFrom, dateTo, TimeAggregation.Day, new TimeSpan(0,1,0), meteringPointIds);
            ToFile(res.Json, $"meterDataTimeSeries_Two points extracted_Consumption one year aggregated to 'Year'.json");
        }

        [TestMethod]
        public async Task GetmeteringReadingsPerHourForAllPoints()
        {
            var response = await Service.GetMeteringPointsAsync();
            var meteringPointIds = await Service.GetMeteringPointIdsAsync();
            var res = await Service.GetReadingTimeSeriesAsync(DateTime.Now.AddMonths(-2), DateTime.Now, TimeAggregation.Hour, new TimeSpan(0, 1, 0), meteringPointIds);
            ToFile(res.Json, "RedingsPerHour.json");
        }

        [TestMethod]
        public async Task GetmeteringReadingsPerDayForAllPoints()
        {
            var meteringPointIds = new string[] { "571313174110769501" };
            var res = await Service.GetReadingTimeSeriesAsync(DateTime.Now.AddYears(-1), DateTime.Now, TimeAggregation.Day, new TimeSpan(0, 0, 1),meteringPointIds);
            ToFile(res.Json, "RedingsPerDay.json");
        }

        [TestMethod]
        public async Task GetmeteringReadingsPerMonthForAllPoints()
        {
            var response = Service.GetMeteringPointsAsync().Result;
            var meteringPointIds = Service.GetMeteringPointIdsAsync().Result;
            var res = await Service.GetReadingTimeSeriesAsync(DateTime.Now.AddYears(-3), DateTime.Now, TimeAggregation.Month, new TimeSpan(0, 1, 0), meteringPointIds);
            ToFile(res.Json, "RedingsPerMonth.json");
        }

        [TestMethod]
        public async Task GetMeterDataTimeSeriesForAYearForAllPoints()
        {
            var response = Service.GetMeteringPointsAsync().Result;
            var meteringPointIds = Service.GetMeteringPointIdsAsync().Result;
            var res = await Service.GetReadingTimeSeriesAsync(DateTime.Now.AddYears(-5), DateTime.Now, TimeAggregation.Year, new TimeSpan(0, 1, 0), meteringPointIds);
            ToFile(res.Json, "RedingsPerYear.json");
        }

        [TestMethod]
        public async Task GetMeterDataTimeSeries()
        {
            var meteringPointIds = new string[] { "571313174115454372", "571313174115454389" };
            var dateFrom = new DateTime(2021, 1, 1);
            var dateTo = new DateTime(2021, 3, 1);

            await GetMeterdataBySpecificAggregation(Service, meteringPointIds, dateFrom, dateTo, TimeAggregation.Day);
            await GetMeterdataBySpecificAggregation(Service, meteringPointIds, dateFrom, dateTo, TimeAggregation.Hour);
            await GetMeterdataBySpecificAggregation(Service, meteringPointIds, dateFrom, dateTo, TimeAggregation.Month);
            await GetMeterdataBySpecificAggregation(Service, meteringPointIds, dateFrom, dateTo, TimeAggregation.Quarter);
            await GetMeterdataBySpecificAggregation(Service, meteringPointIds, dateFrom, dateTo, TimeAggregation.Year);
        }

        [TestMethod]
        public async Task GetMeterReadings()
        {
            var meteringPointIds = new string[] { "571313174115454372", "571313174115454389" };
            var dateFrom = new DateTime(2021, 1, 1);
            var dateTo = new DateTime(2021, 5, 1);

            var res = await Service.GetSingleMeterReadingsAsync(dateFrom, dateTo, null, meteringPointIds);
            ToFile(res, $"meterReadings_Two points extracted_Consumption for January.json");
        }

        private async Task GetMeterdataBySpecificAggregation(WebService service, string[] meteringPointIds, DateTime dateFrom, DateTime dateTo, TimeAggregation aggregation)
        {
            var res = await service.GetReadingTimeSeriesAsync(dateFrom, dateTo, aggregation, new TimeSpan(0, 1, 0), meteringPointIds);
            ToFile(res.Json, $"meterDataTimeSeries_Two points extracted_Consumption for January aggregated to '{aggregation}'.json");
        }

        private static void ToFile(string json, string fileName)
        {
            var filePath = Path.Combine(BasePath, "Files", "In", fileName);
            var prettyJson = JToken.Parse(json).ToString(Formatting.Indented);
            File.WriteAllText(filePath, prettyJson);
        }
    }
}
