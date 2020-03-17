using ExternalDataStoreServiceAccess.DataExchangeServiceReference;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;

namespace ExternalDataStoreServiceAccess.Isap
{
    /// <summary>
    /// This class can be used to access ISAP's wcf services.
    /// All communication is secured using Transport security (https / SSL) and no client certificate is required for this. Instead, username/password security is used on transport level.
    /// </summary>
    public class IsapServiceAccess
    {
        #region Vars
        private string serviceEndpointAddress = null;
        private string username = null;
        private string password = null;
        #endregion

        [Serializable]
        public enum CustomerOrderModes { None = 0, Online = 1, Offline_Pdf = 2 }
        private IsapServiceAccess() { }

        /// <summary>
        /// Creates a new accessor to the ISAP WCF service. The service is secured with transport security (https) and required a username and a password.
        /// The list of valid credentials can be found in ISAP's "CustomUsernamePasswordValidator.cs" in the "SimulatorWcfServices" project.
        /// </summary>
        /// <param name="serviceUrl">The full URL to the DatabaseServices endpoint of the ISAP DataExchangeService WCF service. Standard is: https://www.service.proschlaf.at/SimulatorWcfServices.svc/secureWithAuth </param>
        public IsapServiceAccess(string username, string password, string serviceUrl = "https://www.service.proschlaf.at/SimulatorWcfServices.svc/secureWithAuth")
        {
            this.serviceEndpointAddress = serviceUrl;
            this.username = username;
            this.password = password;

#if DEBUG
            ServicePointManager.ServerCertificateValidationCallback = ((sender, certificate, chain, sslPolicyErrors) => true); //skip the validation of the server certificate in debug mode
#endif
        }

        /// <summary>
        /// OBSOLETE, replaced by GetSecureBindingWithAuthentication().
        /// </summary>
        /// <returns></returns>
        private static WSHttpBinding GetSecureBindingOBSOLETE()
        {
            WSHttpBinding binding = new WSHttpBinding();
            binding.Security.Mode = SecurityMode.Transport;
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;
            return binding;
        }

        /// <summary>
        /// Creates and returns the required binding that is needed to access the ISAP WCF service "SimulatorWcfServices.svc".
        /// </summary>
        /// <returns></returns>
        private static WSHttpBinding GetSecureBindingWithAuthentication()
        {
            WSHttpBinding binding = new WSHttpBinding(SecurityMode.TransportWithMessageCredential);
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;
            binding.Security.Message.ClientCredentialType = MessageCredentialType.UserName;
            return binding;
        }

        /// <summary>
        /// Configures the provided IDataExchangeService properly so that it can be used to access the  ISAP WCF service "SimulatorWcfServices.svc".
        /// Sets username and password because the endpoint used transport security with transport username/password authentication.
        /// </summary>
        /// <param name="cf"></param>
        private void ConfigureChannelFactoryForSecureCommunication(ChannelFactory<IDataExchangeService> cf)
        {
            //set username and password for transport security
            var credentialBehaviour = cf.Endpoint.Behaviors.Find<ClientCredentials>();
            credentialBehaviour.UserName.UserName = username;
            credentialBehaviour.UserName.Password = password;
        }

        /// <summary>
        /// Queries a list of all branch offices that are currently stored in the ISAP system.
        /// <param name="dictionary">A dictionary that will hold the queried data where the key is the BranchOfficeCode and the value is the BranchOfficeName.</param>
        /// </summary>
        /// <returns>Null if successfull or an exception.</returns>
        public Exception GetIsapBranchOffices(out IDictionary<string, string> dictionary)
        {
            dictionary = null;

            try
            {
                // var binding = GetSecureBinding();
                var binding = GetSecureBindingWithAuthentication();

                using (ChannelFactory<IDataExchangeService> cf = new ChannelFactory<IDataExchangeService>(binding, serviceEndpointAddress))
                {
                    //  cf.Credentials.ServiceCertificate.Authentication.CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.None; //no need to validate the server certificate

                    ConfigureChannelFactoryForSecureCommunication(cf);

                    IDataExchangeService channel = cf.CreateChannel();

                    var dict = channel.GetBranchOffices();

                    if (dict != null)
                        dictionary = dict;
                    else
                        return new Exception("Fehler bei der Abfrage der Händler: es wurde NULL zurückgegeben.");

                    return null;
                }
            }
            catch (CommunicationException cex)
            {
                return new Exception("Communication exception", cex);
            }
            catch (Exception ex)
            {
                return new Exception("Unhandled exception", ex);
            }
        }

