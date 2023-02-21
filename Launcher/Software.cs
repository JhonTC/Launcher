using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Launcher
{
    class Software
    {
        public Build[] builds;

        internal Software(Build[] _builds)
        {
            builds = _builds;
        }

        public Task[] CheckForUpdates()
        {
            var tasks = new Task[builds.Length];

            for (int i = 0; i < builds.Length; i++)
            {
                tasks[i] = builds[i].CheckForUpdates();
            }

            return tasks;
        }
    }
}
