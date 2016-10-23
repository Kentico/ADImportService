using System;
using System.Management.Instrumentation;

namespace Kentico.ADImportService
{
	/// <summary>
	/// Provides basic CMS operations with users and groups.
	/// </summary>
	public class RestSender : ISender
	{
		/// <summary>
		/// REST provider object.
		/// </summary>
		public RestProvider Provider
		{
			get;
		}


		/// <summary>
		/// Base URL used in REST requests.
		/// </summary>
		public string BaseUrl
		{
			get;
		}


		/// <summary>
		/// Logger for tracking listener errors and actions.
		/// </summary>
		public Logger Logger
		{
			get;
		}


		/// <summary>
		/// Fired when error occurrs.
		/// </summary>
		public event Action OnError;


		/// <summary>
		/// Fired when first changes are successfully sent to CMS.
		/// </summary>
		public event Action OnSuccess;


		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <param name="configuration">Configuration used to construct REST provider.</param>
		/// <param name="logger">Logger for logging events and errors.</param>
		public RestSender(RestConfiguration configuration, Logger logger)
		{
			Provider = new RestProvider(configuration.Encoding, configuration.UserName, configuration.Password, configuration.SslCertificateLocation);
			BaseUrl = configuration.BaseUrl;
			Logger = logger;
		}


		/// <summary>
		/// Find out whether user with given GUID and name exists.
		/// </summary>
		/// <param name="guid">Guid of user to check.</param>
		/// <param name="codeName"></param>
		public bool UserExists(Guid guid, string codeName)
		{
			return UserExists(codeName) || UserExists(guid);
		}


		/// <summary>
		/// Find out whether user with given name exists.
		/// </summary>
		/// <param name="codeName"></param>
		public bool UserExists(string codeName)
		{
			if (!string.IsNullOrEmpty(codeName))
			{
				var response = Provider.MakeRequest(BaseUrl + "/rest/cms.user/" + codeName);
				return string.Equals(RestHelper.GetAttributeFromReponse(response, "UserName"), codeName, StringComparison.InvariantCultureIgnoreCase);
			}

			return false;
		}


		/// <summary>
		/// Find out whether user with given GUID exists.
		/// </summary>
		/// <param name="guid">Guid of user to check.</param>
		public bool UserExists(Guid guid)
		{
			if (guid != Guid.Empty)
			{
				var response = Provider.MakeRequest(BaseUrl + "/rest/cms.user/" + guid);
				return string.Equals(RestHelper.GetAttributeFromReponse(response, "UserGUID"), guid.ToString("D"), StringComparison.InvariantCultureIgnoreCase);
			}

			return false;
		}


		/// <summary>
		/// Get user XML representation.
		/// </summary>
		/// <param name="guid">Guid of user to find.</param>
		public string GetUser(Guid guid)
		{
			if (guid != Guid.Empty)
			{
				try
				{
					// Get all users
					return Provider.MakeRequest(BaseUrl + "/rest/cms.user/" + guid);
				}
				catch (InstanceNotFoundException)
				{
					// User doesn't exist
					throw;
				}
				catch (Exception ex)
				{
					LogError("Retrieving user " + guid + " failed.", ex);
				}
			}

			return null;
		}


		/// <summary>
		/// Add user to CMS.
		/// </summary>
		/// <param name="user">User to add</param>
		/// <returns>ID of added user.</returns>
		public long? AddUser(User user)
		{
			if (user != null)
			{
				try
				{
					if (UserExists(user.Guid, user.CodeName))
					{
						LogMessage("User " + user.CodeName + " was not added because it already exists.");
					}
					else
					{
						// Send REST request
						var response = Provider.MakeRequest(BaseUrl + "/rest/cms.user", HttpVerb.Post, null, user.ToString());

						var id = Convert.ToInt64(RestHelper.GetAttributeFromReponse(response, user.IdTagName));

						LogMessage("User " + user.CodeName + " has been added.");

						if (OnSuccess != null)
						{
							OnSuccess();
						}

						return id;
					}
				}
				catch (Exception ex)
				{
					LogError("Adding user " + user.CodeName + " failed.", ex);
				}
			}

			return null;
		}


