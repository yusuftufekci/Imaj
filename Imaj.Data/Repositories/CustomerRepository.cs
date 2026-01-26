using Imaj.Core.Entities;
using Imaj.Core.Interfaces.Repositories;
using Imaj.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Imaj.Data.Repositories
{
    public class CustomerRepository : Repository<Customer>, ICustomerRepository
    {
        public CustomerRepository(ImajDbContext context) : base(context)
        {
        }

        public async Task<decimal> GetNextIdAsync()
        {
            var maxId = await _context.Customers.MaxAsync(c => (decimal?)c.Id);
            return (maxId ?? 0) + 1;
        }
    }
}
