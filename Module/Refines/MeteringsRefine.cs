using Bygdrift.CsvTools;
using Bygdrift.Warehouse;
using Module.Refines.Models;
using Module.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Module.Refines
{
    public class MeteringsRefine
    {
        public Csv Csv;

        public MeteringsRefine(AppBase app, Csv meteringsFromDb, IEnumerable<MeteringDetail> meteringDetails)
        {
            ReadPartitionsFromDb = new List<ReadPartition>();
            if (meteringDetails == null || !meteringDetails.Any())
                return;

            MeteringIds = meteringDetails.Select(o => o.meteringPointId).ToArray();
            CreateCsv(meteringsFromDb, meteringDetails, app.Now);
            app.Mssql.MergeCsv(Csv, "Meterings", "meteringPointId", false, false);
        }

        public string[] MeteringIds { get; }

        public List<ReadPartition> ReadPartitionsFromDb { get; private set; }

        public void CreateCsv(Csv fromDb, IEnumerable<MeteringDetail> details, DateTime now)
        {
            Csv = new Csv(
                "meteringPointId, typeOfMP, balanceSupplierName, streetName, buildingNumber, floorId, roomId, postcode, cityName, locationDescription, meterReadingOccurrence, firstConsumerPartyName, secondConsumerPartyName, consumerCVR, dataAccessCVR, meterNumber, consumerStartDate, parentPointId," +
                "energyTimeSeriesMeasureUnit, estimatedAnnualVolume, gridOperator, balanceSupplierStartDate, physicalStatusOfMP, subTypeOfMP, meterCounterMultiplyFactor, meterCounterUnit");

            var r = 0;
            int colDataPerHourFrom = -1;
            int colDataPerHourTo = -1;
            int colDataPerDayFrom = -1;
            int colDataPerDayTo = -1;
            int colDataPerMonthFrom = -1;
            int colDataPerMonthTo = -1;
            if (fromDb != null)
            {
                fromDb.TryGetColId("dataPerHourFrom", out colDataPerHourFrom);
                fromDb.TryGetColId("dataPerHourTo", out colDataPerHourTo);
                fromDb.TryGetColId("dataPerDayFrom", out colDataPerDayFrom);
                fromDb.TryGetColId("dataPerDayTo", out colDataPerDayTo);
                fromDb.TryGetColId("dataPerMonthFrom", out colDataPerMonthFrom);
                fromDb.TryGetColId("dataPerMonthTo", out colDataPerMonthTo);
            }

            foreach (var point in details)
            {
                var dbRow = fromDb?.GetRowRecordsFirstMatch("meteringPointId", point.meteringPointId);
                ReadPartitionsFromDb.Add(new ReadPartition(point.meteringPointId, GetRecordDate(dbRow, colDataPerHourFrom, true), GetRecordDate(dbRow, colDataPerHourTo, true), TimeAggregation.Hour));
                ReadPartitionsFromDb.Add(new ReadPartition(point.meteringPointId, GetRecordDate(dbRow, colDataPerDayFrom, true), GetRecordDate(dbRow, colDataPerDayTo, true), TimeAggregation.Day));
                ReadPartitionsFromDb.Add(new ReadPartition(point.meteringPointId, GetRecordDate(dbRow, colDataPerMonthFrom, true), GetRecordDate(dbRow, colDataPerMonthTo, true), TimeAggregation.Month));

                Csv.AddRecord(r, 0, point.meteringPointId);
                Csv.AddRecord(r, 1, point.typeOfMP);
                Csv.AddRecord(r, 2, point.balanceSupplierName);
                Csv.AddRecord(r, 3, point.streetName);
                Csv.AddRecord(r, 4, point.buildingNumber);
                Csv.AddRecord(r, 5, point.floorId);
                Csv.AddRecord(r, 6, point.roomId);
                Csv.AddRecord(r, 7, point.postcode);
                Csv.AddRecord(r, 8, point.cityName);
                Csv.AddRecord(r, 9, point.locationDescription);
                Csv.AddRecord(r, 10, point.meterReadingOccurrence);
                Csv.AddRecord(r, 11, point.firstConsumerPartyName);
                Csv.AddRecord(r, 12, point.secondConsumerPartyName);
                Csv.AddRecord(r, 13, point.consumerCVR);
                Csv.AddRecord(r, 14, point.dataAccessCVR);
                Csv.AddRecord(r, 15, point.meterNumber);

                Csv.AddRecord(r, 16, point.consumerStartDate.ToString("s"));
                //Details:
                Csv.AddRecord(r, 18, point.energyTimeSeriesMeasureUnit);
                Csv.AddRecord(r, 19, point.estimatedAnnualVolume);
                Csv.AddRecord(r, 20, point.gridOperatorName);
                Csv.AddRecord(r, 21, point.balanceSupplierStartDate?.ToString("s"));
                Csv.AddRecord(r, 22, point.physicalStatusOfMP);
                Csv.AddRecord(r, 23, point.subTypeOfMP);
                Csv.AddRecord(r, 24, point.meterCounterMultiplyFactor);
                Csv.AddRecord(r, 25, point.meterCounterUnit);
                r++;

                foreach (var childPoint in point.childMeteringPoints)
                {
                    Csv.AddRecord(r, 0, childPoint.meteringPointId);
                    Csv.AddRecord(r, 1, childPoint.typeOfMP);
                    Csv.AddRecord(r, 2, point.balanceSupplierName);
                    Csv.AddRecord(r, 3, point.streetName);
                    Csv.AddRecord(r, 4, point.buildingNumber);
                    Csv.AddRecord(r, 5, point.floorId);
                    Csv.AddRecord(r, 6, point.roomId);
                    Csv.AddRecord(r, 7, point.postcode);
                    Csv.AddRecord(r, 8, point.cityName);
                    Csv.AddRecord(r, 9, point.locationDescription);
                    Csv.AddRecord(r, 10, childPoint.meterReadingOccurrence);
                    Csv.AddRecord(r, 11, point.firstConsumerPartyName);
                    Csv.AddRecord(r, 12, point.secondConsumerPartyName);
                    Csv.AddRecord(r, 13, point.consumerCVR);
                    Csv.AddRecord(r, 14, point.dataAccessCVR);
                    Csv.AddRecord(r, 15, childPoint.meterNumber);
                    Csv.AddRecord(r, 16, point.consumerStartDate.ToString("s"));
                    Csv.AddRecord(r, 17, point.meteringPointId);
                    r++;
                }
            }
        }

        private DateTime GetRecordDate(Dictionary<int, object> row, int col, bool ifNullReturnDatetTimeMax)
        {
            if (row != null && row.TryGetValue(col, out object val) && Csv.RecordToType(val, out DateTime res))
                return res;

            return ifNullReturnDatetTimeMax ? DateTime.MaxValue : DateTime.MinValue;
        }
    }
}
