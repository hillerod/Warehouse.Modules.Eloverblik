using System;
using System.Collections.Generic;

namespace Module.Refines.Models
{

    public class MeteringPointDetailResult
    {
        public Result[] result { get; set; }
    }

    public class Result
    {
        public MeteringDetail result { get; set; }
        public bool success { get; set; }
        public string errorCode { get; set; }
        public string errorText { get; set; }
        public string id { get; set; }
        public object stackTrace { get; set; }
    }

    public class MeteringDetail
    {
        public string meteringPointId { get; set; }
        public string parentMeteringPointId { get; set; }

        private string _typeOfMP;
        public string typeOfMP { get { return typeOfMPDictionary.TryGetValue(_typeOfMP, out string res) ? res : _typeOfMP; } set { _typeOfMP = value; } }

        private static Dictionary<string, string> typeOfMPDictionary = new Dictionary<string, string>
        {
            { "D01", "VE Production" },
            { "D02", "Analysis" },
            { "D04", "Surplus production group 6" },
            { "D05", "Net production" },
            { "D06", "Supply to grid" },
            { "D07", "Consumption from grid" },
            { "D08", "Wholesale services / information" },
            { "D09", "Own production" },
            { "D10", "Net from grid" },
            { "D11", "Net to grid" },
            { "D12", "Total consumption" },
            { "D14", "Electrical heating" },
            { "D15", "Net consumption" },
            { "D17", "Other consumption" },
            { "D18", "Other production" },
            { "D99", "Internal use" },
            { "E17", "Consumption" },
            { "E18", "Production" },
        };

        private string _energyTimeSeriesMeasureUnit;
        public string energyTimeSeriesMeasureUnit { get { return energyTimeSeriesMeasureUnitDictionary.TryGetValue(_energyTimeSeriesMeasureUnit, out string res) ? res : _energyTimeSeriesMeasureUnit; } set { _energyTimeSeriesMeasureUnit = value; } }

        private static Dictionary<string, string> energyTimeSeriesMeasureUnitDictionary = new Dictionary<string, string>
        {
            { "AMP", "Ampere" },
            { "H87", "STK" },
            { "K3", "kVArh" },
            { "KWH", "kWh" },
            { "KWT", "kW" },
            { "MAW", "MW" },
            { "MWH", "MWh" },
            { "TNE", "Tonne" },
            { "Z03", "MVAr" },
            { "Z14", "Danish Tariff Code" },
        };

        public string estimatedAnnualVolume { get; set; }

        private string _settlementMethod;
        public string settlementMethod { get { return settlementMethodDictionary.TryGetValue(_settlementMethod, out string res) ? res : _settlementMethod; } set { _settlementMethod = value; } }

        private static readonly Dictionary<string, string> settlementMethodDictionary = new Dictionary<string, string>
        {
            { "D01", "Flex settled" },
            { "E01", "Profiled settled" },
            { "E02", "Non-profiled settled" },
        };

        public string meterNumber { get; set; }
        public string gridOperatorName { get; set; }
        public string meteringGridAreaIdentification { get; set; }

        private string _netSettlementGroup;
        public string netSettlementGroup { get { return netSettlementGroupDictionary.TryGetValue(_netSettlementGroup, out string res) ? res : _netSettlementGroup; } set { _netSettlementGroup = value; } }

        private static readonly Dictionary<string, string> netSettlementGroupDictionary = new Dictionary<string, string>
        {
            { "0", "No Net Settlement" },
            { "1", "Net Settlement Group 1" },
            { "2", "Net Settlement Group 2" },
            { "3", "Net Settlement Group 3" },
            { "4", "Net Settlement Group 4" },
            { "5", "Net Settlement Group 5" },
            { "6", "Net Settlement Group 6" },
            { "7", "Net Settlement Group 7" },
            { "99", "Net Settlement Group 99" },

        };

        private string _physicalStatusOfMP;
        public string physicalStatusOfMP { get { return physicalStatusOfMPDictionary.TryGetValue(_physicalStatusOfMP, out string res) ? res :_physicalStatusOfMP; } set { _physicalStatusOfMP = value; } }

        private static readonly Dictionary<string, string> physicalStatusOfMPDictionary = new Dictionary<string, string>
        {
            { "D03", "New" },
            { "E22", "Connected" },
            { "E23", "Disconnected" },
        };

        public string consumerCategory { get; set; }
        public string powerLimitKW { get; set; }
        public string powerLimitA { get; set; }

        private string _subTypeOfMP;

        public string subTypeOfMP { get { return subTypeOfMPDictionary.TryGetValue(_subTypeOfMP, out string res)? res : _subTypeOfMP; } set { _subTypeOfMP = value; } }

        private static readonly Dictionary<string, string> subTypeOfMPDictionary = new Dictionary<string, string>
        {
            { "D01", "Physical" },
            { "D02", "Virtual" },
            { "D03", "Calculated" },
        };


        public string productionObligation { get; set; }
        public string mpCapacity { get; set; }

