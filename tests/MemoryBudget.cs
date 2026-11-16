using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

// Solo para la prueba en un equipo de desarrollo. Windows aplica el límite al proceso medido.
internal sealed class MemoryBudget : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    private struct BasicLimits
    {
        internal long ProcessTime, JobTime;
        internal uint Flags;
        internal UIntPtr MinWorkingSet, MaxWorkingSet;
        internal uint ActiveProcesses;
        internal UIntPtr Affinity;
        internal uint Priority, Scheduling;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct ExtendedLimits
    {
        internal BasicLimits Basic;
        internal ulong ReadOperations, WriteOperations, OtherOperations, ReadBytes, WriteBytes, OtherBytes;
        internal UIntPtr ProcessMemory, JobMemory, PeakProcessMemory, PeakJobMemory;
    }

    [DllImport("kernel32.dll", SetLastError=true)]
    private static extern SafeFileHandle CreateJobObject(IntPtr attributes, string name);
    [DllImport("kernel32.dll", SetLastError=true)]
    private static extern bool SetInformationJobObject(SafeFileHandle job, int kind, ref ExtendedLimits limits, int length);
    [DllImport("kernel32.dll", SetLastError=true)]
    private static extern bool AssignProcessToJobObject(SafeFileHandle job, IntPtr process);
    private readonly SafeFileHandle job;

    internal MemoryBudget(uint bytes)
    {
        job = CreateJobObject(IntPtr.Zero, null);
        var limits = new ExtendedLimits();
        limits.Basic.Flags = 0x100; // JOB_OBJECT_LIMIT_PROCESS_MEMORY
        limits.ProcessMemory = new UIntPtr(bytes);
        using (var process = Process.GetCurrentProcess())
        {
            if (job.IsInvalid || !SetInformationJobObject(job, 9, ref limits, Marshal.SizeOf(limits)) ||
                !AssignProcessToJobObject(job, process.Handle))
            {
                int error = Marshal.GetLastWin32Error(); job.Dispose(); throw new Win32Exception(error);
            }
        }
    }
    public void Dispose() { job.Dispose(); }
}
