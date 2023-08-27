using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shelly.Command
{
    internal abstract class AbstractCommand
    {
        public abstract AbstractCommand FromOpts();

        public abstract void Handle();
    }
}
