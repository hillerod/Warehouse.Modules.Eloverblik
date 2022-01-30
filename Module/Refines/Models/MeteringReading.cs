using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Module.Refines.Models
{

    public class MeteringReadingResult
    {
        public MeteringReading[] result { get; set; }
    }

    public class MeteringReading
    {
        public Myenergydata_Marketdocument MyEnergyData_MarketDocument { get; set; }
        public bool success { get; set; }
        public string errorCode { get; set; }
        public string errorText { get; set; }
        public string id { get; set; }
        public object stackTrace { get; set; }
    }

    public class Myenergydata_Marketdocument
    {
        public string mRID { get; set; }
        public DateTime createdDateTime { get; set; }
        public string sender_MarketParticipantname { get; set; }
        public Sender_MarketparticipantMrid sender_MarketParticipantmRID { get; set; }
        [JsonProperty("period.timeInterval")]
        public PeriodTimeinterval periodtimeInterval { get; set; }
        public Timesery[] TimeSeries { get; set; }
    }

    public class Sender_MarketparticipantMrid
    {
        public object codingScheme { get; set; }
        public object name { get; set; }
    }

    public class PeriodTimeinterval
    {
        public DateTime start { get; set; }
        public DateTime end { get; set; }
    }

    public class Timesery
    {
        public string mRID { get; set; }

        private string _businessType;
        public string businessType { get { return businessTypeDictionary.TryGetValue(_businessType, out string res) ? res : _businessType; } set { _businessType = value; } }

        private static readonly Dictionary<string, string> businessTypeDictionary = new Dictionary<string, string>(){
            {"A01","Production"},
            {"A04","Consumption"},
            {"A64","Consumption (profiled)"},
        };

        public string curveType { get; set; }

        private string _measurement_Unitname;

        [JsonProperty("measurement_Unit.name")]
        public string measurement_Unitname { get { return measurement_UnitnameDictionary.TryGetValue(_measurement_Unitname, out string res) ? res : _measurement_Unitname; } set { _measurement_Unitname = value; } }

        private static readonly Dictionary<string, string> measurement_UnitnameDictionary = new Dictionary<string, string>(){
            {"K3", "kVArh"},
            {"KWH", "kWh"},
            {"KWT", "kW"},
            {"MAW", "MW"},
            {"MWH", "MWh"},
            {"TNE", "Tonne"},
            {"Z03", "MVAr"},
            {"Z14", "Tariff Code"},
        };

        public Marketevaluationpoint MarketEvaluationPoint { get; set; }
        public Period[] Period { get; set; }
    }

    public class Marketevaluationpoint
    {
        public Mrid mRID { get; set; }
    }

    public class Mrid
    {
        public string codingScheme { get; set; }
        public string name { get; set; }
    }

    public class Period
    {
        public string resolution { get; set; }
        public Timeinterval timeInterval { get; set; }
        public Point[] Point { get; set; }
    }

    public class Timeinterval
    {

        public DateTime start { get; set; }
        public DateTime end { get; set; }
    }

    public class Point
    {
        public int position { get; set; }
        [JsonProperty("out_Quantity.quantity")]
        public double out_Quantityquantity { get; set; }

        private string _out_Quantityquality;

        [JsonProperty("out_Quantity.quality")]
        public string out_Quantityquality { get { return out_QuantityqualityDictionary.TryGetValue(_out_Quantityquality, out string res) ? res : _out_Quantityquality; } set { _out_Quantityquality = value; } }

        private static readonly Dictionary<string, string> out_QuantityqualityDictionary = new Dictionary<string, string>(){
            {"A01", "Adjusted"},
            {"A02", "Not available"},
            {"A03", "Estimated"},
            {"A04", "As provided"},
            {"A05", "Incomplete"},
        };
    }
}
