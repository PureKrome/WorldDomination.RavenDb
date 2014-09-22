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
        private IList<IEnumerable> _dataToBeSeeded;
        private IDictionary<string, IAsyncDocumentSession> _asyncDocumentSessions;
        private IDocumentStore _documentStore;
        private ExistingDocumentStoreSettings _existingDocumentStoreSettings;

        private IList<Type> _indexesToExecute;

        /// <summary>
        ///     Collection of Indexes which will be executed during the document store initialization.
        /// </summary>
        protected IList<Type> IndexesToExecute
        {
            get
            {
                Trace.TraceInformation("* " + (_indexesToExecute == null ? 0 : _indexesToExecute.Count) +
                                       " index(es) have been requested to be executed (indexes to be indexed).");
                return _indexesToExecute;
            }
            set
            {
                if (_documentStore != null)
                {
                    throw new InvalidOperationException(
                        "The DocumentStore has already been created and Initialized. As such, changes to the Index list will not be used. Therefore, set this collection BEFORE your first call to a DocumentSession (which in effect creates the DocumentStore if it has been created).");
                }
                _indexesToExecute = value;
            }
        }

        /// <summary>
        ///     A collection of data, which will be 'seeded' during the document store initialization.
        /// </summary>
        protected IList<IEnumerable> DataToBeSeeded
        {
            get
            {
                Trace.TraceInformation("* " + (_dataToBeSeeded == null ? 0 : _dataToBeSeeded.Count) +
                                       " collection(s) of objects have been requested to be seeded (aka. Stored) in the database.");
                return _dataToBeSeeded;
            }
            set
            {
                if (_documentStore != null)
                {
                    throw new InvalidOperationException(
                        "The DocumentStore has already been created and Initialized. As such, changes to the Seed data list will not be used. Therefore, set this collection BEFORE your first call to a DocumentSession (which in effect creates the DocumentStore if it has been created).");
                }

                _dataToBeSeeded = value;
            }
        }

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
                    Trace.TraceInformation("** NOTE: If you wish to target an existing document store, please set the 'DocumentStoreUrl' property.");

                    documentStore = new EmbeddableDocumentStore
                                    {
                                        RunInMemory = true,
                                    };
                }
                else
                {
                    Trace.TraceInformation("The DocumentStore Url [{0}] was provided. Creating a new (normal) DocumentStore with a Tenant named [{1}].",
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
                    DocumentConvention.DefaultQueryingConsistency = ConsistencyOptions.AlwaysWaitForNonStaleResultsAsOfLastWrite;
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
                Trace.TraceInformation("Async Document Session Key [" + key +
                                       "] doesn't exist. Creating a new dictionary item.");
                _asyncDocumentSessions.Add(key, DocumentStore.OpenAsyncSession());
            }

            return _asyncDocumentSessions[key];
        }
    }
}