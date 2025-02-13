﻿using Aurora.EffectsEngine;
using Aurora.Settings;
using Aurora.Settings.Layers;
using Aurora.Settings.Overrides.Logic;
using System.Collections.Generic;
using System.Drawing;
using Aurora.Settings.Overrides.Logic.Boolean;
using DK = Common.Devices.DeviceKeys;

namespace Aurora.Profiles.TModLoader {
    public class TModLoaderProfile : ApplicationProfile {
        public override void Reset() {
            base.Reset();

            Layers = new System.Collections.ObjectModel.ObservableCollection<Layer>()
            {
                new Layer("Health", new PercentLayerHandler() {
                    Properties = new PercentLayerHandlerProperties() {
                        VariablePath = new VariablePath("Player/Health"),
                        MaxVariablePath = new VariablePath("Player/MaxHealth"),
                        _PrimaryColor = Color.FromArgb(255, 0, 0),
                        _SecondaryColor = Color.FromArgb(128, 0, 255),
                        _Sequence = new KeySequence(new[] {
                            DK.F1, DK.F2, DK.F3, DK.F4, DK.F5, DK.F6, DK.F7, DK.F8, DK.F9, DK.F10, DK.F11, DK.F12
                        }),
                        BlinkThreshold = 0.25
                    }
                }),
                new Layer("Mana", new PercentLayerHandler() {
                    Properties = new PercentLayerHandlerProperties() {
                        VariablePath = new VariablePath("Player/Mana"),
                        MaxVariablePath = new VariablePath("Player/MaxMana"),
                        _PrimaryColor = Color.FromArgb(0, 0, 255),
                        _SecondaryColor = Color.FromArgb(0, 0, 128),
                        _Sequence = new KeySequence(new[] {
                            DK.ONE, DK.TWO, DK.THREE, DK.FOUR, DK.FIVE, DK.SIX, DK.SEVEN, DK.EIGHT, DK.NINE, DK.ZERO, DK.MINUS, DK.EQUALS
                        }),
                        BlinkThreshold = 0.25
                    }
                }),
                new Layer("Background", new PercentGradientLayerHandler() {
                    Properties = new PercentGradientLayerHandlerProperties {
                        Gradient = new EffectBrush() {
                            type = EffectBrush.BrushType.Linear,
                            start = new PointF(0, 0),
                            end = new PointF(1, 0),
                            colorGradients = new SortedDictionary<double, Color> {
                                { 0, Color.White },
                                { 1, Color.DarkGray }
                            }
                        },
                        VariablePath = new VariablePath("Player/Depth"),
                        MaxVariablePath = new VariablePath("Player/MaxDepth"),
                        _PrimaryColor = Color.Transparent,
                        _SecondaryColor = Color.Transparent,
                        PercentType = PercentEffectType.AllAtOnce,
                        _Sequence = new KeySequence(Effects.Canvas.WholeFreeForm)
                    }
                },
                new OverrideLogicBuilder()
                    .SetLookupTable("_Gradient",new OverrideLookupTableBuilder<EffectBrush>()
                        .AddEntry(GetColorGradient(Color.Green, Color.DarkOliveGreen),
                                new BooleanGSIEnum("Player/Biome", GSI.Nodes.TerrariaBiome.Forest))
                        .AddEntry(GetColorGradient(Color.Blue, Color.DarkBlue),
                                new BooleanGSIEnum("Player/Biome", GSI.Nodes.TerrariaBiome.Snow))
                        .AddEntry(GetColorGradient(Color.Yellow, Color.SandyBrown),
                                new BooleanGSIEnum("Player/Biome", GSI.Nodes.TerrariaBiome.Desert))
                        .AddEntry(GetColorGradient(Color.Lime, Color.DarkGreen),
                                new BooleanGSIEnum("Player/Biome", GSI.Nodes.TerrariaBiome.Jungle))
                    )
                ),
            };
        }

        private static EffectBrush GetColorGradient(Color start, Color end)
        {
            return new EffectBrush()
            {
                type = EffectBrush.BrushType.Linear,
                start = new PointF(0, 0),
                end = new PointF(1, 0),
                colorGradients = new SortedDictionary<double, Color> {
                    { 0, start },
                    { 1, end }
                }
            };
        }
    }
}
