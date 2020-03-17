using ExternalDataStoreServiceAccess.ServiceReference_DataStore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExternalDataStoreServiceAccess.DataStore
{
    /// <summary>
    /// This class/DLL can be used by any client to access the Proschlaf DataStore. Its intention is to provide a centralized and unified way of accessing the DataStore in a fairly secure way.
    /// For every instance of this class, a channel is opened to the Proschlaf DataStore. For performance reasons, it is recommended to only use one instance of this class.
    /// Note that all communication is secured using Message security. This requires a client certificate to be present in the main application folder under the name "Client.pfx".
    /// </summary>
    public class DataStoreServiceAccess
    {
        #region Vars
        private string serviceEndpointAddress = null;
        private SecurityTypes security = SecurityTypes.Message;

        private ChannelFactory<IDataStoreServices> myChannel = null;

        string pathToClientCertificate = null;
        string clientCertificatePassword = null;
        #endregion

        #region Enums
        public enum SecurityTypes { Message };
        #endregion

        private DataStoreServiceAccess() { }

        /// <summary>
        /// Creates a new accessor to the DataStore WCF service and checks the connection.
        /// </summary>
        /// <exception cref="EndpointNotFoundException">When the connection to the specified server endpoint cannot be established.</exception>
        /// <param name="serviceUrl">The full URL to the DatabaseServices endpoint of the DataStore WCF service. Standard: http://www.service.proschlaf.at:8733/DataStore/DatabaseServices/Secure </param>
        public DataStoreServiceAccess(string serviceUrl, SecurityTypes security, string pathToClientCertificate, string clientCertificatePassword )
        {
            this.serviceEndpointAddress = serviceUrl;
            this.security = security;
            this.pathToClientCertificate = pathToClientCertificate;
            this.clientCertificatePassword = clientCertificatePassword;
        }

        /// <summary>
        /// Checks if the Proschlaf DataStore service is available.
        /// </summary>
        /// <returns>True if available, false otherwise.</returns>
        public bool CheckServerAvailability(out string error)
        {
            string response = null;
            error = null;
            string errorMsg = null;

            Task t = Task.Factory.StartNew(() =>
            {
                try
                {
                    ChannelFactory<IDataStoreServices> cf = GetChannelFactory();

                    IDataStoreServices channel = cf.CreateChannel();
                    response = channel.TestConnection();
                }
                catch (Exception ex )
                {
                    errorMsg = ex.ToString();
                }
            });

            bool taskFinishedProperly = t.Wait(5000); //if the channel doesn't open within 5 seconds, the service is presumed not available

            error = errorMsg;
            return taskFinishedProperly & response != null; //taskFinishedProperly must be true and response must not be null
        }

        /// <summary>
        /// Gets the channel factory to the Proschlaf DataStore.
        /// The factory is only established once and after that, the factory is recycled. 
        /// Do NOT use the "using" statement for the returned channel factory object!
        /// </summary>
        /// <returns></returns>
        private ChannelFactory<IDataStoreServices> GetChannelFactory()
        {
            if (myChannel != null && myChannel.State == CommunicationState.Opened)
                return myChannel;

            var binding = new WSHttpBinding();
            EndpointAddress address = new EndpointAddress(new Uri(serviceEndpointAddress));

            if (security == SecurityTypes.Message)
            {
                binding.Security.Mode = SecurityMode.Message;
                binding.Security.Message.ClientCredentialType = MessageCredentialType.Certificate;
                address = new EndpointAddress(new Uri(serviceEndpointAddress), EndpointIdentity.CreateDnsIdentity("CompanyXYZ Server")); //usually, the DnsIdentity is the domain name. In our case, it's the value that was entered during the creation of the self-signed certificate.
            }
          
            myChannel = new ChannelFactory<IDataStoreServices>(binding, address);

            if (security == SecurityTypes.Message)
            {
                myChannel.Credentials.ServiceCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.None; //since our certificate is self-signed, we don't want it to be validated with a CA
                myChannel.Credentials.ClientCertificate.Certificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(pathToClientCertificate, clientCertificatePassword);
            }

            return myChannel;
        }

        /// <summary>
        ///<para>Uploads the specified file to the Proschlaf DataStore. There, the file is processed and relevant customer data is extracted and stored in a separate database.</para> 
        ///<para>Note that this method returns after the file has been uploaded and thus no information about the database insert process (which starts after the file has been uploaded) is given. </para>
        ///<para>Also note that the dates stored in the uploaded file MUST be in German DateTime-format.</para>
        ///<para>The client as to ensure the data integrity of the uploaded data; meaning that no data record is uploaded multiple times to the DataStore.</para>
        /// Currently, the data structures of the following Proschlaf softwares are supported: 
        ///     - Liegesimulator
        ///     - Ergonometer
        ///     - Orthonometer
        ///     - Ergonometer NL
        ///     - LS 2.0   
        /// </summary>
        /// <param name="filePath">The path to the file to be uploaded.</param>
        /// <param name="branchOfficeName">The name of the vendor where the uploading software is located at.</param>
        /// <param name="branchOfficeCode">The code of the vendor where the uploading software is located at (usually an internal SAP code).</param>
        /// <param name="softwareName">The name of the uploading software (e.g. "Liegesimulator" or "Ergonometer".</param>
        /// <param name="softwareVersion">The assembly version of the uploading software.</param>
        /// <param name="simulatorDeviceSerialNumbers">A list of ids of the simulator devices connected to the uploading software.</param>
        /// <returns>Null if everything went fine or an exception.</returns>
        public Exception UploadCustomerData(string filePath, string branchOfficeName, string branchOfficeCode, string softwareName, string softwareVersion, List<string> simulatorDeviceSerialNumbers, bool isTestUpload)
        {
            try
            {
                ChannelFactory<IDataStoreServices> cf = GetChannelFactory();

                IDataStoreServices channel = cf.CreateChannel();

                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    string fileName = Path.GetFileName(filePath);

                    RemoteFileInfo file = new RemoteFileInfo(branchOfficeCode, branchOfficeName, fileName, isTestUpload, fileStream.Length, simulatorDeviceSerialNumbers.ToArray(), softwareName, softwareVersion, fileStream);

                    ReturnValue returnVal = channel.UploadDatabaseFile(file);

                    return returnVal.Exception;
                }
            }
            catch (CommunicationException cex)
            {
                return cex;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }
}

//Defines certain enums that are used to normalize certain data fields that have different names accross the different softwares that use this DLL.
namespace ExternalDataStoreServiceAccess.DataStore.NormalizedEnums
{
    #region Enums
    public enum SleepPositions { None, Supine, Lateral, Prone, HalfProne, SupineAndProne, LateralAndHalfProne };

    public enum BackPainAreas { None, UpperArea, LowerArea, BothAreas, ShoulderArea };

    public enum SleepQuality { None, QuietSleep, RestlessSleep };
    #endregion
}