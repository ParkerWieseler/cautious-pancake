using System;
using Microsoft.AspNetCore.Mvc;
using CodeFlip.CodeJar.Api.Models;
using Microsoft.Extensions.Configuration;

namespace CodeFlip.CodeJar.Api.Controllers
{
    [ApiController]
    public class CampaginsController : ControllerBase
    {

        private readonly IConfiguration _config;

        [HttpGet("campaigns")]
        public IActionResult GetAllCampaigns()
        {
            var sql = new SQL(_config.GetConnectionString("Storage"), _config.GetSection("SeedBlobUrl"));

            sql.GetPromotions();
            return Ok();
            
        }



        [HttpGet("campaigns/{id}")]
        public IActionResult GetCampaign(int id, [FromQuery] int page)
        {

            return Ok();
           
        }

        [HttpPost("campaigns")]
        public IActionResult CreateCampaign(Promotion promotion)
        {
            
            if(promotion.DateActive < promotion.DateExpires && promotion.DateActive.Date <= promotion.DateExpires)
            {
                var sql = new SQL(_config.GetConnectionString("Storage"), _config.GetSection("SeedBlobUrl");

                sql.CreateBatch(promotion);
                return Ok(promotion);
            }
            else
            {
                return BadRequest();
            }
           
        }

        [HttpDelete("campaigns/{id}")]
        public IActionResult DeactivateCampaign(int id, [FromBody] Promotion promotion)
        {
            //run the deactivate Promotion method from the SQL class.
            var sql = new SQL(_config.GetConnectionString("Storage"), _config.GetSection("SeedBlobUrl"));

            sql.DeactivatePromotion(promotion);

            return Ok();
        }

        [HttpGet("campaigns/{id}/codes")]
        public IActionResult GetCodes([FromRoute] int id, [FromQuery] int page)
        {
            var sql = new SQL(_config.GetConnectionString("Storage"), _config.GetSection("SeedBlobUrl"));

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
            
            var sql = new SQL(_config.GetConnectionString("Storage"), _config.GetSection("SeedBlobUrl"));

            for (var i = 1; i <= code.Length; i++)
            {
                sql.DeactivateCode(code[i - 1], alphabet);
            }
            return Ok();
        }

        [HttpPost("codes/{code}")]
        public IActionResult RedeemCode([FromRoute] string code)
        {
            var connectionString = _config.GetConnectionString("Storage");

            var alphabet = _config.GetSection("Base26")["alphabet"];
            
            var sql = new SQL(_config.GetConnectionString("Storage"), _config.GetSection("SeedBlobUrl"));

            var codeID = sql.CheckIfCodeCanBeRedeemed(code, alphabet);

            if (codeID != -1)
            {
                return Ok(codeID);
            }
            return BadRequest();
        }
    }
}
