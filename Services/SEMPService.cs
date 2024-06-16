using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace SolaceWebClient.Services
{
    public class SEMPService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SEMPService> _logger;

        public class GetQueuesDetails
        {
            public string queueName { get; set; }
            public string queueOwner { get; set; }
        }

        public SEMPService(HttpClient httpClient, ILogger<SEMPService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<GetQueuesDetails>> GetQueuesAsync(string url, string messageVpn, string username, string password, bool sslVerify)
        {
            string requestUrl = $"{url}/SEMP/v2/config/msgVpns/{messageVpn}/queues";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            var authToken = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{username}:{password}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);

            if (!sslVerify)
            {
                // Bypass SSL certificate validation
                _httpClient.DefaultRequestHeaders.Add("IgnoreSslErrors", "true");
            }

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request);
            }
            catch (HttpRequestException e)
            {
                _logger.LogError($"Request error: {e.Message}");
                throw;
            }

            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            List<GetQueuesDetails> getQueuesList = new List<GetQueuesDetails>();

            using (JsonDocument doc = JsonDocument.Parse(responseBody))
            {
                foreach (var element in doc.RootElement.GetProperty("data").EnumerateArray())
                {
                    getQueuesList.Add(new GetQueuesDetails
                    {
                        queueName = element.GetProperty("queueName").GetString(),
                        queueOwner = element.GetProperty("owner").GetString()
                    });
                }
            }

            return getQueuesList;
        }


        public class ListenerPorts
        {
            public int SmfPort { get; set; }
            public int SmfsPort { get; set; }
            public bool SmfsEnabled {  get; set; }
        }


        public async Task<ListenerPorts> GetListenerAsync(string url, string messageVpn, string username, string password, bool sslVerify)
        {
            string requestUrl = $"{url}/SEMP/v2/config";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            var authToken = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{username}:{password}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);

            HttpResponseMessage response;

            var handler = new HttpClientHandler();
            if (!sslVerify)
            {
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            }

            using (var client = new HttpClient(handler))
            {
                try
                {
                    response = await client.SendAsync(request);
                }
                catch (HttpRequestException e)
                {
                    _logger.LogError($"Request error: {e.Message}");
                    throw;
                }
            }

            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            ListenerPorts config = new ListenerPorts();

            using (JsonDocument doc = JsonDocument.Parse(responseBody))
            {
                var msgVpnElement = doc.RootElement.GetProperty("data");
                config.SmfPort = msgVpnElement.GetProperty("serviceSmfPlainTextListenPort").GetInt32();
                config.SmfsPort = msgVpnElement.GetProperty("serviceSmfTlsListenPort").GetInt32();
                config.SmfsEnabled = msgVpnElement.GetProperty("serviceSempTlsEnabled").GetBoolean(); 
            }
            _logger.LogInformation($"SmfsEnabled: {config.SmfsEnabled}");
            _logger.LogInformation($"SmfPort: {config.SmfPort}");
            _logger.LogInformation($"SmfsPort: {config.SmfsPort}");
            return config;
        }
    }
}
