using System;
using Microsoft.AspNetCore.Mvc;
using CodeFlip.CodeJar.Api.Models;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;

namespace CodeFlip.CodeJar.Api.Controllers
{
    [ApiController]
    public class CampaginsController : ControllerBase
    {

         public CampaginsController(IConfiguration config)
        {
            _config = config;
        }

        private IConfiguration _config;

        [HttpGet("campaigns")]
        public IActionResult GetAllCampaigns()
        {
            var sql = new SQL(connectionString: _config.GetConnectionString("Storage"));
            
            return Ok(sql.GetPromotions());
            
        }

        [HttpGet("campaigns/{id}")]
        public IActionResult GetCampaign(int id, [FromQuery] int page)
        {

            var sql = new SQL(connectionString: _config.GetConnectionString("Storage"));

            var promotion = sql.GetPromotionID(id);

            if(promotion == null)
            {
                return NotFound();
            }

            return Ok(promotion);
           
        }

        [HttpPost("campaigns")]
        public IActionResult CreateBatch([FromBody] CreateCampaignRequest request)
        {
            if(request.CodesNumber >= 1 && request.CodesNumber <= 3000)
            {
            var sql = new SQL(connectionString: _config.GetConnectionString("Storage"));

            var file = new CloudReader(filePath: _config.GetSection("File")["SeedBlobUrl"]);

                var promotion = new Promotion()
                {
                    Name = request.Name,
                    BatchSize = request.CodesNumber
                };

                var firstAndLastOffset = sql.UpdateOffset(promotion.BatchSize);

                var codes = file.GenerateCodes(firstAndLastOffset);

                sql.CreateBatch(promotion, codes);
                return Ok(promotion);
            }

            return BadRequest();
        }

        [HttpDelete("campaigns/{id}")]
        public IActionResult DeactivateCampaign(int id, [FromBody] Promotion promotion)
        {
            var sql = new SQL(connectionString: _config.GetConnectionString("Storage"));

            sql.DeactivatePromotion(promotion);

            return Ok();
        }

        [HttpGet("campaigns/{id}/codes")]
        public IActionResult GetCodes([FromRoute] int id, [FromQuery] int page)
        {
            var sql = new SQL(connectionString: _config.GetConnectionString("Storage"));

            var file = new CloudReader(filePath: _config.GetSection("File")["SeedBlobUrl"]);

            var pageSize = Convert.ToInt32(_config.GetSection("Pagination")["PageNumber"]);

            var alphabet = _config.GetSection("Base26")["Alphabet"];

            var codes = sql.GetCodes(id, page, pageSize, alphabet);

            var pages = sql.PageCount(id);

            return Ok(new TableData(codes, pages));
        }

        [HttpDelete("campaigns/{campaignId}/codes/{code}")]
        public IActionResult DeactivateCode([FromRoute] int campaignId, [FromRoute] string[] code)
        {
            var connectionString = _config.GetConnectionString("Storage");
            var alphabet = _config.GetSection("Base26")["alphabet"];
            
            var sql = new SQL(connectionString: _config.GetConnectionString("Storage"));

            for (var i = 1; i <= code.Length; i++)
            {
                sql.DeactivateCode(code[i - 1], alphabet);
            }
            return Ok();
        }

        [HttpPost("codes/{code}")]
        public IActionResult RedeemCode([FromRoute] string code, [FromBody] string email)
        {
            var sql = new SQL(connectionString: _config.GetConnectionString("Storage"));

            var alphabet = new CodeConverter(_config.GetSection("Base26")["Alphabet"]);

            var seedValue = alphabet.ConvertFromCode(code);
            
            var isRedeemed = sql.CheckIfCodeCanBeRedeemed(seedValue, email);


            if(isRedeemed)
            {
                return Ok();
            }
            return BadRequest();

          
        }
    }
}
