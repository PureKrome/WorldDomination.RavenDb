using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Raven.Client.Indexes;

namespace WorldDomination.Raven.Client.Tests.Entities
{
    public class User
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public IList<string> Tags { get; set; }

        public static IEnumerable<IEnumerable> CreateFakeData()
        {
            var fakeUsers = new List<User>
                            {
                                new User
                                {
                                    Name = "Leah Culver",
                                    Tags = new List<string> {"pounce", "grove.io"}
                                },
                                new User
                                {
                                    Name = "Kristen Bell",
                                    Tags =
                                        new List<string> {"veronica mars", "slave leia", "star wars", "babe"}
                                },
                                new User
                                {
                                    Name = "Ada Lovelace",
                                    Tags = new List<string> {"first computer programmer EVA", "countess"}
                                },
                                new User
                                {
                                    Name = "Han Solo",
                                    Tags = new List<string> {"star wars", "scoundrel", "stud"}
                                },
                            };

            return new List<IEnumerable> { fakeUsers };
        }
    }
}