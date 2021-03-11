using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NLog;
using RVCOfficerLogger.Models;

namespace RVCOfficerLogger.Services
{
    public class SQLiteService
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private string connectionString = "";
        private int logRowCount;

        public SQLiteService(string connectionString, int logRowCount)
        {
            this.connectionString = connectionString;
            this.logRowCount = logRowCount;
        }

        #region check status and data
        public int CheckConnection()
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                }

                return 2;
            }
            catch (Exception ex) { logger.Error(ex, "SQLiteService <CheckConnection> method."); return -2; }
        }

        public bool CheckForData()
        {
            var query = @"select
	                        EmployeeId,FirstName,LastName,[Status], FirstName + ' ' + LastName as 'FullName',RowId
                        from Employee 
                        order by lastname, firstname ";

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        return reader.HasRows;
                    }
                }

            }
            catch (Exception ex) { logger.Error(ex, "SQLiteService <CheckForData> method."); return false; }
        }
        #endregion

        #region create tables
        public bool CreateEmployeeTable()
        {
            var query = @"CREATE TABLE IF NOT EXISTS Employee (
	                        RowId TEXT NOT NULL,
	                        EmployeeId TEXT NOT NULL,
	                        FirstName TEXT NOT NULL,
	                        LastName TEXT NOT NULL,
	                        [Status] INTEGER NOT NULL,
                            Password TEXT NOT NULL
                        );";

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex) { logger.Error(ex, "SQLiteService <CreateEmployeeTable> method."); return false; }
        }

        public bool CreateIncidentTypeTable()
        {
            var query = @"CREATE TABLE IF NOT EXISTS IncidentType
                        (
	                        RowId TEXT,
	                        Code TEXT NOT NULL,
	                        [Description] TEXT NOT NULL,
	                        [Status] INTEGER NOT NULL
                        )";

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex) { logger.Error(ex, "SQLiteService <CreateIncidentTypeTable> method."); return false; }
        }

        public bool CreateLocationTable()
        {
            var query = @"CREATE TABLE IF NOT EXISTS [Location]
                        (
	                        RowId TEXT NOT NULL,
	                        Code TEXT NOT NULL,
	                        [Description] TEXT NOT NULL,
	                        [Status] INTEGER NOT NULL
                        )";

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex) { logger.Error(ex, "SQLiteService <CreateLocationTable> method."); return false; }
        }

        public bool CreateActivityLogTable()
        {
            var query = @"CREATE TABLE IF NOT EXISTS ActivityLog
                        (
	                        RowId TEXT NOT NULL,
	                        [TimeOfLog] TEXT NOT NULL,
	                        EmployeeId TEXT,
	                        IncidentTypeId TEXT,
	                        LocationId TEXT,
	                        [Description] TEXT NOT NULL,
	                        MoneyTransport TEXT NULL,
	                        StartTime TEXT NULL,
	                        EndTime TEXT NULL,
	                        Vehicle TEXT NULL,
	                        StartMileage INTEGER NULL,
	                        EndMileage INTEGER NULL,
	                        Fuel INTEGER NULL,
                            Synced INTEGER NULL,
                            Updated INTEGER NULL
                        )";

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex) { logger.Error(ex, "SQLiteService <CreateActivityLogTable> method."); return false; }
        }
        #endregion

        #region manage logs
        public bool CreateLog(Log log)
        {
            var query = @"INSERT INTO ActivityLog
                                   (RowId,TimeOfLog,IncidentTypeId,LocationId,[Description],MoneyTransport,StartTime,EndTime,Vehicle,StartMileage,EndMileage,Fuel,EmployeeId,Synced,Updated)
                        VALUES
                                   (@RowId,@TimeOfLog,@IncidentTypeId,@LocationId,@Description,@MoneyTransport,@StartTime,@EndTime,@Vehicle,@StartMileage,@EndMileage,@Fuel,@EmployeeId,@Synced,@Updated)";

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@RowId", Guid.NewGuid().ToString());

                        command.Parameters.AddWithValue("@TimeOfLog", log.IncidentDate);
                        command.Parameters.AddWithValue("@IncidentTypeId", log.IncidentTypeId.ToString());
                        command.Parameters.AddWithValue("@LocationId", log.LocationId.ToString());
                        command.Parameters.AddWithValue("@Description", log.Description);
                        command.Parameters.AddWithValue("@MoneyTransport", log.Money);
                        command.Parameters.AddWithValue("@StartTime", log.StartTime);
                        command.Parameters.AddWithValue("@EndTime", log.EndTime);
                        command.Parameters.AddWithValue("@Vehicle", log.Vehicle);
                        command.Parameters.AddWithValue("@StartMileage", log.StartMileage);
                        command.Parameters.AddWithValue("@EndMileage", log.EndMileage);
                        command.Parameters.AddWithValue("@Fuel", log.Fuel);
                        command.Parameters.AddWithValue("@EmployeeId", log.EmployeeId.ToString());
                        command.Parameters.AddWithValue("@Synced", 0);
                        command.Parameters.AddWithValue("@Updated", 0);

                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex) { logger.Error(ex, "SQLiteService <CreateLog> method."); return false; }
        }

        public List<Log> GetLogs(string filter = "", DateTime? start = null, DateTime? end = null, int offset = 0)
        {
            var logs = new List<Log>();

            var query = $@"select
	                        timeoflog, it.code as 'incidenttype', l.code as 'location', al.[description]
	                        , moneytransport, starttime, endtime, vehicle, startmileage, endmileage, fuel
	                        , o.rowid as 'EmployeeId', l.rowid as 'locationId', it.rowid as 'incidentTypeId', al.rowid, o.firstname || ' ' || o.lastname as 'Employee'
                        from ActivityLog al
                        join [Location] l on (l.RowId = al.LocationId)
                        join [IncidentType] it on (it.RowId = al.IncidentTypeId)
                        join Employee o on (o.RowId = al.EmployeeId)
                        @where
                        order by timeoflog desc, starttime desc
                        limit {logRowCount} offset {offset}";

            if (string.IsNullOrEmpty(filter) && !start.HasValue && !end.HasValue)
            {
                query = query.Replace("@where", "");
            }
            else
            {
                var filterBlock = @"(
                                        al.[description] like @filter or it.code like @filter or it.[description] like @filter 
                                        or l.code like @filter or l.[description] like @filter or o.firstname like @filter or o.lastname like @filter
                                        or moneytransport like @filter or fuel like @filter or vehicle like @filter or starttime like @filter
                                    )";

                query = query.Replace("@where", $"where {(start.HasValue ? $"timeoflog >= @start and timeoflog <= @end" : "")} {(string.IsNullOrEmpty(filter) ? "" : (start.HasValue ? $"and {filterBlock}" : filterBlock))} ");
            }

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        if (!string.IsNullOrEmpty(filter))
                        {
                            command.Parameters.AddWithValue("@filter", $"%{filter}%");
                        }

                        if (start.HasValue)
                        {
                            command.Parameters.AddWithValue("@start", start.Value.Date);
                            command.Parameters.AddWithValue("@end", end.Value.Date);
                        }

                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    logs.Add(new Log
                                    {
                                        IncidentDate = reader.GetDateTime(0),
                                        IncidentType = reader.GetString(1),
                                        Location = reader.GetString(2),
                                        Description = reader.GetString(3),
                                        Money = reader.GetString(4),
                                        StartTime = reader.GetDateTime(5),
                                        EndTime = reader.GetDateTime(6),
                                        Vehicle = reader.GetString(7),
                                        StartMileage = reader.GetInt32(8),
                                        EndMileage = reader.GetInt32(9),
                                        Fuel = reader.GetInt32(10),
                                        EmployeeId = reader.GetGuid(11),
                                        LocationId = reader.GetGuid(12),
                                        IncidentTypeId = reader.GetGuid(13),
                                        RowId = reader.GetGuid(14),
                                        LocalDBId = reader.GetGuid(14),
                                        Employee = reader.GetString(15)
                                    });
                                }
                            }
                        }
                    }
                }

                return logs;
            }
            catch (Exception ex) { logger.Error(ex, "SQLiteService <GetLogs> method."); return logs; }
        }

        public int GetLogPagingCounts(string filter = "", DateTime? start = null, DateTime? end = null)
        {
            var logs = new List<Log>();

            var query = @"select
	                        count(timeoflog)
                        from ActivityLog al
                        join [Location] l on (l.RowId = al.LocationId)
                        join [IncidentType] it on (it.RowId = al.IncidentTypeId)
                        join Employee o on (o.RowId = al.EmployeeId)
                        @where";

            if (string.IsNullOrEmpty(filter) && !start.HasValue && !end.HasValue)
            {
                query = query.Replace("@where", "");
            }
            else
            {
                var filterBlock = @"(
                                        al.[description] like @filter or it.code like @filter or it.[description] like @filter 
                                        or l.code like @filter or l.[description] like @filter or o.firstname like @filter or o.lastname like @filter
                                        or moneytransport like @filter or fuel like @filter or vehicle like @filter or starttime like @filter
                                    )";

                query = query.Replace("@where", $"where {(start.HasValue ? $"timeoflog >= @start and timeoflog <= @end" : "")} {(string.IsNullOrEmpty(filter) ? "" : (start.HasValue ? $"and {filterBlock}" : filterBlock))} ");
            }

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        if (!string.IsNullOrEmpty(filter))
                        {
                            command.Parameters.AddWithValue("@filter", $"%{filter}%");
                        }

                        if (start.HasValue)
                        {
                            command.Parameters.AddWithValue("@start", start.Value.Date);
                            command.Parameters.AddWithValue("@end", end.Value.Date);
                        }

                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                reader.Read();

                                return reader.GetInt32(0);
                            }
                            else
                            {
                                return 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { logger.Error(ex, "SQLiteService <GetLogPagingCounts> method."); return 0; }
        }

        public bool EditLog(Log log)
        {
            var query = @"UPDATE ActivityLog
                            SET IncidentTypeId=@IncidentTypeId, LocationId=@LocationId, [Description]=@Description, MoneyTransport=@MoneyTransport
                            , StartTime=@StartTime, EndTime=@EndTime, Vehicle=@Vehicle, StartMileage=@StartMileage, EndMileage=@EndMileage, Fuel=@Fuel, EmployeeId=@EmployeeId, Updated=@Updated
                        WHERE RowId=@RowId";

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TimeOfLog", log.IncidentDate);
                        command.Parameters.AddWithValue("@IncidentTypeId", log.IncidentTypeId.ToString());
                        command.Parameters.AddWithValue("@LocationId", log.LocationId.ToString());
                        command.Parameters.AddWithValue("@Description", log.Description);
                        command.Parameters.AddWithValue("@MoneyTransport", log.Money);
                        command.Parameters.AddWithValue("@StartTime", log.StartTime);
                        command.Parameters.AddWithValue("@EndTime", log.EndTime);
                        command.Parameters.AddWithValue("@Vehicle", log.Vehicle);
                        command.Parameters.AddWithValue("@StartMileage", log.StartMileage);
                        command.Parameters.AddWithValue("@EndMileage", log.EndMileage);
                        command.Parameters.AddWithValue("@Fuel", log.Fuel);
                        command.Parameters.AddWithValue("@EmployeeId", log.EmployeeId.ToString());
                        command.Parameters.AddWithValue("@Updated", 1);

                        command.Parameters.AddWithValue("@RowId", log.RowId.ToString());

                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex) { logger.Error(ex, "SQLiteService <EditLog> method."); return false; }
        }

        public List<Log> GetLocalLogsToTransfer(bool insert = true)
        {
            var logs = new List<Log>();

            var query = @"select
	                        timeoflog, it.code as 'incidenttype', l.code as 'location', al.[description]
	                        , moneytransport, starttime, endtime, vehicle, startmileage, endmileage, fuel
	                        , o.rowid as 'employeeId', l.rowid as 'locationId', it.rowid as 'incidentTypeId', al.rowid, o.firstname || ' ' || o.lastname as 'Employee'
                        from ActivityLog al
                        join [Location] l on (l.RowId = al.LocationId)
                        join [IncidentType] it on (it.RowId = al.IncidentTypeId)
                        join Employee o on (o.RowId = al.EmployeeId)
                        where @InsertOrUpdate
                        order by timeoflog, starttime desc";

            query = query.Replace("@InsertOrUpdate", insert ? "Synced = 0" : "Updated = 1");

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    logs.Add(new Log
                                    {
                                        IncidentDate = reader.GetDateTime(0),
                                        IncidentType = reader.GetString(1),
                                        Location = reader.GetString(2),
                                        Description = reader.GetString(3),
                                        Money = reader.GetString(4),
                                        StartTime = reader.GetDateTime(5),
                                        EndTime = reader.GetDateTime(6),
                                        Vehicle = reader.GetString(7),
                                        StartMileage = reader.GetInt32(8),
                                        EndMileage = reader.GetInt32(9),
                                        Fuel = reader.GetInt32(10),
                                        EmployeeId = reader.GetGuid(11),
                                        LocationId = reader.GetGuid(12),
                                        IncidentTypeId = reader.GetGuid(13),
                                        RowId = reader.GetGuid(14),
                                        LocalDBId = reader.GetGuid(14),
                                        Employee = reader.GetString(15)
                                    });
                                }
                            }
                        }
                    }
                }

                return logs;
            }
            catch (Exception ex) { logger.Error(ex, "SQLiteService <GetLocalLogsToTransfer> method."); return logs; }
        }

        public bool UpdateSyncedLogs(Log log)
        {
            var query = @"UPDATE ActivityLog
                            SET synced = 1
                        WHERE RowId=@RowId";

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@RowId", log.RowId.ToString());
                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex) { logger.Error(ex, "SQLiteService <UpdateSyncedLogs> method."); return false; }
        }

        public bool UpdateUpdatedLogs(Log log)
        {
            var query = @"UPDATE ActivityLog
                            SET Updated = 0
                        WHERE RowId=@RowId";

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@RowId", log.RowId.ToString());
                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex) { logger.Error(ex, "SQLiteService <UpdateSyncedLogs> method."); return false; }
        }
        #endregion

        #region manage employees
        public bool CreateEmployee(Employee employee)
        {
            var query = @"INSERT INTO Employee
                                   (RowID,EmployeeId,FirstName,LastName,[Status],[Password])
                        VALUES
                                   (@RowId,@EmployeeId,@FirstName,@LastName,@Status,@Password)";

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@RowId", employee.RowId.ToString());
                        command.Parameters.AddWithValue("@EmployeeId", employee.EmployeeID);
                        command.Parameters.AddWithValue("@FirstName", employee.FirstName);
                        command.Parameters.AddWithValue("@LastName", employee.LastName);
                        command.Parameters.AddWithValue("@Status", employee.StatusNum);
                        command.Parameters.AddWithValue("@Password", employee.Password);

                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex) { logger.Error(ex, "SQLiteService <CreateEmployee> method."); return false; }
        }

        public List<Employee> GetEmployees()
        {
            var employees = new List<Employee>();

            var query = @"select
	                        EmployeeId,FirstName,LastName,[Status], FirstName || ' ' || LastName as 'FullName',RowId, [Password]
                        from Employee 
                        order by lastname, firstname";

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                employees.Add(new Employee
                                {
                                    EmployeeID = reader.GetString(0),
                                    FirstName = reader.GetString(1),
                                    LastName = reader.GetString(2),
                                    StatusNum = reader.GetInt32(3),
                                    Status = reader.GetInt32(3) == 1 ? "Enabled" : "Disabled",
                                    FullName = reader.GetString(4),
                                    RowId = reader.GetGuid(5),
                                    Password = reader.GetString(6)
                                });
                            }
                        }
                    }
                }

                return employees;
            }
            catch (Exception ex) { logger.Error(ex, "SQLiteService <GetEmployee> method."); return employees; }
        }

        public bool DeleteEmployee()
        {
            var query = @"delete from employee";

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex) { logger.Error(ex, "SQLiteService <DeleteEmployee> method."); return false; }
        }
        #endregion

        #region manage locations
        public bool CreateLocation(LocationItem location)
        {
            var query = @"INSERT INTO Location
                                   (RowId,Code,[Description],[Status])
                        VALUES
                                   (@RowId,@Code,@Description,@Status)";

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@RowId", location.RowId.ToString());
                        command.Parameters.AddWithValue("@Code", location.Code);
                        command.Parameters.AddWithValue("@Description", location.Description);
                        command.Parameters.AddWithValue("@Status", location.StatusNum);

                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex) { logger.Error(ex, "SQLiteService <CreateLocation> method."); return false; }
        }

        public List<LocationItem> GetLocations()
        {
            var locations = new List<LocationItem>();

            var query = @"select
	                        Code,[Description],RowId,[Status]
                        from Location 
                        order by code";

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                locations.Add(new LocationItem { Code = reader.GetString(0), Description = reader.GetString(1), RowId = reader.GetGuid(2), StatusNum = reader.GetInt32(3), Status = reader.GetInt32(3) == 1 ? "Enabled" : "Disabled" });
                            }
                        }
                    }
                }

                return locations;
            }
            catch (Exception ex) { logger.Error(ex, "SQLiteService <GetLocations> method."); return locations; }
        }

        public bool DeleteLocation()
        {
            var query = @"delete from location";

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex) { logger.Error(ex, "SQLiteService <DeleteLocation> method."); return false; }
        }
        #endregion

        #region manage incident types
        public bool CreateIncidentType(IncidentItem incident)
        {
            var query = @"INSERT INTO IncidentType
                                   (RowId,Code,[Description],[Status])
                        VALUES
                                   (@RowId,@Code,@Description,@Status)";

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@RowId", incident.RowId.ToString());
                        command.Parameters.AddWithValue("@Code", incident.Code);
                        command.Parameters.AddWithValue("@Description", incident.Description);
                        command.Parameters.AddWithValue("@Status", incident.StatusNum);

                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex) { logger.Error(ex, "SQLiteService <CreateIncidentType> method."); return false; }
        }

        public List<IncidentItem> GetIncidentTypes()
        {
            var incidents = new List<IncidentItem>();

            var query = @"select
	                        Code,[Description],RowId,[Status]
                        from IncidentType
                        order by code";

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                incidents.Add(new IncidentItem { Code = reader.GetString(0), Description = reader.GetString(1), RowId = reader.GetGuid(2), StatusNum = reader.GetInt32(3), Status = reader.GetInt32(3) == 1 ? "Enabled" : "Disabled" });
                            }
                        }
                    }
                }

                return incidents;
            }
            catch (Exception ex) { logger.Error(ex, "SQLiteService <GetIncidentTypes> method."); return incidents; }
        }

        public bool DeleteIncidentType()
        {
            var query = @"delete from IncidentType";

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex) { logger.Error(ex, "SQLiteService <DeleteIncidentType> method."); return false; }
        }
        #endregion


    }
}