        private string _mpConnectionType;

        public string mpConnectionType { get { return mpConnectionTypeDictionary.TryGetValue(_mpConnectionType, out string res) ? res : _mpConnectionType;  } set { _mpConnectionType = value; } }

        private static readonly Dictionary<string, string> mpConnectionTypeDictionary = new Dictionary<string, string>
        {
            { "D01", "Direct connected" },
            { "D02", "Installation connected" },
        };

        public string disconnectionType { get; set; }

        private string _product;
        public string product { get { return productDictionary.TryGetValue(_product, out string res) ? res : _product; } set { _product = value; } }

        private static readonly Dictionary<string, string> productDictionary = new Dictionary<string, string>
        {
            { "5790001330590", "Tariff" },
            { "5790001330606", "Fuel quantity" },
            { "8716867000016", "Active power" },
            { "8716867000023", "Reactive power" },
            { "8716867000030", "Active energy" },
            { "8716867000047", "Rective energy" },
        };

        public string consumerCVR { get; set; }
        public string dataAccessCVR { get; set; }
        public DateTime consumerStartDate { get; set; }

        private string _meterReadingOccurrence;
        public string meterReadingOccurrence { get { return meterReadingOccurrenceDictionary.TryGetValue(_meterReadingOccurrence, out string res) ? res : _meterReadingOccurrence; } set { _meterReadingOccurrence = value; } }

        private static readonly Dictionary<string, string> meterReadingOccurrenceDictionary = new Dictionary<string, string>
        {
            { "ANDET", "Other" },
            { "P1M", "Monthly" },
            { "PT15M", "15 Minutes" },
            { "PT1H", "Hourly" },
            { "D01", "Automatic meter reading" },
            { "D02", "Manual meter reading" },
        };


        public string mpReadingCharacteristics { get; set; }
        public string meterCounterDigits { get; set; }
        public string meterCounterMultiplyFactor { get; set; }
        public string meterCounterUnit { get; set; }

        private string _meterCounterType;
        public string meterCounterType { get { return meterCounterTypeDictionary.TryGetValue(_meterCounterType, out string res) ? res : _meterCounterType; } set { _meterCounterType = value; } }

        private static readonly Dictionary<string, string> meterCounterTypeDictionary = new Dictionary<string, string>
        {
            { "D01", "Accumulated" },
            { "D02", "Balanced" },
        };

        public string balanceSupplierName { get; set; }
        public DateTime? balanceSupplierStartDate { get; set; }
        public string taxReduction { get; set; }
        public string taxSettlementDate { get; set; }
        public string mpRelationType { get; set; }
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
        public string firstConsumerPartyName { get; set; }
        public string secondConsumerPartyName { get; set; }
        public Contactaddress[] contactAddresses { get; set; }
        public Childmeteringpoint[] childMeteringPoints { get; set; }
    }

    public class Childmeteringpoint
    {
        public string meteringPointId { get; set; }
        public string parentMeteringPointId { get; set; }
        //public string typeOfMP { get; set; }
        private string _typeOfMP;
        public string typeOfMP { get { return typeOfMPDictionary.TryGetValue(_typeOfMP, out string res) ? res : _typeOfMP; } set { _typeOfMP = value; } }

        private static Dictionary<string, string> typeOfMPDictionary = new Dictionary<string, string>
        {
            { "D01", "VE Production" },
            { "D02", "Analysis" },
            { "D04", "Surplus production group 6" },
            { "D05", "Net production" },
            { "D06", "Supply to grid" },
            { "D07", "Consumption from grid" },
            { "D08", "Wholesale services / information" },
            { "D09", "Own production" },
            { "D10", "Net from grid" },
            { "D11", "Net to grid" },
            { "D12", "Total consumption" },
            { "D14", "Electrical heating" },
            { "D15", "Net consumption" },
            { "D17", "Other consumption" },
            { "D18", "Other production" },
            { "D99", "Internal use" },
            { "E17", "Consumption" },
            { "E18", "Production" },
        };
        public string meterReadingOccurrence { get; set; }
        public string meterNumber { get; set; }
    }

    public class Contactaddress
    {
        public string contactName1 { get; set; }
        public string contactName2 { get; set; }

        private string _addressCode;

        public string addressCode { get { return addressCodeDictionary.TryGetValue(_addressCode, out string res) ? res : _addressCode; } set { _addressCode = value; } }

        private static readonly Dictionary<string, string> addressCodeDictionary = new Dictionary<string, string>
        {
            { "D01", "Technical address" },
            { "D04", "Juridical address" },
        };

        public string streetName { get; set; }
        public string buildingNumber { get; set; }
        public string floorId { get; set; }
        public string roomId { get; set; }
        public string citySubDivisionName { get; set; }
        public string postcode { get; set; }
        public string cityName { get; set; }
        public string countryName { get; set; }
        public string contactPhoneNumber { get; set; }
        public string contactMobileNumber { get; set; }
        public string contactEmailAddress { get; set; }
        public object contactType { get; set; }
    }
}
