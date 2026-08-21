using System;
using System.Collections.Generic;
using System.Text;

namespace JDB_Portfolio_API.Domain.Interfaces;

public interface IMapper<TSource, TDestinationRead, TDestinationCreate>
{
    TDestinationRead MapToDto(TSource source);
    TSource MapToEntity(TDestinationCreate destination);
    // void UpdateEntity(TSource entity, TDestinationUpdate destination);
}