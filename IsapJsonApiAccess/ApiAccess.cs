using IsapJsonApiAccess.Exceptions;
using IsapJsonApiAccess.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace IsapJsonApiAccess
{
    public class ApiAccess
    {
        #region Enums
        public enum OperationModes { Debug, Live }
        public enum Endpoints { GetIsAutoUpdateOfMasterDataEnabled, GetArticlesForProductCategories, GetSlattedFramesForCategories, GetPillowsForCategories, ValidateCredentialsAndGetOfficeData, GetBranchOfficeData, CommissionNewCustomerOrder, RequestOrderCancellation, RequestOrderStatusUpdates, RequestOrderCancellationStatusUpdates, GetSoftwareCurrentVersionInfo, GetIsapBranchOffices, UpdateApplicationLocation, UpdateDeactivatedComponents, ReportApplicationActivity, PostDeviceInfoUpdate, RequestPendingApplicationCommands, InsertDataBackup } //must be exactly the same as the actual endpoint in ISAP

        #endregion

        #region Consts
        const string BASE_URL_DEBUG = "http://localhost:61039/";
        const string BASE_URL_LIVE = "https://www.service.proschlaf.at/";

        const string HEADER_USERNAME = "username";
        const string HEADER_PASSWORD = "password";
        #endregion

        #region Vars
        //  OperationModes mode = OperationModes.Debug;
        string username = null;
        string password = null;

        //   JsonSerializerSettings serializerSettings = new JsonSerializerSettings() { Converters = new List<JsonConverter> { new JsonTypeMapper<JsonResult, dynamic>() } }; //custom serializer settings which makes it possible to deserialize JSON objects into their corresponding child object
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
                case Endpoints.GetIsAutoUpdateOfMasterDataEnabled:
                case Endpoints.GetArticlesForProductCategories:
                case Endpoints.GetSlattedFramesForCategories:
                case Endpoints.GetPillowsForCategories:
                case Endpoints.GetBranchOfficeData:
                    return "JsonAPI/ArticleMasterData.aspx/";
                case Endpoints.CommissionNewCustomerOrder:
                case Endpoints.RequestOrderCancellation:
                case Endpoints.RequestOrderStatusUpdates:
                case Endpoints.RequestOrderCancellationStatusUpdates:
                    return "JsonAPI/OrderProcessing.aspx/";
                case Endpoints.GetSoftwareCurrentVersionInfo:
                    return "JsonAPI/SoftwareUpdate.aspx/";
                case Endpoints.GetIsapBranchOffices:
                case Endpoints.UpdateApplicationLocation:
                case Endpoints.UpdateDeactivatedComponents:
                case Endpoints.ValidateCredentialsAndGetOfficeData:
                    return "JsonAPI/BranchOffices.aspx/";
                case Endpoints.ReportApplicationActivity:
                    return "JsonAPI/Tracking.aspx/";
                case Endpoints.PostDeviceInfoUpdate:
                    return "JsonAPI/DeviceManagement.aspx/";
                case Endpoints.RequestPendingApplicationCommands:
                    return "JsonAPI/ApplicationManagement.aspx/";
                case Endpoints.InsertDataBackup:
                    return "JsonAPI/DataManagement.aspx/";
                default:
                    return null;
            }
        }

        #region Article handling

        /// <summary>
        /// Checks whether clients should auto-update their article master data every once in a while.
        /// Note that this server-side setting does not actually prevent clients from doing so because there is currently no way to differentiate between manual and automatic requests.
        /// </summary>
        /// <param name="isEnabled">True if the client should perform auto-updates of their article master data.</param>
        /// <returns></returns>
        public Exception GetIsAutoUpdateOfMasterDataEnabled(string branchOfficeCode, out bool isEnabled)
        {
            try
            {
                isEnabled = false;
                HttpWebRequest webRequest = GetWebRequest(Endpoints.GetIsAutoUpdateOfMasterDataEnabled);

                using (var writer = webRequest.GetRequestStream())
                {
                    string jsonRequest = JsonConvert.SerializeObject(new { branchOfficeCode });
                    byte[] requestData = Encoding.UTF8.GetBytes(jsonRequest);
                    writer.Write(requestData, 0, requestData.Length);
                }

                var webResponse = (HttpWebResponse)webRequest.GetResponse();
                var responseStream = new StreamReader(webResponse.GetResponseStream());

                string responseString = responseStream.ReadToEnd();
                JsonSerializerSettings serializerSettings = new JsonSerializerSettings() { Converters = new List<JsonConverter> { new JsonTypeMapper<JsonResult, JsonArticleMasterDataStatusResponse>() } }; //custom serializer settings which makes it possible to deserialize JSON objects into their corresponding child object
                JsonArticleMasterDataStatusResponse settingResponse = JsonConvert.DeserializeObject<JsonResultHeader>(responseString, serializerSettings).d as JsonArticleMasterDataStatusResponse; //without the custom converter, the received JSON objects are not properly converted into their corresponding child POCO

                if (settingResponse.IsSuccessful)
                {
                    isEnabled = settingResponse.IsAutomaticUpdateOfClientMasterDataEnabled;
                    return null;
                }
                else
                    return new Exception("GetIsAutoUpdateOfMasterDataEnabled() failed", settingResponse.Exception.ToException());
            }
            catch (Exception ex)
            {
                isEnabled = false;
                return new Exception("GetIsAutoUpdateOfMasterDataEnabled() threw an exception", ex);
            }
        }

        /// <summary>
        /// Queries all available SAP articles for the specified product categories.
        /// </summary>
        /// <param name="productCategories">The article categories to be returned.</param>
        /// <param name="branchOfficeCode">The branch office code of the client.</param>
        /// <param name="targetCulture">The culture (language) in which the DisplayNames of the returned articles are supposed to be.</param>
        /// <param name="resultArticles"></param>
        /// <returns></returns>
        public Exception GetArticlesForProductCategories(List<string> productCategories, string branchOfficeCode, CultureInfo targetCulture, out List<JsonArticle> resultArticles)
        {
            try
            {
                resultArticles = null;
                HttpWebRequest webRequest = GetWebRequest(Endpoints.GetArticlesForProductCategories);

                using (var writer = webRequest.GetRequestStream())
                {
                    JsonArticleRequest myRequest = new JsonArticleRequest() { BranchOfficeCode = branchOfficeCode, ArticleCategories = productCategories, TargetCulture = targetCulture.ToString() };
                    string jsonRequest = JsonConvert.SerializeObject(new { request = myRequest });
                    byte[] requestData = Encoding.UTF8.GetBytes(jsonRequest);
                    writer.Write(requestData, 0, requestData.Length);
                }

                var webResponse = (HttpWebResponse)webRequest.GetResponse();
                var responseStream = new StreamReader(webResponse.GetResponseStream());

                string responseString = responseStream.ReadToEnd();
                JsonSerializerSettings serializerSettings = new JsonSerializerSettings() { Converters = new List<JsonConverter> { new JsonTypeMapper<JsonResult, JsonArticlesResponse>() } }; //custom serializer settings which makes it possible to deserialize JSON objects into their corresponding child object
                JsonArticlesResponse articleResponse = JsonConvert.DeserializeObject<JsonResultHeader>(responseString, serializerSettings).d as JsonArticlesResponse; //without the custom converter, the received JSON objects are not properly converted into their corresponding child POCO

                if (articleResponse.IsSuccessful)
                {
                    resultArticles = articleResponse.Articles;
                    return null;
                }
                else
                    return new Exception("GetArticlesForProductCategories() failed for branch office code: " + branchOfficeCode + " and product categories: " + string.Join(",", productCategories), articleResponse.Exception.ToException());
            }
            catch (Exception ex)
            {
                resultArticles = null;
                return new Exception("GetArticlesForProductCategories() threw an exception for branch office code: " + branchOfficeCode + " and product categories: " + string.Join(",", productCategories ?? new List<string>()), ex);
            }
        }

        /// <summary>
        /// Queries all available SAP slatted frames that match the specified names.
        /// </summary>
        /// <param name="partialNames">The partial names of the slatetd frames to be included.</param>
        /// <param name="branchOfficeCode">The branch office code of the client.</param>
        /// <param name="targetCulture">The culture (language) in which the DisplayNames of the returned articles are supposed to be.</param>
        /// <param name="resultArticles"></param>
        /// <returns></returns>
        public Exception GetSlattedFramesForCategories(List<string> categories, string branchOfficeCode, CultureInfo targetCulture, out List<JsonArticle> resultArticles)
        {
            try
            {
                resultArticles = null;
                HttpWebRequest webRequest = GetWebRequest(Endpoints.GetSlattedFramesForCategories);

                using (var writer = webRequest.GetRequestStream())
                {
                    JsonArticleRequest myRequest = new JsonArticleRequest() { BranchOfficeCode = branchOfficeCode, ArticleCategories = categories, TargetCulture = targetCulture.ToString() };
                    string jsonRequest = JsonConvert.SerializeObject(new { request = myRequest });
                    byte[] requestData = Encoding.UTF8.GetBytes(jsonRequest);
                    writer.Write(requestData, 0, requestData.Length);
                }

                var webResponse = (HttpWebResponse)webRequest.GetResponse();
                var responseStream = new StreamReader(webResponse.GetResponseStream());

                string responseString = responseStream.ReadToEnd();
                JsonSerializerSettings serializerSettings = new JsonSerializerSettings() { Converters = new List<JsonConverter> { new JsonTypeMapper<JsonResult, JsonArticlesResponse>() } }; //custom serializer settings which makes it possible to deserialize JSON objects into their corresponding child object
                JsonArticlesResponse articleResponse = JsonConvert.DeserializeObject<JsonResultHeader>(responseString, serializerSettings).d as JsonArticlesResponse; //without the custom converter, the received JSON object are not properly converted into their corresponding child POCO

                if (articleResponse.IsSuccessful)
                {
                    resultArticles = articleResponse.Articles;
                    return null;
                }
                else
                    return new Exception("GetSlattedFramesForCategories() failed for branch office code: " + branchOfficeCode + " and product categories: " + string.Join(",", categories), articleResponse.Exception.ToException());
            }
            catch (Exception ex)
            {
                resultArticles = null;
                return new Exception("GetSlattedFramesForCategories() threw an exception for branch office code: " + branchOfficeCode + " and product categories: " + string.Join(",", categories ?? new List<string>()), ex);
            }
        }

        /// <summary>
        /// Queries all available SAP pillows for the specified categories.
        /// </summary>
        /// <param name="productCategories">The pillow categories to be returned.</param>
        /// <param name="branchOfficeCode">The branch office code of the client.</param>
        /// <param name="targetCulture">The culture (language) in which the DisplayNames of the returned articles are supposed to be.</param>
        /// <param name="resultArticles"></param>
        /// <returns></returns>
        public Exception GetPillowsForCategories(List<string> productCategories, string branchOfficeCode, CultureInfo targetCulture, out List<JsonArticle> resultArticles)
        {
            try
            {
                resultArticles = null;
                HttpWebRequest webRequest = GetWebRequest(Endpoints.GetPillowsForCategories);

                using (var writer = webRequest.GetRequestStream())
                {
                    JsonArticleRequest myRequest = new JsonArticleRequest() { BranchOfficeCode = branchOfficeCode, ArticleCategories = productCategories, TargetCulture = targetCulture.ToString() };
                    string jsonRequest = JsonConvert.SerializeObject(new { request = myRequest });
                    byte[] requestData = Encoding.UTF8.GetBytes(jsonRequest);
                    writer.Write(requestData, 0, requestData.Length);
                }

                var webResponse = (HttpWebResponse)webRequest.GetResponse();
                var responseStream = new StreamReader(webResponse.GetResponseStream());

                string responseString = responseStream.ReadToEnd();
                JsonSerializerSettings serializerSettings = new JsonSerializerSettings() { Converters = new List<JsonConverter> { new JsonTypeMapper<JsonResult, JsonArticlesResponse>() } }; //custom serializer settings which makes it possible to deserialize JSON objects into their corresponding child object
                JsonArticlesResponse articleResponse = JsonConvert.DeserializeObject<JsonResultHeader>(responseString, serializerSettings).d as JsonArticlesResponse; //without the custom converter, the received JSON objects are not properly converted into their corresponding child POCO

                if (articleResponse.IsSuccessful)
                {
                    resultArticles = articleResponse.Articles;
                    return null;
                }
                else
                    return new Exception("GetPillowsForCategories() failed for branch office code: " + branchOfficeCode + " and pillow categories: " + string.Join(",", productCategories), articleResponse.Exception.ToException());
            }
            catch (Exception ex)
            {
                resultArticles = null;
                return new Exception("GetPillowsForCategories() threw an exception for branch office code: " + branchOfficeCode + " and pillow categories: " + string.Join(",", productCategories ?? new List<string>()), ex);
            }
        }
        #endregion

        #region Branch office and application location handling

        /// <summary>
        /// Queries contact and address data for the specified office.
        /// </summary>
        /// <param name="branchOfficeCode">The branch office code of the client.</param>
        /// <param name="resultArticles"></param>
        /// <returns></returns>
        public Exception ValidateCredentialsAndGetOfficeData(out JsonOfficeResponse response)
        {
            try
            {
                response = null;
                HttpWebRequest webRequest = GetWebRequest(Endpoints.ValidateCredentialsAndGetOfficeData);

                using (var writer = webRequest.GetRequestStream())
                {
                    string jsonRequest = JsonConvert.SerializeObject(new { }); //no parameters
                    byte[] requestData = Encoding.UTF8.GetBytes(jsonRequest);
                    writer.Write(requestData, 0, requestData.Length);
                }

                var webResponse = (HttpWebResponse)webRequest.GetResponse();
                var responseStream = new StreamReader(webResponse.GetResponseStream());

                string responseString = responseStream.ReadToEnd();
                JsonSerializerSettings serializerSettings = new JsonSerializerSettings() { Converters = new List<JsonConverter> { new JsonTypeMapper<JsonResult, JsonOfficeResponse>() } }; //custom serializer settings which makes it possible to deserialize JSON objects into their corresponding child object
                JsonOfficeResponse officeResponse = JsonConvert.DeserializeObject<JsonResultHeader>(responseString, serializerSettings).d as JsonOfficeResponse; //without the custom converter, the received JSON objects are not properly converted into their corresponding child POCO

                if (officeResponse.IsSuccessful)
                {
                    response = officeResponse;
                    return null;
                }
                else
                    return new Exception("ValidateCredentialsAndGetOfficeData() failed", officeResponse.Exception.ToException());
            }
            catch (Exception ex)
            {
                response = null;

                if (ex is System.Net.WebException)
                    if ((ex as System.Net.WebException).Status == WebExceptionStatus.ProtocolError && ex.Message.Contains("(403"))
                        return new JsonCredentialsMismatchException("Die angegebenen Zugangsdaten sind falsch oder passen nicht zur Filiale.");

                return new Exception("ValidateCredentialsAndGetOfficeData() threw an exception", ex);
            }
        }

        /// <summary>
        /// Queries contact and address data for the specified office.
        /// </summary>
        /// <param name="branchOfficeCode">The branch office code of the client.</param>
        /// <param name="resultArticles"></param>
        /// <returns></returns>
        public Exception GetBranchOfficeData(string branchOfficeCode, out JsonOfficeResponse response)
        {
            try
            {
                response = null;
                HttpWebRequest webRequest = GetWebRequest(Endpoints.GetBranchOfficeData);

                using (var writer = webRequest.GetRequestStream())
                {
                    JsonOfficeRequest myRequest = new JsonOfficeRequest() { BranchOfficeCode = branchOfficeCode };
                    string jsonRequest = JsonConvert.SerializeObject(new { request = myRequest });
                    byte[] requestData = Encoding.UTF8.GetBytes(jsonRequest);
                    writer.Write(requestData, 0, requestData.Length);
                }

                var webResponse = (HttpWebResponse)webRequest.GetResponse();
                var responseStream = new StreamReader(webResponse.GetResponseStream());

                string responseString = responseStream.ReadToEnd();
                JsonSerializerSettings serializerSettings = new JsonSerializerSettings() { Converters = new List<JsonConverter> { new JsonTypeMapper<JsonResult, JsonOfficeResponse>() } }; //custom serializer settings which makes it possible to deserialize JSON objects into their corresponding child object
                JsonOfficeResponse officeResponse = JsonConvert.DeserializeObject<JsonResultHeader>(responseString, serializerSettings).d as JsonOfficeResponse; //without the custom converter, the received JSON objects are not properly converted into their corresponding child POCO

                if (officeResponse.IsSuccessful)
                {
                    response = officeResponse;
                    return null;
                }
                else
                    return new Exception("GetBranchOfficeData() failed for branch office code: " + branchOfficeCode, officeResponse.Exception.ToException());
            }
            catch (Exception ex)
            {
                response = null;

                if (ex is System.Net.WebException)
                    if ((ex as System.Net.WebException).Status == WebExceptionStatus.ProtocolError && ex.Message.Contains("(403"))
                        return new JsonCredentialsMismatchException("Die angegebenen Zugangsdaten sind falsch oder passen nicht zur Filiale.");

                return new Exception("GetBranchOfficeData() threw an exception for branch office code: " + branchOfficeCode, ex);
            }
        }

        /// <summary>
        /// Queries basic data on all available ISAP branch offices.
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public Exception GetIsapBranchOffices(out JsonIsapOfficesResponse response)
        {
            try
            {
                response = null;

                HttpWebRequest webRequest = GetWebRequest(Endpoints.GetIsapBranchOffices);

                using (var writer = webRequest.GetRequestStream())
                {
                    string jsonRequest = JsonConvert.SerializeObject(new { request = "" });
                    byte[] requestData = Encoding.UTF8.GetBytes(jsonRequest);
                    writer.Write(requestData, 0, requestData.Length);
                }

                var webResponse = (HttpWebResponse)webRequest.GetResponse();
                var responseStream = new StreamReader(webResponse.GetResponseStream());

                string responseString = responseStream.ReadToEnd();
                JsonSerializerSettings serializerSettings = new JsonSerializerSettings() { Converters = new List<JsonConverter> { new JsonTypeMapper<JsonResult, JsonIsapOfficesResponse>() } }; //custom serializer settings which makes it possible to deserialize JSON objects into their corresponding child object
                JsonIsapOfficesResponse officeResponse = JsonConvert.DeserializeObject<JsonResultHeader>(responseString, serializerSettings).d as JsonIsapOfficesResponse; //without the custom converter, the received JSON objects are not properly converted into their corresponding child POCO

                if (officeResponse.IsSuccessful)
                {
                    response = officeResponse;
                    return null;
                }
                else
                    return new Exception("GetIsapBranchOffices() failed for an unknown reason.", officeResponse.Exception.ToException());
            }
            catch (Exception ex)
            {
                response = null;

                if (ex is System.Net.WebException)
                    if ((ex as System.Net.WebException).Status == WebExceptionStatus.ProtocolError && ex.Message.Contains("(403"))
                        return new JsonCredentialsMismatchException("Die angegebenen Zugangsdaten sind falsch oder passen nicht zur Filiale.");

                return new Exception("GetIsapBranchOffices() threw an exception", ex);
            }
        }

        /// <summary>
        /// Updates the internal information on the received application location (usually at the store of a business partner).
        /// All properties in the input request are expected to be filled (data is simply overwritten on server-side).
        /// If the location is yet unknown, it will be created.
        /// </summary>
        /// <returns></returns>
        public Exception UpdateApplicationLocation(JsonApplicationLocationUpdateRequest request, bool isTestUpload)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.BranchOfficeCode))
                    return new ArgumentException("'BranchOfficeCode' is NULL or empty.");

                HttpWebRequest webRequest = GetWebRequest(Endpoints.UpdateApplicationLocation);

                using (var writer = webRequest.GetRequestStream())
                {
                    string jsonRequest = JsonConvert.SerializeObject(new { request = request, isTestUpload = isTestUpload });
                    byte[] requestData = Encoding.UTF8.GetBytes(jsonRequest);
                    writer.Write(requestData, 0, requestData.Length);
                }

                var webResponse = (HttpWebResponse)webRequest.GetResponse();
                var responseStream = new StreamReader(webResponse.GetResponseStream());

                string responseString = responseStream.ReadToEnd();
                JsonResult result = JsonConvert.DeserializeObject<JsonResultHeader>(responseString).d as JsonResult;

                if (result.IsSuccessful)
                {
                    return null;
                }
                else
                    return new Exception("UpdateApplicationLocation() failed for office code: " + request?.BranchOfficeCode, result.Exception.ToException());
            }
            catch (Exception ex)
            {
                if (ex is System.Net.WebException we)
                {
                    if (we.Status == WebExceptionStatus.ProtocolError && ex.Message.Contains("(403"))
                        return new JsonCredentialsMismatchException("Die angegebenen Zugangsdaten sind falsch oder passen nicht zur Filiale.");
                    else
                        return new Exception("UpdateApplicationLocation() threw a WebException for office code: " + request?.BranchOfficeCode + ". Status: " + we.Status.ToString(), ex);
                }

                return new Exception("UpdateApplicationLocation() threw an exception for office code: " + request?.BranchOfficeCode, ex);
            }
        }

        /// <summary>
        /// Updates the active and deactivated components (i.e. toppings and bedcovers) for the provided branch office (application location).
        /// Note that the request must contain ALL currently activated and deactivated component categories as defined on client-side for a specific model category.
        /// </summary>
        /// <returns></returns>
        public Exception UpdateActiveComponentsForApplicationLocation(JsonUpdateDeactivatedComponentsRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.BranchOfficeCode))
                    return new ArgumentException("'BranchOfficeCode' is NULL or empty.");

                HttpWebRequest webRequest = GetWebRequest(Endpoints.UpdateDeactivatedComponents);

                using (var writer = webRequest.GetRequestStream())
                {
                    string jsonRequest = JsonConvert.SerializeObject(new { request = request });
                    byte[] requestData = Encoding.UTF8.GetBytes(jsonRequest);
                    writer.Write(requestData, 0, requestData.Length);
                }

                var webResponse = (HttpWebResponse)webRequest.GetResponse();
                var responseStream = new StreamReader(webResponse.GetResponseStream());

                string responseString = responseStream.ReadToEnd();
                JsonResult result = JsonConvert.DeserializeObject<JsonResultHeader>(responseString).d as JsonResult;

                if (result.IsSuccessful)
                {
                    return null;
                }
                else
                    return new Exception("UpdateDeactivatedComponentsForApplicationLocation() failed for office code: " + request?.BranchOfficeCode, result.Exception.ToException());
            }
            catch (Exception ex)
            {
                if (ex is System.Net.WebException)
                    if ((ex as System.Net.WebException).Status == WebExceptionStatus.ProtocolError && ex.Message.Contains("(403"))
                        return new JsonCredentialsMismatchException("Die angegebenen Zugangsdaten sind falsch oder passen nicht zur Filiale.");

                return new Exception("UpdateDeactivatedComponentsForApplicationLocation() threw an exception for office code: " + request?.BranchOfficeCode, ex);
            }
        }

        #endregion

        #region Order processing

        /// <summary>
        /// Sends the input order to ISAP in order to create a new customer order.
        /// </summary>
        /// <returns></returns>
        public Exception CommissionNewCustomerOrder(JsonCustomerOrderRequest order, out JsonCustomerOrderResponse response)
        {
            try
            {
                response = null;
                HttpWebRequest webRequest = GetWebRequest(Endpoints.CommissionNewCustomerOrder);

                using (var writer = webRequest.GetRequestStream())
                {
                    string jsonRequest = JsonConvert.SerializeObject(new { request = order });
                    byte[] requestData = Encoding.UTF8.GetBytes(jsonRequest);
                    writer.Write(requestData, 0, requestData.Length);
                }

                var webResponse = (HttpWebResponse)webRequest.GetResponse();
                var responseStream = new StreamReader(webResponse.GetResponseStream());

                string responseString = responseStream.ReadToEnd();
                JsonSerializerSettings serializerSettings = new JsonSerializerSettings() { Converters = new List<JsonConverter> { new JsonTypeMapper<JsonResult, JsonCustomerOrderResponse>() } }; //custom serializer settings which makes it possible to deserialize JSON objects into their corresponding child object
                JsonCustomerOrderResponse orderResponse = JsonConvert.DeserializeObject<JsonResultHeader>(responseString, serializerSettings).d as JsonCustomerOrderResponse; //without the custom converter, the received JSON objects are not properly converted into their corresponding child POCO

                if (orderResponse.IsSuccessful)
                {
                    response = orderResponse;
                    return null;
                }
                else
                    return new Exception("CommissionNewCustomerOrders() failed for order: " + order.OrderNumber + "/" + order.CreationDate, orderResponse.Exception.ToException());
            }
            catch (Exception ex)
            {
                response = null;
                return new Exception("CommissionNewCustomerOrders() threw an exception for order: " + order.OrderNumber + "/" + order.CreationDate, ex);
            }
        }

        /// <summary>
        /// Sends a request to cancel an existing order.
        /// </summary>
        /// <returns></returns>
        public Exception SendCancelOrderRequest(JsonOrderCancellationRequest cancellationRequest, out JsonCancelOrderResponse response)
        {
            try
            {
                response = null;
                HttpWebRequest webRequest = GetWebRequest(Endpoints.RequestOrderCancellation);

                using (var writer = webRequest.GetRequestStream())
                {
                    string jsonRequest = JsonConvert.SerializeObject(new { request = cancellationRequest });
                    byte[] requestData = Encoding.UTF8.GetBytes(jsonRequest);
                    writer.Write(requestData, 0, requestData.Length);
                }

                var webResponse = (HttpWebResponse)webRequest.GetResponse();
                var responseStream = new StreamReader(webResponse.GetResponseStream());

                string responseString = responseStream.ReadToEnd();
                JsonSerializerSettings serializerSettings = new JsonSerializerSettings() { Converters = new List<JsonConverter> { new JsonTypeMapper<JsonResult, JsonCancelOrderResponse>() } }; //custom serializer settings which makes it possible to deserialize JSON objects into their corresponding child object
                JsonCancelOrderResponse cancelResponse = JsonConvert.DeserializeObject<JsonResultHeader>(responseString, serializerSettings).d as JsonCancelOrderResponse; //without the custom converter, the received JSON objects are not properly converted into their corresponding child POCO

                if (cancelResponse.IsSuccessful)
                {
                    response = cancelResponse;
                    return null;
                }
                else
                    return new Exception("SendCancelOrderRequest() failed for GUID: " + cancellationRequest.OrderToCancelGuid, cancelResponse.Exception.ToException());
            }
            catch (Exception ex)
            {
                response = null;
                return new Exception("SendCancelOrderRequest() threw an exception for GUID: " + cancellationRequest.OrderToCancelGuid, ex);
            }
        }

        /// <summary>
        /// Sends a request to retrieve the status of transmitted order cancellation requests.
        /// </summary>
        /// <returns></returns>
        public Exception RequestStatusUpdatesForCancellations(JsonOrderCancellationStatusRequest cancellationStatusRequest, out JsonCancellationStatusResponse response)
        {
            try
            {
                response = null;
                HttpWebRequest webRequest = GetWebRequest(Endpoints.RequestOrderCancellationStatusUpdates);

                using (var writer = webRequest.GetRequestStream())
                {
                    string jsonRequest = JsonConvert.SerializeObject(new { request = cancellationStatusRequest });
                    byte[] requestData = Encoding.UTF8.GetBytes(jsonRequest);
                    writer.Write(requestData, 0, requestData.Length);
                }

                var webResponse = (HttpWebResponse)webRequest.GetResponse();
                var responseStream = new StreamReader(webResponse.GetResponseStream());

                string responseString = responseStream.ReadToEnd();
                JsonSerializerSettings serializerSettings = new JsonSerializerSettings() { Converters = new List<JsonConverter> { new JsonTypeMapper<JsonResult, JsonCancellationStatusResponse>() } }; //custom serializer settings which makes it possible to deserialize JSON objects into their corresponding child object
                JsonCancellationStatusResponse cancelResponse = JsonConvert.DeserializeObject<JsonResultHeader>(responseString, serializerSettings).d as JsonCancellationStatusResponse; //without the custom converter, the received JSON objects are not properly converted into their corresponding child POCO

                if (cancelResponse.IsSuccessful)
                {
                    response = cancelResponse;
                    return null;
                }
                else
                    return new Exception("GetCancellationStatus() failed for GUIDs: " + string.Join(",", cancellationStatusRequest.CancelledOrderGuids), cancelResponse.Exception.ToException());
            }
            catch (Exception ex)
            {
                response = null;
                return new Exception("SendCancelOrderRequest() threw an exception for GUIDs: " + string.Join(",", cancellationStatusRequest.CancelledOrderGuids), ex);
            }
        }

        /// <summary>
        /// Sends a request to cancel an existing order.
        /// </summary>
        /// <returns></returns>
        public Exception RequestStatusUpdatesForOrders(JsonOrdersStatusRequest statusRequest, out JsonOrdersStatusResponse response)
        {
            try
            {
                response = null;
                HttpWebRequest webRequest = GetWebRequest(Endpoints.RequestOrderStatusUpdates);

                using (var writer = webRequest.GetRequestStream())
                {
                    string jsonRequest = JsonConvert.SerializeObject(new { request = statusRequest });
                    byte[] requestData = Encoding.UTF8.GetBytes(jsonRequest);
                    writer.Write(requestData, 0, requestData.Length);
                }

                var webResponse = (HttpWebResponse)webRequest.GetResponse();
                var responseStream = new StreamReader(webResponse.GetResponseStream());

                string responseString = responseStream.ReadToEnd();
                JsonSerializerSettings serializerSettings = new JsonSerializerSettings() { Converters = new List<JsonConverter> { new JsonTypeMapper<JsonResult, JsonOrdersStatusResponse>() } }; //custom serializer settings which makes it possible to deserialize JSON objects into their corresponding child object
                JsonOrdersStatusResponse statusResponse = JsonConvert.DeserializeObject<JsonResultHeader>(responseString, serializerSettings).d as JsonOrdersStatusResponse; //without the custom converter, the received JSON objects are not properly converted into their corresponding child POCO

                if (statusResponse.IsSuccessful)
                {
                    response = statusResponse;
                    return null;
                }
                else
                    return new Exception("RequestStatusUpdatesForOrders() failed for GUIDs: " + string.Join(",", statusRequest.OrderGuids), statusResponse.Exception.ToException());
            }
            catch (Exception ex)
            {
                response = null;
                return new Exception("RequestStatusUpdatesForOrders() threw an exception for GUIDs: " + string.Join(",", statusRequest.OrderGuids), ex);
            }
        }

        #endregion

        #region Software update handling
        /// <summary>
        /// Queries the latest version info for the specified Proschlaf application.
        /// </summary>
        /// <param name="branchOfficeCode">The branch office code of the client.</param>
        /// <param name="response"></param>
        /// <returns></returns>
        public Exception GetSoftwareCurrentVersionInfo(string branchOfficeCode, JsonConstants.JsonProschlafApplications application, out JsonSoftwareUpdateVersionResponse response)
        {
            try
            {
                response = null;

                if (string.IsNullOrEmpty(branchOfficeCode) || application == JsonConstants.JsonProschlafApplications.NotSet)
                    return new ArgumentException("One of the specified arguments is either NULL or invalid.");

                HttpWebRequest webRequest = GetWebRequest(Endpoints.GetSoftwareCurrentVersionInfo);

                using (var writer = webRequest.GetRequestStream())
                {
                    JsonSoftwareUpdateVersionRequest myRequest = new JsonSoftwareUpdateVersionRequest() { BranchOfficeCode = branchOfficeCode, ProschlafApplication = application };
                    string jsonRequest = JsonConvert.SerializeObject(new { request = myRequest });
                    byte[] requestData = Encoding.UTF8.GetBytes(jsonRequest);
                    writer.Write(requestData, 0, requestData.Length);
                }

                var webResponse = (HttpWebResponse)webRequest.GetResponse();
                var responseStream = new StreamReader(webResponse.GetResponseStream());

                string responseString = responseStream.ReadToEnd();
                JsonSerializerSettings serializerSettings = new JsonSerializerSettings() { Converters = new List<JsonConverter> { new JsonTypeMapper<JsonResult, JsonSoftwareUpdateVersionResponse>() } }; //custom serializer settings which makes it possible to deserialize JSON objects into their corresponding child object
                JsonSoftwareUpdateVersionResponse versionResponse = JsonConvert.DeserializeObject<JsonResultHeader>(responseString, serializerSettings).d as JsonSoftwareUpdateVersionResponse; //without the custom converter, the received JSON objects are not properly converted into their corresponding child POCO

                if (versionResponse != null)
                {
                    response = versionResponse;
                    return null;
                }
                else
                    return new Exception("GetSoftwareCurrentVersionInfo() failed for application: " + application, versionResponse.Exception.ToException());
            }
            catch (Exception ex)
            {
                response = null;
                return new Exception("GetSoftwareCurrentVersionInfo() threw an exception for application: " + application, ex);
            }
        }
        #endregion

        #region Application tracking

        /// <summary>
        /// Reports an application activity (e.g. a pressure mapping that has just been performed) to ISAP.
        /// </summary>
        /// <returns></returns>
        public Exception ReportApplicationActivity(JsonApplicationActivityReport request, bool isTestUpload)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.BranchOfficeCode))
                    return new ArgumentException("'BranchOfficeCode' is NULL or empty.");

                HttpWebRequest webRequest = GetWebRequest(Endpoints.ReportApplicationActivity);

                using (var writer = webRequest.GetRequestStream())
                {
                    string jsonRequest = JsonConvert.SerializeObject(new { request = request, isTestUpload = isTestUpload }, Formatting.Indented, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto }); //also includes type information when serializing to JSON (required because the server expects an abstract class that has to be deserialized into the actual child activity class)
                    byte[] requestData = Encoding.UTF8.GetBytes(jsonRequest);
                    writer.Write(requestData, 0, requestData.Length);
                }

                var webResponse = (HttpWebResponse)webRequest.GetResponse();
                var responseStream = new StreamReader(webResponse.GetResponseStream());

                string responseString = responseStream.ReadToEnd();
                JsonResult result = JsonConvert.DeserializeObject<JsonResultHeader>(responseString).d as JsonResult;

                if (result.IsSuccessful)
                {
                    return null;
                }
                else
                    return new Exception("ReportApplicationActivity() failed for office code: " + request?.BranchOfficeCode, result.Exception.ToException());
            }
            catch (Exception ex)
            {
                if (ex is System.Net.WebException)
                    if ((ex as System.Net.WebException).Status == WebExceptionStatus.ProtocolError && ex.Message.Contains("(403"))
                        return new JsonCredentialsMismatchException("Die angegebenen Zugangsdaten sind falsch oder passen nicht zur Filiale.");

                return new Exception("ReportApplicationActivity() threw an exception for office code: " + request?.BranchOfficeCode, ex);
            }
        }
        #endregion

        #region Device information management
        /// <summary>
        /// Updates the internal information on the received device information.
        /// If the device is yet unknown, it will be created. Missing devices that are known to be at a specific location are not deleted on server-side.
        /// The endpoint is supposed to be called at every application launch after all required devices were connected to (to ensure that ISAP always has the latest information available).
        /// </summary>
        /// <returns></returns>
        public Exception PostDeviceInfoUpdate(JsonDeviceInfoUpdateRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.BranchOfficeCode))
                    return new ArgumentException("'BranchOfficeCode' is NULL or empty.");

                HttpWebRequest webRequest = GetWebRequest(Endpoints.PostDeviceInfoUpdate);

                using (var writer = webRequest.GetRequestStream())
                {
                    string jsonRequest = JsonConvert.SerializeObject(new { request = request });
                    byte[] requestData = Encoding.UTF8.GetBytes(jsonRequest);
                    writer.Write(requestData, 0, requestData.Length);
                }

                var webResponse = (HttpWebResponse)webRequest.GetResponse();
                var responseStream = new StreamReader(webResponse.GetResponseStream());

                string responseString = responseStream.ReadToEnd();
                JsonResult result = JsonConvert.DeserializeObject<JsonResultHeader>(responseString).d as JsonResult;

                if (result.IsSuccessful)
                {
                    return null;
                }
                else
                    return new Exception("PostDeviceInfoUpdate() failed for office code: " + request?.BranchOfficeCode, result.Exception.ToException());
            }
            catch (Exception ex)
            {
                if (ex is System.Net.WebException)
                    if ((ex as System.Net.WebException).Status == WebExceptionStatus.ProtocolError && ex.Message.Contains("(403"))
                        return new JsonCredentialsMismatchException("Die angegebenen Zugangsdaten sind falsch oder passen nicht zur Filiale.");

                return new Exception("PostDeviceInfoUpdate() threw an exception for office code: " + request?.BranchOfficeCode, ex);
            }
        }
        #endregion

        #region Application management
        /// <summary>
        /// Queries the latest version info for the specified Proschlaf application.
        /// </summary>
        /// <param name="branchOfficeCode">The branch office code of the client.</param>
        /// <param name="response"></param>
        /// <returns></returns>
        public Exception RequestPendingApplicationCommands(string branchOfficeCode, string locationName, string applicationVersion, JsonConstants.JsonProschlafApplications application, out JsonApplicationPendingCommandsResponse response)
        {
            try
            {
                response = null;

                if (string.IsNullOrEmpty(branchOfficeCode) || application == JsonConstants.JsonProschlafApplications.NotSet)
                    return new ArgumentException("One of the specified arguments is either NULL or invalid.");

                HttpWebRequest webRequest = GetWebRequest(Endpoints.RequestPendingApplicationCommands);

                using (var writer = webRequest.GetRequestStream())
                {
                    JsonApplicationPendingCommandsRequest myRequest = new JsonApplicationPendingCommandsRequest() { BranchOfficeCode = branchOfficeCode, ProschlafApplication = application, ApplicationLocationName = locationName, ApplicationVersion = applicationVersion };
                    string jsonRequest = JsonConvert.SerializeObject(new { request = myRequest });
                    byte[] requestData = Encoding.UTF8.GetBytes(jsonRequest);
                    writer.Write(requestData, 0, requestData.Length);
                }

                var webResponse = (HttpWebResponse)webRequest.GetResponse();
                var responseStream = new StreamReader(webResponse.GetResponseStream());

                string responseString = responseStream.ReadToEnd();
                JsonSerializerSettings serializerSettings = new JsonSerializerSettings() { Converters = new List<JsonConverter> { new JsonTypeMapper<JsonResult, JsonApplicationPendingCommandsResponse>() } }; //custom serializer settings which makes it possible to deserialize JSON objects into their corresponding child object
                JsonApplicationPendingCommandsResponse commandResponse = JsonConvert.DeserializeObject<JsonResultHeader>(responseString, serializerSettings).d as JsonApplicationPendingCommandsResponse; //without the custom converter, the received JSON objects are not properly converted into their corresponding child POCO

                if (commandResponse != null)
                {
                    response = commandResponse;
                    return null;
                }
                else
                    return new Exception("RequestPendingApplicationCommands() failed for office: " + branchOfficeCode + "/" + locationName, commandResponse.Exception.ToException());
            }
            catch (Exception ex)
            {
                response = null;
                return new Exception("RequestPendingApplicationCommands() threw an exception for office: " + branchOfficeCode + "/" + locationName, ex);
            }
        }
        #endregion

        #region Data management
        /// <summary>
        /// Performs a data backup of all provided files. It is recommended to zip all files of one directory before sending them.
        /// Note that all files should be compressed and encrypted since they are transmitted over the internet.
        /// Maximum combined file size is 20mB. 
        /// </summary>
        /// <param name="files">A list of tuples where Item1 is the file name and Item2 is the file content in compressed and possibly encryped form.</param>
        /// <returns></returns>
        public Exception PostDataBackup(string branchOfficeCode, string locationName, string applicationVersion, string teamViewerId, JsonConstants.JsonProschlafApplications application, List<Tuple<string, byte[]>> filesTuples, bool isTestUpload, out JsonResult response)
        {
            try
            {
                response = null;

                if (string.IsNullOrEmpty(branchOfficeCode) || application == JsonConstants.JsonProschlafApplications.NotSet || filesTuples == null || filesTuples.Count < 1 || filesTuples.Any(t => t.Item1 == null || t.Item2 == null || t.Item2.LongLength < 1))
                    return new ArgumentException("One of the specified arguments is either NULL or invalid.");
                else if (filesTuples.Sum(t => t.Item2.LongLength) > (1000 * 1000 * 20))
                    return new ArgumentOutOfRangeException("Total file size is bigger than 20mB.");

                HttpWebRequest webRequest = GetWebRequest(Endpoints.InsertDataBackup);

                using (var writer = webRequest.GetRequestStream())
                {
                    var files = filesTuples.Select(t => new JsonBackupFile() { FileContentBase64 = Convert.ToBase64String(t.Item2), FileName = t.Item1, IsCompressed = t.Item1.EndsWith(".zip") }).ToList(); //convert the provided file contents to a base-64 string before sending the request since ASP WebMethods don't support receiving byte arrays
                    JsonDataBackupRequest myRequest = new JsonDataBackupRequest() { BranchOfficeCode = branchOfficeCode, ProschlafApplication = application, ApplicationVersion = applicationVersion, BranchOfficeName = locationName, Files = files };

                    string jsonRequest = JsonConvert.SerializeObject(new { request = myRequest, isTestUpload = isTestUpload });
                    byte[] requestData = Encoding.UTF8.GetBytes(jsonRequest);
                    writer.Write(requestData, 0, requestData.Length);
                }

                var webResponse = (HttpWebResponse)webRequest.GetResponse();
                var responseStream = new StreamReader(webResponse.GetResponseStream());

                string responseString = responseStream.ReadToEnd();
                JsonResult result = JsonConvert.DeserializeObject<JsonResultHeader>(responseString).d as JsonResult;

                if (result != null)
                {
                    response = result;
                    return null;
                }
                else
                    return new Exception("PostDataBackup() failed for office: " + branchOfficeCode + "/" + locationName, result.Exception.ToException());
            }
            catch (Exception ex)
            {
                response = null;
                return new Exception("PostDataBackup() threw an exception for office: " + branchOfficeCode + "/" + locationName, ex);
            }
        }
        #endregion
    }
}
