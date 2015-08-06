using System.Text;
using System.Xml.Serialization;

namespace Kentico.ADImportService
{

    /// <summary>
    /// Kentico user-role binding representation.
    /// </summary>
    public class UserRoleBinding : BaseObject
    {
        /// <summary>
        /// Name of tag of objects ID in REST response.
        /// </summary>
        [XmlIgnore]
        public override string IdTagName
        {
            get
            {
                return "UserRoleID";
            }
        }


        /// <summary>
        /// Id of the bound user.
        /// </summary>
        [XmlElement(ElementName = "User")]
        public long UserId
        {
            get;
            set;
        }


        /// <summary>
        /// Id of the bound role.
        /// </summary>
        [XmlElement(ElementName = "Role")]
        public long RoleId
        {
            get;
            set;
        }


        /// <summary>
        /// Basic constructor.
        /// </summary>
        public UserRoleBinding()
        {
        }


        /// <summary>
        /// Basic constructor for CMS user-role binding.
        /// </summary>
        public UserRoleBinding(long userId, long roleId)
        {
            UserId = userId;
            RoleId = roleId;
        }


        /// <summary>
        /// Return REST representation of CMS user.
        /// </summary>
        public override string ToString()
        {
            var buider = new StringBuilder();

            buider.AppendLine("<data>");
            buider.AppendLine("  <CMS_UserRole>");
            buider.AppendLine("    <UserID>" + UserId + "</UserID>");
            buider.AppendLine("    <RoleID>" + RoleId + "</RoleID>");
            buider.AppendLine("  </CMS_UserRole>");
            buider.AppendLine("</data>");

            return buider.ToString();
        }
    }
}
