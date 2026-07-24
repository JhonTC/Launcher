using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Launcher
{
    public class CustomTab
    {
        public string header { get; set; }

        public VisualBuild clientBuild { get; set; }
        public VisualBuild serverBuild { get; set; }

        public CustomTab(string _header, VisualBuild _clientBuild, VisualBuild _serverBuild)
        {
            header = _header;

            clientBuild = _clientBuild;
            serverBuild = _serverBuild;
        }
    }
}
