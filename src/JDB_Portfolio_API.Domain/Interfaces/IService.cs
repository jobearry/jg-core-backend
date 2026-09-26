using System;
using System.Collections.Generic;
using System.Text;

namespace JDB_Portfolio_API.Domain.Interfaces;

public interface IService<T> where T : class
{
    Task<List<T?>> GetAllAsync();
}
