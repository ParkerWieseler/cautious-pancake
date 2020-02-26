using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using Microsoft.Azure.Storage.Blob;


namespace CodeFlip.CodeJar.Api
{

    public class SQL
    {
        public SQL (string connectionString)

        {
            Connection = new SqlConnection(connectionString);
        }


        public SqlConnection Connection {get; set;}

       
        public long[] UpdateOffset (int batchSize)
        {
            var firstAndLastOffset = new long[2];
            var offsetIncrement = batchSize * 4;

            Connection.Open();

            using(var command = Connection.CreateCommand())
            {
                command.CommandText = @"UPDATE [Offsets] SET OffsetValue = OffsetValue + @offsetIncrement
                                        OUTPUT INSERTED.OffsetValue
                                        WHERE ID = 1";
                command.Parameters.AddWithValue("@offsetIncrement", offsetIncrement);
                var updatedOffset = (long)command.ExecuteScalar();

                firstAndLastOffset[0] = updatedOffset - offsetIncrement;
                firstAndLastOffset[1] = updatedOffset;

            }

           Connection.Close();
            return firstAndLastOffset;
        }

        public void InsertCodes (List<Code> codes, SqlCommand command)
        {
                    foreach(var code in codes)
                  {
                    command.CommandText = @"
                        INSERT INTO Code (State, SeedValue)
                        VALUES (@state, @seedValue)";

                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@state", States.Active);
                    command.Parameters.AddWithValue("@seedValue", code.SeedValue);
                    command.ExecuteNonQuery();
                 }
        }

        public void CreateBatch(Promotion promotion, List<Code> codes)
        {
            SqlTransaction transaction;

            Connection.Open();

            var command = Connection.CreateCommand();

            transaction = Connection.BeginTransaction();

            command.Transaction = transaction;

            try
            {
                command.CommandText = @"Declare @codeIDStart int
                                        SET @codeIdStart = (SELECT ISNULL(MAX(CodeIDEnd), 0)FROM Promotion) + 1
                                        
                                        INSERT INTO Promotion (PromotionName, CodeIDStart, PromotionSize)
                                        Values (@promotionName, @codeIDStart, @promotionSize)
                                        SELECT SCOPE_IDENTITY()";
                command.Parameters.AddWithValue("@promotionName", promotion.Name);
                command.Parameters.AddWithValue("@promotionSize", promotion.BatchSize);
                promotion.ID = Convert.ToInt32(command.ExecuteScalar());

                InsertCodes(codes, command);

                transaction.Commit();
            }
            catch(Exception ex)
            {
                transaction.Rollback();
            }
            Connection.Close();
        }

        public Code GetCode(string stringValue, string alphabet)
        {
            var convertCode = new CodeConverter(alphabet);

            var code = new Code();

            var seedValue = convertCode.ConvertFromCode(stringValue);

            Connection.Open();

            using(var command = Connection.CreateCommand())
            {
                command.CommandText = @"SELECT * FROM Code WHERE [SeedValue] = @seedValue";
                command.Parameters.AddWithValue("@seedValue", seedValue);

                using(var reader = command.ExecuteReader())
                {
                    while(reader.Read())
                    {
                        var seed = (int)reader["SeedValue"];

                        code.State = States.ConvertToString((byte)reader["State"]);
                        code.StringValue = convertCode.ConvertToCode(seed);
                    }
                }
            }
            Connection.Close();
            return code;
        }

        public List<Code> GetCodes(int promotionID, int pageSize, int pageNumber, string alphabet)
        {

            var convertCode = new CodeConverter(alphabet);
            var codes = new List<Code>();

            var p = Pagination.PaginationPageNumber(pageSize, pageNumber);

            Connection.Open();

            using(var command = Connection.CreateCommand())
            {
                command.CommandText = @"Declare @codeIDStart int
                                        Declare @codeIDEnd int
                                        SET @codeIDStart = (SELECT CodeIDStart FROM Promotion WHERE ID = promotionID)
                                        SET @codeIDStart = (SELECT CodeIDEnd FROM Promotion WHERE ID = promotionID)
                                        
                                        Select * FROM Code WHERE ID BETWEEN @codeIDStart AND @codeIDEnd
                                        ORDER BY ID OFFSET @page ROWS FETCH NEXT @pageSize ROWS ONLY";
                command.Parameters.AddWithValue("@page", p);
                command.Parameters.AddWithValue("@pageSize", pageSize);
                command.Parameters.AddWithValue(@"promotionID", promotionID);

                using(var reader = command.ExecuteReader())
                {
                    while(reader.Read())
                    {
                        var code = new Code();

                        var seed = (int)reader["SeedValue"];

                        code.State = States.ConvertToString((byte)reader["State"]);

                        code.StringValue = convertCode.ConvertToCode(seed);

                        codes.Add(code);
                    }
                }
            }

            Connection.Close();
            return codes;

        }

