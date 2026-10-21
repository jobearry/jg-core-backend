using System.Collections.Generic;

namespace JDB_Portfolio_API.Domain.Interfaces;
public interface IRepository<T> where T : class
{
    Task<List<T>> GetAllAsync();
}