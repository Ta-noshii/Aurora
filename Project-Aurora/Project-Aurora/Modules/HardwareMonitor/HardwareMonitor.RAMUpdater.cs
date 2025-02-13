﻿using System.Collections.Generic;
using System.Linq;
using LibreHardwareMonitor.Hardware;

namespace Aurora.Modules.HardwareMonitor;

public partial class HardwareMonitor
{
    public sealed class RamUpdater : HardwareUpdater
    {
        #region Sensors
        private readonly ISensor? _ramUsed;
        public float RamUsed => GetValue(_ramUsed);

        private readonly ISensor? _ramFree;
        public float RamFree => GetValue(_ramFree);
        #endregion

        public RamUpdater(IEnumerable<IHardware> hws)
        {
            hw = hws.FirstOrDefault(h => h.HardwareType == HardwareType.Memory);
            if (hw is null)
            {
                Global.logger.Error("[HardwareMonitor] Could not find hardware of type RAM or hardware monitoring is disabled");
                return;
            }
            _ramUsed = FindSensor("data/0");
            _ramFree = FindSensor("data/1");
        }
    }
}