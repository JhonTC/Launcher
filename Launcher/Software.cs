using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Launcher
{
    public class Software
    {
        public string name { get; set; }
        public Build[] builds { get; set; }

        internal Software(string _name, Build[] _builds)
        {
            name = _name;
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
