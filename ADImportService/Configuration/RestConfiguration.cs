using System.Text;
using System.Xml.Serialization;

namespace Kentico.ADImportService
{
    /// <summary>
    /// Configuration class for REST provider.
    /// </summary>
    public class RestConfiguration
    {
        /// <summary>
        /// User name for REST basic authorization.
        /// </summary>
        [XmlAttribute]
        public string UserName
        {
            get; 
            set;
        }


        /// <summary>
        /// Password for REST basic authorization.
        /// </summary>
        [XmlAttribute]
        public string Password
        {
            get;
            set;
        }


        /// <summary>
        /// Encoding of the data sent to the CMS. Must math allowed encoding in REST setting in the CMS.
        /// </summary>
        [XmlIgnore]
        public Encoding Encoding
        {
            get
            {
                return Encoding.GetEncoding(EncodingString ?? "utf-8");
            }
            set
            {
                EncodingString = value.WebName;
            }
        }


        /// <summary>
        /// Encoding of the data sent to the CMS. Must math allowed encoding in REST setting in the CMS.
        /// </summary>
        [XmlAttribute(AttributeName = "Encoding")]
        public string EncodingString
        {
            get; 
            set;
        }


        /// <summary>
        /// Base URL to CMS. Any REST request will be accessed from this base url appending /rest in the URL.
        /// </summary>
        [XmlAttribute]
        public string BaseUrl
        {
            get;
            set;
        }


        /// <summary>
        /// Filesystem absolute path to SSL certificate.
        /// </summary>
        [XmlAttribute]
        public string SslCertificateLocation
        {
            get;
            set;
        }
    }
}
