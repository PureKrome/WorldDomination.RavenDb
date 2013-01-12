using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Client.Indexes;

namespace WorldDomination.Raven.Client
{
    public static class DocumentStoreExtensions
    {
        /// <summary>
        /// Initialized this instance but with some optional settings, like seed data and indexes.
        /// </summary>
        /// <param name="documentStore">The Raven document store.</param>
        /// <param name="seedData">Optional: A collection of data which will be 'seeded' into the new document store.</param>
        /// <param name="indexesToExecute">Optional: Any index(es) which should be executed during initialization. They need to be assignable from an AbstractIndexCreationTask.</param>
        /// <param name="assemblyToScanForIndexes">Optional: The assembly where the index(es) are located.</param>
        public static void InitializeWithDefaults(this IDocumentStore documentStore,
                                                  IEnumerable<IEnumerable> seedData = null,
                                                  ICollection<Type> indexesToExecute = null,
                                                  Type assemblyToScanForIndexes = null)
        {
            // Default initializtion;
            documentStore.Initialize();

            // Index initialisation.
            if (indexesToExecute != null)
            {
                Type[] indexes = (from type in indexesToExecute
                                  where typeof(AbstractIndexCreationTask).IsAssignableFrom(type)
                                  select type).ToArray();
                if (indexes.Length != indexesToExecute.Count)
                {
                    throw new InvalidOperationException("One or more of the provided indexes are not assignable from an AbstractIndexCreationTask. Please confirm that all the indexes provided are assignable from an AbstractIndexCreationTask.");
                }

                IndexCreation.CreateIndexes(new CompositionContainer(new TypeCatalog(indexes)), documentStore);
            }
            else if (assemblyToScanForIndexes != null)
            {
                IndexCreation.CreateIndexes(assemblyToScanForIndexes.Assembly, documentStore);
            }

            // Create our Seed Data (if provided).
            if (seedData != null)
            {
                CreateSeedData(seedData, documentStore);
            }

            // Now lets check to make sure there are now errors.
            documentStore.AssertDocumentStoreErrors();
        }

        /// <summary>
        /// Asserts if the document store has any errors.
        /// </summary>
        /// <param name="documentStore">The Raven document store.</param>
        public static void AssertDocumentStoreErrors(this IDocumentStore documentStore)
        {
            if (documentStore == null)
            {
                throw new ArgumentNullException("documentStore");
            }

            ServerError[] errors = documentStore.DatabaseCommands.GetStatistics().Errors;
            if (errors == null || errors.Length <= 0)
            {
                return;
            }

            // We have some Errors. NOT. GOOD. :(
            foreach (ServerError serverError in errors)
            {
                string errorMessage = string.Format("Document: {0}; Index: {1}; Error: {2}",
                                                    string.IsNullOrEmpty(serverError.Document)
                                                        ? "No Document Id"
                                                        : serverError.Document,
                                                    string.IsNullOrEmpty(serverError.Index)
                                                        ? "No Index"
                                                        : serverError.Index,
                                                    string.IsNullOrEmpty(serverError.Error)
                                                        ? "No Error message .. err??"
                                                        : serverError.Error);

                Debug.WriteLine(errorMessage);
            }

            throw new InvalidOperationException("DocumentStore has some errors. Dast is nict gut.");
        }

        private static void WaitForStaleIndexesToComplete(this IDocumentStore documentStore)
        {
            while (documentStore.DatabaseCommands.GetStatistics().StaleIndexes.Length != 0)
            {
                Thread.Sleep(50);
                Debug.WriteLine("Waiting for indexes to stop being stale ...");
            }
        }

        private static void CreateSeedData(IEnumerable<IEnumerable> seedData, IDocumentStore documentStore)
        {
            if (documentStore == null)
            {
                throw new ArgumentNullException("documentStore");
            }

            if (seedData == null)
            {
                throw new ArgumentNullException("seedData");
            }

            using (IDocumentSession documentSession = documentStore.OpenSession())
            {
                // First, check to make sure we don't have any data.
                if (documentSession.Advanced.DocumentStore.DatabaseCommands.GetStatistics().CountOfDocuments > 0)
                {
                    // We have documents, so nothing to worry about :)
                    return;
                }

                // Store each collection of fake seeded data.
                Debug.WriteLine("Seeding Data :-");
                foreach (IEnumerable collection in seedData)
                {
                    int count = 0;
                    string entityName = "Unknown";

                    foreach (object entity in collection)
                    {
                        count++;
                        if (count <= 1)
                        {
                            entityName = entity.GetType().ToString();
                        }
                        documentSession.Store(entity);
                    }
                    Debug.WriteLine("   --- {0} {1}", count, entityName);
                }
                Debug.WriteLine("   Done!");

                // Commit this transaction.
                documentSession.SaveChanges();

                // Make sure all our indexes are not stale.
                documentStore.WaitForStaleIndexesToComplete();
            }
        }
    }
}