
// Copyright Christophe Bertrand.

//#define TIMER_DEBUGGING

using System;
using System.Diagnostics;
using System.Threading;

namespace UniversalSerializerResourceTests
{


	// #############################################################################

	public class ResourceCounter : IDisposable
	{
		[System.Runtime.InteropServices.DllImport("KERNEL32")]
		private static extern bool QueryPerformanceCounter(
				ref long lpPerformanceCount);

		[System.Runtime.InteropServices.DllImport("KERNEL32")]
		private static extern bool QueryPerformanceFrequency(
				ref long lpFrequency);

		readonly long startTime;
		long end;
		readonly long freqTime;
		public double ElapsedTimeInMs;

		readonly long WorkingSet64AtStart;
		public long WorkingSet64ConsumptionPeak;

		long WorkingSet64Peak;
		long GCPeak;
#if TIMER_DEBUGGING
		long[] times=new long[500];
		int timerCounter;
#endif


		readonly long initialGCMemory;
		public long GCConsumptionPeak;

		readonly System.Threading.Timer timer;

		public ResourceCounter()
		{
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();

				GCNotificationStatus ns = GC.WaitForFullGCComplete(300);

				this.WorkingSet64AtStart = Process.GetCurrentProcess().WorkingSet64;
			}

			{
				this.initialGCMemory = System.GC.GetTotalMemory(true);
				//Thread.Sleep(600);
			}

			timer = new System.Threading.Timer(TimerCallback, null, 60, 60);

			if (!QueryPerformanceFrequency(ref freqTime))
				throw new Exception();
			if (!QueryPerformanceCounter(ref this.startTime))
				throw new Exception();
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
#if TIMER_DEBUGGING
			long act=0;
			QueryPerformanceCounter(ref act);
			this.times[this.timerCounter++] = act;
#endif
		}

		public void StopAndGetResourceMesures()
		{
			if (!QueryPerformanceCounter(ref this.end))
				throw new Exception();
			this.timer.Dispose();
			long WorkingSet64AtEnd = Process.GetCurrentProcess().WorkingSet64;
			var finalMemory = System.GC.GetTotalMemory(false);


			this.WorkingSet64ConsumptionPeak = Math.Max(WorkingSet64AtEnd, this.WorkingSet64Peak) - this.WorkingSet64AtStart;
			this.GCConsumptionPeak = Math.Max(finalMemory, this.GCPeak) - initialGCMemory;
			this.ElapsedTimeInMs = (double)(this.end - this.startTime) * 1.0e3 / (double)freqTime;


#if TIMER_DEBUGGING
			double[] dtemps = new double[this.timerCounter];
			for (int i = 0; i < this.timerCounter; i++)
				dtemps[i] = (double)(this.times[i] - this.start) * 1.0e3 / (double)freq;
#endif

		}

		public void Dispose()
		{
			this.timer.Dispose();
		}
	}

	// #############################################################################
}