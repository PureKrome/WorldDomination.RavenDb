using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Raven.Client;
using Raven.Client.Document;
using WorldDomination.Raven.Client.Tests.Entities;
using WorldDomination.Raven.Tests.Helpers;
using Xunit;

namespace WorldDomination.Raven.Client.Tests
{
    public class DocumentStoreExtensionTests
    {
        public class InitializeWithDefaultsFacts : RavenDbTestBase
        {
            [Fact]
            public async Task GivenNoSeedDataAndNoIndexes_InitializeWithDefaults_WorksWithNoDataAndNoIndexes()
            {
                // Arrange.
                await CreateDocumentStoreAsync();

                // Act.
                var documentSession = AsyncDocumentSession;

                // Assert.
                Assert.NotNull(documentSession);
                Assert.NotNull(DocumentStore);
                Assert.Equal(0, DocumentStore.DatabaseCommands.GetStatistics().CountOfDocuments);
                Assert.Equal(0, DocumentStore.DatabaseCommands.GetStatistics().CountOfIndexes);
            }

            [Fact]
            public async Task GivenSomeSeedDataAndNoIndexes_InitializeWithDefaults_WorksWithSomeDataAndNoIndexes()
            {
                // Arrange.
                DataToBeSeeded = User.CreateFakeData().ToList();
                // NOTE: Each collection has an identity counter (it is assumed).
                int numberOfDocuments = DataToBeSeeded
                    .Cast<IList>()
                    .Sum(collection => collection.Count) + DataToBeSeeded.Count();
                await CreateDocumentStoreAsync();

                // Act.
                var documentSession = AsyncDocumentSession;

                // Assert.
                Assert.NotNull(documentSession);
                Assert.NotNull(DocumentStore);
                Assert.Equal(numberOfDocuments,
                    DocumentStore.DatabaseCommands.GetStatistics().CountOfDocuments);
                Assert.Equal(0, DocumentStore.DatabaseCommands.GetStatistics().CountOfIndexes);
            }

            [Fact]
            public async Task GivenSomeSeedDataAndTwoIndexes_InitializeWithDefaults_WorksWithSomeDataAndTwoIndexes()
            {
                // Arrange.
                DataToBeSeeded = User.CreateFakeData().ToList();
                // NOTE: Each collection has an identity counter (it is assumed).
                int numberOfDocuments = DataToBeSeeded.Cast<IList>().Sum(collection => collection.Count) +
                                        DataToBeSeeded.Count();
                IndexesToExecute = new List<Type>
                {
                    typeof (Users_Search),
                    typeof (Users_TagsSummary)
                };
                await CreateDocumentStoreAsync();

                // Act.
                var documentSession = AsyncDocumentSession;

                // Assert.
                Assert.NotNull(documentSession);
                Assert.NotNull(DocumentStore);
                Assert.Equal(numberOfDocuments,
                    DocumentStore.DatabaseCommands.GetStatistics().CountOfDocuments);
                Assert.Equal(IndexesToExecute.Count,
                    DocumentStore.DatabaseCommands.GetStatistics().CountOfIndexes);
            }

            [Fact(
                Skip =
                    "I need to create a new index that compiles but fails during runtime. I can't remember a simple one, off the top of my head :("
                )]
            public void GivenSomeSeedDataAndABadIndex_InitializeWithDefaults_StoresTheDataButFailsToCreateTheIndex()
            {
                // Arrange.

                // Act.

                // Assert.
            }

            [Fact]
            public async Task
                GivenSomeFakeDataWhichWeStoreAndUseTwoSessions_InitializeWithDefaults_StoresTheDataAndUsingTwoSessionsWorks
                ()
            {
                // Arrange.
                DataToBeSeeded = User.CreateFakeData().ToList();
                var user = new User {Name = "Oren Eini", Tags = new[] {"RavenDb", "Hibernating Rhino's"}};
                await CreateDocumentStoreAsync();

                var documentSession = AsyncDocumentSession;
                var documentSession2 = AsyncDocumentSessions("AnotherSession");

                // Act.
                await documentSession.StoreAsync(user);
                await documentSession.SaveChangesAsync();

                // We now have a new user Id. So lets do a fresh load of it.
                var existingUser = await documentSession2.LoadAsync<User>(user.Id);

                // Arrange.
                Assert.NotNull(existingUser);
                Assert.Equal(user.Id, existingUser.Id);
                Assert.Equal(user.Name, existingUser.Name);
            }

