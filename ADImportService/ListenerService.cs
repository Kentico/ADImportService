using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;

namespace Kentico.ADImportService
{
    /// <summary>
    /// Windows Service for getting changes about users and groups from Active Directory Domain Services.
    /// </summary>
    public partial class ListenerService : ServiceBase
    {
        private const int ERRORS_LIMIT = 5;
        private const string CONFIGURATION_PATH = @"%PROGRAMDATA%\Kentico AD Import Service\configuration.xml";
        private int mErrorsInARow = 0;


        /// <summary>
        /// Service configuration.
        /// </summary>
        public ServiceConfiguration Configuration
        {
            get;
            private set;
        }


        /// <summary>
        /// Logger for logging to event log.
        /// </summary>
        public Logger Logger
        {
            get; 
            private set; 
        }


        /// <summary>
        /// Asynchronous listener for processing changes in active directory.
        /// </summary>
        public IListener Listener
        {
            get; 
            private set;
        }


        public ISender Sender
        {
            get; 
            private set; 
        }


        public ChangesDispatcher Dispatcher
        {
            get; 
            private set; 
        }


        /// <summary>
        /// Default constructor.
        /// </summary>
        public ListenerService()
        {
            InitializeComponent();

            const string LISTENER_SERVICE_SOURCE = "KenticoADImportServiceSource";
            const string LISTENER_SERVICE_LOG_NAME = "KenticoADImportServiceLog";

            if (!EventLog.SourceExists(LISTENER_SERVICE_SOURCE))
            {
                EventLog.CreateEventSource(LISTENER_SERVICE_SOURCE, LISTENER_SERVICE_LOG_NAME);
            }

            listenerServiceLog.Source = LISTENER_SERVICE_SOURCE;
            listenerServiceLog.Log = LISTENER_SERVICE_LOG_NAME;

            Logger = new Logger(listenerServiceLog);
        }


        protected override void OnStart(string[] args)
        {
            mErrorsInARow = 0;

            // Deserialize configuration from XML
            try
            {
                var serializer = new XmlSerializer(typeof(ServiceConfiguration));

                using (var reader = new StreamReader(Environment.ExpandEnvironmentVariables(CONFIGURATION_PATH)))
                {
                    Configuration = (ServiceConfiguration)serializer.Deserialize(reader);
                }

                Sender = new RestSender(Configuration.Rest, Logger);
                Sender.OnError += Stop;
                Sender.OnSuccess += () => { mErrorsInARow = 0; };

                Dispatcher = new ChangesDispatcher(Sender, Configuration.UserAttributesBindings, Configuration.GroupAttributesBindings, Logger);
                Dispatcher.OnError += OnListenerError;
            }
            catch (Exception ex)
            {
                Logger.LogError("Exception occured during configuration parsing.", ex);
            }

            StartListening();

            Logger.LogMessage("Listener Service has been successfully started.");
        }
        
        
        protected override void OnStop()
        {
            if (Listener != null)
            {
                Listener.Dispose();
            }

            Logger.LogMessage("Listener Service has been successfully stopped.");
            Logger.LogMessage("INFO: Rest request count: " + ((RestSender)Sender).Provider.RequestCounter);
        }


        private void OnListenerError()
        {
            mErrorsInARow++;
            if (mErrorsInARow > ERRORS_LIMIT)
            {
                Logger.LogMessage("Limit of fails in a row exceeded. Sopping service.");
                Stop();
            }
            else
            {
                // Restart listening
                Logger.LogMessage(string.Format("Failed {0} times in a row. Limit is {1}", mErrorsInARow, ERRORS_LIMIT));
                Listener.Dispose();
                StartListening();
            }
        }


        private void StartListening()
        {
            Listener = new ChangeNotificationsListener(Dispatcher, Configuration.Listener, Logger);
            Listener.OnError += OnListenerError;

            // Start synchronization
            try
            {
                 Task.Factory.StartNew(Listener.StartPermanentSynchronization);
            }
            catch (Exception ex)
            {
                Logger.LogError("Exception occured during synchronization.", ex);
            }
        }
    }
}
