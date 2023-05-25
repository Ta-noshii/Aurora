﻿using Aurora.Devices;
using Aurora.Settings;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;

namespace Aurora.Controls
{
    /// <summary>
    /// Interaction logic for Control_DeviceItem.xaml
    /// </summary>
    public partial class Control_DeviceItem
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public static readonly DependencyProperty DeviceProperty = DependencyProperty.Register("Device", typeof(DeviceContainer), typeof(Control_DeviceItem));

        public DeviceContainer Device
        {
            get => (DeviceContainer)GetValue(DeviceProperty);
            set
            {
                SetValue(DeviceProperty, value);

                UpdateControls();
            }
        }

        public Control_DeviceItem()
        {
            InitializeComponent();

            Timer update_controls_timer = new Timer(1000); //Update every second
            WeakEventManager<Timer, ElapsedEventArgs>.AddHandler(
                update_controls_timer,
                nameof(update_controls_timer.Elapsed),
                Update_controls_timer_Elapsed);
            update_controls_timer.Start();
        }

        private void Update_controls_timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                Dispatcher.Invoke(() => { if (IsVisible) UpdateControls(); });
            }
            catch (Exception ex)
            {
                Global.logger.Warn(ex.ToString());
            }
        }

        private void btnToggleOnOff_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button)
            {
                btnStart.Content = "Working...";
                btnStart.IsEnabled = false;
                var device = Device;
                Task.Run(async () =>
                {
                    await device.ActionLock.WaitAsync();
                    try
                    {
                        if (device.Device.IsInitialized)
                        {
                            await device.DisableDevice().ConfigureAwait(false);
                        }
                        else
                        {
                            await device.EnableDevice();
                        }
                    }
                    finally
                    {
                        device.ActionLock.Release();
                    }

                    Dispatcher.Invoke(UpdateControls);
                });
            }
        }

        private void btnToggleEnableDisable_Click(object sender, RoutedEventArgs e)
        {
            if (!Global.Configuration.EnabledDevices.Contains(Device.Device.GetType()))
            {
                Global.Configuration.EnabledDevices.Add(Device.Device.GetType());
                UpdateControls();
            }
            else
            {
                Global.Configuration.EnabledDevices.Remove(Device.Device.GetType());
                var device = Device;
                btnStart.Content = "Working...";
                btnStart.IsEnabled = false;
                Task.Run(async () =>
                {
                    await device.ActionLock.WaitAsync();
                    try
                    {
                        if (device.Device.IsInitialized)
                        {
                            await device.Device.ShutdownDevice();
                        }
                    }
                    finally
                    {
                        device.ActionLock.Release();
                    }

                    Dispatcher.Invoke(UpdateControls);
                });
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateControls();
        }

        private void UpdateControls()
        {
            if (Device.Device.isDoingWork)
            {
                btnStart.Content = "Working...";
                btnStart.IsEnabled = false;
                btnEnable.IsEnabled = false;
            }
            else if (Device.Device.IsInitialized)
            {
                btnStart.Content = "Stop";
                btnStart.IsEnabled = true;
                btnEnable.IsEnabled = true;
            }
            else
            {
                btnStart.Content = "Start";
                btnStart.IsEnabled = true;
                btnEnable.IsEnabled = true;
            }

            deviceName.Text = Device.Device.DeviceName;
            deviceDetails.Text = Device.Device.DeviceDetails;
            devicePerformance.Text = Device.Device.DeviceUpdatePerformance;

            if (Device is Devices.ScriptedDevice.ScriptedDevice)
                btnEnable.IsEnabled = false;
            else
            {
                if (!Global.Configuration.EnabledDevices.Contains(Device.Device.GetType()))
                {
                    btnEnable.Content = "Enable";
                    btnStart.IsEnabled = false;
                }
                else if (!Device.Device.isDoingWork)
                {
                    btnEnable.Content = "Disable";
                    btnStart.IsEnabled = true;
                }
            }

            if (!Device.Device.RegisteredVariables.GetRegisteredVariableKeys().Any())
                btnOptions.IsEnabled = false;
        }

        private void btnViewOptions_Click(object sender, RoutedEventArgs e)
        {
            Window_VariableRegistryEditor optionsWindow = new Window_VariableRegistryEditor();
            optionsWindow.Title = $"{Device.Device.DeviceName} - Options";
            optionsWindow.SizeToContent = SizeToContent.WidthAndHeight;
            optionsWindow.VarRegistryEditor.RegisteredVariables = Device.Device.RegisteredVariables;
            optionsWindow.Closing += (_, _) =>
            {
                ConfigManager.Save(Global.Configuration);
            };

            optionsWindow.ShowDialog();
        }
    }
}
