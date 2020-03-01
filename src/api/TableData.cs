using System;
using System.Collections.Generic;

namespace CodeFlip.CodeJar.Api
{
    public class TableData
    {
        public TableData(List<Code> codes, int pages, int page)
        {
            Codes = new List<Code>(codes);
            Pages = pages;
            Page = page;
        }

        public List<Code> Codes {get; set;}

        public int Pages {get; set;}

        public int Page {get; set;}
    }
}