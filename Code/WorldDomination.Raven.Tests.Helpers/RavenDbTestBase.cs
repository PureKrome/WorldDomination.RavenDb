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
        private IDictionary<string, IDocumentSession> _documentSessions;
        private IDocumentStore _documentStore;
        private string _documentStoreUrl;
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
                Trace.WriteLine("* " + (_dataToBeSeeded == null ? 0 : _dataToBeSeeded.Count) +
                                " collection(s) of objects have been requested to be seeded (aka. Stored) in the database.");
                return _dataToBeSeeded;
            }
            set
            {
                if (_documentStore != null)
                {
                    throw new InvalidOperationException("The DocumentStore has already been created and Initialized. As such, changes to the Seed data list will not be used. Therefore, set this collection BEFORE your first call to a DocumentSession (which in effect creates the DocumentStore if it has been created).");
                }

                _dataToBeSeeded = value;
            }
        }

        /// <summary>
        ///     Optional: Url of another document store to use in the test scenario.
        /// </summary>
        /// <remarks>This is a vary rare case of debugging. Generally, you do not set the value of this property and just use the embedded DocumentStore for in memory tests. Sometimes, you might want to see what data has actually been stored because there's something going wrong and you can't seem to programmatically debug the issue. Therefore, you can use a normal DocumentStore instance.</remarks>
        protected string DocumentStoreUrl
        {
            get
            {
                return _documentStoreUrl;
            }
            set
            {
                if (_documentStore != null)
                {
                    throw new InvalidOperationException("The DocumentStore has already been created and Initialized. As such, changes to the DocumentStore Url will not be used. Therefore, set this value BEFORE your first call to a DocumentSession (which in effect creates the DocumentStore pointing to your desired location).");
                }

                _documentStoreUrl = value;
            }
        }

        private IDocumentStore DocumentStore
        {
            get
            {
                if (_documentStore != null)
                {
                    return _documentStore;
                }

                DocumentStore documentStore;

                if (string.IsNullOrEmpty(DocumentStoreUrl))
                {
                    Trace.TraceInformation("Creating a new Embedded DocumentStore in **RAM**.");
                    documentStore = new EmbeddableDocumentStore
                                    {
                                        RunInMemory = true,
                                    };
                }
                else
                {
                    Trace.TraceInformation("The DocumentStore Url [" + DocumentStoreUrl + "] was provided. Creating a new (normal) DocumentStore with a Tenant named [UnitTests].");
                    documentStore = new DocumentStore
                                    {
                                        Url = DocumentStoreUrl,
                                        DefaultDatabase = "UnitTests"
                                    };
                }

                Trace.TraceInformation("Setting DocumentStore Conventions: ConsistencyOptions.QueryYourWrites.");
                documentStore.Conventions = new DocumentConvention
                                            {
                                                DefaultQueryingConsistency =
                                                    ConsistencyOptions.QueryYourWrites
                                            };

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
        ///     The 'default' Raven document session.
        /// </summary>
        protected IDocumentSession DocumentSession
        {
            get { return DocumentSessions(DefaultSessionKey); }
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
            if (_documentSessions != null)
            {
                Trace.WriteLine("Found some Document Sessions that exist. Lets clean them up :-");
                foreach (var key in _documentSessions.Keys)
                {
                    Trace.TraceInformation("    - Found Key: " + key);
                    _documentSessions[key].Dispose();
                    Trace.TraceInformation(" ... Document Session now disposed! ");
                }
            }

            Trace.TraceInformation("Disposing the Document Store ... ");
            DocumentStore.Dispose();
            Trace.TraceInformation("Done!");
        }

        #endregion

        /// <summary>
        ///     A named Raven document session.
        /// </summary>
        /// <param name="key">The key name of a document session.</param>
        /// <returns>The RavenDb document session.</returns>
        protected IDocumentSession DocumentSessions(string key)
        {
            if (_documentSessions == null)
            {
                Trace.TraceInformation("Creating a new Document Session dictionary to hold all our sessions.");
                _documentSessions = new Dictionary<string, IDocumentSession>();
            }

            // Do we have the key?
            if (!_documentSessions.ContainsKey(key))
            {
                Trace.TraceInformation("Document Session Key [" + key + "] doesn't exist. Creating a new dictionary item.");
                _documentSessions.Add(key, DocumentStore.OpenSession());
            }

            return _documentSessions[key];
        }
    }
}