using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Raven.Client;
using WorldDomination.Raven.Client.Tests.Entities;
using WorldDomination.Raven.Tests.Helpers;
using Xunit;

namespace WorldDomination.Raven.Client.Tests
{
    // ReSharper disable InconsistentNaming

    public class DocumentStoreExtensionTests
    {
        public class InitializeWithDefaultsFacts : RavenDbTestBase
        {
            [Fact]
            public void GivenNoSeedDataAndNoIndexes_InitializeWithDefaults_WorksWithNoDataAndNoIndexes()
            {
                // Arrange.

                // Act.
                IDocumentSession documentSession = DocumentSession;

                // Assert.
                Assert.NotNull(documentSession);
                Assert.NotNull(documentSession.Advanced.DocumentStore);
                Assert.Equal(0, documentSession.Advanced.DocumentStore.DatabaseCommands.GetStatistics().CountOfDocuments);
                Assert.Equal(0, documentSession.Advanced.DocumentStore.DatabaseCommands.GetStatistics().CountOfIndexes);
            }

            [Fact]
            public void GivenSomeSeedDataAndNoIndexes_InitializeWithDefaults_WorksWithSomeDataAndNoIndexes()
            {
                // Arrange.
                DataToBeSeeded = User.CreateFakeData().ToList();
                // NOTE: Each collection has an identity counter (it is assumed).
                int numberOfDocuments = DataToBeSeeded.Cast<IList>().Sum(collection => collection.Count) +
                                        DataToBeSeeded.Count();

                // Act.
                IDocumentSession documentSession = DocumentSession;

                // Assert.
                Assert.NotNull(documentSession);
                Assert.NotNull(documentSession.Advanced.DocumentStore);
                Assert.Equal(numberOfDocuments,
                             documentSession.Advanced.DocumentStore.DatabaseCommands.GetStatistics().CountOfDocuments);
                Assert.Equal(0, documentSession.Advanced.DocumentStore.DatabaseCommands.GetStatistics().CountOfIndexes);
            }

            [Fact]
            public void GivenSomeSeedDataAndTwoIndexes_InitializeWithDefaults_WorksWithSomeDataAndTwoIndexes()
            {
                // Arrange.
                DataToBeSeeded = User.CreateFakeData().ToList();
                // NOTE: Each collection has an identity counter (it is assumed).
                int numberOfDocuments = DataToBeSeeded.Cast<IList>().Sum(collection => collection.Count) +
                                        DataToBeSeeded.Count();
                IndexesToExecute = new List<Type> {typeof (Users_Search), typeof (Users_TagsSummary)};

                // Act.
                IDocumentSession documentSession = DocumentSession;

                // Assert.
                Assert.NotNull(documentSession);
                Assert.NotNull(documentSession.Advanced.DocumentStore);
                Assert.Equal(numberOfDocuments,
                             documentSession.Advanced.DocumentStore.DatabaseCommands.GetStatistics().CountOfDocuments);
                Assert.Equal(IndexesToExecute.Count,
                             documentSession.Advanced.DocumentStore.DatabaseCommands.GetStatistics().CountOfIndexes);
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
            public void
                GivenSomeFakeDataWhichWeStoreAndUseTwoSessions_InitializeWithDefaults_StoresTheDataAndUsingTwoSessionsWorks
                ()
            {
                // Arrange.
                DataToBeSeeded = User.CreateFakeData().ToList();
                var user = new User {Name = "Oren Eini", Tags = new[] {"RavenDb", "Hibernating Rhino's"}};
                IDocumentSession documentSession = DocumentSession;
                IDocumentSession documentSession2 = DocumentSessions("AnotherSession");

                // Act.
                documentSession.Store(user);
                documentSession.SaveChanges();

                // We now have a new user Id. So lets do a fresh load of it.
                var existingUser = documentSession2.Load<User>(user.Id);

                // Arrange.
                Assert.NotNull(existingUser);
                Assert.Equal(user.Id, existingUser.Id);
                Assert.Equal(user.Name, existingUser.Name);
            }
        }
    }

    // ReSharper restore InconsistentNaming
}