using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Client.Indexes;

namespace WorldDomination.Raven.Client
{
    public static class DocumentStoreExtensions
    {
        /// <summary>
        ///     Initialized this instance but with some optional settings, like seed data and indexes.
        /// </summary>
        /// <param name="documentStore">The Raven document store.</param>
        /// <param name="seedData">Optional: A collection of data which will be 'seeded' into the new document store.</param>
        /// <param name="indexesToExecute">Optional: Any index(es) which should be executed during initialization. They need to be assignable from an AbstractIndexCreationTask.</param>
        /// <param name="assembliesToScanForIndexes">Optional: The assembly where the index(es) are located.</param>
        /// <param name="areDocumentStoreErrorsTreatedAsWarnings">Optional: If there are any server errors, do we downgrade them as warnings or keep them as errors, which stops further processing of the document store.</param>
        public static void InitializeWithDefaults(this IDocumentStore documentStore,
            IEnumerable<IEnumerable> seedData = null,
            ICollection<Type> indexesToExecute = null,
            ICollection<Type> assembliesToScanForIndexes = null,
            bool areDocumentStoreErrorsTreatedAsWarnings = false)
        {
            // Default initializtion;
            documentStore.Initialize();

            // Static indexes or ResultTransformers.
            CreateIndexes(indexesToExecute, assembliesToScanForIndexes, documentStore);

            // Create our Seed Data (if provided).
            if (seedData != null)
            {
                CreateSeedDataAsync(seedData, documentStore).Wait();
            }

            // Now lets check to make sure there are now errors.
            documentStore.AssertDocumentStoreErrors(areDocumentStoreErrorsTreatedAsWarnings);

            // Display any statistics.
            ReportOnInitializedStatistics(documentStore);
        }

        /// <summary>
        ///     Asserts if the document store has any errors.
        /// </summary>
        /// <param name="documentStore">The Raven document store.</param>
        /// <param name="areDocumentStoreErrorsTreatedAsWarnings">Optional: If there are any server errors, do we downgrade them as warnings or keep them as errors, which stops further processing of the document store.</param>
        public static void AssertDocumentStoreErrors(this IDocumentStore documentStore,
            bool areDocumentStoreErrorsTreatedAsWarnings = false)
        {
            if (documentStore == null)
            {
                throw new ArgumentNullException("documentStore");
            }

            var errors = documentStore.DatabaseCommands.GetStatistics().Errors;
            if (errors == null ||
                errors.Length <= 0)
            {
                return;
            }

            // We have some Errors. NOT. GOOD. :(
            var errorMessage = "No server errors supplied.";
            foreach (var serverError in errors)
            {
                errorMessage = string.Format("Document: {0}; Index: {1}; Error: {2}",
                    string.IsNullOrEmpty(serverError.Document)
                        ? "No Document Id"
                        : serverError.Document,
                    string.IsNullOrEmpty(serverError.IndexName)
                        ? "No Index"
                        : serverError.IndexName,
                    string.IsNullOrEmpty(serverError.Error)
                        ? "No Error message .. err??"
                        : serverError.Error);
            }

            if (areDocumentStoreErrorsTreatedAsWarnings)
            {
                Trace.TraceWarning(errorMessage);
            }
            else
            {
                Trace.TraceError(errorMessage);
                throw new InvalidOperationException(string.Format(
                    "### DocumentStore has some errors ###. BLECH!. {0}",
                    string.IsNullOrEmpty(errorMessage)
                        ? string.Empty
                        : "Errors: " + errorMessage));
            }
        }

