using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using Microsoft.Azure.Storage.Blob;


namespace CodeFlip.CodeJar.Api
{

    public class SQL
    {
        public SQL (string connectionString, Uri filePath,

        //arguments for the DownloadRangeToByteArray
        byte[] target, int index, long? blobOffset, long? length, Microsoft.Azure.Storage.AccessCondition accessCondition = null, 
        BlobRequestOptions options = null, Microsoft.Azure.Storage.OperationContext operationContext = null)
        {
            Connection = new SqlConnection(connectionString);
            
            var FilePath = new CloudBlockBlob(filePath).DownloadRangeToByteArray(target, index, blobOffset, length, accessCondition, options, operationContext);
        }

        public SqlConnection Connection {get; set;}
        public CloudBlockBlob FilePath {get; set;}

       
        public long[] UpdateOffset (int batchSize, SqlCommand command)
        {
            var firstAndLastOffset = new long[2];
            var offsetIncrement = batchSize * 4;

            command.CommandText = @"Update Offset Set OffsetValue = OffsetValue + @offsetIncrement
                                    OUTPUT INSERTED.OffsetValue
                                    WHERE ID = 1";
            command.Parameters.AddWithValue("@offsetIncrement", offsetIncrement);
            var updatedOffset = (long)command.ExecuteScalar();

            firstAndLastOffset[0] = updatedOffset - offsetIncrement;
            firstAndLastOffset[1] = updatedOffset;

            return firstAndLastOffset;
        }

        public void CreateDigitalCode (int batchSize, DateTime dateActive, SqlCommand command)
        {
            
            using(BinaryReader reader = new BinaryReader(FilePath));
            {
                var firstAndLastOffset = UpdateOffset(batchSize, command);

                if(firstAndLastOffset[0] % 4 != 0)
                {
                    throw new ArgumentException("Offset Must be divisable by 4");
                }

                for(var i = firstAndLastOffset[0]; i < firstAndLastOffset[1]; i +=4)
                {
                    reader.BaseStream.Position = i;

                    var seedValue = reader.ReadInt32();

                    command.Parameters.Clear();

                    command.CommandText = @"INSERT INTO Codes ([State], [SeedValue] VALUES (@seedValue, @stateGenerated))";
                    command.Parameters.AddWithValue("@stateGenerated", States.Generated);
                    command.Parameters.AddWithValue("@seedValue", seedValue);
                    command.ExecuteNonQuery();

                    if(dateActive.Date == DateTime.Now.Date)
                    {
                        command.CommandText = @"Update Codes SET [State] = @stateActive
                                                WHERE SeedValue = @seedValue";
                        command.Parameters.AddWithValue("@stateActive", States.Active);
                        command.Parameters.AddWithValue("@seedValue", seedValue);
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        public void CreateBatch(Promotion promotion)
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
                                        
                                        INSERT INTO Promotion (PromotionName, CodeIDStart, PromotionSize, DateActive, DateExpires)
                                        Values (@promotionName, @codeIDStart, @promotionSize, @dateActive, @dateActive)
                                        SELECT SCOPE_IDENTITY()";
                command.Parameters.AddWithValue("@promotionName", promotion.PromotionName);
                command.Parameters.AddWithValue("@promotionSize", promotion.BatchSize);
                command.Parameters.AddWithValue("@dateActive", promotion.DateActive);
                command.Parameters.AddWithValue("@dateExpires", promotion.DateExpires);
                promotion.ID = Convert.ToInt32(command.ExecuteScalar());

                CreateDigitalCode(promotion.BatchSize, promotion.DateActive, command);

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
                command.CommandText = @"SELECT * FROM Codes WHERE [SeedValue] = @seedValue";
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
                                        
                                        Select * FROM Codes WHERE ID BETWEEN @codeIDStart AND @codeIDEnd
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
                            PromotionName = (string)reader["PromotionName"],
                            CodeIDStart = (int)reader["CodeIDStart"],
                            CodeIDEnd = (int)reader["CodeIDEnd"],
                            BatchSize = (int)reader["PromotionSize"],
                            DateActive = (DateTime)reader["DateActive"],
                            DateExpires = (DateTime)reader["DateExpires"]
                        };

                        promotions.Add(promotion);
                    }
                }
            }
            Connection.Close();
            return promotions;
        }

        public void DeactivateCode(string alphabet, string stringValue)
        {

            var convertCode = new CodeConverter(alphabet);

            var seedValue = convertCode.ConvertFromCode(stringValue);
            Connection.Open();

            using(var command = Connection.CreateCommand())
            {
                command.CommandText =@"Update Codes SET [State] = @inactive WHERE [SeedValue] = @seedValue AND [State] = @active";
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
                command.CommandText =@"Update Codes SET [State] = @inactive WHERE ID BETWEEN @codeIDStart AND @codeIDEnd AND [State] = @active";
                command.Parameters.AddWithValue("@inactive", States.Inactive);
                command.Parameters.AddWithValue("@active", States.Active);
                command.Parameters.AddWithValue("@codeIDStart", promotion.CodeIDStart);
                command.Parameters.AddWithValue("@codeIDEnd", promotion.CodeIDEnd);
                command.ExecuteNonQuery();
            }
        }

        public int CheckIfCodeCanBeRedeemed(string alphabet, string stringValue)
        {
            var codeID = 0;

            var convertCode = new CodeConverter(alphabet);

            var seedValue = convertCode.ConvertFromCode(stringValue);

            Connection.Open();

            using(var command = Connection.CreateCommand())
            {
                command.CommandText = @"Update Codes SET [State] = @redeemed
                                        OUTPUT INSERTED.ID
                                         WHERE [SeedValue] = @seedValue
                                         AND [State] = @active";
                command.Parameters.AddWithValue("@redeemed", States.Redeemed);
                command.Parameters.AddWithValue("@active", States.Active);
                command.Parameters.AddWithValue("@seedValue", seedValue);
                codeID = (int)command.ExecuteScalar();
            }

            if(codeID != 0)
            {
                return codeID;
            }
            else
            {
                return -1;
            }
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