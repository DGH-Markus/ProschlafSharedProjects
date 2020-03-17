using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace SqsJsonApiAccess
{
    public abstract class JsonConstants
    {

    }

    /// <summary>
    /// A base class for all JSON requests to this API.
    /// </summary>
    [Serializable]
    public abstract class JsonRequestHeader
    {
        /// <summary>
        /// The branch office code against which the request is authorized against.
        /// </summary>
        public string BranchOfficeCode { get; set; }
    }

    [Serializable]
    public class JsonResultHeader
    {
        public JsonResult d { get; set; }
    }

    [Serializable]
    public class JsonResult
    {
        public bool IsSuccessful { get; set; }
        public JsonExceptionInfo Exception { get; set; }
    }

    [Serializable]
    public class JsonExceptionInfo
    {
        public string UserMessage { get; set; }
        public string ExceptionMessage { get; set; }
        public string StackTrace { get; set; }

        public JsonExceptionInfo() { }

        public JsonExceptionInfo(string userMessage, string exceptionMessage = null)
        {
            this.UserMessage = userMessage;
            this.ExceptionMessage = exceptionMessage;
        }

        public Exception ToException()
        {
            if (!string.IsNullOrEmpty(UserMessage))
                return new Exception(UserMessage, new Exception(ExceptionMessage)) { };
            else
                return new Exception(ExceptionMessage) { };
        }
    }

    [Serializable]
    public class JsonCommissionDataRequest : JsonRequestHeader
    {
        /// <summary>
        /// Corresponds to the commission name of an order resulting from this customer's data.
        /// </summary>
        public string CommissionName { get; set; }

        /// <summary>
        /// A list of all global handles of customers on which this commission data is based on. 
        /// Must not be NULL or empty when sending data to SQS.
        /// </summary>
        public List<Guid> CustomerGlobalHandles { get; set; }

        public DateTime? DateOfDelivery { get; set; } //when the sold articles arrived at the customer

        public decimal? TotalSalesPrice { get; set; }

        public string SalesPersonName { get; set; }

        public string AdvertisementChannelName { get; set; }

        public bool? SaleIsClosed { get; set; } //determines whether the deal with the customer has been closed or not
    }

    [Serializable]
    public class JsonCommissionDataResponse : JsonResult
    {
        //nothing to do here
    }
}