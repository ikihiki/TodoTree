using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TodoTree
{
    public class TimeRecordCollection
    {
        private readonly List<TimeRecord> timeRecords = new List<TimeRecord>();
        public IEnumerable<TimeRecord> TimeRecords => timeRecords;

        public TimeRecordCollection()
        {

        }

        public TimeRecordCollection(IEnumerable<TimeRecord> timeRecords)
        {
            this.timeRecords.AddRange(timeRecords);
        }

        public bool IsRunning => timeRecords.Count != 0 && timeRecords[^1].IsRunning;

        public void Start()
        {
            if (IsRunning)
            {
                return;
            }

            this.timeRecords.Add(new TimeRecord(DateTime.Now, null));
        }

        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }

            this.timeRecords[^1] = timeRecords[^1].Stop(DateTime.Now);
        }

        public TimeSpan GetEstimateTime()
        {
            var now = DateTime.Now;
            return this.timeRecords.Aggregate(TimeSpan.Zero,
                (total, record) => total + record.EstimateTime ?? now - record.StartDateTime);
        }
    }
}
