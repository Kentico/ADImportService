using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Kentico.ADImportService
{
    /// <summary>
    /// Kentico user representation.
    /// </summary>
    public class User : BaseObject
    {
        /// <summary>
        /// Bindings of basic LDAP attributes to Kentico attributes.
        /// </summary>
        public static Dictionary<string, string> InternalBindings = new Dictionary<string, string> 
        {
            {"name", "UserName"},
            {"userAccountControl", "UserEnabled"},
        };


        /// <summary>
        /// Name of tag of objects ID in REST response.
        /// </summary>
        [XmlIgnore]
        public override string IdTagName
        {
            get
            {
                return "UserID";
            }
        }


        /// <summary>
        /// DN of Active Directory corresponding object.
        /// </summary>
        [XmlAttribute(AttributeName = "DN")]
        public string DistinguishedName
        {
            get; 
            set; 
        }

        /// <summary>
        /// Guid string representation.
        /// </summary>
        [XmlAttribute(AttributeName = "Guid")]
        public string GuidString
        {
            get
            {
                return Guid.ToString("D");
            }
            set
            {
                Guid = Guid.Parse(value);
            }
        }


        /// <summary>
        /// User GUID.
        /// </summary>
        [XmlIgnore]
        public Guid Guid
        {
            get;
            private set;
        }


        /// <summary>
        /// User code name.
        /// </summary>
        [XmlIgnore]
        public string CodeName
        {
            get;
            private set;
        }


        /// <summary>
        /// Is user enabled in CMS.
        /// </summary>
        [XmlIgnore]
        public bool Enabled
        {
            get; 
            private set;
        }


        /// <summary>
        /// Custom data.
        /// </summary>
        [XmlIgnore]
        public List<KeyValuePair<string, string>> CustomData
        {
            get;
            private set;
        }


        /// <summary>
        /// Basic constructor.
        /// </summary>
        public User()
        {
        }


        /// <summary>
        /// Basic constructor for CMS user representation.
        /// </summary>
        /// <param name="guid">User GUID</param>
        /// <param name="codeName">User code name</param>
        /// <param name="enabled">Is user enabled.</param>
        /// <param name="customData">Custom data</param>
        public User(Guid guid, string codeName, bool enabled, List<KeyValuePair<string, string>> customData = null)
        {
            Guid = guid;
            CodeName = codeName;
            Enabled = enabled;
            CustomData = customData;
        }


        /// <summary>
        /// Return REST representation of CMS user.
        /// </summary>
        public override string ToString()
        {
            var buider = new StringBuilder();

            buider.AppendLine("<data>");
            buider.AppendLine("  <CMS_User>");
            buider.AppendLine("    <UserGUID>" + Guid + "</UserGUID>");
            buider.AppendLine("    <UserName>" + CodeName + "</UserName>");
            buider.AppendLine("    <UserEnabled>" + Enabled + "</UserEnabled>");

            if (CustomData != null)
            {
                CustomData.ForEach(d => buider.AppendLine(string.Format("    <{0}>{1}</{0}>", d.Key, d.Value ?? string.Empty)));
            }

            buider.AppendLine("  </CMS_User>");
            buider.AppendLine("</data>");

            return buider.ToString();
        }
    }
}
