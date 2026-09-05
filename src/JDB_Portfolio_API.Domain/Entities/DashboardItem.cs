using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JDB_Portfolio_API.Domain.Entities;
public class DashboardItem
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle {get;set;} = string.Empty;
    public string Icon {get; set;} = string.Empty;
    public int Count {get; set;}
    public string Description {get; set;} = string.Empty;
}