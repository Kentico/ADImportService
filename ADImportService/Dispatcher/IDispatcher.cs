using System.Collections.Generic;
using System.DirectoryServices.Protocols;

namespace Kentico.ADImportService
{
    /// <summary>
    /// Dispatches changes to Kentico.
    /// </summary>
    public interface IDispatcher
    {
        /// <summary>
        /// Dispatch is allowed, dispatcher can send changes as soon as possible.
        /// </summary>
        bool DispatchAllowed { get; set; }


        /// <summary>
        /// Dispatch is currently running and changes are being sent to Kentico.
        /// </summary>
        bool DispatchRunning { get; }


        /// <summary>
        /// Enqueue changes.
        /// </summary>
        /// <param name="entries">Changes to process.</param>
        void AddToQueue(List<SearchResultEntry> entries);
    }
}
