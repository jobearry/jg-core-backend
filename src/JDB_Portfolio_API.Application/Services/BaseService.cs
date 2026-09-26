using JDB_Portfolio_API.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace JDB_Portfolio_API.Application.Services;

public class BaseService<T> : IService<T> where T : class
{
    public readonly IRepository<T> _repo;
    public BaseService(IRepository<T> repo)
    {
        _repo = repo;
    }

    public virtual async Task<List<T?>> GetAllAsync()
    {
        return await _repo.GetAllAsync();
    }
}