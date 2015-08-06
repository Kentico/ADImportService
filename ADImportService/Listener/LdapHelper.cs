using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Kentico.ADImportService
{
    /// <summary>
    /// Class containing basic operations over LDAP objects
    /// </summary>
    public static class LdapHelper
    {
        /// <summary>
        /// Determines if given LDAP object is user.
        /// </summary>
        /// <param name="entry">LDAP search result entry.</param>
        /// <param name="userCategory">User category Distinguished name.</param>
        public static bool IsUser(SearchResultEntry entry, string userCategory = "CN=Person")
        {
            string attributeToCheck = null;
            string valueToCheck = null;

            if (IsDeleted(entry))
            {
                // Tombsones don't have object category, check object class
                attributeToCheck = GetAttributeString(entry.Attributes["objectClass"]);
                valueToCheck = "user";
            }
            else
            {
                attributeToCheck = GetAttributeString(entry.Attributes["objectCategory"]);
                valueToCheck = userCategory;
            }

            return (!string.IsNullOrEmpty(attributeToCheck) && !string.IsNullOrEmpty(valueToCheck) && attributeToCheck.ToLowerInvariant().Contains(valueToCheck.ToLowerInvariant()));
        }


        /// <summary>
        /// Determines if given LDAP object is group.
        /// </summary>
        /// <param name="entry">LDAP search result entry.</param>
        /// <param name="groupCategory">Group category Distinguished name.</param>
        public static bool IsGroup(SearchResultEntry entry, string groupCategory = "CN=Group")
        {
            string attributeToCheck = null;
            string valueToCheck = null;

            if (IsDeleted(entry))
            {
                // Tombsones don't have object category, check object class
                attributeToCheck = GetAttributeString(entry.Attributes["objectClass"]);
                valueToCheck = "group";
            }
            else
            {
                attributeToCheck = GetAttributeString(entry.Attributes["objectCategory"]);
                valueToCheck = groupCategory;
            }

            return (!string.IsNullOrEmpty(attributeToCheck) && !string.IsNullOrEmpty(valueToCheck) && attributeToCheck.ToLowerInvariant().Contains(valueToCheck.ToLowerInvariant()));
        }


        /// <summary>
        /// Determines whether object has been deleted.
        /// </summary>
        /// <param name="entry">LDAP search result entry.</param>
        public static bool IsDeleted(SearchResultEntry entry)
        {
            return entry.Attributes.Contains("isDeleted") && Convert.ToBoolean(GetAttributeString(entry.Attributes["isDeleted"]));
        }


        /// <summary>
        /// Get LDAP attribute string representation.
        /// </summary>
        /// <param name="attribute">LDAP attribute</param>
        /// <param name="replaceInvalidCharacters">Replace invalid characters in result.</param>
        public static string GetAttributeString(DirectoryAttribute attribute, bool replaceInvalidCharacters = false)
        {
            if (attribute != null)
            {
                var builder = new StringBuilder();

                for (int i = 0; i < attribute.Count; i++)
                {
                    if (attribute[i] is string)
                    {
                        builder.Append((string)attribute[i]);
                    }
                    else if (attribute[i] is byte[])
                    {
                        builder.Append(ToHexString((byte[])attribute[i]));
                    }
                    else
                    {
                        throw new InvalidOperationException("Unexpected type for attribute value: " + attribute[i].GetType().Name);
                    }
                }

                // Replace invalid characters
                return replaceInvalidCharacters ? Regex.Replace(builder.ToString(), "[^\\p{L}0-9_.-]+", "_") : builder.ToString();
            }

            return null;
        }


        /// <summary>
        /// Get members of a group.
        /// </summary>
        /// <param name="entry">Group to get members from.</param>
        public static IEnumerable<string> GetGroupMembers(SearchResultEntry entry)
        {
            if ((entry != null) && IsGroup(entry) && (entry.Attributes.Contains("member")))
            {
                return entry.Attributes["member"].GetValues(typeof(string)).Cast<string>();
            }

            return new List<string>();
        }


        /// <summary>
        /// Get hexadecimal representation of byte array.
        /// </summary>
        /// <param name="bytes">Byte array</param>
        private static string ToHexString(byte[] bytes)
        {
            char[] hexDigits =
            {
                '0', '1', '2', '3', '4', '5', '6', '7',
                '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'
            };

            char[] chars = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                int b = bytes[i];
                chars[i * 2] = hexDigits[b >> 4];
                chars[i * 2 + 1] = hexDigits[b & 0xF];
            }
            return new string(chars);
        }


        /// <summary>
        /// Get LDAP object GUID.
        /// </summary>
        /// <param name="entry">LDAP object</param>
        public static Guid GetObjectGuid(SearchResultEntry entry)
        {
            if ((entry != null) && (entry.Attributes.Contains("objectGUID")))
            {
                return new Guid((byte[])entry.Attributes["objectGUID"][0]);
            }

            return Guid.Empty;
        }


        /// <summary>
        /// Determines whether user is enabled in domain.
        /// </summary>
        /// <param name="entry">User object</param>
        public static bool IsUserEnabled(SearchResultEntry entry)
        {
            if ((entry != null) && entry.Attributes.Contains("userAccountControl"))
            {
                int userAccountControl = Int32.Parse(entry.Attributes["userAccountControl"][0].ToString());

                // Conjunct with disabled flag
                return !Convert.ToBoolean(userAccountControl & 0x0002);
            }

            return false;
        }


        /// <summary>
        /// Get LDAP object's attribute long representation.
        /// </summary>
        /// <param name="entry">LDAP object</param>
        /// <param name="attribute">Attribute to get</param>
        public static long? GetLongAttribute(SearchResultEntry entry, string attribute)
        {
            if ((entry != null) && (entry.Attributes.Contains(attribute)))
            {
                return Convert.ToInt64(GetAttributeString(entry.Attributes[attribute]));
            }

            return null;
        }


        /// <summary>
        /// Get LDAP object's uSNChanged attribute value.
        /// </summary>
        /// <param name="entry">LDAP object</param>
        public static long GetUsnChanged(SearchResultEntry entry)
        {
            return GetLongAttribute(entry, "uSNChanged") ?? 0;
        }
    }
}
