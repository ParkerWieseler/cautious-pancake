using System;
using System.Data.SqlClient;

namespace CodeFlip.CodeJar.Api
{
    public class SQL
    {
        public SQL (string connectionString, string filePath)
        {
            Connection = new SqlConnection(connectionString);
            FilePath = filePath;
        }

        public SqlConnection Connection {get; set;}
        public string FilePath {get; set;}

        public long[] UpdateOffset (int batchSize, SqlCommand command)
        {
            var firstAndLastOffset = new long[2];
            var offsetIncrement = batchSize * 4;

            command.CommandText = @"Update Offset Set OffsetValue = OffsetValue + @offsetIncrement
                                    OUTPUT INSERT.OffsetValue
                                    WHERE ID = 1";
            command.Parameters.AddWithValue("@offsetIncrement", offsetIncrement);
            var updatedOffset = (long)command.ExecuteScalar();

            firstAndLastOffset[0] = updatedOffset - offsetIncrement;
            firstAndLastOffset[1] = updatedOffset;

            return firstAndLastOffset;
        }
    }
}