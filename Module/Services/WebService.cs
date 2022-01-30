using Bygdrift.Warehouse.Helpers.Logs;
using Module.Refines.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Module.Services
{
    public class WebService
    {
        private readonly TimeSpan defaultCancelAfter = new TimeSpan(1, 0, 0);
        private DateTime lastDownloadOfAccessToken;
        private HttpClient _client;
        private readonly string bearer;
        private readonly Log log;
        private readonly string baseUrl = "https://api.eloverblik.dk/CustomerApi/";
        public HttpResponseMessage ClientResponse { get; private set; }

        public HttpClient Client
        {
            get
            {
                if (_client == null || lastDownloadOfAccessToken.AddHours(1) < DateTime.Now)
                {
                    _client = GetHttpClient().Result;
                    lastDownloadOfAccessToken = DateTime.Now;
                }

                return _client;
            }
        }

        public WebService(string bearer, Log log)
        {
            this.bearer = bearer;
            this.log = log;
        }

        /// <summary>
        /// Gets an access token that can be used for up to one hour, then it has to be revoked.
        /// </summary>
        internal async Task<HttpClient> GetHttpClient()
        {
            var handler = new HttpClientHandler();
            var client = new HttpClient(handler);
            client.Timeout = new TimeSpan(10, 0, 0);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            client.BaseAddress = new Uri(baseUrl);
            ClientResponse = await client.GetAsync("api/Token");
            if (ClientResponse.StatusCode != HttpStatusCode.OK)
                return null;

            var json = await ClientResponse.Content.ReadAsStringAsync();
            var token = JObject.Parse(json)["result"].ToString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        /// <summary>
        /// This request is used for getting a list of metering points associated with a specific user (either private or business user).
        /// </summary>
        public async Task<string> GetMeteringPointsAsync(TimeSpan? cancelAfter = null)
        {
            return await ConvertToString("GetMeteringPoints", CallType.Get, "api/MeteringPoints/MeteringPoints?includeAll=true", null, cancelAfter);
        }

        /// <summary>
        /// This request is used for getting a list of metering point Id's
        /// </summary>
        public async Task<string[]> GetMeteringPointIdsAsync(TimeSpan? cancelAfter = null)
        {
            var json = await GetMeteringPointsAsync(cancelAfter);
            if (json == null || json == default)
                return null;

            var data = JsonConvert.DeserializeObject<MeteringPointResult>(json).result;
            return data.Select(o => o.meteringPointId).ToArray();
        }

        /// <summary>
        /// This request is used for querying details (master data) for one or more (linked/related) metering point.
        /// </summary>
        public async Task<IEnumerable<MeteringDetail>> GetMeteringPointsDetailsAsync(TimeSpan? cancelAfter, params string[] meteringPointIds)
        {
            var logId = $"GetDetails for id {meteringPointIds.First()} to {meteringPointIds.Last()}";
            log.LogInformation("{LogId}: Start loading...", logId);
            if (Client == null)
            {
                log.LogError("{LogId}: Could not connect to webservice.", logId);
                return null;
            }

            if (meteringPointIds == null || !meteringPointIds.Any())
            {
                log.LogError("{LogId}: Meteringpoints is missing.", logId);
                return null;
            }

            var meteringPointsJson = new JObject(new JProperty("meteringPoints", new JObject(new JProperty("meteringPoint", new JArray(meteringPointIds)))));
            var content = new StringContent(meteringPointsJson.ToString(), Encoding.UTF8, "application/json");
            var meteringDetailsJson = await ConvertToString(logId, CallType.Post, "api/MeteringPoints/MeteringPoint/GetDetails", content, cancelAfter);
            var meteringDetails = JsonConvert.DeserializeObject<MeteringPointDetailResult>(meteringDetailsJson)?.result.Select(o => o.result);
            return meteringDetails;
        }


        /// <summary>
        /// This request is used for querying charge data(subscriptions, tariffs and fees) for one or more
        /// (linked/related) metering points.Charges linked to the metering point at the time of the request or on any future date will be returned.
        /// </summary>
        public async Task<string> GetMeteringPointsChargesAsync(TimeSpan? cancelAfter, params string[] meteringPointIds)
        {
            var logId = $"GetMeteringPointsCharges for id {meteringPointIds.First()} to {meteringPointIds.Last()}";
            log.LogInformation("{LogId}: Start loading...", logId);
            if (Client == null)
            {
                log.LogError("{LogId}: Could not connect to webservice.", logId);
                return null;
            }

            if (meteringPointIds == null || !meteringPointIds.Any())
            {
                log.LogError("{LogId}: Meteringpoints is missing.", logId);
                return null;
            }

            var json = new JObject(new JProperty("meteringPoints", new JObject(new JProperty("meteringPoint", new JArray(meteringPointIds)))));
            var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
            return await ConvertToString(logId, CallType.Post, "api/MeteringPoints/MeteringPoint/GetCharges", content, cancelAfter);
        }

        /// <summary>
        /// This request is used for querying time series for one or more (linked/related) metering points for a specified period and with a specified aggregation level.
        /// </summary>
        public async Task<(string Json, string[] Ids)> GetReadingTimeSeriesAsync(DateTime dateFrom, DateTime dateTo, TimeAggregation aggregation, TimeSpan cancelAfter, params string[] meteringPointIds)
        {
            var logId = $"GetReadingTimeSeries per{aggregation} for id {meteringPointIds.First()} to {meteringPointIds.Last()}. {meteringPointIds.Count()} items.";
            log.LogInformation("{LogId}: Start loading...", logId);
            if (Client == default)
            {
                log.LogError("{LogId}. Could not connect to webservice.", logId);
                return default;
            }

            if (meteringPointIds == null || !meteringPointIds.Any())
            {
                log.LogError("{LogId}. Meteringpoints is missing.", logId);
                return default;
            }
            var json = new JObject(new JProperty("meteringPoints", new JObject(new JProperty("meteringPoint", new JArray(meteringPointIds)))));
            var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
            var res = await ConvertToString(logId, CallType.Post, $"api/MeterData/GetTimeSeries/{dateFrom.ToString("yyyy-MM-dd")}/{dateTo.ToString("yyyy-MM-dd")}/{aggregation}", content, cancelAfter);
            return (res, meteringPointIds);
        }

        /// <summary>
        /// This request is used for querying meter readings for one or more (linked/related) metering points for a specified period.
        /// </summary>
        public async Task<string> GetSingleMeterReadingsAsync(DateTime dateFrom, DateTime dateTo, TimeSpan? cancelAfter, params string[] meteringPointIds)
        {
            var logId = $"GetSingleMeterReadings for id {meteringPointIds.First()} to {meteringPointIds.Last()}";
            log.LogInformation("{LogId}: Start loading...", logId);
            if (Client == null)
            {
                log.LogError("{LogId}: Could not connect to webservice.", logId);
                return null;
            }

            if (meteringPointIds == null || !meteringPointIds.Any())
            {
                log.LogError("{LogId}: Meteringpoints is missing.", logId);
                return null;
            }

            var json = new JObject(new JProperty("meteringPoints", new JObject(new JProperty("meteringPoint", new JArray(meteringPointIds)))));
            var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
            return await ConvertToString(logId, CallType.Post, $"api/MeterData/GetMeterReadings/{dateFrom.ToString("yyyy-MM-dd")}/{dateTo.ToString("yyyy-MM-dd")}", content, cancelAfter);
        }

        private async Task<string> ConvertToString(string logId, CallType callType, string requesUri, StringContent postContent = null, TimeSpan? cancelAfter = null)
        {
            try
            {
                var cts = cancelAfter != null ? new CancellationTokenSource((TimeSpan)cancelAfter).Token : new CancellationTokenSource(defaultCancelAfter).Token;
                var res = callType == CallType.Get ? await Client.GetAsync(requesUri, cts) : await Client.PostAsync(requesUri, postContent, cts);

                if (res == null || res == default)
                    return null;

                if (res.IsSuccessStatusCode)
                {
                    log.LogInformation("{LogId}: Loading complete", logId);
                    return res.Content.ReadAsStringAsync().Result;
                }
                else
                    log.LogError("{LogId}: Could not make request. Statuscode: {status}. Message: {reason}.", logId, res.StatusCode, res.ReasonPhrase);
            }
            catch (OperationCanceledException e)
            {
                log.LogError(e, "{LogId}: Timeout out. Error: {ErrorMessage}", logId, e.Message);
            }
            catch (Exception e)
            {
                log.LogError(e, "{LogId}: There was an error. Error: {ErrorMessage}", logId, e.Message);
            }
            return null;
        }
    }

    public enum TimeAggregation
    {
        Quarter,
        Hour,
        Day,
        Month,
        Year
    }

    public enum CallType
    {
        Get,
        Post
    }
}
