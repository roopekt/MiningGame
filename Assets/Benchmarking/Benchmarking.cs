using System.Diagnostics;
using System.Collections.Generic;
using System;

public static class Benchmarking
{
    private static readonly Stopwatch stopwatch = new Stopwatch();
    public static bool testActive { get; private set; } = false;
    private static List<Test> tests = new List<Test>();
    private static Test activeTest = null;

    public static void StartMeasurement(string name, uint reportFrequency = 100)
    {
        if (testActive)
        {
            UnityEngine.Debug.LogError("BenchMarking.StartMeasurement: Can't start a new measurement while one is already running (" + activeTest.name + ").");
            stopwatch.Stop();
            testActive = false;
            return;
        }
        testActive = true;

        //find or create a test
        int index = tests.FindIndex(t => t.name == name);
        if (index == -1) {
            activeTest = new Test(name, reportFrequency);
            tests.Add(activeTest);
            UnityEngine.Debug.Log("BenchMarking.StartMeasurement: new test created (" + name + ").");
        }
        else {
            activeTest = tests[index];
        }

        //start stopwatch
        stopwatch.Restart();
    }

    public static void StopMeasurement()
    {
        stopwatch.Stop();
        double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;

        if (!testActive)
        {
            UnityEngine.Debug.LogError("BenchMarking.StopMeasurement: Can't stop a measurement while none running.");
            return;
        }
        testActive = false;

        activeTest.AddResult(elapsedMs);
    }

    private class Test
    {
        public readonly string name;
        public readonly uint reportFreq;//how many measurements between reports

        uint measurementCount = 0;//measurements since last report
        double totalTime = 0d;//all measurements since last report added together (ms)
        double maxTime = 0d;//longest time since last report (ms)

        public Test(string name, uint reportFrequency)
        {
            this.name = name;
            this.reportFreq = reportFrequency;
        }

        public void AddResult(double elapsedMs)
        {
            ++measurementCount;
            totalTime += elapsedMs;
            maxTime = Math.Max(maxTime, elapsedMs);

            if (measurementCount >= reportFreq)
                Report();
        }

        private void Report()//print a report to console and reset
        {
            //format message
            string message = string.Format(
                "Benchmark report for: {0}\n" +
                "measurements: {1}\n" +
                "average time (ms): {2}\n" +
                "worst time (ms): {3}\n",
                name, measurementCount, totalTime / measurementCount, maxTime);

            UnityEngine.Debug.Log(message);

            //reset
            measurementCount = 0;
            totalTime = 0d;
            maxTime = 0d;
        }
    }
}