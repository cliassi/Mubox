﻿using System;
using System.Collections.Generic;

namespace Mubox.Control
{
    public class Performance
    {
        public static bool IsPerformanceEnabled { get; private set; }

        static Performance()
        {
            IsPerformanceEnabled = System.Diagnostics.Debugger.IsAttached;
            if (IsPerformanceEnabled)
            {
                try
                {
                    if (!System.Diagnostics.PerformanceCounterCategory.Exists("Mubox"))
                    {
                        List<System.Diagnostics.CounterCreationData> counterCreationDataList = new List<System.Diagnostics.CounterCreationData>();
                        counterCreationDataList.Add(new System.Diagnostics.CounterCreationData("Flags", "", System.Diagnostics.PerformanceCounterType.NumberOfItemsHEX64));
                        counterCreationDataList.Add(new System.Diagnostics.CounterCreationData("Total", "", System.Diagnostics.PerformanceCounterType.NumberOfItems64));
                        counterCreationDataList.Add(new System.Diagnostics.CounterCreationData("Rate", "", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64));

                        System.Diagnostics.PerformanceCounterCategory.Create(
                            "Mubox",
                            "",
                            System.Diagnostics.PerformanceCounterCategoryType.MultiInstance,
                            new System.Diagnostics.CounterCreationDataCollection(counterCreationDataList.ToArray()));
                    }
                    AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
                }
                catch (Exception ex)
                {
                    ex.Log();
                    IsPerformanceEnabled = false;
                }
            }
            else
            {
                ("PerformanceNotEnabled for Mubox").Log();
            }
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            if (!IsPerformanceEnabled)
            {
                return;
            }
            ("Removing Perf Counters").Log();
            // destroy all perf objects
            try
            {
                lock (destructionList)
                {
                    foreach (Performance performanceObject in destructionList)
                    {
                        performanceObject.Flags.Close();
                        performanceObject.Total.Close();
                        performanceObject.Rate.Close();

                        performanceObject.Flags = null;
                        performanceObject.Total = null;
                        performanceObject.Rate = null;
                    }
                }

                // delete category
                if (System.Diagnostics.PerformanceCounterCategory.Exists("Mubox"))
                {
                    System.Diagnostics.PerformanceCounterCategory.Delete("Mubox");
                }

                ("Removed Perf Counters").Log();
            }
            catch (Exception ex)
            {
                ("Did Not Remove Perf Counters").Log();
                ex.Log();
            }
        }

        public string InstanceName { get; set; }

        public System.Diagnostics.PerformanceCounter Flags { get; private set; }

        public System.Diagnostics.PerformanceCounter Total { get; private set; }

        public System.Diagnostics.PerformanceCounter Rate { get; private set; }

        private static List<Performance> destructionList = new List<Performance>();

        public static Performance CreatePerformance(string instanceName)
        {
            if (!IsPerformanceEnabled)
            {
                return null;
            }
            lock (destructionList)
            {
                Performance performance = null;
                try
                {
                    performance = new Performance
                    {
                        InstanceName = instanceName,
                        Flags = new System.Diagnostics.PerformanceCounter("Mubox", "Flags", instanceName, false),
                        Total = new System.Diagnostics.PerformanceCounter("Mubox", "Total", instanceName, false),
                        Rate = new System.Diagnostics.PerformanceCounter("Mubox", "Rate", instanceName, false)
                    };
                    destructionList.Add(performance);
                }
                catch (Exception ex)
                {
                    ex.Log();
                }
                return performance;
            }
        }

        public void Count(long flags)
        {
            Flags.RawValue = flags;
            Total.Increment();
            Rate.Increment();
        }
    }
}