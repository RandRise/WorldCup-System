using Data.Context;
using Data.Entities;
namespace Data.Repos
{
    public class RepositoryManager : IRepositoryManager
    {
        private readonly ApplicationDbContext _context;
        private IRepository<User>? _userRepository;
        private IRepository<Country>? _countryRepository;
        private IRepository<City>? _cityRepository;
        private IRepository<Stadium> _stadiumRepository;
        private IRepository<WorldCup> _worldCupRepository;
        private IRepository<Group> _groupRepository;
        private IRepository<Team>? _teamRepository;
        private IRepository<Coach>? _coachRepository;
        private IRepository<Player>? _playerRepository;
        private IRepository<PlayerPosition>? _playerPositionRepository;
        private IRepository<Match>? _matchRepository;
        private IRepository<TeamStats>? _teamStatsRepository;
        private IRepository<Goal>? _goalRepository;
        private IRepository<Card>? _cardRepository;
        private IRepository<Bet>? _betRepository;
        private IRepository<BetResult>? _betResultRepository;
        private IRepository<Company>? _companyRepository;
        public RepositoryManager(ApplicationDbContext context)
        {
            _context = context;
        }
        public IRepository<User> User
        {
            get
            {
                if (_userRepository == null)
                    _userRepository = new Repository<User>(_context);
                return _userRepository;
            }


        }

        public IRepository<Country> Country
        {
            get
            {
                if (_countryRepository == null)
                    _countryRepository = new Repository<Country>(_context);
                return _countryRepository;
            }

        }

        public IRepository<City> City
        {
            get
            {
                if (_cityRepository == null)
                    _cityRepository = new Repository<City>(_context);
                return _cityRepository;
            }
        }
        public IRepository<Stadium> Stadium
        {
            get
            {
                if (_stadiumRepository == null)
                    _stadiumRepository = new Repository<Stadium>(_context);
                return _stadiumRepository;
            }
        }
        public IRepository<WorldCup> WorldCup
        {
            get
            {
                if (_worldCupRepository == null)
                    _worldCupRepository = new Repository<WorldCup>(_context);
                return _worldCupRepository;
            }
        }

        public IRepository<Group> Group
        {
            get
            {
                if (_groupRepository == null)
                    _groupRepository = new Repository<Group>(_context);
                return _groupRepository;
            }
        }

        public IRepository<Team> Team
        {
            get
            {
                if (_teamRepository == null)
                    _teamRepository = new Repository<Team>(_context);
                return _teamRepository;
            }
        }

        public IRepository<Coach> Coach
        {
            get
            {
                if (_coachRepository == null)
                    _coachRepository = new Repository<Coach>(_context);
                return _coachRepository;
            }
        }

        public IRepository<Player> Player
        {
            get
            {
                if (_playerRepository == null)
                    _playerRepository = new Repository<Player>(_context);
                return _playerRepository;
            }
        }

        public IRepository<PlayerPosition> PlayerPosition
        {
            get
            {
                if (_playerPositionRepository == null)
                    _playerPositionRepository = new Repository<PlayerPosition>(_context);
                return _playerPositionRepository;
            }
        }

        public IRepository<Match> Match
        {
            get
            {
                if (_matchRepository == null)
                    _matchRepository = new Repository<Match>(_context);
                return _matchRepository;
            }
        }

        public IRepository<TeamStats> TeamStats
        {
            get
            {
                if (_teamStatsRepository == null)
                    _teamStatsRepository = new Repository<TeamStats>(_context);
                return _teamStatsRepository;
            }
        }

        public IRepository<Goal> Goal
        {
            get
            {
                if (_goalRepository == null)
                    _goalRepository = new Repository<Goal>(_context);
                return _goalRepository;
            }
        }

        public IRepository<Card> Card
        {
            get
            {
                if (_cardRepository == null)
                    _cardRepository = new Repository<Card>(_context);
                return _cardRepository;
            }
        }

        public IRepository<Bet> Bet
        {
            get
            {
                if (_betRepository == null)
                    _betRepository = new Repository<Bet>(_context);
                return _betRepository;
            }
        }

        public IRepository<BetResult> BetResult
        {
            get
            {
                if (_betResultRepository == null)
                    _betResultRepository = new Repository<BetResult>(_context);
                return _betResultRepository;
            }
        }

        public IRepository<Company> Company
        {
            get
            {
                if (_companyRepository == null)
                    _companyRepository = new Repository<Company>(_context);
                return _companyRepository;
            }
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}


