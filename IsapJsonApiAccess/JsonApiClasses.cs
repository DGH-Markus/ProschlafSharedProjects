using Newtonsoft.Json;
using static SapCommons.SapItemCommons;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using static IsapJsonApiAccess.JsonConstants;
using SapCommons;

namespace IsapJsonApiAccess
{
    public static class JsonConstants
    {
        public enum JsonComponentTypes { Unknown, Basecore, Topping, Bedcover, SlattedFrame } //ALWAYS include "topping" in the value if it's a topping! Note that a slatted frame can be a component as part of a set as well as a stand-alone sales article.
        public enum JsonSlattedFrameTypes { Unknown, Vitario_Flex }
        public enum JsonPillowTypes { Unknown, Vitario_Kissen, Vitario_Kissen_Kartonverpackung }
        public enum JsonMattressSetupTypes { NotSet, _1K_2OS_1ÜZ, _2K_2OS_1ÜZ, _1K_1OS_1ÜZ, _2K_1OS_1ÜZ }

        public enum JsonGenders { NotSet, Male, Female, Other }

        public enum JsonProschlafApplications { NotSet = 0, Liegesimulator2_0 = 1, Orthonometer = 2, Ergonometer_NL = 3, Liegesimulator = 4 } //when adding values here, the ISAP method "GetSoftwareNameForJsonApplication()" in SoftwareUpdate.cs has to be updated as well
        public enum JsonProschlafDeviceTypes { NotSet = 0, Osterer = 1, ngMatt = 2, LS2_0 = 3 } //note that this enum should match with BusinessObjects.SimulatorLocationDeviceInfos.DeviceTypes
        public enum JsonDisactivableComponentTypes { Topping = 1, Bedcover = 2 }
        public enum JsonAppicationCommands { None, LockDownApplication } //a list of possible commands to be issued by ISAP to a specific application
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
    public class JsonArticleMasterDataStatusResponse : JsonResult
    {
        public string Version { get; set; } = "1";
        public bool IsAutomaticUpdateOfClientMasterDataEnabled { get; set; }
    }

    [Serializable]
    public class JsonArticleRequest : JsonRequestHeader
    {
        public string TargetCulture { get; set; }
        public List<string> ArticleCategories { get; set; }
    }

    [Serializable]
    public class JsonArticlesResponse : JsonResult
    {
        public List<JsonArticle> Articles { get; set; }
    }

    [Serializable]
    public class JsonArticle
    {
        public string ArticleName { get; set; }
        public string ArticleCode { get; set; }
        public string DisplayName { get; set; } //the display name that can be shown to the user on client-side
        public string Category { get; set; } //for mattresses only
        public string Category2 { get; set; } //dual mattresses with 2 cores ("2K"-) may have a different model on the second side of the bed --> this property may hold that second category
        public string Firmness { get; set; }
        public string SetupType { get; set; } //e.g. "1K–1OS–1ÜZ" or "2K–2OS–1ÜZ", must still be defined exactly somewhere
        public string EdgeType { get; set; } // "Sitzkante", e.g. "Sitzkante links" or "Sitzkante rechts", must still be defined exactly somewhere
        public string SlattedFrameCategory { get; set; }
        public string PillowCategory { get; set; }
        public int Width { get; set; } //in centimeters
        public int Length { get; set; } //in centimeters
        public List<JsonArticleComponent> Components { get; set; } = new List<JsonArticleComponent>();

        public JsonArticle()
        {

        }

        public JsonArticle(string mattressCategory)
        {
            this.Category = mattressCategory;
        }

        public JsonPillowTypes GetPillowCategory()
        {
            switch (this.PillowCategory)
            {
                case SapItemCommons.PILLOW_VITARIO:
                    return JsonPillowTypes.Vitario_Kissen;
                case SapItemCommons.PILLOW_VITARIO_KARTONVRPACKUNG:
                    return JsonPillowTypes.Vitario_Kissen_Kartonverpackung;
                default:
                    return JsonPillowTypes.Unknown;
            }
        }

        public override string ToString()
        {
            return ArticleName + " [" + ArticleCode + "]";
        }
    }

    [Serializable]
    public class JsonArticleComponent
    {
        public string ComponentName { get; set; }
        public string ComponentCode { get; set; }
        public string ComponentDisplayName { get; set; } //the display name that can be shown to the user on client-side
        public int Width { get; set; } //in centimeters
        public int Length { get; set; } //in centimeters
        public JsonConstants.JsonComponentTypes ComponentType { get; set; }
        public string Firmness { get; set; } //for certain toppings
        public string BedcoverType { get; set; } //e.g. "Kaschmir Kasten", must still be defined exactly somewhere
        public string ToppingType { get; set; } // e.g. "5cm" or "7 Spring", must still be defined exactly somewhere

