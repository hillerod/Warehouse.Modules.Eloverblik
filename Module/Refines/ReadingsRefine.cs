using Bygdrift.CsvTools;
using Module.Refines.Models;
using Module.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Bygdrift.Warehouse;

namespace Module.Refines
{
    public class ReadingsRefine
    {
        private readonly string shortTimeAggregation;
        private readonly AppBase<Settings> app;
        private readonly string tableName;
        public readonly TimeAggregation TimeAggregation;
        public List<string> MeteringPointIds = new();
        public Csv Csv;

        public ReadingsRefine(AppBase<Settings> app, string tableName, string datalakePath, TimeAggregation timeAggregation)
        {
            this.app = app;
            this.tableName = tableName;
            TimeAggregation = timeAggregation;
            shortTimeAggregation = timeAggregation.ToString().Substring(0, 1);
            if (string.IsNullOrEmpty(datalakePath))
                return;

            if (!app.DataLake.GetJson(datalakePath, out MeteringReadingResult meteringReading))
                app.Log.LogWarning($"There where no data in the data lake file: {datalakePath}.");
            else
            {
                var data = meteringReading.result;
                CreateCsv(timeAggregation, data);
                app.Mssql.MergeCsv(Csv, tableName, "id", false, false);
                WriteSuccededMeteringsToDatabase();
            }
        }

        private void WriteSuccededMeteringsToDatabase()
        {
            if (!MeteringPointIds.Any())
                return;

            MeteringPointIds = MeteringPointIds.Distinct().ToList();

            var meteringUpdatesCsv = new Csv("meteringPointId");
            meteringUpdatesCsv.AddHeader($"dataPer{TimeAggregation}From");
            meteringUpdatesCsv.AddHeader($"dataPer{TimeAggregation}To");
            var minLimit = GetMinLimit(app, TimeAggregation).Date;
            foreach (var item in MeteringPointIds)
                meteringUpdatesCsv.AddRow(item, minLimit, app.LoadedLocal.Date);

            app.Mssql.MergeCsv(meteringUpdatesCsv, "Meterings", "meteringPointId", false, false);
        }

        public void RemoveExpiredRowsFromDatabase()
        {
            app.Mssql.RemoveOldRows(tableName, "timeintervalStart", GetMinLimit(app, TimeAggregation));
        }

        private static int GetMonthsToKeepData(AppBase<Settings> app, TimeAggregation aggregration) => aggregration switch
        {
            TimeAggregation.Day => app.Settings.MonthsToKeepReadingsPerDay,
            TimeAggregation.Hour => app.Settings.MonthsToKeepReadingsPerHour,
            TimeAggregation.Month => app.Settings.MonthsToKeepReadingsPerMonth,
            TimeAggregation.Quarter => app.Settings.MonthsToKeepReadingsPerMonth,
            TimeAggregation.Year => app.Settings.MonthsToKeepReadingsPerMonth,
            _ => throw new NotImplementedException()
        };

        private static DateTime GetMinLimit(AppBase<Settings> app, TimeAggregation aggregration)
        {
            return app.LoadedLocal.AddMonths(-GetMonthsToKeepData(app, aggregration));
        }

        public void CreateCsv(TimeAggregation timeAggregation, IEnumerable<MeteringReading> data)
        {
            Csv = new Csv("id, meteringPointId, businessType, measurementUnitName, resolution, timeintervalStart, timeintervalEnd, quantity, quality");
            var records = Enumerable.Empty<ReadingRecord>();
            if (timeAggregation == TimeAggregation.Hour)
                records = TimeAggregationPerHour(data);
            else if (timeAggregation == TimeAggregation.Day)
                records = TimeAggregationPerDay(data);
            else if (timeAggregation == TimeAggregation.Month)
                records = TimeAggregationPerMonth(data);

            var r = 1;
            foreach (var record in records)
            {
                Csv.AddRecord(r, 1, record.Id);
                Csv.AddRecord(r, 2, record.PointId);
                Csv.AddRecord(r, 3, record.BusinessType);
                Csv.AddRecord(r, 4, record.UnitName);
                Csv.AddRecord(r, 5, record.Resolution);
                Csv.AddRecord(r, 6, record.StartDanishTime);
                Csv.AddRecord(r, 7, record.EndDanishTime);
                Csv.AddRecord(r, 8, Math.Round(record.Quantity, 2));
                Csv.AddRecord(r, 9, record.Quality);
                r++;
            }
        }

        private List<ReadingRecord> TimeAggregationPerHour(IEnumerable<MeteringReading> data)
        {
            var res = new List<ReadingRecord>();
            foreach (var reading in data)
            {
                MeteringPointIds.Add(reading.id);
                foreach (var timeSery in reading.MyEnergyData_MarketDocument.TimeSeries)
                    foreach (var period in timeSery.Period)
                        foreach (var point in period.Point)
                            res.Add(new ReadingRecord(timeSery.mRID, period.timeInterval.start.AddHours(point.position - 1), period.timeInterval.start.AddHours(point.position), point.out_Quantityquantity, point.out_Quantityquality, timeSery.businessType, timeSery.measurement_Unitname, period.resolution, shortTimeAggregation));
            }

            RemoveRedundatRows(res);
            return res;
        }