        private static void CreateIndexes(ICollection<Type> indexesToExecute,
            ICollection<Type> assembliesToScanForIndexes,
            IDocumentStore documentStore)
        {
            // Index initialisation.
            if (indexesToExecute != null)
            {
                Trace.TraceInformation(
                    "Executing {0} index{1}/result transformer{1} that have been manually provided ...",
                    indexesToExecute.Count,
                    indexesToExecute.Count == 1 ? string.Empty : "s");
                var indexes = (from type in indexesToExecute
                    where (typeof (AbstractIndexCreationTask).IsAssignableFrom(type) ||
                           typeof (AbstractTransformerCreationTask).IsAssignableFrom(type))
                    select type).ToArray();
                if (indexes.Length != indexesToExecute.Count)
                {
                    throw new InvalidOperationException(
                        "One or more of the provided indexes/result transformers are not assignable from an AbstractIndexCreationTask or an AbstractTransformerCreationTask. Please confirm that all the indexes provided are assignable from an AbstractIndexCreationTask or an AbstractTransformerCreationTask.");
                }

                IndexCreation.CreateIndexes(new CompositionContainer(new TypeCatalog(indexes)), documentStore);
                Trace.TraceInformation("    Done!");
            }
            else if (assembliesToScanForIndexes != null)
            {
                Trace.TraceInformation(
                    "Scanning {0} assembl{1} for indexes or result transformers that might exist within them ...",
                    assembliesToScanForIndexes.Count,
                    assembliesToScanForIndexes.Count == 1 ? "y" : "ies");

                foreach (var assembly in assembliesToScanForIndexes.Select(x => x.Assembly))
                {
                    IndexCreation.CreateIndexes(assembly, documentStore);
                }
                Trace.TraceInformation("    Done!");
            }
            else
            {
                Trace.TraceWarning(
                    "!!WARNING!! : No manual indexes where provided and not asked to scan any assemblies for indexes. That's fine .. but we're just telling you that this -might- be a problem.");
            }
        }

        private static void WaitForStaleIndexesToComplete(this IDocumentStore documentStore)
        {
            while (documentStore.DatabaseCommands.GetStatistics().StaleIndexes.Length != 0)
            {
                Thread.Sleep(50);
                Trace.TraceInformation("Waiting for indexes to stop being stale ...");
            }
        }

        private static async Task CreateSeedDataAsync(IEnumerable<IEnumerable> seedData, IDocumentStore documentStore)
        {
            if (documentStore == null)
            {
                throw new ArgumentNullException("documentStore");
            }

            if (seedData == null)
            {
                throw new ArgumentNullException("seedData");
            }

            using (IAsyncDocumentSession asyncDocumentSession = documentStore.OpenAsyncSession())
            {
                // First, check to make sure we don't have any data.
                if (documentStore.DatabaseCommands.GetStatistics().CountOfDocuments > 0)
                {
                    // We have documents, so nothing to worry about :)
                    return;
                }

                // Store each collection of fake seeded data.
                Trace.TraceInformation("Seeding Data :-");
                foreach (IEnumerable collection in seedData)
                {
                    var count = 0;
                    var entityName = "Unknown";

                    foreach (object entity in collection)
                    {
                        count++;
                        if (count <= 1)
                        {
                            entityName = entity.GetType().ToString();
                        }
                        await asyncDocumentSession.StoreAsync(entity);
                    }
                    Trace.TraceInformation("   --- {0} {1}", count, entityName);
                }
                Trace.TraceInformation("   Done!");

                // Commit this transaction.
                await asyncDocumentSession.SaveChangesAsync();

                // Make sure all our indexes are not stale.
                documentStore.WaitForStaleIndexesToComplete();
            }
        }

        private static void ReportOnInitializedStatistics(IDocumentStore documentStore)
        {
            if (documentStore == null)
            {
                throw new ArgumentNullException("documentStore");
            }

            var statistics = documentStore.DatabaseCommands.GetStatistics();

            Trace.TraceInformation("+-------------------------------------------------------------+");
            Trace.TraceInformation("+  RavenDb Initialization Report                              +");
            Trace.TraceInformation("+  ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^                              +");
            Trace.TraceInformation("+  o) Tenant Id: {0}         +",
                statistics.DatabaseId);
            Trace.TraceInformation("+  o) Number of Documents: {0, -35}+",
                statistics.CountOfDocuments);
            Trace.TraceInformation("+  o) Number of Indexes: {0,-37}+",
                statistics.CountOfIndexes);
            Trace.TraceInformation("+  o) Number of ~Stale Indexes: {0,-30}+",
                statistics.StaleIndexes == null
                    ? 0
                    : statistics.StaleIndexes.Count());

            // TODO: Result transformer report.
            //       This was added to RavenDb -after- this current version, so we need to wait until
            //       the assembly is updated and released to NuGet.
            //Trace.TraceInformation("+  o) Number of Result Transformers: {0,-30}+",
            //    statistics.ResultTransformers == null
            //        ? 0
            //        : statistics.ResultTransformers.Count());

            Trace.TraceInformation("+-------------------------------------------------------------+");
        }
    }
}