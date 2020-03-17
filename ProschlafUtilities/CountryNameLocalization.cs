using Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace ProschlafUtils
{
    /// <summary>
    /// Provides methods to handle ISO 3166 codes and country names. This class uses the App_Data\Config\country_info.csv file which contains (almost) all countries and some information on them.
    /// </summary>
    public static class CountryNameLocalization
    {
        #region Vars
       // static CultureInfo[] cultures = null;
        static DataTable dtCountryInfos = null;
        #endregion

        /// <summary>
        /// Must be called at application startup.
        /// </summary>
        /// <param name="pathToCountryInfoFile">The absolute path to the "country_info.csv" file.</param>
        public static Exception Initialize(string pathToCountryInfoFile)
        {
            try
            {
                //Reads all country names from the App_Data/Config/country_info.csv file and stores them in the ApplicationState.
                dtCountryInfos = CsvHandler.ReadCSVFile(pathToCountryInfoFile);

                if (dtCountryInfos == null || dtCountryInfos.Rows.Count < 1)
                    return new Exception("Could not initialize CountryNameLocalization for path: " + pathToCountryInfoFile);

                //define primary key
                dtCountryInfos.PrimaryKey = new DataColumn[] { dtCountryInfos.Columns["Country code"] }; //ISO 3166 code column

                return null;
            }
            catch (Exception ex)
            {
                return new Exception("An exception occured while initializing CountryNameLocalization.", ex);
            }
        }

        /// <summary>
        /// Attempts to return the german name for a given two-letter code defined in ISO 3166 (e.g. return "Österreich" for "AT").
        /// </summary>
        /// <param name="isoCode"></param>
        /// <returns></returns>
        public static string GetGermanCountryNameFrom2LetterIsoCode(string isoCode)
        {
            if (string.IsNullOrEmpty(isoCode) || isoCode.Length != 2)
                return null;

            DataRow result = dtCountryInfos.Rows.Find(isoCode);

            if (result != null)
                return result["Country(de)"].ToString();

            //fallback for unknown codes
            return GetCountryNameFrom2LetterIsoCode(isoCode); //converts the country code to the full name (gives English names on the live server because the installed .Net framework is in English)
        }

        /// <summary>
        /// Gets the localized country name for the specified 2-letter ISO code based on the language of the installed .Net Framework.
        /// </summary>
        /// <param name="isoCode"></param>
        /// <returns></returns>
        public static string GetCountryNameFrom2LetterIsoCode(string isoCode)
        {
            if (isoCode == null || isoCode.Length != 2)
                return isoCode;

            RegionInfo r = new RegionInfo(isoCode); //live server is running an English .Net framework --> english DisplayName
            if (r != null)
                return r.DisplayName;

            return isoCode;
        }

        /// <summary>
        /// Returns the ISO 3166 code for a given German country name.
        /// </summary>
        /// <param name="countryName">The German name of a country.</param>
        /// <returns></returns>
        public static string GetISO3166TwoLetterCodeForGermanCountryName(string countryName)
        {
            if (string.IsNullOrEmpty(countryName) || countryName.Length == 2)
                return countryName;

           foreach(DataRow row in dtCountryInfos.Rows)
                if(row["Country(de)"].ToString() == countryName)
                    return row["Country code"].ToString();

            return null;
        }


        /// <summary>
        /// Returns the 3-letter ISO 3166 code for a given 2-letter ISO 3166 code.
        /// </summary>
        /// <param name="isoCode">.</param>
        /// <returns></returns>
        public static string GetISO3166ThreeLetterCodeForTwoLetterCode(string isoCode)
        {
            if (string.IsNullOrEmpty(isoCode) || isoCode.Length != 2)
                return isoCode;

            RegionInfo ri = new RegionInfo(isoCode);
            return ri.ThreeLetterISORegionName;
        }
    }
}