        public override string ToString()
        {
            return ComponentType + ": " + ComponentName + " [" + Firmness + "]";
        }
    }

    public class JsonOfficeRequest : JsonRequestHeader
    {

    }

    [Serializable]
    public class JsonOfficeResponse : JsonResult
    {
        public string OfficeName { get; set; }
        public string OfficeCode { get; set; }
        public string Street { get; set; }
        public string ZIP { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Email { get; set; }
        public string Telephone { get; set; }
    }

    [Serializable]
    public class JsonIsapOfficesResponse : JsonResult
    {
        /// <summary>
        /// Key = office code. Value = office name.
        /// </summary>
        public Dictionary<string, string> IsapBranchOffices { get; set; }
    }

    /// <summary>
    /// This class represents a customer order that is based on one or more customers. It contains order lines which in turn contain the articles to be shipped to the customer(s).
    /// </summary>
    [Serializable]
    public class JsonCustomerOrderRequest : JsonRequestHeader
    {
        #region Properties
        public string BranchOfficeName { get; set; } //the branch office display name used to submit the order on client-side
        public Guid Guid { get; set; } //a global identificator set by the client
        public DateTime CreationDate { get; set; }
        public string OrderNumber { get; set; } //the local order number
        public string OrderName { get; set; } //the local order name
        public string OrderComment { get; set; }
        public DateTime? DesiredDueDate { get; set; }
        public bool IsStockOrder { get; set; } //if set to true, no end customers are related to this order

        public string ShippingAddress_Recipient { get; set; }
        public string ShippingAddress_Recipient_AdditionalLine { get; set; }
        public string ShippingAddress_Street { get; set; }
        public string ShippingAddress_City { get; set; }
        public string ShippingAddress_ZIP { get; set; }
        public string ShippingAddress_Country { get; set; }

        public List<JsonCustomerOrderLine> OrderLines { get; set; } = new List<JsonCustomerOrderLine>();

        public JsonCustomerOrderDualMattressSetup DualMattressSetup { get; set; }

        /// <summary>
        /// True if at least one article in this order is not a stock article.
        /// </summary>
        public bool IsCustomOrder
        {
            get
            {
                return this.OrderLines.FirstOrDefault(l => !l.IsSapArticle) != null;
            }
        }

        #endregion
    }

    /// <summary>
    /// Represents one order line that is based on a customer and contains one article to be shipped to the customer.
    /// Multiple lines can reference the same customer.
    /// </summary>
    [Serializable]
    public class JsonCustomerOrderLine
    {
        #region Properties
        public short LineNumber { get; set; } //unique within the order, set by client
        public int CustomerId { get; set; } //the local Id of the customer, set by client
        public Guid? CustomerGlobalHandle { get; set; } //the global Id of the customer, set by client, used to relate this line with the local customer data
        public string CommissionName { get; set; }
        public string CustomerFirstName { get; set; }
        public string CustomerLastName { get; set; } //the customer's name can be changed after an order has been sent, so the order line has to contain the name that was used during the its creation
        public JsonGenders CustomerGender { get; set; }
        public string LineOrderNumber { get; set; } //the local order number for this specific line

        public string ArticleCode { get; set; }
        public string ArticleName { get; set; }
        public string ArticleDisplayName { get; set; } //the name as it was displayed to the user
        public short ArticleQuantity { get; set; }
        public string ArticleCustomName { get; set; } //if the vendor wants to order a custom article, this property contains the custom name and 'ArticleCode' and 'ArticleName' are left blank
        public bool IsStockArticleWithMissingCode { get; set; } //if a stock article has been selected, that does not yet exist in SAP, this flag is true
        public int? ArticleWidthCm { get; set; } //optional data in the width of the item in cm
        public int? ArticleLengthCm { get; set; } //optional data in the length of the item in cm

        public string ArticleFirmness { get; set; } //in case of certain mattress models (e.g. Schulterconception), the model has variable firmness levels instead of a support profile
        public string SupportProfile { get; set; }
        public string PillowSetup { get; set; } //if a pillow is ordered, this field may contain the corresponding profile/setup

        public bool IsSapArticle { get; set; } //determines whether an article can be processed automatically via ISAP (if the user issued a custom order, this flag must be false)
        #endregion
    }

    /// <summary>
    /// Represents a dual mattress setup that involves 2 order lines.
    /// </summary>
    [Serializable]
    public class JsonCustomerOrderDualMattressSetup
    {
        #region Properties
        public int OrderId { get; set; } //the order Id that is the target of this class
        public short LineNumber_LeftSide { get; set; } //the line number of the person on the left half of the dual mattress
        public short LineNumber_RightSide { get; set; } //the line number of the person on the right half of the dual mattress
        public bool IsMattressWithSingleTopper { get; set; }
        public short MattressWidth { get; set; }
        public short MattressLength { get; set; }

