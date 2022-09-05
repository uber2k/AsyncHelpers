using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples
{
    internal class MockRemoteFile
    {
        public string? Url { get; set; }
        public int Seconds { get; set; }

        internal Task Download()
        {
            return Task.Delay(Seconds * 1000);
        }
        internal Task Delete()
        {
            return Task.Delay(Seconds * 100);
        }
    }
}
