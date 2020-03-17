using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProschlafUtils
{
    /// <summary>
    /// Defines contants concerning various product lines by DGH.
    /// </summary>
    public static class ProductConstants
    {
        /// <summary>
        /// The length of the Vitario support profiles used in this application.
        /// Changes to this constant must also be reflected in ISAP.BusinessObjects.BusinessConstants.
        /// </summary>
        public const int PROFILE_LENGTH_VITARIO = 24;

        /// <summary>
        /// The length of the Orthonic support profiles used in this application.
        /// </summary>
        public const int PROFILE_LENGTH_ORTHONIC = 16;

        /// <summary>
        /// The length of the old Spring support profiles used in this application.
        /// </summary>
        public const int PROFILE_LENGTH_SPRING_OLD = 7;

        /// <summary>
        /// The length of the current Spring support profiles used in this application.
        /// Note that the profile consists of it least 10 digits, but may also be longer due to module "10" which has 2 digits and can occur multiple times.
        /// </summary>
        public const int PROFILE_LENGTH_SPRING_CURRENT_MINIMUM = 10;

        /// <summary>
        /// The length of the Vitario Serie Natur support profiles used in this application.
        /// </summary>
        public const int PROFILE_LENGTH_SERIE_N = 11;

        /// <summary>
        /// The length of the Vitario line support profiles used in this application.
        /// </summary>
        public const int PROFILE_LENGTH_VITARIO_LINE = 18;
        public const int PROFILE_LENGTH_VITARIO_LINE_2018 = 20; //Vitario Line was changed to have 20 roles as of 2018

        public const int PROFILE_LENGTH_VITARIO_BASE = 18; //same as the original Vitario Line

        /// <summary>
        /// The length of the Ergo 4 support profiles used in this application. Example: EGBB
        /// </summary>
        public const int PROFILE_LENGTH_ERGO_4 = 4;

        /// <summary>
        /// The length of the Ergo 4 support profiles including the shoulder roles used in this application. Example: EGBB-01011
        /// </summary>
        public const int PROFILE_LENGTH_ERGO_4_EXTENDED = 10;

        /// <summary>
        /// The length of the MLine (with stamps) support profiles used in this application.
        /// </summary>
        public const int PROFILE_LENGTH_MLINE_STAMPS = 24;

        /// <summary>
        /// The length of the MLine (with roles) support profiles used in this application.
        /// </summary>
        public const int PROFILE_LENGTH_MLINE_ROLES = 24;

    }
}
