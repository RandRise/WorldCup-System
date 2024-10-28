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
        Task SaveAsync();
    }
}
