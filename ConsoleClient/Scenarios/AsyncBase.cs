using ConsoleClient.OutputHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleClient.Scenarios
{
    public abstract class AsyncBase : DemoBase
    {
        public abstract Task ExecuteAsync(CancellationToken cancellationToken, IProgress<Progress> progress);
    }
}
