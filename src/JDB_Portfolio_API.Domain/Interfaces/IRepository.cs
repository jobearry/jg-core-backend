using System;
using System.Collections.Generic;
using System.Text;

namespace JDB_Portfolio_API.Domain.Interfaces;
public interface IRepository<T> where T : class
{
    Task<List<T?>> GetAllAsync();
    Task<int> GetCountAsync();
    Task<T?> GetByIdAsync(int id);
    //Task AddNewAsync(T entity);
}