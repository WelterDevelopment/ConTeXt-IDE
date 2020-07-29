using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monaco.Monaco
{
    public sealed class FindMatch
    {
        public void FindMatchBrand() { }

        [JsonProperty("matches", NullValueHandling = NullValueHandling.Ignore)]
        public string[] Matches { get; set; }

        [JsonProperty("range", NullValueHandling = NullValueHandling.Ignore)]
        public Range Range { get; set; }

        //public FindMatch(Range range, IEnumerable<string> matches)
        //{
        //    Range = range;
        //    Matches = matches.ToArray();
        //}
    }

    //public sealed class FindMatchList : List<FindMatch>
    //{
    //    public FindMatchList()
    //    {

    //    }
    //}
}
