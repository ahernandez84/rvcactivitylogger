using System;
using System.Collections.Generic;

using Connect = HelperClasses.Crypto;
using NLog;
using RVCActivityLogger.Models;
using RVCActivityLogger.Services;
using System.Data;

namespace RVCActivityLogger.Controller
{
    public class AppController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private SQLService sql;

        public int LogRowCount { get; set; } = 50;
        public int Page { get; set; } = 0;
        public int Pages { get; set; } = 0;

        public void Initialize(int logRowCount = 50)
        {
            this.LogRowCount = logRowCount;

#if DEBUG
            var connectionString = $@"Data Source=(local)\sqlexpress;Initial Catalog=ActivityLogger;Integrated Security=True;";
#else
            var connectionString = Connect.Unprotect(@"C:\SchneiderElectric\AppData\RVCActivityLogger_connection.connect");
#endif

            logger.Debug($"ConnectionString:  {connectionString}");

            sql = new SQLService(connectionString, logRowCount);
        }

        #region manage logs
        public List<Log> GetLogs(string filter = "", DateTime? start = null, DateTime? end = null, bool logMethodCall = true)
        {
            if (logMethodCall) logger.Info("Calling GetLogs()");

            var offset = 0;
            if (Page >= 0 && Page <= Pages)
            {
                offset += (Page * LogRowCount);
                //Page++;
            }

            return start.HasValue ? sql.GetLogs(filter, start, end, offset) : sql.GetLogs(filter, null, null, offset);
        }

        public int GetLogPagingCounts(string filter = "", DateTime? start = null, DateTime? end = null, bool logMethodCall = true)
        {
            if (logMethodCall) logger.Info("Calling GetLogPagingCounts()");

            Page = 0;

            var rowCount = start.HasValue ? sql.GetLogPagingCounts(filter, start, end) : sql.GetLogPagingCounts(filter, null, null);

            Pages = (int) Math.Ceiling((double)rowCount / LogRowCount);

            return Pages;
        }

        public bool EditLog(Log log)
        {
            logger.Info("Calling EditLogs()");

            return sql.EditLog(log);
        }

        public void SetLogRowCount(int logRowCount)
        {
            this.LogRowCount = LogRowCount;
            sql.SetLogRowCount(logRowCount);
        }

        public DataTable GetLogsForReport(string filter = "", DateTime? start = null, DateTime? end = null)
        {
            logger.Info("Calling GetLogsForReport()");

            return start.HasValue ? sql.GetLogsForReport(filter, start, end) : sql.GetLogsForReport(filter, null, null);
        }
        #endregion

        #region manage employees
        public List<Employee> GetEmployees()
        {
            logger.Info("Calling GetEmployees()");

            return sql.GetEmployees();
        }

        public bool CreateEmployee(Employee officer)
        {
            logger.Info("Calling CreateEmployee()");

            return sql.CreateEmployee(officer);
        }

        public bool UpdateEmployee(Employee employee, string originalEmpId)
        {
            logger.Info("Calling UpdateEmployee()");

            return sql.UpdateEmployee(employee, originalEmpId);
        }

        public bool DisableEmployee(int employeeId)
        {
            logger.Info("Calling DisableEmployee()");

            return sql.DisableEmployee(employeeId);
        }
        #endregion

        #region manage locations
                public List<LocationItem> GetLocations()
                {
                    logger.Info("Calling GetLocations()");

                    return sql.GetLocations();
                }

                public bool CreateLocation(LocationItem location)
                {
                    logger.Info("Calling CreateLocation()");

                    return sql.CreateLocation(location);
                }

                public bool UpdateLocation(LocationItem location, Guid rowId)
                {
                    logger.Info("Calling UpdateLocation()");

                    return sql.UpdateLocation(location, rowId);
                }

                public bool DisableLocation(Guid rowId)
                {
                    logger.Info("Calling DisableLocation()");

                    return sql.DisableLocation(rowId);
                }
        #endregion

        #region manage incident types
                public List<IncidentItem> GetIncidentTypes()
                {
                    logger.Info("Calling GetIncidentTypes()");

                    return sql.GetIncidentTypes();
                }

                public bool CreateIncidentType(IncidentItem incident)
                {
                    logger.Info("Calling CreateIncidentType()");

                    return sql.CreateIncidentType(incident);
                }

                public bool UpdateIncidentType(IncidentItem incident, Guid rowId)
                {
                    logger.Info("Calling UpdateIncidentType()");

                    return sql.UpdateIncidentType(incident, rowId);
                }

                public bool DisableIncidentType(Guid rowId)
                {
                    logger.Info("Calling DisableIncidentType()");

                    return sql.DisableIncidentType(rowId);
                }
        #endregion

    }
}
