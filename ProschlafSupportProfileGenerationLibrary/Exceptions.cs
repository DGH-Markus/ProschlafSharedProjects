using System;
using System.Collections.Generic;
using System.Text;

namespace ProschlafSupportProfileGenerationLibrary
{
    public class UnknownProfileElementException : Exception
    {
        public string Letter { get; set; }

        public GenerationConstants.ProfileElements Element { get; set; }
        
        public override string ToString()
        {
            return "Unknown profile element letter '" + Letter + "' for product line: " + Element + Environment.NewLine + base.ToString();
        }
    }
}
