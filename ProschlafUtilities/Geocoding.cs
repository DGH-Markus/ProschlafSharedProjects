using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProschlafUtils
{
    /// <summary>
    /// This class used the Reimer.Core and Reimers.Google.Maps libs to provide geocoding different functionalities.
    /// </summary>
    public static class Geocoding
    {
        /// <summary>
        /// Returns the distance between two given GeoCoordinates in kilometers using the "Haversine" formula.
        /// </summary>
        /// <param name="pos1"></param>
        /// <param name="pos2"></param>
        /// <returns></returns>
        public static double GetDistanceInKM(System.Device.Location.GeoCoordinate pos1, System.Device.Location.GeoCoordinate pos2)
        {
            return pos1.GetDistanceTo(pos2) / 1000;
        }

        
    }
}