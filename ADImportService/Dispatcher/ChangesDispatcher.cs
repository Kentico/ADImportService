using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.DirectoryServices.Protocols;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Kentico.ADImportService
{
	/// <summary>
	/// Class for dispatching changes to Kentico via REST.
	/// </summary>
	public class ChangesDispatcher : IDispatcher, IDisposable
	{
		/// <summary>
		/// Path to minimalistic directory replica.
		/// </summary>
		public const string REPLICA_PATH = @"%PROGRAMDATA%\ADListenerService\replica.xml";

		private object mQueueLock = new object();
		private bool mDispatchAllowed = false;


		/// <summary>
		/// User custom attributes bindings.
		/// </summary>
		public List<AttributeBinding> UserBindings
		{
			get;
			private set;
		}


		/// <summary>
		/// Role custom attributes bindings.
		/// </summary>
		public List<AttributeBinding> GroupBindings
		{
			get;
			private set;
		}


		/// <summary>
		/// Queue of changes to be processed.
		/// </summary>
		private List<SearchResultEntry> Queue
		{
			get;
			set;
		}


		/// <summary>
		/// Dispatch is allowed, dispatcher can send changes as soon as possible.
		/// </summary>
		public bool DispatchAllowed
		{
			get
			{
				return mDispatchAllowed;
			}
			set
			{
				lock (mQueueLock)
				{
					mDispatchAllowed = value;

					if (mDispatchAllowed)
					{
						DispatchRunning = true;
						Task.Factory.StartNew(SendChanges, TaskCreationOptions.LongRunning);
					}
				}
			}
		}


		/// <summary>
		/// Dispatch is currently running and changes are being sent to Kentico.
		/// </summary>
		public bool DispatchRunning
		{
			get;
			set;
		}


		/// <summary>
		/// Sender that provides operations on REST service.
		/// </summary>
		public ISender Sender
		{
			get;
			private set;
		}


		/// <summary>
		/// Highest uSNChanged currently processed.
		/// </summary>
		public long HighestUsnChanged
		{
			get
			{
				if (Replica == null)
				{
					LoadDirecotryReplica();
				}

				return Replica.HighestUsnChanged;
			}
		}


		/// <summary>
		/// Minimalistic directory replica.
		/// </summary>
		private DirectoryReplica Replica
		{
			get;
			set;
		}


		/// <summary>
		/// Logger for tracking dispatcher errors and actions.
		/// </summary>
		public Logger Logger
		{
			get;
			private set;
		}


		/// <summary>
		/// Fired when error occurrs.
		/// </summary>
		public event Action OnError;


		/// <summary>
		/// Basic constructor.
		/// </summary>
		/// <param name="sender">Sender for REST operations.</param>
		/// <param name="userBindings">Custom user attribute bindings.</param>
		/// <param name="groupBindings">Custom role attribute bindings.</param>
		/// <param name="logger">Errors and events logger.</param>
		public ChangesDispatcher(ISender sender, List<AttributeBinding> userBindings, List<AttributeBinding> groupBindings, Logger logger)
		{
			Logger = logger;
			Sender = sender;
			UserBindings = userBindings;
			GroupBindings = groupBindings;
			Queue = new List<SearchResultEntry>();
		}


		/// <summary>
		/// Enqueue changes.
		/// </summary>
		/// <param name="entries">Changes to process.</param>
		public void AddToQueue(List<SearchResultEntry> entries)
		{
			if ((entries != null) && entries.Any())
			{
				lock (mQueueLock)
				{
					// Add items to query
					Queue.AddRange(entries);

					// Sort query by uSNChanged and process users first
					Queue.Sort((a, b) =>
					{
						if (LdapHelper.IsUser(a) ^ LdapHelper.IsUser(b))
						{
							return LdapHelper.IsUser(a) ? -1 : 1;
						}

						return LdapHelper.GetUsnChanged(a).CompareTo(LdapHelper.GetUsnChanged(b));
					});

					// Start sending changes
					if (DispatchAllowed && !DispatchRunning)
					{
						DispatchRunning = true;
						Task.Factory.StartNew(SendChanges, TaskCreationOptions.LongRunning);
					}
				}
			}
		}


		/// <summary>
		/// Send changes to CMS one-by-one.
		/// </summary>
		private void SendChanges()
		{
			// Re-enumerate queue in each iteration
			while (Queue.Any() && DispatchAllowed)
			{
				lock (mQueueLock)
				{
					try
					{
						if (Replica == null)
						{
							LoadDirecotryReplica();
						}

						// Process first entry
						var entry = Queue.First();

						if (entry == null)
						{
							// Remove all nulls
							Queue.RemoveAll(e => e == null);
							continue;
						}

						// Handle incoming change
						if (LdapHelper.IsUser(entry))
						{
							HandleUser(entry);
						}
						else if (LdapHelper.IsGroup(entry))
						{
							HandleGroup(entry);
						}

						Queue.Remove(entry);

						// Set actual uSNChanged attribute
						long newUsn = LdapHelper.GetUsnChanged(entry);
						if ((Replica != null) && (Replica.HighestUsnChanged < newUsn))
						{
							Replica.HighestUsnChanged = newUsn;
						}

						SaveDirecotryReplica();
					}
					catch (Exception ex)
					{
						LogError("Exception occurred when processing object.", ex);
					}
				}
			}

			DispatchRunning = false;
		}


		/// <summary>
		/// Handle single user.
		/// </summary>
		/// <param name="entry">User LDAP object.</param>
		private void HandleUser(SearchResultEntry entry)
		{
			// Create CMS object from LDAP object
			var user = new User(
				LdapHelper.GetObjectGuid(entry),
				LdapHelper.GetAttributeString(entry.Attributes["name"], true),
				LdapHelper.IsUserEnabled(entry),
				UserBindings.Select(k => new KeyValuePair<string, string>(k.Cms, LdapHelper.GetAttributeString(entry.Attributes[k.Ldap]))).ToList());

			// Find existing object in LDAP replica
			var existing = Replica.Users.FirstOrDefault(u => u.Guid == user.Guid);

			if (LdapHelper.IsDeleted(entry))
			{
				if (existing != null)
				{
					// Remove user
					Sender.RemoveUser(existing);
					Replica.Users.Remove(existing);
				}
			}
			else if (existing != null)
			{
				// Check if any attribute has changed
				var userXml = Sender.GetUser(user.Guid);

				if (!string.IsNullOrEmpty(userXml))
				{
					bool userChanged =
						User.InternalBindings.Any(
							b =>
								RestHelper.GetAttributeFromReponse(userXml, b.Value) !=
								((b.Key == "userAccountControl" ? LdapHelper.IsUserEnabled(entry).ToString().ToLowerInvariant() : LdapHelper.GetAttributeString(entry.Attributes[b.Key], b.Key == "name")) ?? string.Empty));
					userChanged |=
						UserBindings.Any(
							b =>
								RestHelper.GetAttributeFromReponse(userXml, b.Cms) !=
								(LdapHelper.GetAttributeString(entry.Attributes[b.Ldap]) ?? string.Empty));

					if (userChanged)
					{
						// Modify user
						Sender.ModifyUser(user);
					}
				}
			}
			else
			{
				// Add user
				long? userId = Sender.AddUser(user);

				if (userId != null)
				{
					user.Id = userId.Value;
					user.DistinguishedName = entry.DistinguishedName;
					Replica.Users.Add(user);
				}
			}
		}


		/// <summary>
		/// Handle single role.
		/// </summary>
		/// <param name="entry">Group LDAP object.</param>
		private void HandleGroup(SearchResultEntry entry)
		{
			// Create CMS object from LDAP object
			var role = new Role(LdapHelper.GetObjectGuid(entry),
				LdapHelper.GetAttributeString(entry.Attributes["sAMAccountName"], true),
				LdapHelper.GetAttributeString(entry.Attributes["displayName"]),
				GroupBindings.Select(k => new KeyValuePair<string, string>(k.Cms, LdapHelper.GetAttributeString(entry.Attributes[k.Ldap]))).ToList());

			var existing = Replica.Groups.FirstOrDefault(g => g.Guid == role.Guid);

			List<User> currentMembers = (existing == null) ? new List<User>() : Replica.Bindings.Where(b => b.RoleId == existing.Id).SelectMany(b => Replica.Users.Where(u => u.Id == b.UserId)).ToList();
			List<User> newMembers = LdapHelper.GetGroupMembers(entry).SelectMany(d => Replica.Users.Where(u => string.Equals(u.DistinguishedName, d, StringComparison.InvariantCultureIgnoreCase))).ToList();

			if (LdapHelper.IsDeleted(entry))
			{
				if (existing != null)
				{
					// Delete role
					Sender.RemoveRole(existing);
					Replica.Groups.Remove(existing);
				}
			}
			else
			{
				if (existing != null)
				{
					role.Id = existing.Id;

					// Check if any attribute has changed
					var roleXml = Sender.GetRole(role.Id);

					if (!string.IsNullOrEmpty(roleXml))
					{
						bool roleChanged =
							Role.InternalBindings.Any(
								b =>
									RestHelper.GetAttributeFromReponse(roleXml, b.Value) !=
									(LdapHelper.GetAttributeString(entry.Attributes[b.Key], b.Key == "sAMAccountName") ?? string.Empty));
						roleChanged |=
							GroupBindings.Any(
								b =>
									RestHelper.GetAttributeFromReponse(roleXml, b.Cms) !=
									(LdapHelper.GetAttributeString(entry.Attributes[b.Ldap]) ?? string.Empty));

						if (roleChanged)
						{
							// Modify role
							Sender.ModifyRole(role);
						}
					}
				}
				else
				{
					// Add role
					long? roleId = Sender.AddRole(role);

					if (roleId != null)
					{
						role.Id = roleId.Value;
						Replica.Groups.Add(role);
					}
				}

				// Add members
				var addedMembers = newMembers.Where(m => currentMembers.All(c => c.Guid != m.Guid)).ToList();
				foreach (var member in addedMembers)
				{
					var userroleId = Sender.AddUserToRole(member.Id, role.Id);

					if (userroleId != null)
					{
						Replica.Bindings.Add(new UserRoleBinding(member.Id, role.Id) { Id = userroleId.Value });
					}
				}

				// Remove members
				var removedMembers =
					currentMembers.Where(m => newMembers.All(c => c.Guid != m.Guid))
						.SelectMany(m => Replica.Bindings.Where(b => (b.RoleId == role.Id) && (b.UserId == m.Id)))
						.ToList();
				foreach (var member in removedMembers)
				{
					Sender.RemoveUserFromRole(member.Id);
					Replica.Bindings.Remove(member);
				}
			}
		}


		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}


		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				// Save directory replica on dispose.
				SaveDirecotryReplica();
			}
		}


		/// <summary>
		/// Load directory replica into memory.
		/// </summary>
		private void LoadDirecotryReplica()
		{
			// Load directory replica
			var serailizer = new XmlSerializer(typeof(DirectoryReplica));

			if (File.Exists(Environment.ExpandEnvironmentVariables(REPLICA_PATH)))
			{
				using (var stream = new FileStream(Environment.ExpandEnvironmentVariables(REPLICA_PATH), FileMode.OpenOrCreate))
				{
					Replica = (DirectoryReplica)serailizer.Deserialize(stream);
				}
			}
			else
			{
				Replica = new DirectoryReplica
				{
					HighestUsnChanged = 0,
					Bindings = new List<UserRoleBinding>(),
					Groups = new List<Role>(),
					Users = new List<User>()
				};
			}
		}


		/// <summary>
		/// Load directory replica into memory.
		/// </summary>
		[SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")] // http://stackoverflow.com/questions/3831676/ca2202-how-to-solve-this-case
		private void SaveDirecotryReplica()
		{
			if (Replica != null)
			{
				var serlializer = new XmlSerializer(typeof(DirectoryReplica));

				using (var stream = new FileStream(Environment.ExpandEnvironmentVariables(REPLICA_PATH), FileMode.Create))
				{
					using (var writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true }))
					{
						serlializer.Serialize(writer, Replica);
					}
				}
			}
		}


		/// <summary>
		/// Minimalistic replica of current users and groups synchronized.
		/// </summary>
		public class DirectoryReplica
		{
			/// <summary>
			/// Highest uSNChanged attribute value
			/// </summary>
			[XmlAttribute(AttributeName = "HighestUSNChanged")]
			public long HighestUsnChanged { get; set; }

			/// <summary>
			/// Already added users.
			/// </summary>
			[XmlArray(ElementName = "Users")]
			public List<User> Users { get; set; }

			/// <summary>
			/// Already added roles.
			/// </summary>
			[XmlArray(ElementName = "Roles")]
			public List<Role> Groups { get; set; }

			/// <summary>
			/// Already added user-role bindings.
			/// </summary>
			[XmlArray(ElementName = "Bindings")]
			public List<UserRoleBinding> Bindings { get; set; }
		}


		private void LogError(string message, Exception ex)
		{
			if (Logger != null)
			{
				Logger.LogError(message, ex);
			}

			if (OnError != null)
			{
				OnError();
			}
		}
	}
}
