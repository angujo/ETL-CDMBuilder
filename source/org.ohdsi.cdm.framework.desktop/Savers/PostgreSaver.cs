﻿using Npgsql;
using NpgsqlTypes;
using org.ohdsi.cdm.framework.common.Enums;
using org.ohdsi.cdm.framework.desktop.Enums;
using org.ohdsi.cdm.framework.desktop.Helpers;
using System;
using System.Data.Odbc;

namespace org.ohdsi.cdm.framework.desktop.Savers
{
    public class PostgreSaver : Saver
    {
        private NpgsqlConnection _connection;

        public override ISaver Create(string connectionString, CdmVersions cdmVersion, string sourceSchema,
            string destinationSchema)
        {
            CdmVersion        = cdmVersion;
            SourceSchema      = sourceSchema;
            DestinationSchema = destinationSchema;

            var odbc = new OdbcConnectionStringBuilder(connectionString);

            var connectionStringTemplate =
                "Server={server};Port=5432;Database={database};User Id={username};Password={password};Trust Server Certificate=true";

            var npgsqlConnectionString = connectionStringTemplate.Replace("{server}", odbc["server"].ToString())
                                                                 .Replace("{database}", odbc["database"].ToString())
                                                                 .Replace("{username}", odbc["uid"].ToString())
                                                                 .Replace("{password}", odbc["pwd"].ToString());

            Console.WriteLine("npgsqlConnectionString=" + npgsqlConnectionString);
            _connection = SqlConnectionHelper.OpenNpgsqlConnection(npgsqlConnectionString);

            return this;
        }

        public override Database GetDatabaseType()
        {
            return Database.Postgre;
        }

        public override void Write(int? chunkId, int? subChunkId, System.Data.IDataReader reader, string tableName)
        {
            if (reader == null)
                return;
            var error="observation".Equals(tableName);
            var msg = $"{tableName}";

            if (tableName.ToLower().StartsWith("_chunks"))
            {
                tableName = SourceSchema + "." + tableName;
            }
            else
            {
                tableName = DestinationSchema + "." + tableName;
            }

            var fields = new string[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                fields[i] = reader.GetName(i);
            }

            var q = $"COPY {tableName} ({string.Join(",", fields)}) from STDIN (FORMAT BINARY)";

            if (error) msg += $"{q}";

            try
            {
                using (var inStream = _connection.BeginBinaryImport(q))
                {
                    var r = 0;
                    while (reader.Read())
                    {
                        r++;
                        inStream.StartRow();

                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            var value = reader.GetValue(i);
                            try
                            {
                                    var fd = GetFieldType(reader.GetFieldType(i), reader.GetName(i));
                                if (error && "qualifier_concept_id".Equals(reader.GetName(i).ToLower())) msg += $"{r}\tTYPE: [{fd}]\t\tVALUE: [{value}]::";
                                if (value is null || null==value)
                                {
                                    inStream.WriteNull();
                                }
                                else
                                {
                                    inStream.Write(value, fd);
                                }
                            }catch (Exception ex)
                            {
                                throw new Exception($"VAL:[{value}] {ex.Message}");
                            }

                            
                        }
                    }

                    inStream.Complete();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                //throw new Exception(msg+"\n"+e.Message);
                throw;
            }
        }

        private static NpgsqlDbType GetFieldType(Type type, string fieldName)
        {
            var name           = type.Name;
            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
            {
                name = underlyingType.Name;
            }

            // if (fieldName.ToLower().Equals("visit_occurrence_id")) throw new Exception($"{fieldName}::{name}");

            switch (name)
            {
                case "Int64":
                    return NpgsqlDbType.Bigint;
                case "Int32":
                    return NpgsqlDbType.Integer;
                case "Decimal":
                    return NpgsqlDbType.Numeric;
                case "DateTime":
                {
                    if (fieldName.ToLower().Contains("time"))
                        return NpgsqlDbType.Timestamp;

                    return NpgsqlDbType.Date;
                }
                case "TimeSpan":
                    return NpgsqlDbType.Time;
                case "String":
                {
                    // workaround for 5.3 schema, string used for Timestamp
                    if (fieldName.ToLower().Contains("time"))
                        return NpgsqlDbType.Time;


                    return NpgsqlDbType.Varchar;
                }

                default:
                    throw new NotImplementedException();
            }
        }

        public override void Commit()
        {
            //_transaction.Commit();
        }

        public override void Rollback()
        {
            //_transaction.Rollback();
        }

        public override void Dispose()
        {
            //_transaction.Dispose();
            _connection.Dispose();
        }
    }
}