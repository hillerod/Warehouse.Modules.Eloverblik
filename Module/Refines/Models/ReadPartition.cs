using Module.Services;
using System;

namespace Module.Refines.Models
{
    public class ReadPartition
    {
        public ReadPartition()
        {

        }

        public ReadPartition(string id, DateTime from, DateTime to, TimeAggregation aggregation)
        {
            Id = id;
            DataLakePath = null;
            From = from;
            To = to;
            Aggregation = aggregation;
            LoadedToDataLake = false;
            LoadedToDatabase = null;
            TimesTriedLoadingFromService = 0;
        }

        public string Id { get; set; }
        public string DataLakePath { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public TimeAggregation Aggregation { get; set; }
        public bool LoadedToDataLake { get; set; }
        public int TimesTriedLoadingFromService { get; set; }
        public bool? LoadedToDatabase { get; set; }
        public double DaysSpan { get; set; }
    }
}