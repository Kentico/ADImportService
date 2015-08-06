using System;

namespace Kentico.ADImportService
{
    /// <summary>
    /// Listener that track user and group changes in Active Directory Domain Services.
    /// </summary>
    public interface IListener : IDisposable
    {
        /// <summary>
        /// Fully synchronizes current state and starts permanent synchronization.
        /// </summary>
        void StartPermanentSynchronization();


        /// <summary>
        /// Retrieves current state of LDAP database and reflects it to the CMS.
        /// </summary>
        void Synchronize();


        /// <summary>
        /// Resister listening to changes in LDAP database. 
        /// Only changes, that occueerd after this method invocation are processed.
        /// </summary>
        void StartIncrementalSynchronization();


        /// <summary>
        /// Fired when error occurrs.
        /// </summary>
        event Action OnError;
    }
}
