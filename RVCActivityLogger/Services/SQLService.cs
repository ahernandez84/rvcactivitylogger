using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

using NLog;
using RVCActivityLogger.Models;

namespace RVCActivityLogger.Services
{
    internal class SQLService
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private string connectionString = "";
        private int logRowCount;

        public SQLService(string connectionString, int logRowCount)
        {
            this.connectionString = connectionString;
            this.logRowCount = logRowCount;
        }

        public void SetLogRowCount(int logRowCount) => this.logRowCount = logRowCount;

        #region manage logs
        public List<Log> GetLogs(string filter = "", DateTime? start = null, DateTime? end = null, int offset = 0)
        {
            var logs = new List<Log>();

            var query = $@"select 
	                        timeoflog, it.code as 'incidenttype', l.code as 'location', al.[description]
	                        , moneytransport, starttime, endtime, vehicle, startmileage, endmileage, fuel
	                        , o.rowid as 'employeeId', l.rowid as 'locationId', it.rowid as 'incidentTypeId', al.rowid, o.firstname + ' ' + o.lastname as 'employee'
                        from ActivityLog al
                        join [Location] l on (l.RowId = al.LocationId)
                        join [IncidentType] it on (it.RowId = al.IncidentTypeId)
                        join Employee o on (o.RowId = al.EmployeeId)
                        @where
                        order by timeoflog desc, starttime desc 
                        offset {offset} rows fetch next {logRowCount} rows only";

            if (string.IsNullOrEmpty(filter) && !start.HasValue && !end.HasValue)
            {
                query = query.Replace("@where", "");
            }
            else
            {
                var filterBlock = @"(
                                        al.[description] like @filter or it.code like @filter or it.[description] like @filter 
                                        or l.code like @filter or l.[description] like @filter or o.firstname like @filter or o.lastname like @filter
                                        or moneytransport like @filter or fuel like @filter or vehicle like @filter or convert(varchar(8), starttime, 108) like @filter
                                    )";

                query = query.Replace("@where", $"where {(start.HasValue ? $"timeoflog >= @start and timeoflog <= @end" : "" )} {(string.IsNullOrEmpty(filter) ? "" : (start.HasValue ? $"and {filterBlock}" : filterBlock))} ");
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
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

                        using (SqlDataReader reader = command.ExecuteReader())
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
                                        Employee = reader.GetString(15)
                                    });
                                }
                            }
                        }
                    }
                }

                return logs;
            }
            catch (Exception ex) { logger.Error(ex, "SQLService <GetLogs> method."); return logs; }
        }

        public int GetLogPagingCounts(string filter = "", DateTime? start = null, DateTime? end = null)
        {
            var query = $@"select count(timeoflog)
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
                                        or moneytransport like @filter or fuel like @filter or vehicle like @filter or convert(varchar(8), starttime, 108) like @filter
                                    )";

                query = query.Replace("@where", $"where {(start.HasValue ? $"timeoflog >= @start and timeoflog <= @end" : "")} {(string.IsNullOrEmpty(filter) ? "" : (start.HasValue ? $"and {filterBlock}" : filterBlock))} ");
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
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

                        using (SqlDataReader reader = command.ExecuteReader())
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
            catch (Exception ex) { logger.Error(ex, "SQLService <GetLogPagingCounts> method."); return 0; }
        }

        public bool EditLog(Log log)
        {
            var query = @"UPDATE ActivityLog
                            SET IncidentTypeId=@IncidentTypeId, LocationId=@LocationId, [Description]=@Description, MoneyTransport=@MoneyTransport, StartTime=@StartTime, EndTime=@EndTime, Vehicle=@Vehicle, StartMileage=@StartMileage, EndMileage=@EndMileage, Fuel=@Fuel, EmployeeId=@EmployeeId 
                        WHERE RowId=@RowId";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TimeOfLog", log.IncidentDate);
                        command.Parameters.AddWithValue("@IncidentTypeId", log.IncidentTypeId);
                        command.Parameters.AddWithValue("@LocationId", log.LocationId);
                        command.Parameters.AddWithValue("@Description", log.Description);
                        command.Parameters.AddWithValue("@MoneyTransport", log.Money);
                        command.Parameters.AddWithValue("@StartTime", log.StartTime);
                        command.Parameters.AddWithValue("@EndTime", log.EndTime);
                        command.Parameters.AddWithValue("@Vehicle", log.Vehicle);
                        command.Parameters.AddWithValue("@StartMileage", log.StartMileage);
                        command.Parameters.AddWithValue("@EndMileage", log.EndMileage);
                        command.Parameters.AddWithValue("@Fuel", log.Fuel);
                        command.Parameters.AddWithValue("@EmployeeId", log.EmployeeId);
                        command.Parameters.AddWithValue("@RowId", log.RowId);

                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex) { logger.Error(ex, "SQLService <EditLog> method."); return false; }
        }

        public DataTable GetLogsForReport(string filter = "", DateTime? start = null, DateTime? end = null)
        {
            var table = new DataTable("Report");

            var query = $@"select 
	                        timeoflog as 'IncidentDate', it.code as 'IncidentType', l.code as 'Location', al.[Description]
	                        , MoneyTransport, StartTime, EndTime, Vehicle, StartMileage, EndMileage, Fuel
	                        ,o.firstname + ' ' + o.lastname as 'Employee'
                        from ActivityLog al
                        join [Location] l on (l.RowId = al.LocationId)
                        join [IncidentType] it on (it.RowId = al.IncidentTypeId)
                        join Employee o on (o.RowId = al.EmployeeId)
                        @where
                        order by IncidentDate desc, StartTime desc";

            if (string.IsNullOrEmpty(filter) && !start.HasValue && !end.HasValue)
            {
                query = query.Replace("@where", "");
            }
            else
            {
                var filterBlock = @"(
                                        al.[description] like @filter or it.code like @filter or it.[description] like @filter 
                                        or l.code like @filter or l.[description] like @filter or o.firstname like @filter or o.lastname like @filter
                                        or moneytransport like @filter or fuel like @filter or vehicle like @filter or convert(varchar(8), starttime, 108) like @filter
                                    )";

                query = query.Replace("@where", $"where {(start.HasValue ? $"timeoflog >= @start and timeoflog <= @end" : "")} {(string.IsNullOrEmpty(filter) ? "" : (start.HasValue ? $"and {filterBlock}" : filterBlock))} ");
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
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

                        using (var adapter = new SqlDataAdapter(command))
                        {
                            adapter.Fill(table);

                            return table;
                        }
                    }
                }
            }
            catch (Exception ex) { logger.Error(ex, "SQLService <GetLogsForReport> method."); return null; }
        }
        #endregion

        #region manage employees
        public bool CreateEmployee(Employee employee)
        {
            var query = @"INSERT INTO Employee
                                   (EmployeeId,FirstName,LastName,[Status],[Password])
                        VALUES
                                   (@EmployeeId,@FirstName,@LastName,@Status,@Password)";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
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
            catch (Exception ex) { logger.Error(ex, "SQLService <CreateEmployee> method."); return false; }
        }

        public List<Employee> GetEmployees()
        {
            var employees = new List<Employee>();

            var query = @"select
	                        EmployeeId,FirstName,LastName,[Status], RowId, [Password]
                        from Employee
                        order by lastname, firstname ";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                employees.Add(new Employee { 
                                    EmployeeID = reader.GetString(0), 
                                    FirstName = reader.GetString(1), 
                                    LastName = reader.GetString(2), 
                                    StatusNum = reader.GetInt32(3), 
                                    Status = reader.GetInt32(3) == 1 ? "Enabled" : "Disabled", 
                                    RowId=reader.GetGuid(4),
                                    Password = reader.IsDBNull(5) ? "" : reader.GetString(5)
                                });
                            }
                        }
                    }
                }

                return employees;
            }
            catch (Exception ex) { logger.Error(ex, "SQLService <GetEmployees> method."); return employees; }
        }

        public bool UpdateEmployee(Employee employee, string originalEmpId)
        {
            var query = @"Update Employee
                            set EmployeeId = @EmployeeId, FirstName = @FirstName, Lastname = @Lastname, [Status] = @Status, [Password] = @Password
                        where
                            EmployeeId = @OrgEmpId";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@EmployeeId", employee.EmployeeID);
                        command.Parameters.AddWithValue("@FirstName", employee.FirstName);
                        command.Parameters.AddWithValue("@LastName", employee.LastName);
                        command.Parameters.AddWithValue("@Status", employee.StatusNum);
                        command.Parameters.AddWithValue("@Password", employee.Password);
                        command.Parameters.AddWithValue("@OrgEmpId", originalEmpId);

                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex) { logger.Error(ex, "SQLService <UpdateEmployee> method."); return false; }
        }

        public bool DisableEmployee(int employeeId)
        {
            var query = @"Update Employee
                            set [status] = 0
                        where
                            EmployeeId = @EmployeeId";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@EmployeeId", employeeId);

                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex) { logger.Error(ex, "SQLService <DisableEnable> method."); return false; }
        }
        #endregion

        #region manage locations
        public bool CreateLocation(LocationItem location)
        {
            var query = @"INSERT INTO Location
                                   (Code,[Description],[Status])
                        VALUES
                                   (@Code,@Description,@Status)";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Code", location.Code);
                        command.Parameters.AddWithValue("@Description", location.Description);
                        command.Parameters.AddWithValue("@Status", location.StatusNum);

                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex) { logger.Error(ex, "SQLService <CreateLocation> method."); return false; }
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
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    using (SqlDataReader reader = command.ExecuteReader())
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
            catch (Exception ex) { logger.Error(ex, "SQLService <GetLocations> method."); return locations; }
        }

        public bool UpdateLocation(LocationItem location, Guid rowId)
        {
            var query = @"Update Location
                            set Code = @Code, [Description] = @Description, [Status] = @Status
                        where
                            RowId = @RowId";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Code", location.Code);
                        command.Parameters.AddWithValue("@Description", location.Description);
                        command.Parameters.AddWithValue("@Status", location.StatusNum);
                        command.Parameters.AddWithValue("@RowId", rowId);

                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex) { logger.Error(ex, "SQLService <UpdateLocation> method."); return false; }
        }

        public bool DisableLocation(Guid rowId)
        {
            var query = @"Update Location
                            set [status] = 0
                        where
                            RowId = @RowId";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@RowId", rowId);

                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex) { logger.Error(ex, "SQLService <DisableLocation> method."); return false; }
        }
        #endregion

        #region manage incident types
        public bool CreateIncidentType(IncidentItem incident)
        {
            var query = @"INSERT INTO IncidentType
                                   (Code,[Description],[Status])
                        VALUES
                                   (@Code,@Description,@Status)";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Code", incident.Code);
                        command.Parameters.AddWithValue("@Description", incident.Description);
                        command.Parameters.AddWithValue("@Status", incident.StatusNum);

                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex) { logger.Error(ex, "SQLService <CreateIncidentType> method."); return false; }
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
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    using (SqlDataReader reader = command.ExecuteReader())
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
            catch (Exception ex) { logger.Error(ex, "SQLService <GetIncidentTypes> method."); return incidents; }
        }

        public bool UpdateIncidentType(IncidentItem incident, Guid rowId)
        {
            var query = @"Update IncidentType
                            set Code = @Code, [Description] = @Description, [Status] = @Status
                        where
                            RowId = @RowId";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Code", incident.Code);
                        command.Parameters.AddWithValue("@Description", incident.Description);
                        command.Parameters.AddWithValue("@Status", incident.StatusNum);
                        command.Parameters.AddWithValue("@RowId", rowId);

                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex) { logger.Error(ex, "SQLService <UpdateIncidentType> method."); return false; }
        }

        public bool DisableIncidentType(Guid rowId)
        {
            var query = @"Update IncidentType
                            set [status] = 0
                        where
                            RowId = @RowId";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@RowId", rowId);

                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex) { logger.Error(ex, "SQLService <DisableIncidentType> method."); return false; }
        }
        #endregion

    }
}