        public JsonMattressSetupTypes MattressSetupType { get; set; } = JsonMattressSetupTypes.NotSet;

        #endregion
    }

    [Serializable]
    public class JsonCustomerOrderResponse : JsonResult
    {
        public Guid ConfirmedOrderGuid { get; set; }
        public int ConfirmedOrderLinesCount { get; set; }
    }

    /// <summary>
    /// Used to request the canncellation of a customer order after an order has been transmitted to ISAP.
    /// </summary>
    [Serializable]
    public class JsonOrderCancellationRequest : JsonRequestHeader
    {
        public Guid OrderToCancelGuid { get; set; }
    }

    [Serializable]
    public class JsonCancelOrderResponse : JsonResult
    {
        /// <summary>
        /// If cancellation is possible, this property is set to 'true'.
        /// </summary>
        public bool IsCancellationConfirmed { get; set; }

        /// <summary>
        ///  If cancellation is possible, this property contains the GUID of the order that has been cancelled.
        /// </summary>
        public Guid? CancelledOrderGuid { get; set; }
    }

    /// <summary>
    /// Used to request the status of canncellation requests concerning one or more customer orders.
    /// </summary>
    [Serializable]
    public class JsonOrderCancellationStatusRequest : JsonRequestHeader
    {
        public List<Guid> CancelledOrderGuids { get; set; }
    }

    [Serializable]
    public class JsonCancellationStatusResponse : JsonResult
    {
        public enum CancellationStates { WaitingForProcessing, CurrentlyProcessing, Completed, CancellationDenied, CancellationNotFound }

        public List<JsonCancellationStatus> JsonCancellationStates { get; set; }
    }

    [Serializable]
    public class JsonCancellationStatus
    {
        public Guid OrderToCancelGuid { get; set; }
        public JsonCancellationStatusResponse.CancellationStates Status { get; set; }
        public DateTime? TimeOfCompletion { get; set; }
    }

    /// <summary>
    /// Used to request an update on the status of one or more orders.
    /// </summary>
    [Serializable]
    public class JsonOrdersStatusRequest : JsonRequestHeader
    {
        public List<Guid> OrderGuids { get; set; }
    }

    /// <summary>
    /// The response to an order status update request.
    /// Includes information on the order confirmation data for the requested orders.
    /// </summary>
    [Serializable]
    public class JsonOrdersStatusResponse : JsonResult
    {
        public int StatusUpdatesCount { get; set; }

        public List<JsonOrdersStatusResponseLine> StatusLines { get; set; }
    }

    [Serializable]
    public class JsonOrdersStatusResponseLine
    {
        public Guid Guid { get; set; }
        public bool IsPresentInISAP { get; set; } = true; //if the order has been deleted manually, this flag is false
        public int? ConfirmedSapOrderNumber { get; set; }
        public bool? IsCancelledInSAP { get; set; } //if the SAP order has been cancelled (with or without a CancellationRequest by LS 2.0)
        public DateTime? ConfirmedDeliveryDate { get; set; }
        public DateTime? DeliveryNoteDate { get; set; }
    }

    [Serializable]
    public class JsonSoftwareUpdateVersionRequest : JsonRequestHeader
    {
        public JsonProschlafApplications ProschlafApplication { get; set; }
    }

    [Serializable]
    public class JsonSoftwareUpdateVersionResponse : JsonResult
    {
        public string BranchOfficeCode { get; set; }
        public JsonProschlafApplications ProschlafApplication { get; set; }
        public string Version { get; set; } //convertable from/to VersionInfo
        public DateTime VersionDateUTC { get; set; } 
        public DateTime VersionDate { get; set; } //legacy (can be removed when no more LS 2.0 version < 1.0.19.19 is in use)
        public bool IsTestVersion { get; set; }
        public bool IsUpdatePossible { get; set; }
        public string ReleaseNotes { get; set; }
        public string Filename { get; set; }
        public long FileSizeBytes { get; set; }
        public Uri DownloadLink { get; set; }
    }

    [Serializable]
    public class JsonApplicationLocationUpdateRequest : JsonRequestHeader
    {
        public string BranchOfficeName { get; set; }
        public JsonProschlafApplications ProschlafApplication { get; set; }
        public string ApplicationVersion { get; set; }
        public List<JsonDeviceInformation> DeviceInfos { get; set; }
        public JsonOfficeAddress BranchOfficeAddress { get; set; }
        public string Telephone { get; set; }
        public string Email { get; set; }
        public string TeamViewerId { get; set; }
        public string TeamViewerPassword { get; set; }
        public string Notes { get; set; }
    }