        /// <summary>
        /// Queries a list of all branch offices relevant for the Dutch Ergonometer NL software that are currently stored in the ISAP system.
        /// OBSOLETE AS OF MAY 2017 (ISAP will return a list containing only 1 warning entry).
        /// <param name="offices">An array of IsapBranchOffices that will hold the queried data.</param>
        /// </summary>
        /// <returns>Null if successfull or an exception.</returns>
        public Exception GetBranchOfficesForErgonometerNL(out IsapBranchOffice[] offices)
        {
            offices = null;

            try
            {
                var binding = GetSecureBindingWithAuthentication();
                using (ChannelFactory<IDataExchangeService> cf = new ChannelFactory<IDataExchangeService>(binding, serviceEndpointAddress))
                {
                    ConfigureChannelFactoryForSecureCommunication(cf);

                    IDataExchangeService channel = cf.CreateChannel();

                    Exception ex = channel.GetBranchOfficesForErgonometerNL(out offices);

                    if (ex != null)
                    {
                        offices = null;
                        return ex;
                    }

                    return null;
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

        public Exception UpdateIsapBranchOffice(string officeCode, List<string> simulatorIds, string simulatorSoftwareVersion, string simulatorSoftwareName)
        {
            try
            {
                var binding = GetSecureBindingWithAuthentication();

                using (ChannelFactory<IDataExchangeService> cf = new ChannelFactory<IDataExchangeService>(binding, serviceEndpointAddress))
                {
                    ConfigureChannelFactoryForSecureCommunication(cf);

                    IDataExchangeService channel = cf.CreateChannel();

                    return channel.UpdateBranchOfficeByCode(officeCode, string.Join(", ", simulatorIds), simulatorSoftwareName, simulatorSoftwareVersion);
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

        /// <summary>
        /// Sends the stored Masterdata of this software to the ISAP system.
        /// </summary>
        /// <returns>Null if successfull or an exception.</returns>
        public Exception CreateOrUpdateIsapMasterData(string locationID, string locationName, List<DeviceInformationContract> simulatorInfos, string softwareName, string softwareVersion, string address_street, string address_ZIP, string address_location, string address_country, string phoneNumber, string email, string teamViewerID, string teamViewerPW)
        {
            try
            {
                var binding = GetSecureBindingWithAuthentication();

                using (ChannelFactory<IDataExchangeService> cf = new ChannelFactory<IDataExchangeService>(binding, serviceEndpointAddress))
                {
                    ConfigureChannelFactoryForSecureCommunication(cf);

                    IDataExchangeService channel = cf.CreateChannel();
                    return channel.CreateOrUpdateApplicationLocation(locationID, locationName, simulatorInfos.ToArray(), softwareName, softwareVersion, address_street, address_ZIP, address_location, address_country, phoneNumber, email, teamViewerID, teamViewerPW);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tempDirForZipFiles">The folder where the *zip files can be written to.</param>
        /// <param name="pathCustomerDirectory"></param>
        /// <param name="pathConfigFile"></param>
        /// <param name="pathMasterDataDirectory"></param>
        /// <param name="softwareName"></param>
        /// <param name="softwareVersion"></param>
        /// <param name="branchOfficeName"></param>
        /// <param name="branchOfficeCode"></param>
        /// <param name="isTestUpload"></param>
        /// <returns></returns>
        public Exception PerformDataBackup(string tempDirForZipFiles, string pathCustomerDirectory, string pathConfigFile, string pathMasterDataDirectory, string softwareName, string softwareVersion, string branchOfficeName, string branchOfficeCode, bool isTestUpload)
        {
            string customerDataZipFileName = Path.Combine(tempDirForZipFiles, "Kundendaten.zip");
            string masterDataZipFileName = Path.Combine(tempDirForZipFiles, "Stammdaten.zip");

            try
            {
                #region Validation
                if (string.IsNullOrEmpty(tempDirForZipFiles) || string.IsNullOrEmpty(pathCustomerDirectory) || string.IsNullOrEmpty(pathMasterDataDirectory) || string.IsNullOrEmpty(softwareName) || string.IsNullOrEmpty(softwareVersion) || string.IsNullOrEmpty(branchOfficeName) || string.IsNullOrEmpty(branchOfficeCode))
                    return new ArgumentNullException("", "Einer der angegebenen Parameter ist NULL.");
                #endregion

                #region Create ZIP archives before upload

                //Customer data directory
                if (!Directory.Exists(pathCustomerDirectory))
                    throw new DirectoryNotFoundException("pathCustomerDirectory: " + pathCustomerDirectory);

                string[] customerDataFiles = Directory.GetFiles(pathCustomerDirectory);

                if (customerDataFiles.Length < 1) //no files present
                    throw new Exception("Der angegebene Kundendaten-Ordner ist leer!");

                foreach (string filePath in customerDataFiles)
                    if(!filePath.EndsWith("Backup"))
                        AddFileToZip(customerDataZipFileName, filePath);

                //master data directory
                if (!Directory.Exists(pathMasterDataDirectory))
                    throw new DirectoryNotFoundException("pathMasterDataDirectory: " + pathMasterDataDirectory);

                string[] masterDataFiles = Directory.GetFiles(pathMasterDataDirectory);

                if (masterDataFiles.Length < 1) //no files present
                    throw new Exception("Der angegebene Stammdaten-Ordner ist leer!");

                foreach (string filePath in masterDataFiles)
                    if (!filePath.EndsWith("Backup"))
                        AddFileToZip(masterDataZipFileName, filePath);
                #endregion

                //read files as byte arrays
                byte[] customerDataZipFile = File.ReadAllBytes(customerDataZipFileName);
                byte[] masterDataZipFile = File.ReadAllBytes(masterDataZipFileName);
                byte[] configFile = string.IsNullOrEmpty(pathConfigFile) ? new byte[10] : File.ReadAllBytes(pathConfigFile);

                var binding = GetSecureBindingWithAuthentication();

                using (ChannelFactory<IDataExchangeService> cf = new ChannelFactory<IDataExchangeService>(binding, serviceEndpointAddress))
                {
                    ConfigureChannelFactoryForSecureCommunication(cf);

                    IDataExchangeService channel = cf.CreateChannel();

                    Exception ex = channel.InsertLiegesimulatorCustomerDataBackup(customerDataZipFile, configFile, masterDataZipFile, softwareName, softwareVersion, branchOfficeName, branchOfficeCode, isTestUpload);

                    if (ex != null)
                        return ex;

                    return null;
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
            finally
            {
                //cleanup
                if (File.Exists(customerDataZipFileName))
                    File.Delete(customerDataZipFileName);

                if (File.Exists(masterDataZipFileName))
                    File.Delete(masterDataZipFileName);
            }
        }

        private static void AddFileToZip(string zipFilename, string fileToAdd)
        {
            using (Package zip = System.IO.Packaging.Package.Open(zipFilename, FileMode.OpenOrCreate))
            {
                string destFilename = ".\\" + Path.GetFileName(fileToAdd);
                Uri uri = PackUriHelper.CreatePartUri(new Uri(destFilename, UriKind.Relative)); //this has the slight downside that it replaces special characters like space with "%20"

                if (zip.PartExists(uri))
                    zip.DeletePart(uri);

                PackagePart part = zip.CreatePart(uri, "", CompressionOption.Maximum);
                using (FileStream fileStream = new FileStream(fileToAdd, FileMode.Open, FileAccess.Read))
                {
                    using (Stream dest = part.GetStream())
                    {
                        CopyStream(fileStream, dest);
                    }
                }
            }
        }

        private static void CopyStream(System.IO.FileStream inputStream, System.IO.Stream outputStream)
        {
            long bufferSize = inputStream.Length < 4096 ? inputStream.Length : 4096;
            byte[] buffer = new byte[bufferSize];
            int bytesRead = 0;
            long bytesWritten = 0;
            while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                outputStream.Write(buffer, 0, bytesRead);
                bytesWritten += bytesRead;
            }
        }

        /// <summary>
        /// Sends the specified orders to ISAP for commissioning and further processing. During this process, the orders will be automatically inserted into the company's SAP system.
        /// The data transmitted has to be validated properly before sending it.
        /// Furhtermore, detailed error handling has to be performed in case of errors. The user must be informed about non-transmitted orders to that they can react properly.
        /// It is important that no orders are lost or forgotton during the whole process!
        /// </summary>
        /// <param name="ordersToSend"></param>
        /// <returns></returns>
        /*
        public Exception SendNewCommissionsToIsap(CustomerOrderContract[] ordersToSend)
        {
            var binding = GetSecureBindingWithAuthentication();
         
            using (ChannelFactory<IDataExchangeService> cf = new ChannelFactory<IDataExchangeService>(binding, serviceEndpointAddress))
            {
                ConfigureChannelFactoryForSecureCommunication(cf);

                IDataExchangeService channel = cf.CreateChannel();

                int processedCnt;
                Dictionary<CustomerOrderContract, String[]> failedOrders;
                Exception ex = channel.CommissionNewCustomerSalesOrders(out processedCnt, out failedOrders, ordersToSend);

                if (ex != null)
                    return new Exception("Beim Übermitteln der Aufträge ist es zu Fehlern gekommen. Von " + ordersToSend.Count() + " Aufträgen konnten " + failedOrders + " nicht übermittelt werden.", ex);

                return ex;
            }
        }
        */

        public Exception ReportApplicationActivity(ApplicationActivityReportContract activity, string officeCode, string officeName, string applicationName, string applicationVersion)
        {
            try
            {
                var binding = GetSecureBindingWithAuthentication();

                using (ChannelFactory<IDataExchangeService> cf = new ChannelFactory<IDataExchangeService>(binding, serviceEndpointAddress))
                {
                    ConfigureChannelFactoryForSecureCommunication(cf);

                    IDataExchangeService channel = cf.CreateChannel();

                    return channel.PostApplicationActivity(officeCode, officeName, applicationName, applicationVersion, activity);
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
