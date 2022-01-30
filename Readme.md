# Warehouse module: Eloverblik

`Status: 2021-12-31: I have now changed the Warehouse to .Net6.0 so I could build Eloverblik as Azure Durable Functions. It is spinning perfect when testing locally. I have now put it up to production environment and I will update the text here, when it runs perfect on the server. Do not try to install it yet!!`

With this module, you can push data from [Eloverblik](https://eloverblik.dk/welcome) into your own data warehouse on Azure.

Eloverblik is a Danish platform that provides data about electricity consumption and generation.

The module is build with [Bygdrift Warehouse](https://github.com/Bygdrift/Warehouse), that makes it possible to attach multiple modules within the same azure environment, that can collect data from all kinds of services, in a cheap data lake and database.
The data data lake, is structured as a Common Data Model (CDM), which enables an easy integration to Microsoft Power BI, through Power BI Dataflows. And the Microsoft SQL database, makes it even easier to fetch data to Excel, Power BI and a lot of other systems.

# Testing module

If you want to test the module, then download the project and open it with Visual Studio or Visual Studio Code.
Fill out the settings in `ModuleTests/appsettings.json`.
Debug the file: `ModuleTests/ImporterTest.cs`, by opening the file, right-click `TestRunModule()` and select 'Debug test(s)'.

# Install on Azure

Wil come in start February.

# License

[MIT License](https://github.com/hillerod/Warehouse.Modules.Eloverblik/blob/master/License.md)
