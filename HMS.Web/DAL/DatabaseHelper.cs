using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace HMS.Web.DAL
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

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

        public DataTable ExecuteDataTable(string query, SqlParameter[] parameters = null)
        {
            var ds = ExecuteDataSet(query, parameters);
            return ds.Tables.Count > 0 ? ds.Tables[0] : null;
        }

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

        public List<T> ExecuteQuery<T>(string query, Func<DataRow, T> map, SqlParameter[] parameters = null)
        {
            try
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
            catch (Exception ex)
            {
                throw new Exception($"Data Mapping Error: {ex.Message}. Query: {query}", ex);
            }
        }

        public SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
