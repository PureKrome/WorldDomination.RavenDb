using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Embedded;
using WorldDomination.Raven.Client;
using WorldDomination.Raven.Client.Listeners;

namespace WorldDomination.Raven.Tests.Helpers
{
    public abstract class RavenDbTestBase : IDisposable
    {
        private const string DefaultSessionKey = "DefaultSession";
        private IDictionary<string, IAsyncDocumentSession> _asyncDocumentSessions;
        private IList<IEnumerable> _dataToBeSeeded;
        private IDocumentStore _documentStore;
        private ExistingDocumentStoreSettings _existingDocumentStoreSettings;
        private IList<Type> _indexesToExecute;

        /// <summary>
        /// A collection of data, which will be 'seeded' during the document store initialization.
        /// </summary>
        protected IList<IEnumerable> DataToBeSeeded
        {
            get
            {
                Trace.TraceInformation(
                    "* {0} collection(s) of objects have been requested to be seeded (aka. Stored) in the database.",
                    _dataToBeSeeded == null
                        ? 0
                        : _dataToBeSeeded.Count);
                return _dataToBeSeeded;
            }
            set
            {
                EnsureDocumentStoreHasNotBeenInitialized("DataToBeSeeded");
                _dataToBeSeeded = value;
            }
        }

        /// <summary>
        ///     Collection of Indexes which will be executed during the document store initialization.
        /// </summary>
        protected IList<Type> IndexesToExecute
        {
            get
            {
                Trace.TraceInformation("* {0} index(es)/result transformer(s) have been requested to be executed.",
                    _indexesToExecute == null 
                    ? 0 
                    : _indexesToExecute.Count);
                                       
                return _indexesToExecute;
            }
            set
            {
                EnsureDocumentStoreHasNotBeenInitialized("IndexesToExecute");
                _indexesToExecute = value;
            }
        }

        /// <summary>
        /// Optional: provide an instance to an ExistingDocumentStoreSettings if you wish to connect to a real database.
        /// </summary>
        /// <remarks><b>CAREFUL!</b> This should rarely be used and most likely not during unit tests. The common scenario for using this is for some specific debugging against a real database (and hopefully that's a copy of some live database, also!) **</remarks>
        protected ExistingDocumentStoreSettings ExistingDocumentStoreSettings
        {
            get { return _existingDocumentStoreSettings; }
            set
            {
                if (_documentStore != null)
                {
                    throw new InvalidOperationException(
                        "The DocumentStore has already been created and Initialized. As such, the ExistingDocumentStoreSettings instance cannot be used. Therefore, set this value BEFORE your first call to a AsyncDocumentSession (which in effect creates the DocumentStore pointing to your desired location).");
                }

                _existingDocumentStoreSettings = value;
            }
        }

        /// <summary>
        ///     Some custom document conventions. Eg. You might require a custom JsonContractResolver to serialize/deserialize IPAddresses.
        /// </summary>
        protected DocumentConvention DocumentConvention { get; set; }

