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
        IRepository<Coach> Coach { get; }
        IRepository<Player> Player { get; }
        IRepository<PlayerPosition> PlayerPosition { get; }
        IRepository<Match> Match { get; }
        IRepository<TeamStats> TeamStats { get; }
        IRepository<Goal> Goal { get; }
        IRepository<Card> Card { get; }
        IRepository<Bet> Bet { get; }
        IRepository<BetResult> BetResult { get; }
        IRepository<Company> Company { get; }
        Task SaveAsync();
    }
}
