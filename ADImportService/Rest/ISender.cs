using System;

namespace Kentico.ADImportService
{
    /// <summary>
    /// Provides basic CMS operations with users and groups.
    /// </summary>
    public interface ISender
    {
        /// <summary>
        /// Find out whether user with given GUID exist.
        /// </summary>
        /// <param name="guid">Guid of user to check.</param>
        bool UserExists(Guid guid);


        /// <summary>
        /// Get user XML representation.
        /// </summary>
        /// <param name="guid">Guid of user to find.</param>
        string GetUser(Guid guid);


        /// <summary>
        /// Add user to CMS.
        /// </summary>
        /// <param name="user">User to add</param>
        /// <returns>ID of added user.</returns>
        long? AddUser(User user);


        /// <summary>
        /// Modify existing user in CMS.
        /// </summary>
        /// <param name="user">Modified user</param>
        /// <returns>ID of modified user.</returns>
        long? ModifyUser(User user);


        /// <summary>
        /// Remove user from CMS.
        /// </summary>
        /// <param name="user">User to delete.</param>
        void RemoveUser(User user);


        /// <summary>
        /// Find out whether role with given ID exist.
        /// </summary>
        /// <param name="guid">Guid of role to check</param>
        bool RoleExists(Guid guid);


        /// <summary>
        /// Get XML representation of role.
        /// </summary>
        /// <param name="id">Id of role to find</param>
        string GetRole(long id);


        /// <summary>
        /// Add role to CMS.
        /// </summary>
        /// <param name="role">Role to add</param>
        /// <returns>ID of added role</returns>
        long? AddRole(Role role);


        /// <summary>
        /// Modify existing role.
        /// </summary>
        /// <param name="role">Role to modify</param>
        long? ModifyRole(Role role);


        /// <summary>
        /// Remove role from CMS.
        /// </summary>
        /// <param name="role">Role to delete.</param>
        void RemoveRole(Role role);


        /// <summary>
        /// Create user-role relationship.
        /// </summary>
        /// <param name="userId">User to add to role</param>
        /// <param name="roleId">Role to assign</param>
        /// <returns>ID of added user-role relationship</returns>
        long? AddUserToRole(long userId, long roleId);


        /// <summary>
        /// Remove user-role relationship.
        /// </summary>
        /// <param name="id">User-role memberhip id.</param>
        void RemoveUserFromRole(long id);


        /// <summary>
        /// Fires when error occurs.
        /// </summary>
        event Action OnError;


        /// <summary>
        /// Fires when first successful change is made to CMS.
        /// </summary>
        event Action OnSuccess;
    }
}
