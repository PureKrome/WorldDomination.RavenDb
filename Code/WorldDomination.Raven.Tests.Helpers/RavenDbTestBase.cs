using System;
using System.Collections;
using System.Collections.Generic;
using Raven.Client;
using Raven.Client.Embedded;
using WorldDomination.Raven.Client;
using WorldDomination.Raven.Client.Listeners;

namespace WorldDomination.Raven.Tests.Helpers
{
    public abstract class RavenDbTestBase : IDisposable
    {
        private const string DefaultSessionKey = "DefaultSession";
        private IDictionary<string, IDocumentSession> _documentSessions;
        private IDocumentStore _documentStore;

        /// <summary>
        /// Collection of Indexes which will be executed during the document store initialization.
        /// </summary>
        protected IList<Type> IndexesToExecute { get; set; }

        /// <summary>
        /// A collection of data, which will be 'seeded' during the document store initialization.
        /// </summary>
        protected IEnumerable<IEnumerable> DataToBeSeeded { get; set; }

        private IDocumentStore DocumentStore
        {
            get
            {
                if (_documentStore != null)
                {
                    return _documentStore;
                }

                var documentStore = new EmbeddableDocumentStore
                {
                    RunInMemory = true
                };
                documentStore.InitializeWithDefaults(DataToBeSeeded, IndexesToExecute);

                // Force query's to wait for index's to catch up. Unit Testing only :P
                documentStore.RegisterListener(new NoStaleQueriesListener());

                _documentStore = documentStore;

                return _documentStore;
            }
        }

        /// <summary>
        /// The 'default' Raven document session.
        /// </summary>
        protected IDocumentSession DocumentSession
        {
            get { return DocumentSessions(DefaultSessionKey); }
        }

        /// <summary>
        /// A named Raven document session.
        /// </summary>
        /// <param name="key">The key name of a document session.</param>
        /// <returns>The RavenDb document session.</returns>
        protected IDocumentSession DocumentSessions(string key)
        {
            if (_documentSessions == null)
            {
                _documentSessions = new Dictionary<string, IDocumentSession>();
            }

            // Do we have the key?
            if (!_documentSessions.ContainsKey(key))
            {
                _documentSessions.Add(key, DocumentStore.OpenSession());
            }

            return _documentSessions[key];
        }

        #region IDisposable Members

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (DocumentStore.WasDisposed)
            {
                return;
            }

            // Assert for any errors.
            DocumentStore.AssertDocumentStoreErrors();

            // Clean up.
            if (_documentSessions != null)
            {
                foreach (IDocumentSession documentSession in _documentSessions.Values)
                {
                    documentSession.Dispose();
                }
            }

            DocumentStore.Dispose();
        }

        #endregion
    }
}