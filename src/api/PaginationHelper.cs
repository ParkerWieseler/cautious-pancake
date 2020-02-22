using System;

namespace CodeFlip.CodeJar.Api
{
    public static class Pagination
    {
        public static int PaginationPageNumber(int pageSize, int pageNumber)
        {
            var p = pageNumber;

            p--;

            if(p > 0)
            {
                p *= pageSize;
            }
            return p;
        }
    }
}