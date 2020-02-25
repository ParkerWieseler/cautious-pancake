using System;

namespace CodeFlip.CodeJar.Api.Models
{
    public class Promotion
    {
         public int ID {get; set;}
        public string Name {get; set;}
        public int CodeIDStart {get; set;}
        public int CodeIDEnd {get; set;}
        public int BatchSize {get; set;}
    }
}