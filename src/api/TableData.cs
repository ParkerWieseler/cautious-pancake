using System;
using System.Collections.Generic;

namespace CodeFlip.CodeJar.Api
{
    public class TableData
    {
        public TableData(List<Code> codes, int pages)
        {
            Codes = codes;
            Pages = pages;
        }

        public List<Code> Codes {get; set;} = new List<Code>();

        public int Pages {get; set;}
    }
}