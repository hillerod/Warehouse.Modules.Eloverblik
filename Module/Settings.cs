using Bygdrift.Warehouse.Helpers.Attributes;

namespace Module
{
    public class Settings
    {
        [ConfigSecret(NotSet = NotSet.ThrowError, ErrorMessage = "Get this token ffrom Eloverblik")]
        public string Token { get; set; }

        [ConfigSetting(Default = 6)]
        public int MonthsToKeepReadingsPerHour { get; set; }

        [ConfigSetting(Default = 60)]
        public int MonthsToKeepReadingsPerDay { get; set; }

        [ConfigSetting(Default = 120)]
        public int MonthsToKeepReadingsPerMonth { get; set; }

        [ConfigSetting(Default = "0 0 1 * * *")]
        public string StarterScheduleExpression { get; set; }
    }
}
