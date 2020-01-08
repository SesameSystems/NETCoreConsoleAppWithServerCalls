using System;
using System.Xml.Serialization;

namespace NETCoreConsoleAppWithServerCalls
{

    [Serializable]
    public class SPDatabaseDetailsRequest
    {

        [XmlIgnore]
        public string DatabaseId { get; set; }

    }

}
