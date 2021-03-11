using System;
using System.Configuration;

using NLog;

namespace RVCActivityLogger.Services
{
    public class AppConfigService
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public bool EnableLogRefresh { get; private set; } = false;
        public int LogRefreshRate { get; private set; } = 10000;
        public int LogRowCount { get; private set; } = 50;

        public bool ReadSettings()
        {
            try
            {
                EnableLogRefresh = Convert.ToBoolean(ConfigurationManager.AppSettings["EnableLogRefresh"]);
                LogRefreshRate = Convert.ToInt32(ConfigurationManager.AppSettings["LogRefreshRate"]) * 1000;

                LogRowCount = Convert.ToInt32(ConfigurationManager.AppSettings["LogRowCount"]);

                return true;
            }
            catch (Exception ex) { logger.Error(ex, "AppConfigService <ReadSettings> method."); return false; }
        }

        public static void SetSettingValue(string settingName, string value)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;

                settings[settingName].Value = value;

                configFile.Save(ConfigurationSaveMode.Modified);

                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception ex) { logger.Error(ex, "AppConfigService <SetSettingValue> method."); }
        }

    }
}
