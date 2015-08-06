using System.Collections.Generic;
using System.Xml.Serialization;

namespace Kentico.ADImportService
{    
    /// <summary>
    /// Configuration class for whole service.
    /// </summary>
    public class ServiceConfiguration
    {
        /// <summary>
        /// Configuration for AD listeners.
        /// </summary>
        [XmlElement]
        public ListenerConfiguration Listener
        {
            get;
            set;
        }


        /// <summary>
        /// Configuration for REST provider.
        /// </summary>
        [XmlElement]
        public RestConfiguration Rest
        {
            get;
            set;
        }


        /// <summary>
        /// Bindings of user LDAP attributes to Kentico attributes.
        /// </summary>
        [XmlArray(ElementName = "UserAttributesBindings")]
        [XmlArrayItem(ElementName = "Binding")]
        public List<AttributeBinding> UserAttributesBindings
        {
            get;
            set;
        }


        /// <summary>
        /// Bindings of role LDAP attributes to Kentico attributes.
        /// </summary>
        [XmlArray(ElementName = "GroupAttributesBindings")]
        [XmlArrayItem(ElementName = "Binding")]
        public List<AttributeBinding> GroupAttributesBindings
        {
            get;
            set;
        }
    }
}
