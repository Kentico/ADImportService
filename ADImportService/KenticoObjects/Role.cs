using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Kentico.ADImportService
{
    /// <summary>
    /// Kentico role object representation.
    /// </summary>
    public class Role : BaseObject
    {
        /// <summary>
        /// Bindings of basic LDAP attributes to Kentico attributes.
        /// </summary>
        public static Dictionary<string, string> InternalBindings = new Dictionary<string, string> 
        {
            {"sAMAccountName", "RoleName"},
            {"displayName", "RoleDisplayName"},
        };


        /// <summary>
        /// Name of tag of objects ID in REST response.
        /// </summary>
        [XmlIgnore]
        public override string IdTagName
        {
            get
            {
                return "RoleID";
            }
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
        /// Role GUID.
        /// </summary>
        [XmlIgnore]
        public Guid Guid
        {
            get;
            private set;
        }


        /// <summary>
        /// Role code name.
        /// </summary>
        [XmlIgnore]
        public string CodeName
        {
            get;
            private set;
        }


        /// <summary>
        /// Role display name.
        /// </summary>
        [XmlIgnore]
        public string Name
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
        public Role()
        {
        }


        /// <summary>
        /// Basic constructor for CMS role representation.
        /// </summary>
        /// <param name="guid">Role GUID</param>
        /// <param name="codeName">Role code name</param>
        /// <param name="name">Role display name</param>
        /// <param name="customData">Custom data</param>
        public Role(Guid guid, string codeName, string name, List<KeyValuePair<string, string>> customData = null)
        {
            Guid = guid;
            CodeName = codeName;
            Name = name;
            CustomData = customData;
        }


        /// <summary>
        /// Return REST representation of CMS role.
        /// </summary>
        public override string ToString()
        {
            StringBuilder buider = new StringBuilder();

            buider.AppendLine("<data>");
            buider.AppendLine("  <CMS_Role>");
            buider.AppendLine("    <RoleGUID>" + Guid + "</RoleGUID>");
            buider.AppendLine("    <RoleName>" + CodeName + "</RoleName>");
            buider.AppendLine("    <RoleDisplayName>" + Name + "</RoleDisplayName>");
            
            if (CustomData != null)
            {
                CustomData.ForEach(d => buider.AppendLine(string.Format("    <{0}>{1}</{0}>", d.Key, d.Value ?? string.Empty)));
            }

            buider.AppendLine("  </CMS_Role>");
            buider.AppendLine("</data>");

            return buider.ToString();
        }
    }
}
