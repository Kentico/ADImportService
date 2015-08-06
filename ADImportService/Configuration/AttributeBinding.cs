using System.Xml.Serialization;

namespace Kentico.ADImportService
{
    /// <summary>
    /// Binding between LDAP and CMS user/role attributes
    /// </summary>
    public class AttributeBinding
    {
        /// <summary>
        /// LDAP part of attribute binding
        /// </summary>
        [XmlAttribute]
        public string Ldap
        {
            get; 
            set;
        }


        /// <summary>
        /// CMS part of attribute binding
        /// </summary>
        [XmlAttribute]
        public string Cms
        {
            get;
            set;
        }
    }
}
