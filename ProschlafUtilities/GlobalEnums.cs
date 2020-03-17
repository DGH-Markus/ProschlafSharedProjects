using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProschlafUtils
{
    public static class GlobalEnums
    {
        public enum ProschlafApplications { Unknown = 0, Liegesimulator2_0 = 1, Orthonometer = 2, Ergonometer_NL = 3, Liegesimulator = 4 } //this enum should correspond to IsapJsonApiAccess.JsonConstants.JsonProschlafApplications
    }
}
