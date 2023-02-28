using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigManager
{
    public class Settings
    {
        private bool m_bCheckSubfolders = false;
        private bool m_bOnlyShowFavourites = false;
        private bool m_bSearchCaseSensitive = false;

        public bool CheckSubfolders
        {
            get { return m_bCheckSubfolders; }
            set { m_bCheckSubfolders = value; }
        }

        public bool OnlyShowFavourites
        {
            get { return m_bOnlyShowFavourites; }
            set { m_bOnlyShowFavourites = value; }
        }

        public bool SearchCaseSensitive
        {
            get { return m_bSearchCaseSensitive; }
            set { m_bSearchCaseSensitive = value; }
        }
    }
}