        private List<ReadingRecord> TimeAggregationPerDay(IEnumerable<MeteringReading> data)
        {
            var res = new List<ReadingRecord>();
            foreach (var reading in data)
            {
                MeteringPointIds.Add(reading.id);
                foreach (var timeSery in reading.MyEnergyData_MarketDocument.TimeSeries)
                    foreach (var period in timeSery.Period)
                    {
                        var point = period.Point.First();  //There are only multiple points when working with hours

                        if (period.resolution == "PT1D")  //There is a record for each day
                            res.Add(new ReadingRecord(timeSery.mRID, period.timeInterval.start, period.timeInterval.end, point.out_Quantityquantity, point.out_Quantityquality, timeSery.businessType, timeSery.measurement_Unitname, period.resolution, shortTimeAggregation));
                        else  //There are multiple days between each record:
                        {
                            var days = Math.Floor((period.timeInterval.end - period.timeInterval.start).TotalDays);
                            var quantityPerDay = point.out_Quantityquantity / days;
                            for (int day = 0; day < days; day++)
                                res.Add(new ReadingRecord(timeSery.mRID, period.timeInterval.start.AddDays(day), period.timeInterval.start.AddDays(day + 1), quantityPerDay, point.out_Quantityquality + "_DividedPerDay", timeSery.businessType, timeSery.measurement_Unitname, period.resolution, shortTimeAggregation));
                        }
                    }
            }
            RemoveRedundatRows(res);
            return res;
        }

        private List<ReadingRecord> TimeAggregationPerMonth(IEnumerable<MeteringReading> data)
        {
            var res = new List<ReadingRecord>();
            var fragmentedRecordsPerDay = new List<ReadingRecord>();

            foreach (var reading in data)
            {
                MeteringPointIds.Add(reading.id);
                foreach (var timeSery in reading.MyEnergyData_MarketDocument.TimeSeries)
                    foreach (var period in timeSery.Period)
                    {
                        var point = period.Point.First();  //There are only multiple points when working with hours

                        if (period.resolution == "P1M")  //There is a record for each month
                            res.Add(new ReadingRecord(timeSery.mRID, period.timeInterval.start, period.timeInterval.end, point.out_Quantityquantity, point.out_Quantityquality, timeSery.businessType, timeSery.measurement_Unitname, period.resolution, shortTimeAggregation));
                        else  //There are multiple days between each record and sometimes it can be 2 days in march and another group with 28 days in the sam month. So if this method is called, I need to go thorugh all records and lay them out per day and gather them into months:
                        {
                            var days = Math.Floor((period.timeInterval.end - period.timeInterval.start).TotalDays);
                            var quantityPerDay = point.out_Quantityquantity / days;
                            for (int day = 0; day < days; day++)
                                fragmentedRecordsPerDay.Add(new ReadingRecord(timeSery.mRID, period.timeInterval.start.AddDays(day), period.timeInterval.start.AddDays(day + 1), quantityPerDay, point.out_Quantityquality, timeSery.businessType, timeSery.measurement_Unitname, period.resolution, shortTimeAggregation));
                        }
                    }
            }

            foreach (var fragmentGroup in fragmentedRecordsPerDay.GroupBy(o => o.PointId))
            {
                var recordsPerMonthCurrentPointId = res.Where(o => o.PointId == fragmentGroup.Key).ToList();
                foreach (var fragment in fragmentGroup)
                {
                    var recordPerMonth = recordsPerMonthCurrentPointId.SingleOrDefault(o => o.StartDanishTime.Year == fragment.StartDanishTime.Year && o.StartDanishTime.Month == fragment.StartDanishTime.Month);
                    if (recordPerMonth == null)  //then create a new month:
                    {
                        recordPerMonth = fragment;
                        recordPerMonth.Resolution = "P1M";
                        recordPerMonth.StartDanishTime = new DateTime(fragment.StartDanishTime.Year, fragment.StartDanishTime.Month, 1);
                        recordPerMonth.EndDanishTime = recordPerMonth.StartDanishTime.AddMonths(1);

                        res.Add(recordPerMonth);
                        recordsPerMonthCurrentPointId.Add(recordPerMonth);
                    }
                    else
                        recordPerMonth.Quantity += fragment.Quantity;
                }
            }

            RemoveRedundatRows(res);
            return res;
        }

        private static void RemoveRedundatRows(List<ReadingRecord> res)
        {
            var duplicates = res.GroupBy(o => o.Id).Where(o => o.Count() > 1);
            if (duplicates.Any())
                foreach (var item in duplicates)
                {
                    var best = item.FirstOrDefault(o => o.Quality.Equals("As provided"));
                    if (best == null)
                        best = item.FirstOrDefault(o => o.Quality.StartsWith("As provided"));
                    if (best == null)
                        best = item.First();

                    foreach (var sub in item)
                        if (sub != best)
                            res.Remove(sub);
                }
        }
    }

    class ReadingRecord
    {
        private static readonly TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
        public string PointId;
        public DateTime StartDanishTime;
        public DateTime EndDanishTime;
        public double Quantity;
        public string Quality;
        public string BusinessType;
        public string UnitName;
        public string Resolution;
        public string Id;

        public ReadingRecord(string pointId, DateTime start, DateTime end, double quantity, string quality, string businessType, string unitName, string resolution, string shortTimeAggregation)
        {
            PointId = pointId;
            StartDanishTime = TimeZoneInfo.ConvertTimeFromUtc(start, timeZone);
            EndDanishTime = TimeZoneInfo.ConvertTimeFromUtc(end, timeZone);
            Quantity = quantity;
            Quality = quality;
            BusinessType = businessType;
            UnitName = unitName;
            Resolution = resolution;
            Id = PointId + "_" + StartDanishTime.ToString("yyyyMMddHH") + "_" + shortTimeAggregation;
        }
    }
}