/*
 * FILE: DatabaseHelper.cs
 * PURPOSE: Core database abstraction layer.
 * COMMUNICATES WITH: All Repositories (DAL)
 */
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace HMS.Web.DAL
{
    /// <summary>
    /// Core data access utility providing a high-level wrapper around ADO.NET.
    /// It manages connection lifecycles, command execution, and result mapping.
    /// OPTIMIZATION: [Telemetry] Embedded Stopwatch for performance profiling.
    /// OPTIMIZATION: [Connection Management] Explicit closure logic ensures high pool availability.
    /// </summary>
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        /// <summary>
        /// Executes a SQL query and returns a DataSet.
        /// </summary>
        public DataSet ExecuteDataSet(string query, SqlParameter[] parameters = null)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand(query, connection))
                    {
                        if (parameters != null)
                        {
                            command.Parameters.AddRange(parameters);
                        }

                        var adapter = new SqlDataAdapter(command);
                        var dataSet = new DataSet();
                        adapter.Fill(dataSet);
                        return dataSet;
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new Exception($"Database Error (DataSet): {ex.Message}. Query: {query}", ex);
            }
        }

        /// <summary>
        /// Executes a SQL query and returns a DataTable.
        /// </summary>
        public DataTable ExecuteDataTable(string query, SqlParameter[] parameters = null)
        {
            var ds = ExecuteDataSet(query, parameters);
            return ds.Tables.Count > 0 ? ds.Tables[0] : null;
        }

        /// <summary>
        /// Asynchronously executes a SQL query and returns a DataTable.
        /// </summary>
        public async Task<DataTable> ExecuteDataTableAsync(string query, SqlParameter[] parameters = null)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand(query, connection))
                    {
                        if (parameters != null)
                        {
                            command.Parameters.AddRange(parameters);
                        }

                        await connection.OpenAsync();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var dataTable = new DataTable();
                            dataTable.Load(reader);
                            return dataTable;
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new Exception($"Database Error (DataTableAsync): {ex.Message}. Query: {query}", ex);
            }
        }

        /// <summary>
        /// Executes a non-query SQL command (INSERT, UPDATE, DELETE).
        /// </summary>
        public int ExecuteNonQuery(string query, SqlParameter[] parameters = null, SqlTransaction transaction = null)
        {
            try
            {
                var connection = transaction?.Connection ?? new SqlConnection(_connectionString);
                using (var command = new SqlCommand(query, connection, transaction))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    if (connection.State != ConnectionState.Open)
                        connection.Open();

                    int result = command.ExecuteNonQuery();

                    if (transaction == null)
                        connection.Close();

                    return result;
                }
            }
            catch (SqlException ex)
            {
                throw new Exception($"Database Error (NonQuery): {ex.Message}. Query: {query}", ex);
            }
        }

        /// <summary>
        /// Asynchronously executes a non-query SQL command.
        /// </summary>
        public async Task<int> ExecuteNonQueryAsync(string query, SqlParameter[] parameters = null, SqlTransaction transaction = null)
        {
            try
            {
                var connection = transaction?.Connection ?? new SqlConnection(_connectionString);
                using (var command = new SqlCommand(query, connection, transaction))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    if (connection.State != ConnectionState.Open)
                        await connection.OpenAsync();

                    int result = await command.ExecuteNonQueryAsync();

                    if (transaction == null)
                        await connection.CloseAsync();

                    return result;
                }
            }
            catch (SqlException ex)
            {
                throw new Exception($"Database Error (NonQueryAsync): {ex.Message}. Query: {query}", ex);
            }
        }

        /// <summary>
        /// Executes a SQL command and returns the first column of the first row (scalar).
        /// </summary>
        public object ExecuteScalar(string query, SqlParameter[] parameters = null, SqlTransaction transaction = null)
        {
            try
            {
                var connection = transaction?.Connection ?? new SqlConnection(_connectionString);
                using (var command = new SqlCommand(query, connection, transaction))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    if (connection.State != ConnectionState.Open)
                        connection.Open();

                    object result = command.ExecuteScalar();

                    if (transaction == null)
                        connection.Close();

                    return result;
                }
            }
            catch (SqlException ex)
            {
                throw new Exception($"Database Error (Scalar): {ex.Message}. Query: {query}", ex);
            }
        }

        /// <summary>
        /// Asynchronously executes a SQL command and returns the first column of the first row.
        /// </summary>
        public async Task<object> ExecuteScalarAsync(string query, SqlParameter[] parameters = null, SqlTransaction transaction = null)
        {
            try
            {
                var connection = transaction?.Connection ?? new SqlConnection(_connectionString);
                using (var command = new SqlCommand(query, connection, transaction))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    if (connection.State != ConnectionState.Open)
                        await connection.OpenAsync();

                    object result = await command.ExecuteScalarAsync();

                    if (transaction == null)
                        await connection.CloseAsync();

                    return result;
                }
            }
            catch (SqlException ex)
            {
                throw new Exception($"Database Error (ScalarAsync): {ex.Message}. Query: {query}", ex);
            }
        }

        public T ExecuteScalar<T>(string query, SqlParameter[] parameters = null, SqlTransaction transaction = null)
        {
            var result = ExecuteScalar(query, parameters, transaction);
            if (result == null || result == DBNull.Value) return default;
            return (T)Convert.ChangeType(result, typeof(T));
        }

        public async Task<T> ExecuteScalarAsync<T>(string query, SqlParameter[] parameters = null, SqlTransaction transaction = null)
        {
            var result = await ExecuteScalarAsync(query, parameters, transaction);
            if (result == null || result == DBNull.Value) return default;
            return (T)Convert.ChangeType(result, typeof(T));
        }

        /// <summary>
        /// Executes a query and maps results to a list using a data reader for efficiency.
        /// OPTIMIZATION: [Performance Telemetry] Added Stopwatch to log slow queries (>500ms).
        /// OPTIMIZATION: [Connection Management] Explicitly handles connection lifecycles to prevent leaks.
        /// </summary>
        public List<T> ExecuteQuery<T>(string query, Func<SqlDataReader, T> map, SqlParameter[]? parameters = null, SqlTransaction? transaction = null)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            List<T> list = new List<T>();
            SqlConnection connection = transaction?.Connection ?? new SqlConnection(_connectionString);
            try
            {
                if (connection.State != ConnectionState.Open) connection.Open();
                using (var command = new SqlCommand(query, connection, transaction))
                {
                    if (parameters != null) command.Parameters.AddRange(parameters);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read()) list.Add(map(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Database Error (Query): {ex.Message}. Query: {query}", ex);
            }
            finally
            {
                if (transaction == null && connection.State != ConnectionState.Closed) connection.Close();
                sw.Stop();
                if (sw.ElapsedMilliseconds > 500) System.Diagnostics.Debug.WriteLine($"SLOW QUERY ({sw.ElapsedMilliseconds}ms): {query}");
            }
            return list;
        }

        /// <summary>
        /// Asynchronously executes a query and maps results to a list using a data reader.
        /// OPTIMIZATION: [Async Performance] Threshold for logging is higher (800ms) to account for initial connection scaling.
        /// OPTIMIZATION: [Memory Footprint] Uses forward-only readers to minimize RAM usage on large datasets.
        /// </summary>
        public async Task<List<T>> ExecuteQueryAsync<T>(string query, Func<SqlDataReader, T> map, SqlParameter[]? parameters = null, SqlTransaction? transaction = null)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            List<T> list = new List<T>();
            SqlConnection connection = transaction?.Connection ?? new SqlConnection(_connectionString);
            try
            {
                if (connection.State != ConnectionState.Open) await connection.OpenAsync();
                using (var command = new SqlCommand(query, connection, transaction))
                {
                    if (parameters != null) command.Parameters.AddRange(parameters);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync()) list.Add(map(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Database Error (QueryAsync): {ex.Message}. Query: {query}", ex);
            }
            finally
            {
                if (transaction == null && connection.State != ConnectionState.Closed) await connection.CloseAsync();
                sw.Stop();
                if (sw.ElapsedMilliseconds > 800) System.Diagnostics.Debug.WriteLine($"SLOW ASYNC QUERY ({sw.ElapsedMilliseconds}ms): {query}");
            }
            return list;
        }

        /// <summary>
        /// Legacy method for DataRow mapping (still supported but reader-based ExecuteQuery is preferred).
        /// </summary>
        public List<T> ExecuteQuery<T>(string query, Func<DataRow, T> map, SqlParameter[] parameters = null)
        {
            var table = ExecuteDataTable(query, parameters);
            var list = new List<T>();
            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    list.Add(map(row));
                }
            }
            return list;
        }

        public SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}

