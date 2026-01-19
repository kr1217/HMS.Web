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



        public async Task<T> ExecuteScalarAsync<T>(string query, SqlParameter[] parameters = null, SqlTransaction transaction = null)
        {
            var result = await ExecuteScalarAsync(query, parameters, transaction);
            if (result == null || result == DBNull.Value) return default;
            return (T)Convert.ChangeType(result, typeof(T));
        }



        /// <summary>
        /// Asynchronously executes a query and maps results to a list using a data reader.
        /// OPTIMIZATION: [Async Performance] Threshold for logging is higher (800ms) to account for initial connection scaling.
        /// OPTIMIZATION: [Memory Footprint] Uses forward-only readers to minimize RAM usage on large datasets.
        /// </summary>
        public async Task<List<T>>  ExecuteQueryAsync<T>(string query, Func<SqlDataReader, T> map, SqlParameter[]? parameters = null, SqlTransaction? transaction = null)
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



        public SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}

