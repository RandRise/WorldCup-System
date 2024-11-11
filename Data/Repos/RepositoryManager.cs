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
        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}


