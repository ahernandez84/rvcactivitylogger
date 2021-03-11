using System;
using System.Configuration;

using NLog;

namespace RVCOfficerLogger.Services
{
    public class AppConfigService
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public int CheckCentralDatabaseTimer { get; private set; } = 5000;
        public bool EnableLogRefresh { get; private set; } = false;
        public int LogRefreshRate { get; private set; } = 10000;
        public int LogRowCount { get; private set; } = 50;

        public bool ReadSettings()
        {
            try
            {
                CheckCentralDatabaseTimer = Convert.ToInt32(ConfigurationManager.AppSettings["CheckCentralDatabaseTimer"]) * 1000;

                EnableLogRefresh = Convert.ToBoolean(ConfigurationManager.AppSettings["EnableLogRefresh"]);
                LogRefreshRate = Convert.ToInt32(ConfigurationManager.AppSettings["LogRefreshRate"]) * 1000;

                LogRowCount = Convert.ToInt32(ConfigurationManager.AppSettings["LogRowCount"]);

                return true;
            }
            catch (Exception ex) { logger.Error(ex, "AppConfigService <ReadSettings> method."); return false; }
        }

    }
}
