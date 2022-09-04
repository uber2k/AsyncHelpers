using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncHelpers
{
    public class ThrottledTaskResult<T>
    {
        public T? Result { get; internal set; }
        public DateTime TaskStarted { get; internal set; }
        public DateTime TaskEnded { get; internal set; }
        public bool HadErrors { get; internal set; }
        public Exception? Exception { get; internal set; }
    }
}
