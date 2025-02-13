﻿using System.Windows.Controls;
using Aurora.Profiles;
using Aurora.Settings.Layers.Controls;
using Aurora.Settings.Overrides;

namespace Aurora.Settings.Layers
{
    [LogicOverrideIgnoreProperty("_PrimaryColor")]
    [LogicOverrideIgnoreProperty("_Opacity")]
    [LogicOverrideIgnoreProperty("_Enabled")]
    [LogicOverrideIgnoreProperty("_Sequence")]
    [LayerHandlerMeta(Order = -1, IsDefault = true)]
    public class DefaultLayerHandler : LayerHandler<LayerHandlerProperties>
    {
        protected override UserControl CreateControl()
        {
            return new Control_DefaultLayer();
        }
    }
}
