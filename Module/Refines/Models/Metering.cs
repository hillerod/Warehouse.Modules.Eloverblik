using System;
using System.Collections.Generic;

namespace Module.Refines.Models
{
    public class MeteringPointResult
    {
        public Metering[] result { get; set; }
    }

    public class Metering
    {
        public string meteringPointId { get; set; }
        public string balanceSupplierName { get; set; }
        public string streetCode { get; set; }
        public string streetName { get; set; }
        public string buildingNumber { get; set; }
        public string floorId { get; set; }
        public string roomId { get; set; }
        public string postcode { get; set; }
        public string cityName { get; set; }
        public string citySubDivisionName { get; set; }
        public string municipalityCode { get; set; }
        public string locationDescription { get; set; }
        //public string settlementMethod { get; set; }

        private string _settlementMethod;
        public string settlementMethod
        {
            get { return settlementMethodDictionary.TryGetValue(_settlementMethod, out string res) ? res : _settlementMethod; }
            set { _settlementMethod = value; }
        }

        private static readonly Dictionary<string, string> settlementMethodDictionary = new Dictionary<string, string>(){
            {"D01","Flex settled"},
            {"E01","Profiled settled"},
            {"E02","Non-profiled settled"},
        };

        public string meterReadingOccurrence { get; set; }
        public string firstConsumerPartyName { get; set; }
        public string secondConsumerPartyName { get; set; }
        public string consumerCVR { get; set; }
        public string dataAccessCVR { get; set; }
        public string meterNumber { get; set; }
        public DateTime consumerStartDate { get; set; }
        public bool hasRelation { get; set; }
        public Childmeteringpoint[] childMeteringPoints { get; set; }

        //public string typeOfMP { get; set; }

        private string _typeOfMP;
        public string typeOfMP
        {
            get { return typeOfMPDictionary.TryGetValue(_typeOfMP, out string res) ? res: _typeOfMP; }
            set { _typeOfMP = value; }
        }

        private static readonly Dictionary<string, string> typeOfMPDictionary = new Dictionary<string, string>(){
            {"D01","VE Production"},
            {"D02","Analysis"},
            {"D04","Surplus production group 6"},
            {"D05","Net production"},
            {"D06","Supply to grid"},
            {"D07","Consumption from grid"},
            {"D08","Wholesale services / information"},
            {"D09","Own production"},
            {"D10","Net from grid"},
            {"D11","Net to grid"},
            {"D12","Total consumption"},
            {"D14","Electrical heating"},
            {"D15","Net consumption"},
            {"D17","Other consumption"},
            {"D18","Other production"},
            {"D99","Internal use"},
            {"E17","Consumption"},
            {"E18","Production"},
        };
    }

    //public class Childmeteringpoint
    //{
    //    public string meteringPointId { get; set; }
    //    public string parentMeteringPointId { get; set; }
    //    public string typeOfMP { get; set; }
    //    public string meterReadingOccurrence { get; set; }
    //    public string meterNumber { get; set; }
    //}
}
