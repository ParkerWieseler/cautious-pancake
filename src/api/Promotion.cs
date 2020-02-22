using System;

namespace CodeFlip.CodeJar.Api
{
    public class Promotion
    {
        public int ID {get; set;}
        public string PromotionName {get; set;}
        public int CodeIDStart {get; set;}
        public int CodeIDEnd {get; set;}
        public int BatchSize {get; set;}
        public DateTime DateActive {get;set;}
        public DateTime DateExpires {get; set;}
    }
}