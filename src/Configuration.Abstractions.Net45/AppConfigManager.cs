using System;
using System.Configuration;
using System.Linq;

namespace Configuration.Abstractions
{
    public class AppConfigManager : ConfigManager
    {
        public AppConfigManager() : base(new AppConfigSource())
        {
            
        }
    }

    public class AppConfigSource : IConfigSource
    {
        public bool CanUpdate => true;
        public T Get<T>(string key) where T : class
        {
            try
            {
                return Get(key) as T;
            }
            catch
            {
                throw new NotImplementedException("App.config files do not support non-string values");
            }
            
        }

        public string Get(string key)
        {
            return ConfigurationManager.AppSettings.Get(key);
        }

        public void Save(string key, string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (ConfigurationManager.AppSettings.AllKeys.Contains(key))
            {
                //update
                ConfigurationManager.AppSettings.Set(key, value);
            }
            else
            {
                ConfigurationManager.AppSettings.Add(key, value);
            }
        }
    }
}