            [Fact]
            public async Task GivenSomeIndexToExecuteAfterTheDocumentStoreWasCreated_InitializeWithDefaults_ThrowsAnException()
            {
                // Arrange.
                await CreateDocumentStoreAsync();
                
                // Act and Assert.
                var result = Assert.Throws<InvalidOperationException>(() =>
                    IndexesToExecute =
                        new List<Type> {typeof (Users_Search)});
                Assert.NotNull(result);
                Assert.Equal(
                    "The DocumentStore has already been created and Initialized. As such, changes to the IndexesToExecute list will not be used. Therefore, set this collection BEFORE your first call to a DocumentSession (which in effect creates the DocumentStore if it has been created).",
                    result.Message);
            }

            [Fact]
            public async Task GivenSomeSeedDataAfterTheDocumentStoreWasCreated_InitializeWithDefaults_ThrowsAnException()
            {
                // Arrange.
                await CreateDocumentStoreAsync();

                // Act and Assert.
                var result = Assert.Throws<InvalidOperationException>(() =>
                    DataToBeSeeded =
                        new List<IEnumerable> {User.CreateFakeData()});
                Assert.NotNull(result);
                Assert.Equal(
                    "The DocumentStore has already been created and Initialized. As such, changes to the DataToBeSeeded list will not be used. Therefore, set this collection BEFORE your first call to a DocumentSession (which in effect creates the DocumentStore if it has been created).",
                    result.Message);
            }

            [Fact]
            public async Task GivenADocumentStoreUrlAfterTheDocumentStoreWasCreated_InitializeWithDefaults_ThrowsAnException()
            {
                // Arrange.
                await CreateDocumentStoreAsync();

                // Act and Assert.
                var result = Assert.Throws<InvalidOperationException>(
                    () => ExistingDocumentStoreSettings = new ExistingDocumentStoreSettings("whatever"));
                Assert.NotNull(result);
                Assert.Equal(
                    "The DocumentStore has already been created and Initialized. As such, the ExistingDocumentStoreSettings instance cannot be used. Therefore, set this value BEFORE your first call to a AsyncDocumentSession (which in effect creates the DocumentStore pointing to your desired location).",
                    result.Message);
            }

            [Fact]
            public async Task GivenADocumentConvention_InitializeWithDefaults_Works()
            {
                // Arrange.
                DocumentConvention = new DocumentConvention
                {
                    // Will get overriden.
                    DefaultQueryingConsistency = ConsistencyOptions.None
                };
                await CreateDocumentStoreAsync();

                // Act.
                var documentSession = AsyncDocumentSession;

                // Assert.
                Assert.NotNull(documentSession);
                Assert.NotNull(DocumentStore);
                Assert.Equal(0, DocumentStore.DatabaseCommands.GetStatistics().CountOfDocuments);
                Assert.Equal(0, DocumentStore.DatabaseCommands.GetStatistics().CountOfIndexes);
                Assert.Equal(ConsistencyOptions.AlwaysWaitForNonStaleResultsAsOfLastWrite, DocumentStore.Conventions.DefaultQueryingConsistency);
            }

            [Fact]
            public async Task GivenSomeSeedDataAndNoIndexesButOneResultTransformer_InitializeWithDefaults_WorksWithSomeDataAndNoIndexes()
            {
                // Arrange.
                DataToBeSeeded = User.CreateFakeData().ToList();
                // NOTE: Each collection has an identity counter (it is assumed).
                int numberOfDocuments = DataToBeSeeded
                    .Cast<IList>()
                    .Sum(collection => collection.Count) + DataToBeSeeded.Count();
                
                IndexesToExecute = new List<Type>
                {
                    typeof (User_SearchTransformer)
                };
                
                await CreateDocumentStoreAsync();

                // Act.
                var documentSession = AsyncDocumentSession;

                new User_SearchTransformer().Execute(DocumentStore);

                // Assert.
                Assert.NotNull(documentSession);
                Assert.NotNull(DocumentStore);
                Assert.Equal(numberOfDocuments,
                    DocumentStore.DatabaseCommands.GetStatistics().CountOfDocuments);

                // There's no way to find out how many result tranformers we have.
                Assert.Equal(0,
                    DocumentStore.DatabaseCommands.GetStatistics().CountOfIndexes);
            }
        }
    }
}