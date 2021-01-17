using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;

namespace TodoTree
{
    public record TimeRecord
    {
        public DateTime StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public bool IsRunning => EndDateTime == null;
        public TimeSpan? EstimateTime => EndDateTime - StartDateTime;

        public TimeRecord(DateTime startDateTime, DateTime? endDateTime) =>
            (StartDateTime, EndDateTime) = (startDateTime, endDateTime);
        
        public TimeRecord() { }
        

        public TimeRecord Stop(DateTime endDateTime)
        {
            return new TimeRecord(this.StartDateTime, endDateTime);
        }
    }
}
