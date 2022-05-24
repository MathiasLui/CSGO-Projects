using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamShared.SourceConfig
{
    public class SourceCFGCommandValue
    {
        public string? Value { get; set; }

        public int? GetInt()
        {
            if (int.TryParse(this.Value, out int parsed))
                return parsed;
            else
                return null;
        }

        public float? GetFloat()
        {
            if (float.TryParse(this.Value, out float parsed))
                return parsed;
            else
                return null;
        }

        public List<int?>? GetInts()
        {
            if (this.Value == null)
                return null;

            var res = new List<int?>();

            string[] values = this.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for(int i = 0; i < values.Length; i++)
            {
                if(int.TryParse(values[i], out int parsed))
                {
                    res.Add(parsed);
                }
            }

            if(res.Count > 0)
                return res;
            
            return null;
        }

        public List<float?>? GetFloats()
        {
            if (this.Value == null)
                return null;

            var res = new List<float?>();

            string[] values = this.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (int i = 0; i < values.Length; i++)
            {
                if (float.TryParse(values[i], out float parsed))
                {
                    res.Add(parsed);
                }
            }

            if (res.Count > 0)
                return res;

            return null;
        }
    }
}
