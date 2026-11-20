using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JDB_Portfolio_API.ExternalService.Github.Settings
{
    public class GithubAPISettings
    {
        public string Token { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.github.com";
    }
}