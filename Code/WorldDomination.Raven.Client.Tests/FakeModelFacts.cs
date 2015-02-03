using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Raven.Client;
using WorldDomination.Raven.Tests.Helpers;
using Xunit;

namespace WorldDomination.Raven.Client.Tests
{
    public class FakeModelFacts : RavenDbTestBase
    {
        [Fact]
        public async Task GivenAFakeModel_StoreAsync_StoresTheModel()
        {
            // Arrange.
            var fakeModel1 = new FakeModel
            {
                Name = "Anabel",
                Age = 25,
                CreatedOn = DateTime.UtcNow
            };

            DataToBeSeeded = new List<IEnumerable> {new[] {fakeModel1}};
            await CreateDocumentStoreAsync();

            // Now fake data to store -after- the default data is store
            await AsyncDocumentSession.StoreAsync(new FakeModel
            {
                Name = "Lily",
                Age = 5,
                CreatedOn = DateTime.UtcNow
            });

            await AsyncDocumentSession.StoreAsync(new FakeModel
            {
                Name = "Jett",
                Age = 7,
                CreatedOn = DateTime.UtcNow
            });

            // Act.
            // Note: First save.
            await AsyncDocumentSession.SaveChangesAsync();

            // 2nd save (to see if the Id's are in order -- ie. reusing the same client).
            await AsyncDocumentSession.StoreAsync(new FakeModel
            {
                Name = "Jenson",
                Age = 3,
                CreatedOn = DateTime.UtcNow
            });
            await AsyncDocumentSession.SaveChangesAsync();

            // 3rd save with a different Session.
            await AsyncDocumentSessions("pewpew").StoreAsync(new FakeModel
            {
                Name = "PewPew",
                Age = 69,
                CreatedOn = DateTime.UtcNow
            });
            await AsyncDocumentSessions("pewpew").SaveChangesAsync();


            // Assert.
            var models = await AsyncDocumentSessions("hi").Query<FakeModel>().ToListAsync();
            Assert.Equal("FakeModels/1", models[0].Id);
            Assert.Equal("Anabel", models[0].Name);
            Assert.Equal("FakeModels/2", models[1].Id);
            Assert.Equal("Lily", models[1].Name);
            Assert.Equal("FakeModels/3", models[2].Id);
            Assert.Equal("Jett", models[2].Name);
            Assert.Equal("FakeModels/4", models[3].Id);
            Assert.Equal("Jenson", models[3].Name);
            Assert.Equal("FakeModels/5", models[4].Id);
            Assert.Equal("PewPew", models[4].Name);
        }
    }
}