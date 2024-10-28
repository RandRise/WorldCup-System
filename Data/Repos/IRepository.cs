using System.Linq.Expressions;

namespace Data.Repos
{
    public interface IRepository<T> where T : class
    {
        IQueryable<T> GetAllAsync();
        Task<T> GetByIdAsync(int id);
        void Create(T entity);
        void Update(T entity);
        void Delete(T entity);
        public IQueryable<T> Find(Expression<Func<T, bool>> predicate);
        
    }
}
