﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamShared.Models
{
    public class Settings
    {
        public SteamHelper SteamHelper = new SteamHelper();

        public CsgoHelper CsgoHelper = new CsgoHelper();

        public CsgoSocketConnection CsgoSocket = new CsgoSocketConnection();
    }
}
