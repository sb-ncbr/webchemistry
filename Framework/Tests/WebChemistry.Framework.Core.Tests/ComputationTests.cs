using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using WebChemistry.Framework.Core;
using System.Reactive.Concurrency;

namespace WebChemistry.Framework.Core.Tests
{
    [TestClass]
    public class ComputationTests
    {
        [TestInitialize]
        public void Init()
        {
            Computation.DefaultScheduler = Scheduler.CurrentThread;
        }

        [TestMethod]
        public void TestComputation()
        {
            List<ProgressTick> ticks = new List<ProgressTick>();

            var comp = Computation.Create(p => { p.UpdateStatus("test status"); p.UpdateProgress(42, 50); return 1; })
                .ObservedOn(Scheduler.CurrentThread)
                .WhenProgressUpdated(t => ticks.Add(t));
            var ret = comp.RunSynchronously();

            Assert.AreEqual(1, ret);
            Assert.AreEqual("test status", ticks[1].StatusText);
            Assert.AreEqual(42, ticks[2].Current);
            Assert.AreEqual(4, ticks.Count);
        }

        [TestMethod]
        public void TestRepeatedComputationCall()
        {
            List<ProgressTick> ticks = new List<ProgressTick>();

            var comp = Computation.Create(p => { return 1; })
                 .ObservedOn(Scheduler.CurrentThread)
                 .WhenProgressUpdated(t => ticks.Add(t));

            comp.RunSynchronously();

            try
            {
                comp.RunSynchronously();
                Assert.Fail();
            }
            catch (Exception e)
            {
                if (!(e is InvalidOperationException)) Assert.Fail();

            }
        }

        [TestMethod]
        public void TestComputationExceptions()
        {
            bool reactedOnException = false;
            string message = null;
            try
            {
                Computation.Create(() => { throw new ArgumentException("message"); })
                    .ObservedOn(Scheduler.CurrentThread)
                    .WhenError(e =>
                        {
                            reactedOnException = true;
                            message = e.Message;
                        })
                    .RunSynchronously();
            }
            catch
            {
            }

            Assert.AreEqual("message", message);
            Assert.IsTrue(reactedOnException);
        }

        [TestMethod]
        public void TestOnCompleted()
        {
            var comp = Computation.Create(p => { return 2; }).ObservedOn(Scheduler.CurrentThread);
            int ret = 0;
            comp.WhenCompleted(r => ret = r).RunSynchronously();
            Assert.AreEqual(2, ret);
        }
    }
}
