using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models
{
    public class SteamLibrary
    {
        public SteamLibrary()
        {
            // Nothing to do
        }

        public SteamLibrary(string path)
        {
            this.Path = path;
            this.DoesExist = System.IO.Directory.Exists(path);
        }

        public SteamLibrary(string path, bool doesExist)
        {
            this.Path = path;
            this.DoesExist = doesExist;
        }

        public string? Path { get; set; }

        public bool DoesExist { get; set; }
    }
}
