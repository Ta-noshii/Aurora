﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Aurora.EffectsEngine;
using ColorBox;
using Xceed.Wpf.Toolkit;

namespace Aurora.Settings.Layers.Controls;

/// <summary>
/// Interaction logic for Control_GradientLayer.xaml
/// </summary>
public partial class Control_GradientLayer
{
    private bool settingsset;

    public Control_GradientLayer()
    {
        InitializeComponent();
    }

    public Control_GradientLayer(GradientLayerHandler dataContext)
    {
        InitializeComponent();

        DataContext = dataContext;
    }

    public void SetSettings()
    {
        if (DataContext is not GradientLayerHandler || settingsset) return;
        wave_size_slider.Value = ((GradientLayerHandler)DataContext).Properties.GradientConfig.GradientSize;
        wave_size_label.Text = ((GradientLayerHandler)DataContext).Properties.GradientConfig.GradientSize + " %";
        effect_speed_slider.Value = ((GradientLayerHandler)DataContext).Properties._GradientConfig.Speed;
        effect_speed_label.Text = "x " + ((GradientLayerHandler)DataContext).Properties._GradientConfig.Speed;
        effect_angle.Text = ((GradientLayerHandler)DataContext).Properties._GradientConfig.Angle.ToString();
        effect_animation_type.SelectedValue = ((GradientLayerHandler)DataContext).Properties._GradientConfig.AnimationType;
        effect_animation_reversed.IsChecked = ((GradientLayerHandler)DataContext).Properties._GradientConfig.AnimationReverse;
        var brush = ((GradientLayerHandler)DataContext).Properties._GradientConfig.Brush.GetMediaBrush();
        try
        {
            gradient_editor.Brush = brush;
        }
        catch (Exception exc)
        {
            Global.logger.Error(exc, "Could not set brush");
        }

        KeySequence_keys.Sequence = ((GradientLayerHandler)DataContext).Properties._Sequence;

        settingsset = true;
    }

    private void Gradient_editor_BrushChanged(object? sender, BrushChangedEventArgs e)
    {
        if (IsLoaded && settingsset && DataContext is GradientLayerHandler && sender is ColorBox.ColorBox colorBox)
            ((GradientLayerHandler)DataContext).Properties._GradientConfig.Brush = new EffectBrush(colorBox.Brush);
    }

    private void Button_SetGradientRainbow_Click(object? sender, RoutedEventArgs e)
    {
        ((GradientLayerHandler)DataContext).Properties._GradientConfig.Brush = new EffectBrush(ColorSpectrum.Rainbow);

        var brush = ((GradientLayerHandler)DataContext).Properties._GradientConfig.Brush.GetMediaBrush();
        try
        {
            gradient_editor.Brush = brush;
        }
        catch (Exception exc)
        {
            Global.logger.Error(exc, "Could not set brush");
        }
    }

    private void Button_SetGradientRainbowLoop_Click(object? sender, RoutedEventArgs e)
    {
        ((GradientLayerHandler)DataContext).Properties._GradientConfig.Brush = new EffectBrush(ColorSpectrum.RainbowLoop);

        var brush = ((GradientLayerHandler)DataContext).Properties._GradientConfig.Brush.GetMediaBrush();
        try
        {
            gradient_editor.Brush = brush;
        }
        catch (Exception exc)
        {
            Global.logger.Error(exc, "Could not set brush");
        }
    }
    private void effect_speed_slider_ValueChanged(object? sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded || !settingsset || DataContext is not GradientLayerHandler || sender is not Slider slider) return;
        ((GradientLayerHandler)DataContext).Properties._GradientConfig.Speed = (float)slider.Value;

        if (effect_speed_label != null)
            effect_speed_label.Text = "x " + slider.Value;
    }

    private void wave_size_slider_ValueChanged(object? sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded || !settingsset || DataContext is not GradientLayerHandler || sender is not Slider) return;
        ((GradientLayerHandler)DataContext).Properties.GradientConfig.GradientSize = (float)e.NewValue;
                
        if (wave_size_label != null)
        {
            wave_size_label.Text = e.NewValue + " %";
            if (e.NewValue == 0)
            {
                wave_size_label.Text = "Stop";
            }
        }
        TriggerPropertyChanged();
    }

    private void effect_angle_ValueChanged(object? sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (!IsLoaded || !settingsset || DataContext is not GradientLayerHandler || sender is not IntegerUpDown integerUpDown) return;

        if (float.TryParse(integerUpDown.Text, out var outval))
        {
            integerUpDown.Background = new SolidColorBrush(Color.FromArgb(255, 24, 24, 24));

            ((GradientLayerHandler)DataContext).Properties._GradientConfig.Angle = outval;
        }
        else
        {
            integerUpDown.Background = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
            integerUpDown.ToolTip = "Entered value is not a number";
        }
        TriggerPropertyChanged();
    }

    private void effect_animation_type_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || !settingsset || DataContext is not GradientLayerHandler || sender is not ComboBox comboBox) return;

        ((GradientLayerHandler)DataContext).Properties._GradientConfig.AnimationType = (AnimationType)comboBox.SelectedValue;
        TriggerPropertyChanged();
    }

    private void effect_animation_reversed_Checked(object? sender, RoutedEventArgs e)
    {
        if (!IsLoaded || !settingsset || DataContext is not GradientLayerHandler || sender is not CheckBox checkBox) return;

        ((GradientLayerHandler)DataContext).Properties._GradientConfig.AnimationReverse = checkBox.IsChecked.HasValue ? checkBox.IsChecked.Value : false;
        TriggerPropertyChanged();
    }

    private void KeySequence_keys_SequenceUpdated(object? sender, RoutedPropertyChangedEventArgs<KeySequence> e)
    {
        if (!IsLoaded || !settingsset || DataContext is not GradientLayerHandler) return;

        ((GradientLayerHandler)DataContext).Properties._Sequence = e.NewValue;
        TriggerPropertyChanged();
    }

    private void UserControl_Loaded(object? sender, RoutedEventArgs e)
    {
        SetSettings();

        Loaded -= UserControl_Loaded;
    }

    protected void TriggerPropertyChanged()
    {
        var layerHandler = (GradientLayerHandler) DataContext;
        layerHandler.Properties.OnPropertiesChanged(this);
    }
}