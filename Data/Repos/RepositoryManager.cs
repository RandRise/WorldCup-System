using Data.Context;
using Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repos
{
    public class RepositoryManager : IRepositoryManager
    {
        private readonly ApplicationDbContext _context;
        private IRepository<User>? _userRepository;
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
        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}


