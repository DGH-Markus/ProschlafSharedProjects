using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace  SqsJsonApiAccess.Exceptions
{
    [Serializable]
    public class MissingRequestDataException : Exception
    {
        public string MissingDataFieldName { get; set; }

        public MissingRequestDataException()
        {
        }

        public MissingRequestDataException(string message) : base(message)
        {
        }

        public MissingRequestDataException(string message, Exception inner) : base(message, inner)
        {
        }

        protected MissingRequestDataException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            MissingDataFieldName = info.GetString("MissingDataFieldName");
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");

            info.AddValue("MissingDataFieldName", MissingDataFieldName);
            base.GetObjectData(info, context);
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine + "The following data field is missing: " + MissingDataFieldName;
        }
    }

    [Serializable]
    public class JsonCredentialsMismatchException : Exception
    {
        public JsonCredentialsMismatchException()
        {
        }

        public JsonCredentialsMismatchException(string message) : base(message)
        {
        }

        public JsonCredentialsMismatchException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
