using System;
using Imaj.Core.Entities;

namespace Imaj.Core.Interfaces.Repositories
{
    public interface ICustomerRepository : IRepository<Customer>
    {
        Task<decimal> GetNextIdAsync();
    }
}
