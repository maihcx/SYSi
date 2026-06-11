namespace SYSi.Models;

/// <summary>
/// Immutable snapshot of all hardware data collected in one parallel pass.
/// </summary>
public sealed class HardwareSnapshot
{
    public CpuInfo Cpu { get; set; } = new();

    public List<GpuInfo> Gpus { get; set; } = [];

    public RamInfo Ram { get; set; } = new();

    public List<StorageDriveInfo> Drives { get; set; } = [];

    public MotherboardInfo Motherboard { get; set; } = new();

    public List<NetworkAdapterInfo> Networks { get; set; } = [];
}