using SqsJsonApiAccess.Exceptions;
using SqsJsonApiAccess.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace  SqsJsonApiAccess
{
    public class ApiAccess
    {
        #region Enums
        public enum OperationModes { Debug, Live }
        public enum Endpoints { HttpGetEcho } //must be exactly the same as the actual endpoint in SQS

        #endregion

        #region Consts
        const string BASE_URL_DEBUG = "http://localhost:58721/";
        const string BASE_URL_LIVE = "www.schlaftek.at/";

        const string HEADER_USERNAME = "username";
        const string HEADER_PASSWORD = "password";
        #endregion

        #region Vars
        string username = null;
        string password = null;
        #endregion

        #region Props
        public OperationModes Mode { get; private set; }
        #endregion

        private ApiAccess() { }

        /// <summary>
        ///
        /// </summary>
        /// <param name="mode">Debug = local test server, live = ISAP live server.</param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public ApiAccess(OperationModes mode, string username, string password)
        {
            this.Mode = mode;
            this.username = username;
            this.password = password;
        }

        private HttpWebRequest GetWebRequest(Endpoints endpoint)
        {
            string base_url = Mode == OperationModes.Live ? BASE_URL_LIVE : BASE_URL_DEBUG;
            string apiSubPage = GetSubPageForEndppoint(endpoint);
            string endpointUrl = base_url + apiSubPage + endpoint.ToString();
            HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(endpointUrl);
            webRequest.Method = "POST";
            webRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip; //this requests dynamic content compression from the server (cuts down the response size by about 70%)
            webRequest.ContentType = "application/json; charset=utf-8";
            webRequest.Headers.Add(HEADER_USERNAME, username);
            webRequest.Headers.Add(HEADER_PASSWORD, password);
            return webRequest;
        }

        private static string GetSubPageForEndppoint(Endpoints endpoint)
        {
            switch (endpoint)
            {
                case Endpoints.HttpGetEcho:
                    return "JsonAPI/CustomerOrderProcessing.asmx/";
                default:
                    return null;
            }
        }

        #region Order processing
        /// <summary>
        /// Sends the provided commission data to SQS. If successful, the data should not be modified anymore on client-side.
        /// </summary>
        /// <returns></returns>
        public Exception SendCommissionData(JsonCommissionDataRequest request)
        {
            try
            {
                HttpWebRequest webRequest = GetWebRequest(Endpoints.HttpGetEcho);

                using (var writer = webRequest.GetRequestStream())
                {
                    string jsonRequest = JsonConvert.SerializeObject(new { request = request });
                    byte[] requestData = Encoding.UTF8.GetBytes(jsonRequest);
                    writer.Write(requestData, 0, requestData.Length);
                }

                var webResponse = (HttpWebResponse)webRequest.GetResponse();
                var responseStream = new StreamReader(webResponse.GetResponseStream());

                string responseString = responseStream.ReadToEnd();
                JsonSerializerSettings serializerSettings = new JsonSerializerSettings() { Converters = new List<JsonConverter> { new JsonTypeMapper<JsonResult, JsonCommissionDataResponse>() } }; //custom serializer settings which makes it possible to deserialize JSON objects into their corresponding child object
                JsonCommissionDataResponse response = JsonConvert.DeserializeObject<JsonResultHeader>(responseString, serializerSettings).d as JsonCommissionDataResponse; //without the custom converter, the received JSON objects are not properly converted into their corresponding child POCO

                if (response.IsSuccessful)
                {
                    return null;
                }
                else
                    return new Exception("SendCommissionData() failed for commission: " + request.CommissionName);
            }
            catch (Exception ex)
            {
                return new Exception("SendCommissionData() threw an exception for commission: " + request.CommissionName, ex);
            }
        }
        #endregion
    }
}