        public List<Promotion> GetPromotions()
        {
            Connection.Open();

            var promotions = new List<Promotion>();

            using(var command = Connection.CreateCommand())
            {
                command.CommandText =@"SELECT * FROM Promotion";

                using(var reader = command.ExecuteReader())
                {
                    while(reader.Read())
                    {
                        var promotion = new Promotion()
                        {
                            ID = (int)reader["ID"],
                            Name = (string)reader["PromotionName"],
                            CodeIDStart = (int)reader["CodeIDStart"],
                            CodeIDEnd = (int)reader["CodeIDEnd"],
                            BatchSize = (int)reader["PromotionSize"],
                        };

                        promotions.Add(promotion);
                    }
                }
            }
            Connection.Close();
            return promotions;
        }

        public Promotion GetPromotionID(int id)
        {
            var promotion = new Promotion();

            Connection.Open();

            using(var command = Connection.CreateCommand())
            {
                command.CommandText =@"SELECT [ID], [PromotionName], [PromotionSize] FROM Promotion
                                    WHERE [ID] = @id";
                command.Parameters.AddWithValue("@id", id);

                using(var reader = command.ExecuteReader())
                {
                    if(reader.Read())
                    {
                        promotion.ID = (int)reader["ID"];
                        promotion.Name = (string)reader["PromotionName"];
                        promotion.BatchSize = (int)reader["PromotionSize"];
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            Connection.Close();
            return promotion;
        }

        public void DeactivateCode(string alphabet, string stringValue)
        {

            var convertCode = new CodeConverter(alphabet);

            var seedValue = convertCode.ConvertFromCode(stringValue);
            Connection.Open();

            using(var command = Connection.CreateCommand())
            {
                command.CommandText =@"Update Code SET [State] = @inactive WHERE [SeedValue] = @seedValue";
                command.Parameters.AddWithValue("@seedValue", seedValue);
                command.Parameters.AddWithValue("@inactive", States.Inactive);
                command.Parameters.AddWithValue("@active", States.Active);
                command.ExecuteNonQuery();
            }
        }

        public void DeactivatePromotion (Promotion promotion)
        {
            Connection.Open();

            using(var command = Connection.CreateCommand())
            {
                command.CommandText =@"Update Code SET [State] = @inactive WHERE ID BETWEEN @codeIDStart AND @codeIDEnd";
                command.Parameters.AddWithValue("@inactive", States.Inactive);
                command.Parameters.AddWithValue("@active", States.Active);
                command.Parameters.AddWithValue("@codeIDStart", promotion.CodeIDStart);
                command.Parameters.AddWithValue("@codeIDEnd", promotion.CodeIDEnd);
                command.ExecuteNonQuery();
            }
        }

        public bool CheckIfCodeCanBeRedeemed(int seedValue, string email)
        {
            var affected = 0;
            Connection.Open();

            var transaction = Connection.BeginTransaction();
            var command = Connection.CreateCommand();
            command.Transaction = transaction;

            try
            {
                command.CommandText = @"
                    UPDATE Code SET [State] = @redeemed
                    WHERE SeedValue = @seedValue
                    AND [State] = @active
                ";
                command.Parameters.AddWithValue("@redeemed", States.Redeemed);
                command.Parameters.AddWithValue("@active", States.Active);
                command.Parameters.AddWithValue("@seedValue", seedValue);
                command.Parameters.AddWithValue("@email", email);
                affected = command.ExecuteNonQuery();

                command.CommandText = @"
                    INSERT INTO RedeemedList (CodeSeedValue, Email)
                    VALUES (@seedValue, @email)
                ";
                command.ExecuteNonQuery();
                transaction.Commit();
            }
            catch(Exception e)
            {
                transaction.Rollback();
                return false;
            }

            Connection.Close();

            if(affected > 0)
            {
                return true;
            }
            return false;
        }

        public int PageCount(int id)
        {
            var pages = 0;
            var pageRemainder = 0;

            Connection.Open();

            using(var command = Connection.CreateCommand())
            {
                command.CommandText = @"SELECT PromotionSize FROM Promotion WHERE ID = id";
                command.Parameters.AddWithValue("@id", id);

                using(var reader = command.ExecuteReader())
                {
                    while(reader.Read())
                    {
                        var pageNumber = (int)reader["PromotionSize"];

                        pages = pageNumber / 10;

                        pageRemainder = pageNumber % 10;

                        if(pageRemainder > 0)
                        {
                            pages ++;
                        }
                    }
                }
            }
            Connection.Close();
            return pages;
        }
    }
}