		/// <summary>
		/// Modify existing user in CMS.
		/// </summary>
		/// <param name="user">Modified user</param>
		/// <returns>ID of modified user.</returns>
		public long? ModifyUser(User user)
		{
			if (user != null)
			{
				try
				{
					if (!UserExists(user.Guid))
					{
						LogMessage("User " + user.CodeName + " was not modified because it doesn't exists.");
					}
					else
					{
						// Send REST request
						var response = Provider.MakeRequest(BaseUrl + "/rest/cms.user/" + user.Guid, HttpVerb.Put,
							null, user.ToString());

						var id = Convert.ToInt64(RestHelper.GetAttributeFromReponse(response, user.IdTagName));

						LogMessage("User " + user.CodeName + " has been modified.");

						if (OnSuccess != null)
						{
							OnSuccess();
						}

						return id;
					}
				}
				catch (Exception ex)
				{
					LogError("Modifying user " + user.CodeName + " failed.", ex);
				}
			}

			return null;
		}


		/// <summary>
		/// Remove user from CMS.
		/// </summary>
		/// <param name="user">User to delete.</param>
		public void RemoveUser(User user)
		{
			if (user != null)
			{
				try
				{
					// Send REST request
					Provider.MakeRequest(BaseUrl + "/rest/cms.user/" + user.Guid.ToString("D"), HttpVerb.Delete);

					LogMessage("User " + user.CodeName + " has been removed.");

					if (OnSuccess != null)
					{
						OnSuccess();
					}
				}
				catch (Exception ex)
				{
					LogError("Removing user " + user.CodeName + " failed.", ex);
				}
			}
		}


		/// <summary>
		/// Find out whether role with given GUID exist.
		/// </summary>
		/// <param name="guid">Guid of role to check</param>
		public bool RoleExists(Guid guid)
		{
			try
			{
				var response = Provider.MakeRequest(BaseUrl + "/rest/cms.role/" + guid);

				return (!string.IsNullOrEmpty(response));
			}
			catch (InstanceNotFoundException)
			{
				// Role doesn't exist
			}
			catch (Exception ex)
			{
				LogError("Retrieving role " + guid + " failed.", ex);
			}

			return false;
		}


		/// <summary>
		/// Get XML representation of given role.
		/// </summary>
		/// <param name="id">Id of role to find</param>
		public string GetRole(long id)
		{
			try
			{
				return Provider.MakeRequest(BaseUrl + "/rest/cms.role/" + id);
			}
			catch (InstanceNotFoundException)
			{
				// Role doesn't exist
				throw;
			}
			catch (Exception ex)
			{
				LogError("Retrieving role " + id + " failed.", ex);
			}

			return null;
		}


		/// <summary>
		/// Find out whether role with given ID exist.
		/// </summary>
		/// <param name="id">Id of role to check</param>
		public bool RoleExists(long id)
		{
			try
			{
				return (!string.IsNullOrEmpty(GetRole(id)));
			}
			catch (InstanceNotFoundException)
			{
				// Role doesn't exist
			}
			catch (Exception ex)
			{
				LogError("Retrieving role " + id + " failed.", ex);
			}

			return false;
		}


		/// <summary>
		/// Add role to CMS.
		/// </summary>
		/// <param name="role">Role to add</param>
		/// <returns>ID of added role</returns>
		public long? AddRole(Role role)
		{
			if (role != null)
			{
				try
				{
					if (RoleExists(role.Id))
					{
						LogMessage("Role " + role.CodeName + " was not added because it already exists.");
					}
					else
					{
						// Send REST request
						var response = Provider.MakeRequest(BaseUrl + "/rest/cms.role", HttpVerb.Post, null,
							role.ToString());

						var id = Convert.ToInt64(RestHelper.GetAttributeFromReponse(response, role.IdTagName));

						LogMessage("Role " + role.CodeName + " has been added.");

						if (OnSuccess != null)
						{
							OnSuccess();
						}

						return id;
					}

				}
				catch (Exception ex)
				{
					LogError("Adding role " + role.CodeName + " failed.", ex);
				}

			}

			return null;
		}


