using Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repos
{
    public interface IRepositoryManager
    {
        IRepository<User> User { get; }
        IRepository<Country> Country { get; }
        IRepository<City> City { get; }
        IRepository<Stadium> Stadium { get; }
        IRepository<WorldCup> WorldCup { get; }
        IRepository<Group> Group { get; }
        IRepository<Team> Team { get; }
        IRepository<Match> Match { get; }
        Task SaveAsync();
    }
}