        /// <summary>
        /// The main Document Store where all your lovely data will live and smile.
        /// </summary>
        protected IDocumentStore DocumentStore
        {
            get
            {
                if (_documentStore != null)
                {
                    return _documentStore;
                }

                DocumentStore documentStore;

                if (ExistingDocumentStoreSettings == null ||
                    string.IsNullOrWhiteSpace(ExistingDocumentStoreSettings.DocumentStoreUrl))
                {
                    Trace.TraceInformation("Creating a new Embedded DocumentStore in **RAM**.");
                    Trace.TraceInformation(
                        "** NOTE: If you wish to target an existing document store, please set the 'DocumentStoreUrl' property.");

                    documentStore = new EmbeddableDocumentStore
                    {
                        RunInMemory = true,
                    };
                }
                else
                {
                    Trace.TraceInformation(
                        "The DocumentStore Url [{0}] was provided. Creating a new (normal) DocumentStore with a Tenant named [{1}].",
                        ExistingDocumentStoreSettings.DocumentStoreUrl,
                        ExistingDocumentStoreSettings.DefaultDatabase);
                    documentStore = new DocumentStore
                    {
                        Url = ExistingDocumentStoreSettings.DocumentStoreUrl,
                        DefaultDatabase = ExistingDocumentStoreSettings.DefaultDatabase
                    };
                }

                if (DocumentConvention != null)
                {
                    Trace.TraceInformation(
                        "* Using the provided DocumentStore DocumentConvention object :) Forcing the default DefaultQueryingConsistency to be ConsistencyOptions.AlwaysWaitForNonStaleResultsAsOfLastWrite.");
                    DocumentConvention.DefaultQueryingConsistency =
                        ConsistencyOptions.AlwaysWaitForNonStaleResultsAsOfLastWrite;
                    documentStore.Conventions = DocumentConvention;
                }
                else
                {
                    Trace.TraceInformation("Setting DocumentStore Conventions: ConsistencyOptions.QueryYourWrites.");
                    documentStore.Conventions = new DocumentConvention
                    {
                        DefaultQueryingConsistency =
                            ConsistencyOptions.AlwaysWaitForNonStaleResultsAsOfLastWrite
                    };
                }

                Trace.TraceInformation("Initializing data with Defaults :-");
                documentStore.InitializeWithDefaults(DataToBeSeeded, IndexesToExecute);
                Trace.TraceInformation("   Done!");

                // Force query's to wait for index's to catch up. Unit Testing only :P
                Trace.TraceInformation(
                    "Forcing queries to always wait until they are not stale. aka. It's like => WaitForNonStaleResultsAsOfLastWrite.");
                documentStore.RegisterListener(new NoStaleQueriesListener());

                Trace.TraceInformation("** Finished initializing the Document Store.");
                Trace.TraceInformation("    ** Number of Documents: " +
                                       documentStore.DatabaseCommands.GetStatistics().CountOfDocuments);
                Trace.TraceInformation("    ** Number of Indexes: " +
                                       documentStore.DatabaseCommands.GetStatistics().CountOfIndexes);

                _documentStore = documentStore;

                return _documentStore;
            }
        }

        /// <summary>
        ///     The 'default' Raven async document session.
        /// </summary>
        protected IAsyncDocumentSession AsyncDocumentSession
        {
            get { return AsyncDocumentSessions(DefaultSessionKey); }
        }

        /// <summary>
        ///     A named Raven async document session.
        /// </summary>
        /// <param name="key">The key name of an async document session.</param>
        /// <returns>The RavenDb async document session.</returns>
        protected IAsyncDocumentSession AsyncDocumentSessions(string key)
        {
            if (_asyncDocumentSessions == null)
            {
                Trace.TraceInformation("Creating a new async Document Session dictionary to hold all our sessions.");
                _asyncDocumentSessions = new Dictionary<string, IAsyncDocumentSession>();
            }

            // Do we have the key?
            if (!_asyncDocumentSessions.ContainsKey(key))
            {
                Trace.TraceInformation("Async Document Session Key [{0}] doesn't exist. Creating a new dictionary item.", key);
                _asyncDocumentSessions.Add(key, DocumentStore.OpenAsyncSession());
            }

            return _asyncDocumentSessions[key];
        }

        #region IDisposable Members

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Trace.TraceInformation(
                "Disposing of RavenDbTest class. This will clean up any Document Sessions and the Document Store.");
            if (DocumentStore.WasDisposed)
            {
                return;
            }

            // Assert for any errors.
            Trace.TraceInformation("Asserting for any DocumentStore errors.");
            DocumentStore.AssertDocumentStoreErrors();

            // Clean up.
            if (_asyncDocumentSessions != null)
            {
                Trace.TraceInformation("Found some Document Sessions that exist. Lets clean them up :-");
                foreach (var key in _asyncDocumentSessions.Keys)
                {
                    Trace.TraceInformation("    - Found Key: " + key);
                    _asyncDocumentSessions[key].Dispose();
                    Trace.TraceInformation(" ... Document Session now disposed! ");
                }
            }

            Trace.TraceInformation("Disposing the Document Store ... ");
            DocumentStore.Dispose();
            Trace.TraceInformation("Done!");
        }

        #endregion

        private void EnsureDocumentStoreHasNotBeenInitialized(string listName)
        {
            if (string.IsNullOrWhiteSpace(listName))
            {
                throw new ArgumentNullException("listName");
            }

            if (_documentStore != null)
            {
                var errorMessage =
                    string.Format(
                        "The DocumentStore has already been created and Initialized. As such, changes to the {0} list will not be used. Therefore, set this collection BEFORE your first call to a DocumentSession (which in effect creates the DocumentStore if it has been created).",
                        listName);
                throw new InvalidOperationException(errorMessage);
            }
        }
    }
}