using Core.Interfaces.Components.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LoggerTest.App
{
    public class LogAbuser
    {
        private Thread abuseThread;
        private readonly ILogger _logger;
        private static bool _isRunning;
        private ManualResetEvent _stopped;

        public int SleepTimeInMilliseconds { get; set; }
        public bool VeryAbusive { get; set; }
        public bool SuperAbusive { get; set; }
        public bool UltraAbusive { get; set; }

        public LogAbuser(ILogger logger)
        {
            _logger = logger;
        }

        public void Start()
        {
            _stopped = new ManualResetEvent(false);

            abuseThread = new Thread(new ThreadStart(AbusiveWorker));
            abuseThread.Start();


            if (VeryAbusive)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(AbusiveWorkerNumber), (object)"");
                ThreadPool.QueueUserWorkItem(new WaitCallback(AbusiveWorkerNegativeNumber), (object)"\t");
                ThreadPool.QueueUserWorkItem(new WaitCallback(AbusiveWorkerLetterUpper), (object)"\t\t");
                ThreadPool.QueueUserWorkItem(new WaitCallback(AbusiveWorkerLetterLower), (object)"\t\t\t");
            }

            if (SuperAbusive)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(AbusiveWorker), (object)"");

                ThreadPool.QueueUserWorkItem(new WaitCallback(AbusiveWorkerNumber), (object)"");
                ThreadPool.QueueUserWorkItem(new WaitCallback(AbusiveWorkerNegativeNumber), (object)"\t");
                ThreadPool.QueueUserWorkItem(new WaitCallback(AbusiveWorkerLetterUpper), (object)"\t\t");
                ThreadPool.QueueUserWorkItem(new WaitCallback(AbusiveWorkerLetterLower), (object)"\t\t\t");
            }
        }

        public void Stop()
        {
            _isRunning = false;

            _stopped.WaitOne();
        }

        private void AbusiveWorkerNumber(object o)
        {
            long count = 0;
            _isRunning = true;

            while (_isRunning)
            {
                count++;
                _logger.Log(o.ToString() + count.ToString());
                if(!UltraAbusive)
                    Thread.Sleep(SleepTimeInMilliseconds);
            }
        }


        private void AbusiveWorkerLetterUpper(object o)
        {
            string s = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            int index = 0;
            _isRunning = true;

            while (_isRunning)
            {
                _logger.Log(o.ToString() + s[index].ToString(), severity: LogMessageSeverity.Warning);
                if (!UltraAbusive)
                    Thread.Sleep(SleepTimeInMilliseconds);

                index++;
                if (index == 26)
                    index = 0;

            }
        }

        private void AbusiveWorkerLetterLower(object o)
        {
            string s = "abcdefghijklmnopqrstuvwxyz";
            int index = 0;
            _isRunning = true;

            while (_isRunning)
            {
                _logger.Log(o.ToString() + s[index].ToString());
                if (!UltraAbusive)
                    Thread.Sleep(SleepTimeInMilliseconds);

                index++;
                if (index == 26)
                    index = 0;

            }

            _stopped.Set();
        }


        private void AbusiveWorkerNegativeNumber(object o)
        {
            long count = 0;
            _isRunning = true;

            while (_isRunning)
            {
                count--;
                _logger.Log(o.ToString() + count.ToString(), severity: LogMessageSeverity.Error);
                if (!UltraAbusive)
                    Thread.Sleep(SleepTimeInMilliseconds);

            }
        }

        private void AbusiveWorker(object o)
        {
            AbusiveWorker();
        }

        private void AbusiveWorker()
        {
            _isRunning = true;

            while(_isRunning)
            {
                _logger.Log("Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore");
                _logger.Log("Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore");
                _logger.Log("Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore");
                _logger.Log("Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore");

                _logger.Log("Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat", severity: LogMessageSeverity.Warning);
                _logger.Log("Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat", severity: LogMessageSeverity.Warning);

                _logger.Log("Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur", severity: LogMessageSeverity.Error);
                _logger.Log("Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur", severity: LogMessageSeverity.Error);
                _logger.Log("Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur", severity: LogMessageSeverity.Error);

                _logger.Log("Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.", severity: LogMessageSeverity.Critical);

                if (!UltraAbusive)
                    Thread.Sleep(SleepTimeInMilliseconds);

            }

            _stopped.Set();
        }

    }
}
