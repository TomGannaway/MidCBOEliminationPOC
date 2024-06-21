using InterfaceManuscriptBrokerWebAPI.Mapping;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace DuckCreekInterfaceManuscriptBrokerAPI
{
    [ApiController]
    [Route("[controller]")]
    public class BrokerController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public BrokerController(IHttpClientFactory httpClientFactory, 
            ILogger<BrokerController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _httpClient = _httpClientFactory.CreateClient("BrokerHttpClient");
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IMappedResponse>> ProcessGetResponse()
        {
            try
            {
                var headers = Request.Headers;
                string? url = headers["Url"];

                PopulateHeaders(headers);

                var result = await ProcessGetAsyncCall(url);
                if (result == null)
                {
                    return NotFound();
                }
                else
                {
                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "The Broker API encountered a general error: {message}", ex.Message);
                throw;
            }
        }

        private async Task<string?> ProcessGetAsyncCall(string url)
        {
            try
            {
                // have to convert the response back to XML before returning
                var response = await _httpClient.GetStringAsync(new Uri(url == null ? "" : url));
                XmlDocument doc = JsonConvert.DeserializeXmlNode(response);
                return doc?.InnerXml;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProcessGetAsyncCall() in the Broker API encountered an error: {message}", ex.Message);
                throw;
            }
        }

        [HttpPost]
        [ActionName("ProcessPostResponse")]
        public async Task<ActionResult<IMappedResponse>> ProcessPostResponse()
        {
            try
            {
                var headers = Request.Headers;
                string? url = headers["Url"];

                // format the request body into an XML string
                using var sr = new StreamReader(Request.Body);
                var requestBody = await sr.ReadToEndAsync(); // this comes across as XML
                requestBody = "<root>" + requestBody + "</root>";
                var requestElement = XElement.Parse(requestBody);

                // convert to JSON format
                var requestElemString = requestElement.ToString();
                var jsonBody = JsonConvert.SerializeXNode(requestElement);

                PopulateHeaders(headers);

                var result = await ProcessPostAsyncCall(url, jsonBody);
                if (result == null)
                {
                    return NotFound();
                }
                else
                {
                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "The ProcessPostResponse() in the Broker API encountered a general error: {message}", ex.Message);
                throw;
            }
        }

        private void PopulateHeaders(IHeaderDictionary headers)
        {
            try
            {
                foreach (var header in headers)
                {
                    if (header.Key.Equals("Host")) continue; // if we add the Host, at lease when running locally, the SSL connection will not be established
                    string headerName = header.Key;
                    string headerContent = header.Value.ToString();
                    if (headerName.Equals("Content-Type") || headerName.Equals("Content-Length"))
                    {
                        continue;
                    }
                    _httpClient.DefaultRequestHeaders.Add(headerName, headerContent);
                }

                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PopulateHeaders() in the Broker API encountered an error: {message}", ex.Message);
                throw;
            }
        }

        private async Task<string> ProcessPostAsyncCall(string url, string bodyParameters)
        {
            try
            {
                // remove the outer root node of the JSON
                var cleansed = bodyParameters.Substring(bodyParameters.IndexOf(":") + 1);
                cleansed = cleansed.Remove(cleansed.Length - 1);

                var content = new StringContent(cleansed, Encoding.UTF8, "application/json"); //CONTENT-TYPE header
                var httpResponseMessage = await _httpClient.PostAsync(url, content);
                var responseContent = await httpResponseMessage.Content.ReadAsStringAsync();

                // now we have to convert the response back to XML before returning
                XmlDocument doc = JsonConvert.DeserializeXmlNode(responseContent);
                var xml = doc?.InnerXml;

                return xml;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProcessPostAsyncCall() in the Broker API encountered an error: {message}", ex.Message);
                throw;
            }
        }
    }
}
