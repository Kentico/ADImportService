using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

using SearchScope = System.DirectoryServices.Protocols.SearchScope;

namespace Kentico.ADImportService
{
    /// <summary>
    /// Listener that track user and group changes in Active Directory Domain Services using Change notifications control.
    /// </summary>
    public class ChangeNotificationsListener : IListener
    {
        private LdapDirectoryIdentifier mDomainController = null;
        private LdapConnection mConnection = null;
        private string mDefaultNamingContext = null;
        private string mConfigurationNamingContext = null;

        private readonly List<IAsyncResult> mSearches = new List<IAsyncResult>();
        

        /// <summary>
        /// Listener configuration.
        /// </summary>
        public ListenerConfiguration Configuration
        {
            get;
            private set;
        }


        /// <summary>
        /// Domain controller which is used to track AD changes.
        /// </summary>
        public LdapDirectoryIdentifier DomainController
        {
            get
            {
                if ((mDomainController == null) && (!string.IsNullOrEmpty(Configuration.DomainController)))
                {
                    mDomainController = new LdapDirectoryIdentifier(Configuration.DomainController);
                }

                return mDomainController;
            }
        }


        /// <summary>
        /// Connection to domain controller.
        /// </summary>
        public LdapConnection Connection
        {
            get
            {
                if ((mConnection == null) && (Configuration.Credentials != null) && (DomainController != null))
                {
                    mConnection = new LdapConnection(DomainController, Configuration.Credentials, AuthType.Ntlm);
                    mConnection.SessionOptions.ProtocolVersion = 3;
                    mConnection.Timeout = TimeSpan.FromHours(1);
                    mConnection.AutoBind = true;

                    mConnection.SessionOptions.SecureSocketLayer = Configuration.UseSsl;
                    if (Configuration.UseSsl)
                    {
                        mConnection.SessionOptions.VerifyServerCertificate = (conn, cert) => Certificate.Equals(cert);
                    }
                }

                return mConnection;
            }
        }


        /// <summary>
        /// Certificate used for validation of communication with LDAP server.
        /// </summary>
        public X509Certificate Certificate
        {
            get; 
            private set; 
        }


        /// <summary>
        /// Default namimg context.
        /// </summary>
        public string DefaultNamingContext
        {
            get
            {
                if (string.IsNullOrEmpty(mDefaultNamingContext))
                {
                    SearchResponse response = null;
                    try
                    {
                        // Try to get default naming context
                        var request = new SearchRequest(null, "(objectClass=*)", SearchScope.Base, "defaultNamingContext");
                        response = (SearchResponse)Connection.SendRequest(request);
                    }
                    catch (Exception)
                    {
                        LogMessage("Failed to get default naming context. The given domain controller distinguished name will be used.");
                    }

                    if ((response != null) && (response.Entries.Count > 0) && (response.Entries[0].Attributes.Contains("defaultNamingContext")))
                    {
                        mDefaultNamingContext = response.Entries[0].Attributes["defaultNamingContext"][0] as string;
                    }

                    // Parse DC name if naming context was not yet set
                    if (string.IsNullOrEmpty(mDefaultNamingContext))
                    {
                        mDefaultNamingContext =
                            Configuration.DomainController
                                .Split('.')
                                .Aggregate(string.Empty, (result, next) => result + ("DC=" + next + ","))
                                .TrimEnd(',');
                    }
                }

                return mDefaultNamingContext;
            }
        }


        /// <summary>
        /// Default namimg context.
        /// </summary>
        public string ConfigurationNamingContext
        {
            get
            {
                if (string.IsNullOrEmpty(mConfigurationNamingContext))
                {
                    SearchResponse response = null;
                    try
                    {
                        // Try to get default naming context
                        var request = new SearchRequest(null, "(objectClass=*)", SearchScope.Base, "configurationNamingContext");
                        response = (SearchResponse)Connection.SendRequest(request);
                    }
                    catch (Exception)
                    {
                        LogMessage("Failed to get configuration naming context. The default naming context with CN=Configuration will be used.");
                    }

                    if ((response != null) && (response.Entries.Count > 0) && (response.Entries[0].Attributes.Contains("configurationNamingContext")))
                    {
                        mConfigurationNamingContext = response.Entries[0].Attributes["configurationNamingContext"][0] as string;
                    }

                    // Parse DC name if naming context was not yet set
                    if (string.IsNullOrEmpty(mConfigurationNamingContext))
                    {
                        mConfigurationNamingContext = "CN=Configuration," + DefaultNamingContext;
                    }
                }

                return mConfigurationNamingContext;
            }
        }


        /// <summary>
        /// Object category for groups.
        /// </summary>
        public string GroupObjectCategory
        {
            get
            {
                return "CN=Group,CN=Schema," + ConfigurationNamingContext;
            }
        }


        /// <summary>
        /// Object category for users.
        /// </summary>
        public string PersonObjectCategory
        {
            get
            {
                return "CN=Person,CN=Schema," + ConfigurationNamingContext;
            }
        }
        

        /// <summary>
        /// Logger for tracking listener errors and actions.
        /// </summary>
        public Logger Logger
        {
            get;
            private set;
        }


        /// <summary>
        /// Dispatches changes to CMS.
        /// </summary>
        public ChangesDispatcher Dispatcher
        {
            get; 
            private set;
        }
        

