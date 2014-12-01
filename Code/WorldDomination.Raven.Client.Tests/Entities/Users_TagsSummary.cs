using System.Linq;
using Raven.Client.Indexes;

namespace WorldDomination.Raven.Client.Tests.Entities
{
    public class Users_TagsSummary : AbstractIndexCreationTask<User, Users_TagsSummary.ReduceResult>
    {
        public Users_TagsSummary()
        {
            Map = users => from user in users
                from tag in user.Tags
                select new
                {
                    TagName = tag,
                    Count = 1
                };

            Reduce = results => from result in results
                group result by result.TagName
                into g
                select new ReduceResult
                {
                    TagName = g.Key,
                    Count = g.Sum(x => x.Count)
                };
        }

        public class ReduceResult
        {
            public string TagName { get; set; }
            public int Count { get; set; }
        }
    }
}