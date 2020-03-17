using Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProschlafUtils
{
    /// <summary>
    /// This class provides methods do determine the product type (e.g. mattress or slatted frame) of an existing DGH article.
    /// Recognition is based on the article name only.
    /// </summary>
    public static class MattressModelRecognition
    {
        #region Vars
        private static List<string> queryAllMattressNames = new List<string>(); //contains the mattress model (partial) names of all known mattress products
        private static List<string> queryMattressNamesWhiteList = new List<string>(); //contains the mattress model (partial) names that are supposed to be imported into ISAP
        private static List<string> queryMattressNamesBlackList = new List<string>(); //model names that are not allowed

        private static ConcurrentDictionary<string, ProductTypes> recognizedModels = new ConcurrentDictionary<string, ProductTypes>();

        private static string filePathMattressImportNames = null;
        #endregion

        #region Enums
        /// <summary>
        /// The different types of Proschlaf products this software can handle.
        /// </summary>
        public enum ProductTypes { MattressModel, SlattedFrameModel, PillowModel, ProductSet, Unknown };

        public enum VitarioBaseCoreColors { Silver, Avocado }
        #endregion

        /// <summary>
        /// Must be called before using any other methods of this class. 
        /// </summary>
        /// <param name="_filePathMattressImportNames">The path to the *csv file that contains the definitions of allowed/disallowed mattress model names.</param>
        public static void Initialize(string _filePathMattressImportNames)
        {
            filePathMattressImportNames = _filePathMattressImportNames;
            UpdateMattressModelNamesLists();
        }

        /// <summary>
        /// Updates the allowed/not allowed mattress model names by reading the config *csv file.
        /// Must be called at application start!
        /// </summary>
        private static void UpdateMattressModelNamesLists()
        {
            if (!File.Exists(filePathMattressImportNames))
                return;

            queryMattressNamesWhiteList.Clear();
            queryMattressNamesBlackList.Clear();
            queryAllMattressNames.Clear();

            DataTable dtMattressNames = CsvHandler.ReadCSVFile(filePathMattressImportNames);

            foreach (DataRow row in dtMattressNames.Rows)
            {
                if (row["isMattress"].ToString() == "true")
                    queryAllMattressNames.Add(row["partialName"].ToString().ToLower());

                if (row["mustContain"].ToString() == "true")
                    queryMattressNamesWhiteList.Add(row["partialName"].ToString().ToLower());
                else
                    queryMattressNamesBlackList.Add(row["partialName"].ToString().ToLower());
            }
        }

        /// <summary>
        /// Vitario has different base cores with different colors. This method determines the color of the core by looking at the model's name and the support profile.
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="mattressName">Must be specified.</param>
        /// <returns></returns>
        public static VitarioBaseCoreColors GetVitarioBaseCoreColor(string profile, string mattressName)
        {
            VitarioBaseCoreColors baseColor = VitarioBaseCoreColors.Silver;

            if (mattressName == null)
                return VitarioBaseCoreColors.Silver;

            if ( (profile != null && profile.Contains("A")) || (mattressName.Contains("Comfort") && (mattressName.Contains("7cm") || mattressName.Contains("9,5cm") || mattressName.Contains("XD"))) || mattressName.Contains("450") || mattressName.Contains("Sensitiv") || mattressName.Contains("Latex")) //the new Comfort models and the Ergodeluxe 450 have the avocado-colored core and are named like this: "Vitario Comfort H 1 - Premium Ka. Kl. 100x200 OS 7cm" || "Ergodeluxe 450 H1 90x200"
                if (!mattressName.Contains("Ergodeluxe 500"))
                    baseColor = VitarioBaseCoreColors.Avocado;

            return baseColor;
        }

        #region Product type identifier methods

        /// <summary>
        /// Determines the main type of an article sold by the company.
        /// </summary>
        /// <param name="productName"></param>
        /// <returns></returns>
        public static ProductTypes GetProductType(string productName)
        {
            if (productName == null)
                return ProductTypes.Unknown;

            ProductTypes type;

            if (recognizedModels.ContainsKey(productName)) //check for cached value
            {
                if (recognizedModels.TryGetValue(productName, out type))
                    return type;
            }

            //determine product type
            if (MattressModelRecognition.IsProductSet(productName, true))
                type = ProductTypes.ProductSet;
            else if (MattressModelRecognition.IsMattressModel(productName, true))
                type = ProductTypes.MattressModel;
            else if (MattressModelRecognition.IsPillowModel(productName))
                type = ProductTypes.PillowModel;
            else if (MattressModelRecognition.IsFrameworkModel(productName))
                type = ProductTypes.SlattedFrameModel;
            else
                type = ProductTypes.Unknown;

            recognizedModels.TryAdd(productName, type);

            return type;
        }

        /// <summary>
        /// Checks whether the specified item name is recognized as either a mattress or a product set.
        /// </summary>
        /// <param name="itemName"></param>
        /// <param name="checkBlacklist"></param>
        /// <returns></returns>
        public static bool IsProductSetOrMattress(string itemName, bool checkBlacklist)
        {
            if (IsProductSet(itemName, checkBlacklist))
                return true;

            return IsMattressModel(itemName, checkBlacklist);
        }

        /// <summary>
        /// Determines whether an item is a Set (mattress + framework) or not.
        /// </summary>
        /// <param name="itemName">The name of the product to be identified.</param>
        /// <param name="checkBlacklist">If set to true, the product name will also be checked against a black list that contains products that are not to be impored into ISAP.</param>
        /// <returns>True if set, false otherwise.</returns>
        public static bool IsProductSet(string itemName, bool checkBlacklist)
        {
            if (queryMattressNamesBlackList == null || queryMattressNamesBlackList.Count < 1)
                UpdateMattressModelNamesLists(); //make sure the lists arte properly initialized

            if (string.IsNullOrEmpty(itemName))
                return false;

            if (checkBlacklist)
            {
                if (!itemName.StartsWith("Set:"))
                    return false;

                //product sets are problematic because they can contain blacklisted article names, but are valid products (e.g. "Set: Ergo 4 H3 RG55 90x200 + Lattenrost Basic NV")
                if (itemName.Contains("+")) //split the article name and only check the first one if it's blacklisted
                {
                    if (NotContainsAny(itemName.Split('+')[0], queryMattressNamesBlackList))
                        return true;
                }
                else if (NotContainsAny(itemName, queryMattressNamesBlackList)) //check the whole name
                    return true;
            }
            else
                return itemName.StartsWith("Set:");

            return false;
        }

        /// <summary>
        /// Determines whether an item is a ISAP-compatible mattress model or not. The item name is checked against a mattress name whitelist and a product name blacklist.
        /// </summary>
        /// <param name="itemName">The case-sensitive name of the product to be identified.</param>
        /// <param name="checkBlacklist">If set to true, the product name will also be checked against a black list that contains products that are not to be impored into ISAP.</param>
        /// <returns>True if mattress model, false otherwise.</returns>
        public static bool IsMattressModel(string itemName, bool checkBlacklist)
        {
            if (queryMattressNamesBlackList == null || queryMattressNamesBlackList.Count < 1)
                UpdateMattressModelNamesLists(); //make sure the lists are properly initialized

            if (string.IsNullOrEmpty(itemName))
                return false;

            if (itemName.StartsWith("Lattenrost"))
                return false;

            if (checkBlacklist)
            {
                if (ContainsAny(itemName, queryMattressNamesWhiteList) && NotContainsAny(itemName, queryMattressNamesBlackList))
                    return true;
            }
            else
            {
                if (itemName.Contains("Kopfkissen"))
                    return false;

                //check the cache before checking against a list of pre-defined partial mattress names
                if (recognizedModels.ContainsKey(itemName)) //check for cached value
                {
                    ProductTypes type;

                    if (recognizedModels.TryGetValue(itemName, out type))
                        return type == ProductTypes.MattressModel || type == ProductTypes.ProductSet;
                }

                return ContainsAny(itemName, queryAllMattressNames);
            }

            return false;
        }

        /// <summary>
        /// Determines whether an item is a framework model or not.
        /// </summary>
        /// <param name="itemName">The case-sensitive name of the product to be identified.</param>
        /// <returns>True if framework model, false otherwise.</returns>
        public static bool IsFrameworkModel(string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
                return false;

            if (itemName.ToLower().Contains("lattenrost") && !itemName.StartsWith("Set:"))
                return true;

            return false;
        }

        /// <summary>
        /// Determines whether an item is a pillow model or not.
        /// </summary>
        /// <param name="itemName">The case-sensitive name of the product to be identified.</param>
        /// <returns>True if pillow model, false otherwise.</returns>
        public static bool IsPillowModel(string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
                return false;

            if (itemName.ToLower().Contains("kissen") && !itemName.StartsWith("Set:") && !itemName.Contains("keil") && !itemName.Contains("platte"))
                return true;

            return false;
        }

        #endregion

        #region ContainsAny / NotContainsAny methods

        /// <summary>
        /// Checks whether the supplied string contains any of the strings in the list.
        /// </summary>
        /// <param name="item">The string to be checked.</param>
        /// <param name="whiteList">The list with the strings which might be contained in the input string.</param>
        /// <returns>True if the input string contains at least one of the strings in the list, false otherwise.</returns>
        public static bool ContainsAny(string item, List<string> whiteList)
        {
            if (string.IsNullOrEmpty(item) || whiteList == null)
                return false;

            item = item.ToLower();

            foreach (string s in whiteList)
                if (item.Contains(s))
                    return true;

            return false;
        }


        /// <summary>
        /// Checks whether the supplied string contains any of the strings in the list.
        /// </summary>
        /// <param name="item">The string to be checked.</param>
        /// <param name="whiteList">The list with the strings which might be contained in the input string.</param>
        /// <returns>False if the input string contains at least one of the strings in the list, true otherwise.</returns>
        public static bool NotContainsAny(string item, List<string> blackList)
        {
            if (string.IsNullOrEmpty(item) || blackList == null)
                return true;

            item = item.ToLower();

            foreach (string s in blackList)
                if (item.Contains(s))
                    return false;

            return true;
        }
        #endregion
    }
}
