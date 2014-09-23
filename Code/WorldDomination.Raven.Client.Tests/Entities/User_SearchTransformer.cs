using System.Linq;
using Raven.Client.Indexes;

namespace WorldDomination.Raven.Client.Tests.Entities
{
    public class User_SearchTransformer : AbstractTransformerCreationTask<User>
    {
        public User_SearchTransformer()
        {
            TransformResults = users => from user in users
                select new
                {
                    user.Name,
                    user.Tags.Count
                };
        }
    }
}