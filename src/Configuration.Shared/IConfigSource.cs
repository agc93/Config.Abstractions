using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Configuration.Abstractions
{
    public interface IConfigSource
    {
        bool CanUpdate { get; }
        T Get<T>(string key) where T : class;
        string Get(string key);
        void Save(string key, string value);
    }
}
