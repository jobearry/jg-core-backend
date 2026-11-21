using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Portfolio.Domain.Interfaces;

public interface IRepository<T> where T : class
{
    Task<List<T>> GetAllAsync(CancellationToken cancellationToken);
}
