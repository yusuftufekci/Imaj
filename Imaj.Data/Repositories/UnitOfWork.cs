using System;
using System.Threading.Tasks;
using Imaj.Core.Entities;
using Imaj.Core.Interfaces.Repositories;
using Imaj.Data.Context;

namespace Imaj.Data.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ImajDbContext _context;

        public UnitOfWork(ImajDbContext context)
        {
            _context = context;
        }

        public async Task<int> CommitAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        public IRepository<T> Repository<T>() where T : BaseEntity
        {
            return new Repository<T>(_context);
        }
    }
}
