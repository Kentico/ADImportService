using System.Net;
using System.Xml.Serialization;

namespace Kentico.ADImportService
{
    /// <summary>
    /// Configuration class for listeners.
    /// </summary>
    public class ListenerConfiguration
    {
        /// <summary>
        /// Domain controller domain name or IP address.
        /// </summary>
        [XmlAttribute]
        public string DomainController
        {
            get;
            set;
        }


        /// <summary>
        /// Use SSL to connect to the LDAP server.
        /// </summary>
        [XmlAttribute]
        public bool UseSsl
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


        /// <summary>
        /// Credentials to log on to domain controller.
        /// </summary>
        [XmlElement]
        public NetworkCredential Credentials
        {
            get; 
            set;
        }
    }
}
