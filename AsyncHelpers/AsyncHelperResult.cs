using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncHelpers
{
    public class AsyncHelperResult<T>
    {
        public T? Result { get; internal set; }
        public DateTime TaskStarted { get; internal set; }
        public DateTime TaskEnded { get; internal set; }
        public bool HadErrors { get; internal set; }
        public Exception? Exception { get; internal set; }
    }

    public class AsyncHelperResult<T, E> : AsyncHelperResult<T>
    {
        public E? Entity { get; internal set; }
    }
}
