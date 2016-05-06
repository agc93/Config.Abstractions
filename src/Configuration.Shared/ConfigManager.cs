using System;
using System.Collections.Generic;
using System.Text;

namespace Configuration.Abstractions
{
    public abstract class ConfigManager : IConfigManager
    {
        protected ConfigManager(IConfigSource settingsSource, IConfigSource secretSource = null)
        {
            Settings = settingsSource;
            Secrets = secretSource ?? settingsSource;
        }

        public IConfigSource Settings { get; set; }
        public IConfigSource Secrets { get; set; }
    }
}
