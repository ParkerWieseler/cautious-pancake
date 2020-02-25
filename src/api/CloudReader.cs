using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Azure.Storage.Blob;
using System.Threading;
using System.Threading.Tasks;

namespace CodeFlip.CodeJar.Api
{
    public class CloudReader
    {
        public CloudReader(string filePath)
        {
            FilePath = new Uri(filePath);
        }

        public Uri FilePath { get; private set; }

        public List<Code> GenerateCodes(long[] offset)
        {
            var codes = new List<Code>();
            
            var file = new CloudBlockBlob(FilePath);

            for(var i = offset[0]; i < offset[1]; i += 4)
            {
                var bytes = new byte[4];
                file.DownloadRangeToByteArray(bytes, index: 0, blobOffset: i, length: 4);
                var seedValue = BitConverter.ToInt32(bytes, 0);
                var code = new Code()
                {
                    SeedValue = seedValue
                };
                codes.Add(code);
            }

            return codes;
        }
    }
}
