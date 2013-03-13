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
        private IList<Type> _indexesToExecute;

        /// <summary>
        ///     Collection of Indexes which will be executed during the document store initialization.
        /// </summary>
        protected IList<Type> IndexesToExecute
        {
            get
            {
                Trace.WriteLine("* " + (_indexesToExecute == null ? 0 : _indexesToExecute.Count) +
                                " index(es) have been requested to be executed (indexes to be indexed).");
                return _indexesToExecute;
            }
            set { _indexesToExecute = value; }
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
            set { _dataToBeSeeded = value; }
        }

        private IDocumentStore DocumentStore
        {
            get
            {
                if (_documentStore != null)
                {
                    return _documentStore;
                }

                Trace.WriteLine("Creating a new Embedded DocumentStore : In Ram and ConsistencyOptions.QueryYourWrites.");
                var documentStore = new EmbeddableDocumentStore
                                    {
                                        RunInMemory = true,
                                        Conventions = new DocumentConvention
                                        {
                                            DefaultQueryingConsistency = ConsistencyOptions.QueryYourWrites
                                        } 
                                    };

                Trace.WriteLine("Initializing data with Defaults :-");
                documentStore.InitializeWithDefaults(DataToBeSeeded, IndexesToExecute);
                Trace.WriteLine("   Done!");

                // Force query's to wait for index's to catch up. Unit Testing only :P
                Trace.WriteLine("Forcing queries to always wait until they are not stale. aka. It's like => WaitForNonStaleResultsAsOfLastWrite.");
                documentStore.RegisterListener(new NoStaleQueriesListener());

                Trace.WriteLine("** Finished initializing the Document Store.");
                Trace.WriteLine("    ** Number of Documents: " +
                                documentStore.DatabaseCommands.GetStatistics().CountOfDocuments);
                Trace.WriteLine("    ** Number of Indexes: " +
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
            Trace.WriteLine(
                "Disposing of RavenDbTest class. This will clean up any Document Sessions and the Document Store.");
            if (DocumentStore.WasDisposed)
            {
                return;
            }

            // Assert for any errors.
            Trace.WriteLine("Asserting for any DocumentStore errors.");
            DocumentStore.AssertDocumentStoreErrors();

            // Clean up.
            if (_documentSessions != null)
            {
                Trace.WriteLine("Found some Document Sessions that exist. Lets clean them up :-");
                foreach (var key in _documentSessions.Keys)
                {
                    Trace.Write("    - Found Key: " + key);
                    _documentSessions[key].Dispose();
                    Trace.WriteLine(" ... Document Session now disposed! ");
                }
            }

            Trace.Write("Disposing the Document Store ... ");
            DocumentStore.Dispose();
            Trace.WriteLine("Done!");
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
                Trace.WriteLine("Creating a new Document Session dictionary to hold all our sessions.");
                _documentSessions = new Dictionary<string, IDocumentSession>();
            }

            // Do we have the key?
            if (!_documentSessions.ContainsKey(key))
            {
                Trace.WriteLine("Document Session Key [" + key + "] doesn't exist. Creating a new dictionary item.");
                _documentSessions.Add(key, DocumentStore.OpenSession());
            }

            return _documentSessions[key];
        }
    }
}