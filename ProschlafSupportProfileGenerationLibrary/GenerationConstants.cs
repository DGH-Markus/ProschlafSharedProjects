using System;
using System.Collections.Generic;
using System.Text;

namespace ProschlafSupportProfileGenerationLibrary
{
    public abstract class GenerationConstants
    {
        #region Enums
        /// <summary>
        /// The possible measurement positions as required by the profile generation algorithm.
        /// </summary>
        public enum MeasurementPositions { Supine, Lateral };

        /// <summary>
        /// the possible values for the prefered sleeping position of a test person.
        /// </summary>
        public enum TestpersonSleepPositions { Supine, Lateral, Prone };

        public enum Genders { Male, Female };

        public enum FirmnessLevels { None = 0, H0 = 1, H1 = 2, H2 = 3, H3 = 4, H4 = 5 };

        /// <summary>
        /// A cross-application enum that can be used to represent the profile elements currently being used.
        /// Existing values should not be changed because applications using them might stop working.
        /// </summary>
        public enum ProfileElements { Stamps = 0, Roles = 1, Ergo4Roles = 2, OrthonicBars = 3 }
        #endregion
    }
}
