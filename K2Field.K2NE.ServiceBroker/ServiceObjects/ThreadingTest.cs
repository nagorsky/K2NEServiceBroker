using SourceCode.SmartObjects.Services.ServiceSDK.Objects;
using SourceCode.SmartObjects.Services.ServiceSDK.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;

namespace K2Field.K2NE.ServiceBroker
{
    class ThreadingTestSO : ServiceObjectBase
    {
        public ThreadingTestSO(K2NEServiceBroker api) : base(api) { }

        public override List<SourceCode.SmartObjects.Services.ServiceSDK.Objects.ServiceObject> DescribeServiceObjects()
        {
            ServiceObject so = Helper.CreateServiceObject("ThreadingTest", "A test to see if we can do multi threading in a broker. If takes a number of thread, number of loops and then does a random sleep and adds this time to a list.");

            so.Properties.Add(Helper.CreateProperty(Constants.Properties.Threadingtest.ThreadCount, SoType.Number, "The number of threads"));
            so.Properties.Add(Helper.CreateProperty(Constants.Properties.Threadingtest.ItemCount, SoType.Number, "How many loops to do"));
            so.Properties.Add(Helper.CreateProperty(Constants.Properties.Threadingtest.SleepTime, SoType.Number, "What the maximum sleeptime should be."));
            so.Properties.Add(Helper.CreateProperty(Constants.Properties.Threadingtest.ThreadId, SoType.Number, "The ID of the thread"));

            Method threadrun = Helper.CreateMethod(Constants.Methods.Threadingtest.RunThreads, "Run the threads.", MethodType.List);
            threadrun.InputProperties.Add(Constants.Properties.Threadingtest.ThreadCount);
            threadrun.InputProperties.Add(Constants.Properties.Threadingtest.ItemCount);
            threadrun.InputProperties.Add(Constants.Properties.Threadingtest.SleepTime);
            threadrun.ReturnProperties.Add(Constants.Properties.Threadingtest.ThreadId);
            threadrun.ReturnProperties.Add(Constants.Properties.Threadingtest.SleepTime);
            threadrun.ReturnProperties.Add(Constants.Properties.Threadingtest.ItemCount);
            threadrun.Validation.RequiredProperties.Add(Constants.Properties.Threadingtest.ThreadCount);
            threadrun.Validation.RequiredProperties.Add(Constants.Properties.Threadingtest.ItemCount);
            threadrun.Validation.RequiredProperties.Add(Constants.Properties.Threadingtest.SleepTime);
            so.Methods.Add(threadrun);

            return new List<ServiceObject>() { so };
        }

        public override void Execute()
        {
            int threadCount = base.GetIntProperty(Constants.Properties.Threadingtest.ThreadCount, true);
            int itemCount = base.GetIntProperty(Constants.Properties.Threadingtest.ItemCount, true);
            int sleepTime = base.GetIntProperty(Constants.Properties.Threadingtest.SleepTime, true);

            List<Thread> threads = new List<Thread>();

            ServiceObject serviceObject = base.ServiceBroker.Service.ServiceObjects[0];
            serviceObject.Properties.InitResultTable();

            Random r = new Random();
            for (int i = 0; i < threadCount; i++)
            {
                this.ServiceBroker.Logger.LogDebug("Starting thread {0} with count {1} and sleep {2}", i, itemCount, sleepTime);
                System.Threading.Thread.Sleep(r.Next(sleepTime));
                Thread t = StartThread(itemCount, sleepTime, i);
                threads.Add(t);
            }



            foreach (Thread t in threads)
            {
                t.Join();
            }
        }


        private Thread StartThread(int itemCount, int sleepTime, int threadNr)
        {
            Thread t = new Thread(() => RunThread(itemCount, sleepTime, threadNr));
            t.Start();
            return t;
        }

        private void RunThread(int itemCount, int sleepTime, int threadNr)
        {
            Random r = new Random();
            for (int i = 0; i < itemCount; i++)
            {
                int wait = r.Next(sleepTime);
                base.ServiceBroker.Logger.LogDebug("Thread {0} sleeping {1} for itemcount {2}", System.Threading.Thread.CurrentThread.ManagedThreadId, wait, i);
                System.Threading.Thread.Sleep(wait);
                

                lock (base.ServiceBroker.ServicePackage.ResultTable)
                {
                    DataTable results = base.ServiceBroker.ServicePackage.ResultTable;
                    DataRow dr = results.NewRow();

                    dr[Constants.Properties.Threadingtest.ThreadId] = System.Threading.Thread.CurrentThread.ManagedThreadId;
                    dr[Constants.Properties.Threadingtest.SleepTime] = wait;
                    dr[Constants.Properties.Threadingtest.ItemCount] = i;
                    results.Rows.Add(dr);
                }
            }
        }
    }
}
