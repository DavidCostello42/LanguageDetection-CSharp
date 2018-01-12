using System;
using System.Collections.Generic;
using System.Text;

namespace com.deltamango.LanguageDetection.ConsoleDetection
{
    public class ThreadResult
    {
        public String ThreadId { get; set; }
        public Dictionary<String, List<double>> Scores { get; set; }
        public double ExecutionTime { get; set; }

        public ThreadResult()
        {
            ThreadId = String.Empty;
            Scores = new Dictionary<string, List<double>>();
            ExecutionTime = 0.0;
        }

        public ThreadResult(String id, Dictionary<String, List<double>> scores, double executionTime)
        {
            ThreadId = id;
            Scores = scores;
            ExecutionTime = executionTime;
        }
    }
}
