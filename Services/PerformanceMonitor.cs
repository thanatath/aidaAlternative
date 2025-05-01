using LibreHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace aidaAlternative.Services
{
    public class PerformanceMonitor : IDisposable
    {
        private PerformanceCounter cpuCounter;
        private PerformanceCounter ramCounter;
        private List<PerformanceCounter> networkDownloadCounters;
        private List<PerformanceCounter> networkUploadCounters;
        private Computer computer;

        public PerformanceMonitor()
        {
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            networkDownloadCounters = new List<PerformanceCounter>();
            networkUploadCounters = new List<PerformanceCounter>();
            var networkCategory = new PerformanceCounterCategory("Network Interface");
            foreach (var instance in networkCategory.GetInstanceNames())
            {
                networkDownloadCounters.Add(new PerformanceCounter("Network Interface", "Bytes Received/sec", instance));
                networkUploadCounters.Add(new PerformanceCounter("Network Interface", "Bytes Sent/sec", instance));
            }

            computer = new Computer { IsGpuEnabled = true, IsNetworkEnabled = true };
            computer.Open();
        }

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);

        private float GetTotalRam()
        {
            long memKb;
            GetPhysicallyInstalledSystemMemory(out memKb);
            return memKb / 1024f;
        }

        public Dictionary<string, (string Value, float? Percentage)> GetStats()
        {
            var stats = new Dictionary<string, (string, float?)>();

            try
            {
                // CPU
                float cpuUsage = cpuCounter.NextValue();
                stats["CPU"] = ($"{cpuUsage:F1}%", cpuUsage / 100f);

                // RAM
                float ramAvailable = ramCounter.NextValue();
                float totalRam = GetTotalRam() / 1024;
                float ramUsed = totalRam - ramAvailable / 1024;
                stats["RAM"] = ($"{ramUsed:F1} GB / {totalRam:F1} GB", ramUsed / totalRam);

                // Network
                float downloadSpeed = networkDownloadCounters.Sum(counter => counter.NextValue()) / (1024f * 1024f) * 10;
                float uploadSpeed = networkUploadCounters.Sum(counter => counter.NextValue()) / (1024f * 1024f) * 10;
                stats["Download"] = ($"{downloadSpeed:F1} MB/s", null);
                stats["Upload"] = ($"{uploadSpeed:F1} MB/s", null);

                // GPU
                float? gpuUsage = null, gpuVramUsed = null, gpuVramTotal = null, gpuTemp = null;
                foreach (var hardware in computer.Hardware)
                {
                    hardware.Update();
                    if (hardware.HardwareType == HardwareType.GpuNvidia || hardware.HardwareType == HardwareType.GpuAmd)
                    {
                        foreach (var sensor in hardware.Sensors)
                        {
                            if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("GPU Core"))
                                gpuUsage = sensor.Value;
                            else if (sensor.SensorType == SensorType.SmallData && sensor.Name.Contains("GPU Memory Used"))
                                gpuVramUsed = sensor.Value / 1024;
                            else if (sensor.SensorType == SensorType.SmallData && sensor.Name.Contains("GPU Memory Total"))
                                gpuVramTotal = sensor.Value / 1024;
                            else if (sensor.SensorType == SensorType.Temperature && sensor.Name.Contains("GPU Core"))
                                gpuTemp = sensor.Value;
                        }
                    }
                }

                stats["GPU Usage"] = (gpuUsage.HasValue ? $"{gpuUsage:F1}%" : "N/A", gpuUsage.HasValue ? gpuUsage / 100f : null);
                stats["GPU VRAM"] = (gpuVramUsed.HasValue && gpuVramTotal.HasValue
                    ? $"{gpuVramUsed:F1} GB / {gpuVramTotal:F1} GB"
                    : "N/A", gpuVramUsed.HasValue && gpuVramTotal.HasValue ? gpuVramUsed.Value / gpuVramTotal.Value : null);
                stats["GPU Temp"] = (gpuTemp.HasValue ? $"{gpuTemp:F1} °C" : "N/A", null);
            }
            catch (Exception ex)
            {
                stats["CPU"] = ($"Error: {ex.Message}", null);
            }

            return stats;
        }

        public void Dispose()
        {
            cpuCounter?.Dispose();
            ramCounter?.Dispose();
            networkDownloadCounters?.ForEach(c => c.Dispose());
            networkUploadCounters?.ForEach(c => c.Dispose());
            computer?.Close();
        }
    }
}