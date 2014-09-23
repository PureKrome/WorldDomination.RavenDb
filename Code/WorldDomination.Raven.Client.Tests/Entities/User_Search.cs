using System.Linq;
using Raven.Client.Indexes;

namespace WorldDomination.Raven.Client.Tests.Entities
{
    public class Users_Search : AbstractIndexCreationTask<User>
    {
        public Users_Search()
        {
            Map = users => from user in users
                select new
                {
                    user.Name
                };
        }
    }
}