using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Imaj.Core.Entities;
using Imaj.Core.Interfaces.Repositories;
using Imaj.Data.Context;

namespace Imaj.Data.Repositories
{
    /// <summary>
    /// Unit of Work pattern implementasyonu.
    /// Transaction yönetimi ve repository caching sağlar.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ImajDbContext _context;
        
        // Repository cache - her tip için tek instance
        private readonly Dictionary<Type, object> _repositories = new();
        
        // Dispose edilip edilmediğini takip et
        private bool _disposed;

        public UnitOfWork(ImajDbContext context)
        {
            _context = context;
        }

        public async Task<int> CommitAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        /// <summary>
        /// Generic repository döndürür.
        /// Aynı tip için tekrar çağrıldığında cache'ten döner.
        /// </summary>
        public IRepository<T> Repository<T>() where T : BaseEntity
        {
            var type = typeof(T);
            
            // Cache'te varsa oradan dön
            if (_repositories.TryGetValue(type, out var repository))
            {
                return (IRepository<T>)repository;
            }
            
            // Yoksa yeni oluştur ve cache'e ekle
            var newRepository = new Repository<T>(_context);
            _repositories[type] = newRepository;
            return newRepository;
        }

        /// <summary>
        /// IDisposable pattern implementasyonu.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose metodu.
        /// Türetilmiş sınıflar bu metodu override edebilir.
        /// </summary>
        /// <param name="disposing">Managed kaynaklar da temizlensin mi</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Managed kaynakları temizle
                    _context.Dispose();
                }
                
                _disposed = true;
            }
        }
    }
}
