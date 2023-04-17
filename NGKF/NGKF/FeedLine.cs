using Fortnite_API.Objects.V1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGKF
{
    public class FeedLine : EventArgs
    {
        public FeedLine(BrStatsV2V1 stats, DateTime timeStamp, string id)
        {
            Stats = stats;
            TimeStamp = timeStamp;
            Id = id;
        }

        public BrStatsV2V1 Stats { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Id { get; set; }
    }
}