    [Serializable]
    public class JsonDeviceInfoUpdateRequest : JsonRequestHeader
    {
        public JsonProschlafApplications ProschlafApplication { get; set; }
        public List<JsonDeviceInformation> DeviceInfos { get; set; }
    }

    [Serializable]
    public class JsonDeviceInformation
    {
        public string SerialNumber { get; set; }
        public string FirmwareVersion { get; set; }
        public JsonProschlafDeviceTypes DeviceType { get; set; }
    }

    [Serializable]
    public class JsonOfficeAddress
    {
        public string Street { get; set; }
        public string ZIP { get; set; }
        public string City { get; set; }
        public string Country { get; set; } //the localized country name is expected here
    }

    /// <summary>
    /// IMPORTANT: changes to this class must be reflected in the custom JavaScriptConverter on server side (JsonApplicationActivityReportConverter).
    /// </summary>
    [Serializable]
    public abstract class JsonApplicationActivityReport : JsonRequestHeader
    {
        public string BranchOfficeName { get; set; }
        public string TeamViewerId { get; set; } //required to be able to differentiate between mutliple instances at the same location
        public JsonProschlafApplications Application { get; set; }
        public string ApplicationVersion { get; set; }
        public DateTime ActivityDate { get; set; }
    }

    [Serializable]
    public class JsonMeasurementActivityReport : JsonApplicationActivityReport
    {
        public int NumberOfMeasurements { get; set; }
        public string DeviceSerialNumber { get; set; }
        public List<int[]> MeasurementValues { get; set; }
    }

    [Serializable]
    public class JsonSimulationActivityReport : JsonApplicationActivityReport
    {
        public int NumberOfSimulations { get; set; }
        public List<string> DeviceSerialNumbers { get; set; }
        public string OriginalSimulationProfile { get; set; }
        public string LastSimulationProfile { get; set; }
    }

    [Serializable]
    public class JsonArchiveActivityReport : JsonApplicationActivityReport
    {
        public int NumberOfCustomersViewed { get; set; }
        public int NumberOfCustomersSimulatedAgain { get; set; }
    }

    [Serializable]
    public class JsonErrorActivityReport : JsonApplicationActivityReport
    {
        public string ErrorMessage { get; set; }
        public string LastUserActivities { get; set; }
        public string Exception { get; set; }
    }

    /// <summary>
    /// Used to update the list of activated and deactivated components for a simulator location.
    /// </summary>
    [Serializable]
    public class JsonUpdateDeactivatedComponentsRequest : JsonRequestHeader
    {
        public string TeamViewerId { get; set; } //required to be able to differentiate between mutliple instances at the same location
        public SapCommons.SapItemCommons.SAPModelCategories Model { get; set; }
        public List<JsonActiveComponent> ActiveComponents { get; set; }
        public List<JsonDeactivatedComponent> DeactivatedComponents { get; set; }
    }

    [Serializable]
    public class JsonActiveComponent
    {
        public JsonConstants.JsonDisactivableComponentTypes ComponentType { get; set; }
        public List<string> ComponentCategories { get; set; }
    }

    [Serializable]
    public class JsonDeactivatedComponent
    {
        public JsonConstants.JsonDisactivableComponentTypes ComponentType { get; set; }
        public List<string> ComponentCategories { get; set; }
    }

    [Serializable]
    public class JsonApplicationPendingCommandsRequest : JsonRequestHeader
    {
        public JsonProschlafApplications ProschlafApplication { get; set; }
        public string ApplicationVersion { get; set; }
        public string ApplicationLocationName { get; set; }
    }

    [Serializable]
    public class JsonApplicationPendingCommandsResponse : JsonResult
    {
        /// <summary>
        /// A list of pending commands to be executed upon retrieval.
        /// </summary>
        public List<JsonAppicationCommands> Commands { get; set; }
    }

    [Serializable]
    public class JsonDataBackupRequest : JsonRequestHeader
    {
        public string BranchOfficeName { get; set; }
        public string TeamViewerId { get; set; } //required to be able to differentiate between mutliple instances at the same location
        public JsonProschlafApplications ProschlafApplication { get; set; }
        public string ApplicationVersion { get; set; }
        public List<JsonBackupFile> Files { get; set; } = new List<JsonBackupFile>();
    }

    [Serializable]
    public class JsonBackupFile
    {
        public bool IsCompressed { get; set; }
        public string FileName { get; set; }
        public string FileContentBase64 { get; set; }

    }
}