		/// <summary>
		/// Modify existing role.
		/// </summary>
		/// <param name="role">Role to modify</param>
		public long? ModifyRole(Role role)
		{
			if (role != null)
			{
				try
				{
					if (!RoleExists(role.Id))
					{
						LogMessage("Role " + role.CodeName + " was not modified because it doesn't exists.");
					}
					else
					{
						// Send REST request
						var response = Provider.MakeRequest(BaseUrl + "/rest/cms.role/" + role.Id, HttpVerb.Put,
							null, role.ToString());

						var newId = Convert.ToInt64(RestHelper.GetAttributeFromReponse(response, role.IdTagName));

						LogMessage("Role " + role.CodeName + " has been modified.");

						if (OnSuccess != null)
						{
							OnSuccess();
						}

						return newId;
					}
				}
				catch (Exception ex)
				{
					LogError("Modifying role " + role.CodeName + " failed.", ex);
				}
			}

			return null;
		}


		/// <summary>
		/// Remove role from CMS.
		/// </summary>
		/// <param name="role">Role to delete.</param>
		public void RemoveRole(Role role)
		{
			if (role != null)
			{
				try
				{
					// Send REST request
					Provider.MakeRequest(BaseUrl + "/rest/cms.role/" + role.Guid.ToString("D"), HttpVerb.Delete);

					LogMessage("Role " + role.CodeName + " has been removed.");

					if (OnSuccess != null)
					{
						OnSuccess();
					}
				}
				catch (Exception ex)
				{
					LogError("Deleting role " + role.CodeName + " failed.", ex);
				}
			}
		}


		/// <summary>
		/// Create user-role relationship.
		/// </summary>
		/// <param name="userId">User to add to role</param>
		/// <param name="roleId">Role to assign</param>
		/// <returns>ID of added user-role relationship</returns>
		public long? AddUserToRole(long userId, long roleId)
		{
			if ((userId > 0) && (roleId > 0))
			{
				try
				{
					var userRole = new UserRoleBinding { UserId = userId, RoleId = roleId };

					// Send REST request
					var response = Provider.MakeRequest(BaseUrl + "/rest/cms.userrole", HttpVerb.Post, null, userRole.ToString());
					var id = Convert.ToInt64(RestHelper.GetAttributeFromReponse(response, userRole.IdTagName));

					LogMessage("User " + userId + " has been added to role " + roleId + ".");

					if (OnSuccess != null)
					{
						OnSuccess();
					}

					return id;

				}
				catch (Exception ex)
				{
					LogError("Adding user " + userId + " to role " + roleId + " failed.", ex);
				}
			}

			return null;
		}


		/// <summary>
		/// Remove user-role relationship.
		/// </summary>
		/// <param name="id">User-role memberhip id.</param>
		public void RemoveUserFromRole(long id)
		{
			if (id > 0)
			{
				try
				{
					// Send REST request
					Provider.MakeRequest(BaseUrl + "/rest/cms.userrole/" + id, HttpVerb.Delete);

					LogMessage("User membership " + id + " has been removed.");

					if (OnSuccess != null)
					{
						OnSuccess();
					}
				}
				catch (Exception ex)
				{
					LogError("Removing uer-role relationship with id " + id + " failed.", ex);
				}
			}
		}


		private void LogMessage(string message)
		{
			if (Logger != null)
			{
				Logger.LogMessage(message);
			}
		}


		private void LogError(string message, Exception ex = null)
		{
			if (Logger != null)
			{
				Logger.LogError(message + "Check Kentico event log for more details.", ex);
			}

			if (OnError != null)
			{
				OnError();
			}
		}
	}
}
