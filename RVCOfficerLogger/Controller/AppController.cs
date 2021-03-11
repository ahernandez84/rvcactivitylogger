using System;
using System.Collections.Generic;
using System.IO;

using Connect = HelperClasses.Crypto;
using NLog;
using RVCOfficerLogger.Models;
using RVCOfficerLogger.Services;

namespace RVCOfficerLogger.Controller
{
    public class AppController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private SQLService sql;
        private SQLiteService sqlite;
        public bool IsCentralDatabaseOffline { get; set; } = false;

        private int logRowCount = 50;
        public int Page { get; set; } = 0;
        public int Pages { get; set; } = 0;

        public int Initialize(int logRowCount = 50)
        {
            try
            {
                this.logRowCount = logRowCount;

#if DEBUG
            var connectionString = $@"Data Source=(local)\sqlexpress;Initial Catalog=ActivityLogger;Integrated Security=True;";
#else
                var connectionString = Connect.Unprotect(@"C:\SchneiderElectric\AppData\RVCActivityLogger_connection.connect");
#endif
                logger.Debug($"ConnectionString:  {connectionString}");

                sql = new SQLService(connectionString, logRowCount);

                var isSqlOnline = -1;
                var isSqliteOnline = -2;
                var isLocalDBReady = false;
                var isSqlitePopulated = false;

                if (CreateLocalDBFolder())
                {
                    sqlite = new SQLiteService($@"Data Source={Environment.CurrentDirectory}\LocalDB\RVCOfficerLogger.sqlite; Version=3;", logRowCount);
                    
                    isLocalDBReady = CreateLocalDBTables();
                    isSqliteOnline = sqlite.CheckConnection();
                    isSqlitePopulated = sqlite.CheckForData();
                }

                isSqlOnline = sql.CheckConnection();
                if (isSqlOnline == -1) IsCentralDatabaseOffline = true;

                return isSqlOnline + isSqliteOnline + (isLocalDBReady ? 4 : 0) + (isSqlitePopulated ? 8 : 0);
            }
            catch (Exception ex) { logger.Error(ex, "AppController <Initialize> method."); return -2; }
        }

        public bool CheckStatusOfCentralDB() => IsCentralDatabaseOffline = sql.CheckConnection() == -1;

        #region local database
        public bool CreateLocalDBFolder()
        {
            try
            {
                if (!Directory.Exists($@"{ Environment.CurrentDirectory}\LocalDB"))
                {
                    logger.Info("Creating local database folder.");

                    Directory.CreateDirectory($@"{ Environment.CurrentDirectory}\LocalDB");
                }

                return true;
            }
            catch (Exception ex) { logger.Error(ex, "AppController <CreateLocalDBFolder> mehtod."); return false; }
        }

        public bool CreateLocalDBTables()
        {
            try
            {
                var r1 = sqlite.CreateEmployeeTable();
                var r2 = sqlite.CreateLocationTable();
                var r3 = sqlite.CreateIncidentTypeTable();
                var r4 = sqlite.CreateActivityLogTable();

                if (r1 && r2 && r3 && r4)
                    logger.Info("Local database tables exist.");
                else
                    logger.Info("Failed to verify or create local database tables.");

                return r1 && r2 && r3 && r4;
            }
            catch (Exception ex) { logger.Error(ex, "AppController <CreateLocalDBTables> mehtod."); return false; }
        }

        public bool CreateOfficerInLocalDB(Employee officer) => sqlite.CreateEmployee(officer);

        public bool CreateLocationInLocalDB(LocationItem location) => sqlite.CreateLocation(location);

        public bool CreateIncidentTypeInLocalDB(IncidentItem incident) => sqlite.CreateIncidentType(incident);

        public void DeleteLocalTables()
        {
            sqlite.DeleteEmployee();
            sqlite.DeleteLocation();
            sqlite.DeleteIncidentType();
        }

        public void InsertOrUpdateLogsToCentralDatabase()
        {
            // insert new logs
            sqlite.GetLocalLogsToTransfer().ForEach(l =>
            {
                if (sql.CreateLog(l) == 0) 
                    sqlite.UpdateSyncedLogs(l);
            });

            // update existing logs
            sqlite.GetLocalLogsToTransfer(false).ForEach(l =>
            {
                if (sql.EditLog(l))
                    sqlite.UpdateUpdatedLogs(l);
            });
        }
        #endregion

        #region manage log
        public List<Log> GetLogs(string filter = "", DateTime? start = null, DateTime? end = null)
        {
            logger.Info("Calling GetLogs()");
            logger.Debug($"Is central database offline?  {IsCentralDatabaseOffline}");

            var offset = 0;
            if (Page >= 0 && Page <= Pages)
            {
                offset += (Page * logRowCount);
            }

            return start.HasValue ? (IsCentralDatabaseOffline ? sqlite.GetLogs(filter, start, end, offset) : sql.GetLogs(filter, start, end, offset)) : (IsCentralDatabaseOffline ? sqlite.GetLogs(filter, offset: offset) : sql.GetLogs(filter, offset: offset));
        }

        public int GetLogPagingCounts(string filter = "", DateTime? start = null, DateTime? end = null)
        {
            logger.Info("Calling GetLogPagingCounts()");

            Page = 0;

            var rowCount = start.HasValue ? (IsCentralDatabaseOffline? sqlite.GetLogPagingCounts(filter, start, end) : sql.GetLogPagingCounts(filter, start, end)) : (IsCentralDatabaseOffline ? sqlite.GetLogPagingCounts(filter) : sql.GetLogPagingCounts(filter));

            Pages = (int)Math.Ceiling((double)rowCount / logRowCount);

            return Pages;
        }

        public bool CreateLog(Log log)
        {
            logger.Info("Calling CreateLog()");
            logger.Debug($"Is central database offline?  {IsCentralDatabaseOffline}");

            bool saveResult;
            if (IsCentralDatabaseOffline)
            {
                saveResult = sqlite.CreateLog(log) == true;
            }
            else
            {
                if (sql.CreateLog(log) != 0)
                {
                    saveResult = sqlite.CreateLog(log) == true;
                }
                else
                {
                    saveResult = true;
                }
            }

            return saveResult;
        }

        public bool EditLog(Log log)
        {
            logger.Info("Calling EditLogs()");
            logger.Debug($"Is central database offline?  {IsCentralDatabaseOffline}");

            return IsCentralDatabaseOffline ? sqlite.EditLog(log) : sql.EditLog(log);
        }
        #endregion

        #region manage officers
        public List<Employee> GetEmployees()
        {
            logger.Info("Calling GetEmployees()");
            logger.Debug($"Is central database offline?  {IsCentralDatabaseOffline}");

            return IsCentralDatabaseOffline ? sqlite.GetEmployees() : sql.GetEmployees();
        }

        public bool UpdateEmployeePassword(Employee employee) => sql.UpdateEmployeePassword(employee);

        #endregion

        #region manage locations
        public List<LocationItem> GetLocations()
        {
            logger.Info("Calling GetLocations()");
            logger.Debug($"Is central database offline?  {IsCentralDatabaseOffline}");

            return IsCentralDatabaseOffline ? sqlite.GetLocations() : sql.GetLocations();
        }
        #endregion

        #region manage incident types
        public List<IncidentItem> GetIncidentTypes()
        {
            logger.Info("Calling GetIncidnetTypes()");
            logger.Debug($"Is central database offline?  {IsCentralDatabaseOffline}");

            return IsCentralDatabaseOffline ? sqlite.GetIncidentTypes() : sql.GetIncidentTypes();
        }
        #endregion

    }
}
