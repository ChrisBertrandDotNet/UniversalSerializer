
// Copyright Christophe Bertrand.

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace UniversalSerializerResourceTests
{


	// #############################################################################

	public class ResourceCounter : IDisposable
	{
		public double ElapsedTimeInMs { get { return sw.ElapsedMilliseconds; } }

		readonly long WorkingSet64AtStart;
		public long WorkingSet64ConsumptionPeak;

		long WorkingSet64Peak;
		long GCPeak;

		readonly long initialGCMemory;
		public long GCConsumptionPeak;

		readonly System.Threading.Timer timer;
		readonly Stopwatch sw;

		/// <summary>
		/// Initializes then starts counting now.
		/// </summary>
		public ResourceCounter(int periodAsMs = 60)
		{
			this.sw = new Stopwatch();

			ResourceCounter.CleanRAM();

			this.initialGCMemory = System.GC.GetTotalMemory(true);
			this.WorkingSet64AtStart = Process.GetCurrentProcess().WorkingSet64;

			timer = new System.Threading.Timer(TimerCallback, null, periodAsMs, periodAsMs);

			sw.Start();
		}

		/// <summary>
		/// Lets GC do a large collection.
		/// </summary>
		public static void CleanRAM()
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			GCNotificationStatus ns = GC.WaitForFullGCComplete(300);
		}

		// We measure the RAM usage periodically.
		void TimerCallback(object state)
		{
			var currentWS = Process.GetCurrentProcess().WorkingSet64;
			if (currentWS > this.WorkingSet64Peak)
				this.WorkingSet64Peak = currentWS;
			var currentGC = System.GC.GetTotalMemory(false);
			if (currentGC > this.GCPeak)
				this.GCPeak = currentGC;
		}

		public void StopAndGetResourceMesures()
		{
			sw.Stop();
			this.timer.Dispose();
			long WorkingSet64AtEnd = Process.GetCurrentProcess().WorkingSet64;
			var finalMemory = System.GC.GetTotalMemory(false);

			this.WorkingSet64ConsumptionPeak = Math.Max(WorkingSet64AtEnd, this.WorkingSet64Peak) - this.WorkingSet64AtStart;
			this.GCConsumptionPeak = Math.Max(finalMemory, this.GCPeak) - initialGCMemory;
		}

		public string Abstract
		{
			get
			{
				return string.Format("{0} ms ellapsed. {1} MB peak RAM used (GC). {2} MB peak RAM used (WorkingSet).",
					this.ElapsedTimeInMs,
					this.GCConsumptionPeak / (1024L * 1024L),
					this.WorkingSet64ConsumptionPeak / (1024L * 1024L));
			}
		}

		public string AbstractAndProcessRAMState
		{
			get
			{
				return this.Abstract + "\n" + ResourceCounter.ProcessRAMState;
			}
		}

		public static string ProcessRAMStateAferCleaning
		{
			get
			{
				ResourceCounter.CleanRAM();
				return "After cleaning, " + ResourceCounter.ProcessRAMState;
			}
		}

		/// <summary>
		/// Get current process RAM state.
		/// </summary>
		public static string ProcessRAMState
		{
			get
			{
				return string.Format("Current process RAM = {0} MB (GC) ; {1} MB (WorkingSet).",
					System.GC.GetTotalMemory(false) / (1024L * 1024L), Process.GetCurrentProcess().WorkingSet64 / (1024L * 1024L));
			}
		}

		public void Dispose()
		{
			this.timer.Dispose();
		}
	}
}