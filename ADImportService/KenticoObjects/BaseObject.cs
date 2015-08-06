using System.Xml.Serialization;

namespace Kentico.ADImportService
{
    /// <summary>
    /// Kentico object general representation.
    /// </summary>
    public class BaseObject
    {
        /// <summary>
        /// Name of tag of objects ID in REST response.
        /// </summary>
        [XmlIgnore]
        public virtual string IdTagName
        {
            get
            {
                return "ObjectID";
            }
        }


        /// <summary>
        /// Object ID.
        /// </summary>
        [XmlAttribute(AttributeName = "ID")]
        public long Id
        {
            get;
            set;
        }
    }
}
