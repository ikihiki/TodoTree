using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;

namespace TodoTree
{
    public record TimeRecord
    {
        public DateTimeOffset StartDateTime { get; set; }
        public DateTimeOffset? EndDateTime { get; set; }
        public bool IsRunning => EndDateTime == null;
        public TimeSpan? EstimateTime => EndDateTime - StartDateTime;

        public TimeRecord(DateTimeOffset startDateTime, DateTimeOffset? endDateTime) =>
            (StartDateTime, EndDateTime) = (startDateTime, endDateTime);
        
        public TimeRecord() { }
        

        public TimeRecord Stop(DateTimeOffset endDateTime)
        {
            return new TimeRecord(this.StartDateTime, endDateTime);
        }
    }
}
