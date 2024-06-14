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

        public SEMPService(HttpClient httpClient, ILogger<SEMPService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<string>> GetQueuesAsync(string url, string messageVpn, string username, string password, bool sslVerify)
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
            var queueNames = new List<string>();

            using (JsonDocument doc = JsonDocument.Parse(responseBody))
            {
                foreach (var element in doc.RootElement.GetProperty("data").EnumerateArray())
                {
                    queueNames.Add(element.GetProperty("queueName").GetString());
                }
            }

            return queueNames;
        }
    }
}
