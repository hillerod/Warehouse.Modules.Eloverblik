# Warehouse module: Eloverblik

With this module, you can push data from [Eloverblik](https://eloverblik.dk/welcome) into your own data warehouse on Azure.

Eloverblik is a Danish platform that provides data about electricity consumption and generation.

This module fetches data from all meterings that a given token from Eloverblik grants access to. The module are used in a case with one private metering and in another case with 367 meterings.

Data are stored into a database, refined into three tables, with a frequency of each hour, each day and each month. When installing the module, you can define how long back, data should be stored. As a standard it is set to 
- Per hour 6 months back
- Per day 5 years back 
- Per month 10 years back

The module is per standard set to run one time each day. Loading all this data from 367 meterings, takes about 40 minutes the first time. The next time the module runs, it only goes one month back, so it runs much faster, but still, think about how much data to save. You can save data per hour 10 years back, but it will produce a lot of data that possibly isn't very interesting - why should one know the difference between the power consumption at 9AM and 12AM 7 years ago? So when looking long back in history, it is interesting to look at consumption per month.

367 meterings saved per hour for a half year, will produce 367 meterings * 24 hours * (365 days / 2) = 1,607,460 rows of data.


The module is build with [Bygdrift Warehouse](https://github.com/Bygdrift/Warehouse), that enables one to attach multiple modules within the same azure environment, that can collect and wash data from all kinds of services, in a cheap data lake and database.
By saving data to a MS SQL database, it is:
- easy to fetch data with Power BI, Excel and other systems
- easy to control who has access to what - actually, it can be controlled with AD so you don't have to handle credentials
- It's cheap

Short video on how to setup a Bygdrift Warehouse and install the Eloverblik Module without deeper explanations (it's in English):
<div align="left">
      <a href="https://www.youtube.com/watch?v=PUgLiGKdE2E">
         <img src="https://img.youtube.com/vi/PUgLiGKdE2E/0.jpg">
      </a>
</div>

2021-11-04: [Combine data from Eloverblik and Dalux FM in Microsoft Power BI](https://youtu.be/wR0epPgs438) (in English):

## Installation

All modules can be installed and facilitated with ARM templates (Azure Resource Management): [Use ARM templates to setup and maintain this module](https://github.com/hillerod/Warehouse.Modules.Eloverblik/tree/master/Deploy).

## License

[MIT License](https://github.com/hillerod/Warehouse.Modules.Eloverblik/blob/master/License.md)

## Database content

| TABLE_NAME   | COLUMN_NAME                 | DATA_TYPE |
| :----------- | :-------------------------- | :-------- |
| Meterings    | meteringPointId             | bigint    |
| Meterings    | typeOfMP                    | varchar   |
| Meterings    | balanceSupplierName         | varchar   |
| Meterings    | streetName                  | varchar   |
| Meterings    | buildingNumber              | varchar   |
| Meterings    | floorId                     | varchar   |
| Meterings    | roomId                      | varchar   |
| Meterings    | postcode                    | int       |
| Meterings    | cityName                    | varchar   |
| Meterings    | locationDescription         | varchar   |
| Meterings    | meterReadingOccurrence      | varchar   |
| Meterings    | firstConsumerPartyName      | varchar   |
| Meterings    | consumerCVR                 | varchar   |
| Meterings    | dataAccessCVR               | varchar   |
| Meterings    | meterNumber                 | int       |
| Meterings    | consumerStartDate           | datetime  |
| Meterings    | parentPointId               | varchar   |
| Meterings    | energyTimeSeriesMeasureUnit | varchar   |
| Meterings    | estimatedAnnualVolume       | varchar   |
| Meterings    | gridOperator                | varchar   |
| Meterings    | balanceSupplierStartDate    | varchar   |
| Meterings    | physicalStatusOfMP          | varchar   |
| Meterings    | subTypeOfMP                 | varchar   |
| Meterings    | meterCounterMultiplyFactor  | varchar   |
| Meterings    | meterCounterUnit            | varchar   |
| Meterings    | dataPerHourFrom             | datetime  |
| Meterings    | dataPerHourTo               | datetime  |
| Meterings    | dataPerDayFrom              | datetime  |
| Meterings    | dataPerDayTo                | datetime  |
| Meterings    | dataPerMonthFrom            | datetime  |
| Meterings    | dataPerMonthTo              | datetime  |
| DataPerHour  | id                          | varchar   |
| DataPerHour  | meteringPointId             | bigint    |
| DataPerHour  | businessType                | varchar   |
| DataPerHour  | measurementUnitName         | varchar   |
| DataPerHour  | resolution                  | varchar   |
| DataPerHour  | timeintervalStart           | datetime  |
| DataPerHour  | timeintervalEnd             | datetime  |
| DataPerHour  | quantity                    | float     |
| DataPerHour  | quality                     | varchar   |
| DataPerDay   | id                          | varchar   |
| DataPerDay   | meteringPointId             | bigint    |
| DataPerDay   | businessType                | varchar   |
| DataPerDay   | measurementUnitName         | varchar   |
| DataPerDay   | resolution                  | varchar   |
| DataPerDay   | timeintervalStart           | datetime  |
| DataPerDay   | timeintervalEnd             | datetime  |
| DataPerDay   | quantity                    | float     |
| DataPerDay   | quality                     | varchar   |
| DataPerMonth | id                          | varchar   |
| DataPerMonth | meteringPointId             | bigint    |
| DataPerMonth | businessType                | varchar   |
| DataPerMonth | measurementUnitName         | varchar   |
| DataPerMonth | resolution                  | varchar   |
| DataPerMonth | timeintervalStart           | datetime  |
| DataPerMonth | timeintervalEnd             | datetime  |
| DataPerMonth | quantity                    | float     |
| DataPerMonth | quality                     | varchar   |

## Data lake content

In the data lake container with this modules name, there is one main folder called `Raw` and it doesn't contain information of interest. The data is saved for use under the analysing and refines. Personally, I would save data in my data lake for a year back, so if there should show up an error, it gives very good information and a programmer can write some code that can traverse the raw data and purify it. Memory space is cheap on a data lake.
- MeteringIds: A list of all ids that's imported
- MeteringDetails: Details about each meterring
- Readings per day, hour and month: All the readings
- ReadPartitions: An internal json used to registrate status for the import from Eloverblik  
- ReadPartitionsFromDB: An internal json used to registrate status for the import from Eloverblik

 The folder structure:

+ Raw
    - {yyyy the year}
        - {MM the month}
            - {dd the month}
                - MeteringDetails.json
                - MeteringIds.json
                - ReadingsPerDay_{MeteringId 1}-{MeteringId 20}.json
                - ReadingsPerDay_{MeteringId N}-{MeteringId N + 20}.json
                - ReadingsPerHour_{MeteringId 1}-{MeteringId 20}.json
                - ReadingsPerHour_{MeteringId N}-{MeteringId N + 20}.json
                - ReadingsPerMonth_{MeteringId 1}-{MeteringId 20}.json
                - ReadingsPerMonth_{MeteringId N}-{MeteringId N + 20}.json
                - ReadPartitions.json
                - ReadPartitionsFromDb.json