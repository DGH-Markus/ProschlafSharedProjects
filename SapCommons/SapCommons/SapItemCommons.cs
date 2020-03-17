using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SapCommons
{
    public static class SapItemCommons
    {
        #region Enums
        /// <summary>
        /// The current SAP mattress model categories that can be processed.
        /// Do not change the numbers of existing enum values or the saving system in LS 2.0 will break.
        /// </summary>
        public enum SAPModelCategories { Unknown = 0, Vitario_Spring = 1, Vitario_Spring_Premium = 2, Vitario_Premium = 3, Vitario_Premium_Sensitiv = 4, Vitario_Comfort = 5, Vitario_Premium_XD_NED = 6, Vitario_Sensitiv = 7, Vitario_Serie_Natur = 8, Vitario_Line = 9, Vitario_Line_Sensitiv = 10, Ergodeluxe_350 = 11, Ergodeluxe_450 = 12, Ergodeluxe_450_Sensitiv = 13, Ergodeluxe_500 = 14, Ergo4 = 15, Die_Variable = 16, SchulterConception = 17, ErgomenWomen = 18 }

        /// <summary>
        /// The current SAP slatted frame categories that can be processed.
        /// Do not change the numbers of existing enum values or the saving system in LS 2.0 will break.
        /// </summary>
        public enum SAPSlattedFrameCategories { Unknown = 0, Vitario_Flex_NV = 1, Vitario_Flex_KF = 2, Vitario_Flex_Motor = 3, Vitario_Flex_4_Motor = 4, Physio_Flex_NV = 5, Physio_Flex_KF = 6, Basic_NV = 7, Basic_KF = 8, Basic_Motor = 9, Stabilo_NV = 10, Stabilo_KF, Stabilo_Motor = 11, Stabilo_Gasdruck = 12, Serie_Natur_NV = 13, Serie_Natur_KF = 14, Serie_Natur_Motor = 15, Eco_Move_Motor, Effekt_NV = 16, Effekt_KF = 17, Happy2_KF = 18, Happy2_Motor = 19, Kreation_KFS = 20, Kreation_NV = 21, Kreation_Motor = 22, Monoflex_NV = 23, Monoflex_KF, Rekord_NV = 24 }

        public enum SAPMattressSetupTypes { NotSet, OneCore_1OS_1ÜZ, OneCore_2OS_1ÜZ, TwoCores_1OS_1ÜZ, TwoCores_2OS_1ÜZ }

        public enum SAPToppingCategories { Unknown, Topping_7cm, Topping_5cm, Topping_2_5cm, Topping_7_Spring, Topping_7_XD, Topping_7_Neu, Topping_4_Ice, Topping_9_5_Ice }

        public enum SapGenders { m, w } //the 2 common values that are usually entered manually in SAP
        #endregion

        #region Consts
        //These constants hold the exact category names as defined and used by SAP in the article master data
        public const string CATEGORY_VITARIO_PREMIUM = "Vitario_Premium";
        public const string CATEGORY_VITARIO_PREMIUM_SENSITIV = "Vitario_Premium_Sensitiv";
        public const string CATEGORY_VITARIO_COMFORT = "Vitario_Comfort";
        public const string CATEGORY_VITARIO_SENSITIV = "Vitario_Sensitiv";
        public const string CATEGORY_VITARIO_PREMIUM_XD_NED = "Vitario Premium XD NED";
        public const string CATEGORY_VITARIO_LINE = "Vitario Line";
        public const string CATEGORY_VITARIO_LINE_SENSITIV = "VItario Line Sensitiv";
        public const string CATEGORY_VITARIO_SPRING = "Vitario Spring";
        public const string CATEGORY_VITARIO_SPRING_PREMIUM = "Vitario Spring Premium";
        public const string CATEGORY_VITARIO_SERIE_NATUR = "Vitario Serie Natur";
        public const string CATEGORY_ERGODELUXE_350 = "Ergodeluxe 350";
        public const string CATEGORY_ERGODELUXE_450 = "Ergodeluxe 450";
        public const string CATEGORY_ERGODELUXE_450_SENSITIV = "Ergodeluxe 450 Sensitiv";
        public const string CATEGORY_ERGODELUXE_500 = "Ergodeluxe 500";

        public const string CATEGORY_DIE_VARIABLE = "Die Variable";
        public const string CATEGORY_SCHULTER_CONCEPTION = "Schulter Conception";
        public const string CATEGORY_ERGO_4 = "Ergo 4";

        public const string CATEGORY_TRI_MED = "TriMed";
        public const string CATEGORY_ERGO_MEN_WOMEN = "Ergo Men/Women";

        public const string CATEGORY_PILLOW_VITARIO = "Vitario Kissen";
        public const string CATEGORY_PILLOW_VITARIO_WITH_CARTON = "Vitario Kissen Kartonverpackung";

        public const string GENERIC_MATTRESS_FIRMNESS_H1 = "H1";
        public const string GENERIC_MATTRESS_FIRMNESS_H2 = "H2";
        public const string GENERIC_MATTRESS_FIRMNESS_H3 = "H3";
        public const string GENERIC_MATTRESS_FIRMNESS_H4 = "H4";

        public const string FRAME_NOT_SET = "Keine Auswahl";
        public const string FRAME_VITARIO_FLEX_NV = "Vitario Flex NV";
        public const string FRAME_VITARIO_FLEX_KF = "Vitario Flex KF";
        public const string FRAME_VITARIO_FLEX_MOTOR = "Vitario Flex Motor";
        public const string FRAME_VITARIO_FLEX_4_MOTOR = "Vitario Flex 4-Motor";
        public const string FRAME_PHYSIO_FLEX_NV = "Physio Flex NV";
        public const string FRAME_PHYSIO_FLEX_KF = "Physio Flex KF";
        public const string FRAME_PHYSIO_MOTOR = "Physio Motor";
        public const string FRAME_PHYSIO_FLEX_4_MOTOR = "Physio Flex 4-Motor";
        public const string FRAME_BASIC_NV = "Basic NV";
        public const string FRAME_BASIC_KF = "Basic KF";
        public const string FRAME_BASIC_MOTOR = "Basic Motor";
        public const string FRAME_STABILO_NV = "Stabilo NV";
        public const string FRAME_STABILO_KF = "Stabilo KF";
        public const string FRAME_STABILO_KF_GASDRUCK = "Stabilo KF + Gasdruck";
        public const string FRAME_STABILO_MOTOR = "Stabilo Motor";
        public const string FRAME_SERIE_NATUR_KF = "Serie Natur KF";
        public const string FRAME_SERIE_NATUR_NV = "Serie Natur NV";
        public const string FRAME_SERIE_NATUR_MOTOR = "Serie Natur Motor";
        public const string FRAME_ECO_MOVE_MOTOR = "EcoMove Motorflachrahmen";
        public const string FRAME_EFFEKT_KF = "Effekt KF";
        public const string FRAME_EFFEKT_NV = "Effekt NV";
        public const string FRAME_HAPPY_2_KF = "Happy 2 KF";
        public const string FRAME_HAPPY_2_MOTOR = "Happy 2 Motor";
        public const string FRAME_KREATION_KFS = "Kreation KFS";
        public const string FRAME_KREATION_NV = "Kreation NV";
        public const string FRAME_KREATION_MOTOR = "Kreation Motor";
        public const string FRAME_MONOFLEX_KF = "Monoflex KF";
        public const string FRAME_MONOFLEX_NV = "Monoflex NV";
        public const string FRAME_REKORD_NV = "Rekord NV";

        public const string PILLOW_VITARIO = "Vitario Kissen";
        public const string PILLOW_VITARIO_KARTONVRPACKUNG = "Vitario Kissen Kartonverpackung";
        public const string PILLOW_ORTHONIC = "Orthonic Kissen";

        public const string PILLOW_FIRMNESS_SOFT = "weich";
        public const string PILLOW_FIRMNESS_FIRM = "fest";

        public const string TOPPING_7_XD = "7 XD";
        public const string TOPPING_4_ICE = "4 Ice";
        public const string TOPPING_9_5_ICE = "9,5 Gel Ice";
        public const string TOPPING_7_SPRING = "7 Spring";
        public const string TOPPING_2_5_CM = "2,5cm";
        public const string TOPPING_5_CM = "5cm";
        public const string TOPPING_7_CM = "7cm";
        public const string TOPPING_7_NEU = "7 neu";
        public const string TOPPING_NONE = "Keine Auswahl";

        public const string BEDCOVER_KASCHMIR_KASTEN = "Kaschmir Kasten";
        public const string BEDCOVER_KASCHMIR_SIMULATOR = "Kaschmir Simulator";
        public const string BEDCOVER_TENCEL_STANDARD = "Tencel Standard";
        public const string BEDCOVER_TENCEL_KASTEN = "Tencel Kasten";
        public const string BEDCOVER_PREMIUM_KASTEN = "Premium Kasten";
        public const string BEDCOVER_PREMIUM_KASTEN_21 = "Premium Kasten 21";
        public const string BEDCOVER_LUREX_SIMULATOR = "Lurex Simulator";
        public const string BEDCOVER_VARIABLE_MÖBELSTOFF = "Variable Möbelstoff";
        public const string BEDCOVER_VITARIO_NATUR = "Vitario Natur";
        public const string BEDCOVER_MZE_KASTEN = "MZE Kasten";
        public const string BEDCOVER_SMT_2013_BISE = "SMT 2013 Bise";
        public const string BEDCOVER_NONE = "Keine Auswahl";

        //These constants hold the exact values as defined and used by SAP
        public const string SETUP_TYPE_NOT_SET = "Keine_Auswahl";
        public const string SETUP_TYPE_1K_1OS_1ÜZ = "1K–1OS–1ÜZ";
        public const string SETUP_TYPE_1K_2OS_1ÜZ = "1K–2OS–1ÜZ";
        public const string SETUP_TYPE_2K_1OS_1ÜZ = "2K–1OS–1ÜZ";
        public const string SETUP_TYPE_2K_2OS_1ÜZ = "2K–2OS–1ÜZ";

        public const string DUAL_MATTRESS_TYPE_NONE = "Nein";

        public const string ITEM_CODE_DUAL_MATTRESS = "V104018";
        public const string ITEM_CODE_SUPPORT_SERVICE_6_MONTHS = "V101961"; //Betreuung 6 Monate
        public const string ITEM_CODE_SUPPORT_SERVICE_3_YEARS = "V102576"; //Betreuung	3 Jahre
        public const string ITEM_CODE_SUPPORT_SERVICE_5_YEARS = "V103995"; //Betreuung	5 Jahre
        public const string ITEM_CODE_SUPPORT_SERVICE_10_YEARS = "V101629"; //Betreuung	10 Jahre
        public const string ITEM_CODE_NAME_AND_QUANTITY_SEE_BELOW = "V1001216"; //Artikelbezeichnung + Menge siehe unten
        public const string ITEM_CODE_SHIPPING_FLAT_CHARGE = "V101578"; //Zustellpauschale
        public const string ITEM_NAME_SHIPPING_FLAT_CHARGE = "Zustellpauschale";
        public const string ITEM_CODE_SPECIAL_ITEM = "V108630"; //"ACHTUNG Sonderartikel"

        public const string SAP_WAREHOUSE_INTERNAL = "01";
        public const string SAP_WAREHOUSE_EXTERNAL_JCL = "L 3";

        public const int SAP_TRUCKING_COMPANY_CODE_DPD_AT = 7;
        public const int SAP_TRUCKING_COMPANY_CODE_DPD_INT = 8;
        public const int SAP_TRUCKING_COMPANY_CODE_ZUSTELLUNG_VERTRETER = 13;
        public const int SAP_TRUCKING_COMPANY_CODE_JCL_EXTERN = 19;
        #endregion

        #region Vars
        private static Dictionary<string, SAPModelCategories> mattressModelDictionary = new Dictionary<string, SAPModelCategories>();
        private static Dictionary<string, SAPSlattedFrameCategories> slattedFrameModelDictionary = new Dictionary<string, SAPSlattedFrameCategories>();
        private static Dictionary<string, SAPMattressSetupTypes> setupTypeDictionary = new Dictionary<string, SAPMattressSetupTypes>();
        #endregion

        static SapItemCommons()
        {
            //build a translation structure from the SAP model category name to the corresponding ISAP enum (all new models must be added here)
            mattressModelDictionary.Add(CATEGORY_VITARIO_COMFORT, SAPModelCategories.Vitario_Comfort);
            mattressModelDictionary.Add(CATEGORY_VITARIO_SENSITIV, SAPModelCategories.Vitario_Sensitiv);
            mattressModelDictionary.Add(CATEGORY_VITARIO_PREMIUM_XD_NED, SAPModelCategories.Vitario_Premium_XD_NED);
            mattressModelDictionary.Add(CATEGORY_VITARIO_PREMIUM, SAPModelCategories.Vitario_Premium);
            mattressModelDictionary.Add(CATEGORY_VITARIO_PREMIUM_SENSITIV, SAPModelCategories.Vitario_Premium_Sensitiv);
            mattressModelDictionary.Add(CATEGORY_VITARIO_LINE, SAPModelCategories.Vitario_Line);
            mattressModelDictionary.Add(CATEGORY_VITARIO_LINE_SENSITIV, SAPModelCategories.Vitario_Line_Sensitiv);
            mattressModelDictionary.Add(CATEGORY_VITARIO_SPRING, SAPModelCategories.Vitario_Spring);
            mattressModelDictionary.Add(CATEGORY_VITARIO_SPRING_PREMIUM, SAPModelCategories.Vitario_Spring_Premium);
            mattressModelDictionary.Add(CATEGORY_VITARIO_SERIE_NATUR, SAPModelCategories.Vitario_Serie_Natur);
            mattressModelDictionary.Add(CATEGORY_ERGODELUXE_350, SAPModelCategories.Ergodeluxe_350);
            mattressModelDictionary.Add(CATEGORY_ERGODELUXE_450, SAPModelCategories.Ergodeluxe_450);
            mattressModelDictionary.Add(CATEGORY_ERGODELUXE_450_SENSITIV, SAPModelCategories.Ergodeluxe_450_Sensitiv);
            mattressModelDictionary.Add(CATEGORY_ERGODELUXE_500, SAPModelCategories.Ergodeluxe_500);

            mattressModelDictionary.Add(CATEGORY_DIE_VARIABLE, SAPModelCategories.Die_Variable);
            mattressModelDictionary.Add(CATEGORY_SCHULTER_CONCEPTION, SAPModelCategories.SchulterConception);
            mattressModelDictionary.Add(CATEGORY_ERGO_4, SAPModelCategories.Ergo4);
            mattressModelDictionary.Add(CATEGORY_ERGO_MEN_WOMEN, SAPModelCategories.ErgomenWomen);

            slattedFrameModelDictionary.Add(FRAME_VITARIO_FLEX_NV, SAPSlattedFrameCategories.Vitario_Flex_NV);
            slattedFrameModelDictionary.Add(FRAME_VITARIO_FLEX_KF, SAPSlattedFrameCategories.Vitario_Flex_KF);
            slattedFrameModelDictionary.Add(FRAME_VITARIO_FLEX_MOTOR, SAPSlattedFrameCategories.Vitario_Flex_Motor);
            slattedFrameModelDictionary.Add(FRAME_VITARIO_FLEX_4_MOTOR, SAPSlattedFrameCategories.Vitario_Flex_4_Motor);
            slattedFrameModelDictionary.Add(FRAME_PHYSIO_FLEX_KF, SAPSlattedFrameCategories.Physio_Flex_KF);
            slattedFrameModelDictionary.Add(FRAME_PHYSIO_FLEX_NV, SAPSlattedFrameCategories.Physio_Flex_NV);
            slattedFrameModelDictionary.Add(FRAME_BASIC_KF, SAPSlattedFrameCategories.Basic_KF);
            slattedFrameModelDictionary.Add(FRAME_BASIC_NV, SAPSlattedFrameCategories.Basic_NV);
            slattedFrameModelDictionary.Add(FRAME_BASIC_MOTOR, SAPSlattedFrameCategories.Basic_Motor);
            slattedFrameModelDictionary.Add(FRAME_STABILO_KF, SAPSlattedFrameCategories.Stabilo_KF);
            slattedFrameModelDictionary.Add(FRAME_STABILO_NV, SAPSlattedFrameCategories.Stabilo_NV);
            slattedFrameModelDictionary.Add(FRAME_STABILO_MOTOR, SAPSlattedFrameCategories.Stabilo_Motor);
            slattedFrameModelDictionary.Add(FRAME_STABILO_KF_GASDRUCK, SAPSlattedFrameCategories.Stabilo_Gasdruck);
            slattedFrameModelDictionary.Add(FRAME_SERIE_NATUR_KF, SAPSlattedFrameCategories.Serie_Natur_KF);
            slattedFrameModelDictionary.Add(FRAME_SERIE_NATUR_NV, SAPSlattedFrameCategories.Serie_Natur_NV);
            slattedFrameModelDictionary.Add(FRAME_SERIE_NATUR_MOTOR, SAPSlattedFrameCategories.Serie_Natur_Motor);
            slattedFrameModelDictionary.Add(FRAME_ECO_MOVE_MOTOR, SAPSlattedFrameCategories.Eco_Move_Motor);
            slattedFrameModelDictionary.Add(FRAME_EFFEKT_KF, SAPSlattedFrameCategories.Effekt_KF);
            slattedFrameModelDictionary.Add(FRAME_EFFEKT_NV, SAPSlattedFrameCategories.Effekt_NV);
            slattedFrameModelDictionary.Add(FRAME_HAPPY_2_KF, SAPSlattedFrameCategories.Happy2_KF);
            slattedFrameModelDictionary.Add(FRAME_HAPPY_2_MOTOR, SAPSlattedFrameCategories.Happy2_Motor);
            slattedFrameModelDictionary.Add(FRAME_KREATION_KFS, SAPSlattedFrameCategories.Kreation_KFS);
            slattedFrameModelDictionary.Add(FRAME_KREATION_NV, SAPSlattedFrameCategories.Kreation_NV);
            slattedFrameModelDictionary.Add(FRAME_KREATION_MOTOR, SAPSlattedFrameCategories.Kreation_Motor);
            slattedFrameModelDictionary.Add(FRAME_MONOFLEX_KF, SAPSlattedFrameCategories.Monoflex_KF);
            slattedFrameModelDictionary.Add(FRAME_MONOFLEX_NV, SAPSlattedFrameCategories.Monoflex_NV);
            slattedFrameModelDictionary.Add(FRAME_REKORD_NV, SAPSlattedFrameCategories.Rekord_NV);

            setupTypeDictionary.Add(SETUP_TYPE_NOT_SET, SAPMattressSetupTypes.NotSet);
            setupTypeDictionary.Add(SETUP_TYPE_1K_1OS_1ÜZ, SAPMattressSetupTypes.OneCore_1OS_1ÜZ);
            setupTypeDictionary.Add(SETUP_TYPE_1K_2OS_1ÜZ, SAPMattressSetupTypes.OneCore_2OS_1ÜZ);
            setupTypeDictionary.Add(SETUP_TYPE_2K_1OS_1ÜZ, SAPMattressSetupTypes.TwoCores_1OS_1ÜZ);
            setupTypeDictionary.Add(SETUP_TYPE_2K_2OS_1ÜZ, SAPMattressSetupTypes.TwoCores_2OS_1ÜZ);
        }

        public static SAPModelCategories GetMattressCategory(string category)
        {
            if (string.IsNullOrEmpty(category))
                return SAPModelCategories.Unknown;

            if (mattressModelDictionary.ContainsKey(category))
                return mattressModelDictionary[category];
            else
                return SAPModelCategories.Unknown;
        }

        public static SAPSlattedFrameCategories GetSlattedFrameCategory(string slattedFrameCategory)
        {
            if (string.IsNullOrEmpty(slattedFrameCategory))
                return SAPSlattedFrameCategories.Unknown;

            if (slattedFrameModelDictionary.ContainsKey(slattedFrameCategory))
                return slattedFrameModelDictionary[slattedFrameCategory];
            else
                return SAPSlattedFrameCategories.Unknown;
        }

        public static string GetSapMattressCategory(SAPModelCategories category)
        {
            if (mattressModelDictionary.ContainsValue(category))
                return mattressModelDictionary.First(t => t.Value == category).Key;
            else
                return null;
        }

        public static string GetSapSlattedFrameCategory(SAPSlattedFrameCategories category)
        {
            if (slattedFrameModelDictionary.ContainsValue(category))
                return slattedFrameModelDictionary.First(t => t.Value == category).Key;
            else
                return null;
        }

        public static SAPMattressSetupTypes GetMattressSetupType(string setup)
        {
            if (string.IsNullOrEmpty(setup))
                return SAPMattressSetupTypes.NotSet;

            if (setupTypeDictionary.ContainsKey(setup))
                return setupTypeDictionary[setup];
            else
                return SAPMattressSetupTypes.NotSet;
        }
    }
}