        /// <summary>
        /// Fired when error occurrs.
        /// </summary>
        public event Action OnError;
        

        /// <summary>
        /// Basic constructor for change notifications listener.
        /// </summary>
        /// <param name="dispatcher">Changes dispatcher</param>
        /// <param name="configuration">Listener configuration</param>
        /// <param name="logger">Logger</param>
        public ChangeNotificationsListener(ChangesDispatcher dispatcher, ListenerConfiguration configuration, Logger logger)
        {
            Configuration = configuration;
            Logger = logger;

            Dispatcher = dispatcher;
            Dispatcher.DispatchAllowed = true;

            if (Configuration.UseSsl)
            {
                if (!File.Exists(Configuration.SslCertificateLocation))
                {
                    throw new ArgumentException("Provided path to certificate doesn't exist.");
                }

                // Load certificate
                Certificate = X509Certificate.CreateFromCertFile(Configuration.SslCertificateLocation);
            }
        }
        

        /// <summary>
        /// Fully synchronizes current state and starts incremental synchronization.
        /// </summary>
        public void StartPermanentSynchronization()
        {
            // Set dispatcher not to send changes until full synchronization is over.
            Dispatcher.DispatchAllowed = false;
            
            StartIncrementalSynchronization();

            Synchronize();

            // All changes are recieved, dispatcher can start sending changes
            Dispatcher.DispatchAllowed = true;

            LogMessage("Synchronization started at " + DateTime.Now);
        }

        
        /// <summary>
        /// Retrieves current state of LDAP database and reflects it to the CMS.
        /// </summary>
        public void Synchronize()
        {
            try
            {
                var request = new SearchRequest(DefaultNamingContext, "(&(|(objectClass=user)(objectClass=group))(usnchanged>="+ (Dispatcher.HighestUsnChanged + 1) +"))", SearchScope.Subtree, null);
                request.Controls.Add(new ShowDeletedControl());

                // Page result
                var prc = new PageResultRequestControl(5);
                request.Controls.Add(prc);
                var soc = new SearchOptionsControl(System.DirectoryServices.Protocols.SearchOption.DomainScope);
                request.Controls.Add(soc);

                while (true)
                {
                    var searchResponse = (SearchResponse) Connection.SendRequest(request);

                    if (searchResponse != null)
                    {
                        // Find the returned page response control
                        foreach (DirectoryControl control in searchResponse.Controls)
                        {
                            if (control is PageResultResponseControl)
                            {
                                //update the cookie for next set
                                prc.Cookie = ((PageResultResponseControl) control).Cookie;
                                break;
                            }
                        }

                        Dispatcher.AddToQueue(searchResponse.Entries.Cast<SearchResultEntry>().Where(p =>LdapHelper.IsUser(p, PersonObjectCategory) || LdapHelper.IsGroup(p, GroupObjectCategory)).ToList());

                        if (prc.Cookie.Length == 0)
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Full synchronization failed.", ex);
            }
        }


        /// <summary>
        /// Resister asynchronous listening to changes in LDAP database. 
        /// Only changes, that occueerd after this method invocation are processed.
        /// </summary>
        public void StartIncrementalSynchronization()
        {
            try
            {
                // Set filter to Users node
                var request = new SearchRequest(DefaultNamingContext, "(objectclass=*)", SearchScope.Subtree);

                // Add change notifications control
                request.Controls.Add(new DirectoryNotificationControl());

                var result = Connection.BeginSendRequest(
                    request,
                    PartialResultProcessing.ReturnPartialResultsAndNotifyCallback,
                    RunAsyncSearch,
                    null
                    );

                // Store searches for object disposal
                mSearches.Add(result);

                LogMessage("Change notification synchronization initialized.");
            }
            catch (Exception ex)
            {
                LogError("Initializing change notification synchronization failed.", ex);
            }
        }
        

        /// <summary>
        /// Asynchronous callback that processes changes from Active Directory.
        /// </summary>
        /// <param name="asyncResult">Result of permanens search</param>
        private void RunAsyncSearch(IAsyncResult asyncResult)
        {
            var results = new List<SearchResultEntry>();

            // Get changes
            if (!asyncResult.IsCompleted)
            {
                PartialResultsCollection partialResults = null;

                try
                {
                    partialResults = Connection.GetPartialResults(asyncResult);
                }
                catch (Exception e)
                {
                    LogError("Retrieving partial results from Active Directory asynchronous search failed.", e);
                }

                if (partialResults != null)
                {
                    // Add only users and groups
                    results.AddRange(partialResults.OfType<SearchResultEntry>().Where(p => LdapHelper.IsUser(p, PersonObjectCategory) || LdapHelper.IsGroup(p, GroupObjectCategory)));                    
                }
            }
            else
            {                
                LogMessage("The change notification control unexpectedly ended the search.");

                mSearches.Remove(asyncResult);
                StartIncrementalSynchronization();
            }

            // Send changes to CMS
            Dispatcher.AddToQueue(results);
        }
        

        public void Dispose()
        {
            // Abort all searches
            mSearches.ForEach(s => Connection.Abort(s));
        }
        

        private void LogMessage(string message)
        {
            if (Logger != null)
            {
                Logger.LogMessage(message);
            }
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
