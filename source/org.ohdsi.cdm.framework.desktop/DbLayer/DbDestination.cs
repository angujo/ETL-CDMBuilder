using org.ohdsi.cdm.framework.desktop.Helpers;
using System;
using System.Data.Odbc;

namespace org.ohdsi.cdm.framework.desktop.DbLayer
{
    public class DbDestination
    {
        private readonly string _connectionString;
        private readonly string _schemaName;

        public bool IsMssql =>
            _connectionString.ToLower().Contains("sql") && _connectionString.ToLower().Contains("server");

        public bool IsMysql          => _connectionString.ToLower().Contains("mysql");
        public bool IsPostgreSQL     => _connectionString.ToLower().Contains("postgresql");
        public bool IsAmazonRedshift => _connectionString.ToLower().Contains("amazon redshift");

        public DbDestination(string connectionString, string schemaName)
        {
            _connectionString = connectionString;
            _schemaName       = schemaName;
        }

        public void CreateDatabase(string query)
        {
            var sqlConnectionStringBuilder = new OdbcConnectionStringBuilder(_connectionString);
            var database                   = sqlConnectionStringBuilder["database"];

            // TMP
            var mySql = _connectionString.ToLower().Contains("mysql");

            if (IsMysql)
                sqlConnectionStringBuilder["database"] = "mysql";
            else if (IsAmazonRedshift)
                sqlConnectionStringBuilder["database"] = "poc";
            else
                sqlConnectionStringBuilder["database"] = database;

            // throw new Exception(_connectionString+" [QUERY] "+query);

            using (var connection = SqlConnectionHelper.OpenOdbcConnection(sqlConnectionStringBuilder.ConnectionString))
            {
                if (!DBExists(connection, (string) sqlConnectionStringBuilder["database"]))
                {
                    query = string.Format(query, database);

                    foreach (var subQuery in query.Split(new[] {"\r\nGO", "\nGO"}, StringSplitOptions.None))
                    {
                        using (var command = new OdbcCommand(subQuery, connection))
                        {
                            command.CommandTimeout = 30000;
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }

            if (!IsMysql && _schemaName.ToLower().Trim() != "dbo")
            {
                CreateSchema();
            }
        }

        public void CreateSchema()
        {
            using (var connection = SqlConnectionHelper.OpenOdbcConnection(_connectionString))
            {
                var query = $"create schema IF NOT EXISTS {_schemaName}";

                using (var command = new OdbcCommand(query, connection))
                {
                    command.CommandTimeout = 0;
                    command.ExecuteNonQuery();
                }
            }
        }

        public void ExecuteQuery(string query)
        {
            using (var connection = SqlConnectionHelper.OpenOdbcConnection(_connectionString))
            {
                query = query.Replace("{sc}", _schemaName);

                foreach (var subQuery in query.Split(new[] {"\r\nGO", "\nGO", ";"}, StringSplitOptions.None))
                {
                    if (string.IsNullOrEmpty(subQuery))
                        continue;
                    try
                    {
                        using (var command = new OdbcCommand(subQuery, connection))
                        {
                            command.CommandTimeout = 30000;
                            command.ExecuteNonQuery();
                        }
                    }
                    catch (Exception e)
                    {
                        throw new Exception(subQuery+"\n"+e.Message);
                    }
                }
            }
        }

        private bool DBExists(OdbcConnection connection, string dbName)
        {
            if (IsPostgreSQL)
            {
                using var cmd = new OdbcCommand(
                    $"SELECT 1 dbname FROM pg_catalog.pg_database WHERE datname = '{dbName}'", connection);
                return null != cmd.ExecuteScalar();
            }

            return false;
        }
    }
}