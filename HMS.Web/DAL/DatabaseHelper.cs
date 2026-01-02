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

        public DataTable ExecuteDataTable(string query, SqlParameter[] parameters = null)
        {
            var ds = ExecuteDataSet(query, parameters);
            return ds.Tables.Count > 0 ? ds.Tables[0] : null;
        }

        public int ExecuteNonQuery(string query, SqlParameter[] parameters = null)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    connection.Open();
                    return command.ExecuteNonQuery();
                }
            }
        }

        public object ExecuteScalar(string query, SqlParameter[] parameters = null)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    connection.Open();
                    return command.ExecuteScalar();
                }
            }
        }

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
    }
}
