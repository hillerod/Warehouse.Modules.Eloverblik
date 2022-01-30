using Bygdrift.Warehouse;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.Raw;
using Module.Refines.Models;
using Module.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ModuleTests.Refine
{
    [TestClass]
    public class ReadingsRawTests
    {
        private readonly AppBase app = new();
        private readonly DateTime now = DateTime.Now;

        [TestMethod]
        public async Task ReadingRawProcessAsync()
        {
            var readingsRaw = new ReadingsRaw(app);
            var minLimit = now.AddDays(-30);
            var groups = new List<ReadPartition> {
                new ReadPartition("570715000000125263", minLimit, now.AddDays(-10), TimeAggregation.Month),
                new ReadPartition("570715000000183645", minLimit, now.AddDays(-10), TimeAggregation.Month),
                new ReadPartition("570715000000787782", minLimit, now.AddDays(-10), TimeAggregation.Month),
                new ReadPartition("571313174111418781", minLimit, now.AddDays(-10), TimeAggregation.Month),
                new ReadPartition("571313174111420500", minLimit, now.AddDays(-10), TimeAggregation.Month),
            };
            var readings = await readingsRaw.GetReads(groups, 3, 6);
            var path = await readingsRaw.SaveLoadedReadPartitionToDataLake(readings);
            Assert.IsTrue(readingsRaw.TryGetAlreadyLoadedReads(out _));
        }



        //[TestMethod]
        //public async Task SaveToDataLakeAsync()
        //{
        //    var now = DateTime.Now;
        //    var minLimit = now.AddDays(-30);
        //    var readingsRaw = new ReadingsRaw(app, now);

        //    var groups = new List<ReadPartition> {
        //        new ReadPartition("570715000000125263", minLimit, now.AddDays(-10)),
        //        new ReadPartition("570715000000183645", minLimit, now.AddDays(-10)),
        //        new ReadPartition("570715000000787782", minLimit, now.AddDays(-10)),
        //        new ReadPartition("570715000000183645", minLimit, now.AddDays(-10)),
        //        new ReadPartition("570715000000787782", minLimit, now.AddDays(-10)),
        //    };

        //    var readings = await readingsRaw.GetReads(TimeAggregation.Day, groups);
        //    var paths = await readingsRaw.SaveReadingsToDataLakeAsync(TimeAggregation.Day, readings, new TimeSpan(0, 3, 0));
        //}

        //[TestMethod]
        //public async Task GroupReadingSpans2()
        //{
        //    var now = DateTime.Now;
        //    var stopTime = DateTime.Now.AddMinutes(7);
        //    var res = new List<string>();
        //    var LastMeteringReadsJson = await GetDataAsync("LastMeteringReads.json");
        //    var lastMeteringReads = JsonConvert.DeserializeObject<List<ReadPartition>>(LastMeteringReadsJson);
        //    var readingsRaw = new ReadingsRaw(app, now, 50, new TimeSpan(0, 4, 0), new TimeSpan(0, 5, 0));
        //    foreach (var item in lastMeteringReads.GroupBy(o => o.Aggregation))
        //    {
        //        var readings = await readingsRaw.GetReadingsAsync(item.Key, item.ToList());
        //        res.AddRange(await readingsRaw.SaveReadingsToDataLakeAsync(item.Key, readings, new TimeSpan(0, 3, 0)));
        //        if (DateTime.Now > stopTime)
        //            break;
        //    }
        //    var errors = app.Log.GetErrors();
        //    Assert.IsFalse(errors.Any());
        //}

        [TestMethod]
        public void GroupReadingSpans()
        {
            var now = DateTime.Now;
            var minLimit = now.AddDays(-30);

            var res = ReadingsRaw.GroupReadPartitions(minLimit, now, new List<ReadPartition> { new ReadPartition("0", DateTime.MinValue, DateTime.MaxValue, TimeAggregation.Month) }, 2, 1);
            Assert.IsTrue(res.First().From == now.Date && res.First().To == now.Date);

            res = ReadingsRaw.GroupReadPartitions(minLimit, now, new List<ReadPartition> { new ReadPartition("0", DateTime.MaxValue, DateTime.MaxValue, TimeAggregation.Month) }, 2, 1);
            Assert.IsTrue(res.First().From == minLimit.Date && res.First().To == now.Date);

            res = ReadingsRaw.GroupReadPartitions(minLimit, now, new List<ReadPartition> { new ReadPartition("0", DateTime.MaxValue, DateTime.MinValue, TimeAggregation.Month) }, 2, 1);
            Assert.IsTrue(res.First().From == minLimit.Date && res.First().To == now.Date);

            res = ReadingsRaw.GroupReadPartitions(minLimit, now, new List<ReadPartition> { new ReadPartition("0", DateTime.MinValue, DateTime.MinValue, TimeAggregation.Month) }, 2, 1);
            Assert.IsTrue(res.First().From == minLimit.Date && res.First().To == now.Date);

            res = ReadingsRaw.GroupReadPartitions(minLimit, now, new List<ReadPartition> { new ReadPartition("0", minLimit, now.AddDays(1), TimeAggregation.Month) }, 2, 1);
            Assert.IsTrue(res.First().From == now.Date);

            res = ReadingsRaw.GroupReadPartitions(minLimit, now, new List<ReadPartition> { new ReadPartition("0", minLimit, now.AddDays(-1), TimeAggregation.Month) }, 2, 1);
            Assert.IsTrue(res.First().From.AddDays(2) == now.Date);

            res = ReadingsRaw.GroupReadPartitions(minLimit, now, new List<ReadPartition> { new ReadPartition("0", minLimit.AddDays(1), now, TimeAggregation.Month) }, 2, 1);
            Assert.IsTrue(res.First().From == minLimit.Date);

            res = ReadingsRaw.GroupReadPartitions(minLimit, now, new List<ReadPartition> { new ReadPartition("0", minLimit.AddDays(-1), now, TimeAggregation.Month) }, 2, 1);
            Assert.IsTrue(res.First().From == now.Date);

            res = ReadingsRaw.GroupReadPartitions(minLimit, now, new List<ReadPartition> {
                new ReadPartition("0", now.AddDays(-40), now, TimeAggregation.Month),
                new ReadPartition("0", now.AddDays(-30), now, TimeAggregation.Month) }, 2, 1);
            Assert.IsTrue(res.First().From == now.Date);
        }
    }
}
