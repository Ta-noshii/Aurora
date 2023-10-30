﻿using RGB.NET.Core;
using RGB.NET.Devices.SteelSeries;

namespace AurorDeviceManager.Devices.RGBNet;

public class SteelSeriesRgbNetDevice : RgbNetDevice
{
    protected override IRGBDeviceProvider Provider => SteelSeriesDeviceProvider.Instance;

    public override string DeviceName => "SteelSeries (RGB.NET)";
}