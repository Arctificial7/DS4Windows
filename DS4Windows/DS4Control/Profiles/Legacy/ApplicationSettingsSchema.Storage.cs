﻿using System;
using DS4Windows;
using DS4WinWPF.DS4Control.Profiles.Legacy.Converters;
using ExtendedXmlSerializer;
using ExtendedXmlSerializer.Configuration;

namespace DS4WinWPF.DS4Control.Profiles.Legacy
{
    /// <summary>
    ///     Represents application-wide settings.
    /// </summary>
    public partial class DS4WindowsAppSettings : XmlSerializable<DS4WindowsAppSettings>
    {
        public new static IExtendedXmlSerializer GetSerializer()
        {
            return new ConfigurationContainer()
                .EnableReferences()
                .WithUnknownContent().Continue()
                .EnableImplicitTyping(typeof(DS4WindowsAppSettings))
                .Type<DS4Color>().Register().Converter().Using(DS4ColorConverter.Default)
                .Type<bool>().Register().Converter().Using(BooleanConverter.Default)
                .Type<CustomLedProxyType>().Register().Converter().Using(CustomLedConverter.Default)
                .Type<DateTime>().Register().Converter().Using(DateTimeConverter.Default)
                .Create();
        }

        /// <summary>
        ///     Converts properties from <see cref="IBackingStore" /> to this <see cref="DS4WindowsAppSettings" /> instance.
        /// </summary>
        /// <param name="store">The <see cref="IBackingStore" />.</param>
        public void CopyFrom(IBackingStore store)
        {
            UseExclusiveMode = store.UseExclusiveMode;
            StartMinimized = store.StartMinimized;
            MinimizeToTaskbar = store.MinToTaskBar;

            FormWidth = store.FormWidth;
            FormHeight = store.FormHeight;
            FormLocationX = store.FormLocationX;
            FormLocationY = store.FormLocationY;

            for (var i = 0; i < Global.MAX_DS4_CONTROLLER_COUNT; i++)
            {
                var value = !Global.LinkedProfileCheck[i] ? store.ProfilePath[i] : store.OlderProfilePath[i];

                GetType().GetProperty($"Controller{i + 1}")?.SetValue(this, value);
            }

            LastChecked = store.LastChecked;
            CheckWhen = store.CheckWhen;
            // TODO: improve this conversion mess
            LastVersionChecked = store.LastVersionCheckedNumber.ToString();
            Notifications = store.Notifications;
            DisconnectBTAtStop = store.DisconnectBluetoothAtStop;
            SwipeProfiles = store.SwipeProfiles;
            QuickCharge = store.SwipeProfiles;
            CloseMinimizes = store.CloseMini;
            UseLang = store.UseLang;
            DownloadLang = store.DownloadLang;
            FlashWhenLate = store.FlashWhenLate;
            FlashWhenLateAt = store.FlashWhenLateAt;
            AppIcon = store.UseIconChoice;
            AppTheme = store.ThemeChoice;

            UseUDPServer = store.IsUdpServerEnabled;
            UDPServerPort = store.UdpServerPort;
            UDPServerListenAddress = store.UdpServerListenAddress;
            UDPServerSmoothingOptions.UseSmoothing = store.UseUdpSmoothing;
            UDPServerSmoothingOptions.UdpSmoothMinCutoff = store.UdpSmoothingMincutoff;
            UDPServerSmoothingOptions.UdpSmoothBeta = store.UdpSmoothingBeta;

            UseCustomSteamFolder = store.UseCustomSteamFolder;
            CustomSteamFolder = store.CustomSteamFolder;

            AutoProfileRevertDefaultProfile = store.AutoProfileRevertDefaultProfile;

            DeviceOptions.DS4SupportSettings.Enabled = store.DeviceOptions.Ds4DeviceOpts.Enabled;
            DeviceOptions.DualSenseSupportSettings.Enabled = store.DeviceOptions.DualSenseOpts.Enabled;
            DeviceOptions.SwitchProSupportSettings.Enabled = store.DeviceOptions.SwitchProDeviceOpts.Enabled;
            DeviceOptions.JoyConSupportSettings.Enabled = store.DeviceOptions.JoyConDeviceOpts.Enabled;
            DeviceOptions.JoyConSupportSettings.LinkMode = store.DeviceOptions.JoyConDeviceOpts.LinkedMode;
            DeviceOptions.JoyConSupportSettings.JoinedGyroProvider = store.DeviceOptions.JoyConDeviceOpts.JoinGyroProv;

            CustomLed1 = new CustomLedProxyType()
            {
                IsEnabled = store.LightbarSettingInfo[0].Ds4WinSettings.UseCustomLed,
                CustomColor = store.LightbarSettingInfo[0].Ds4WinSettings.Led
            };
            CustomLed2 = new CustomLedProxyType()
            {
                IsEnabled = store.LightbarSettingInfo[1].Ds4WinSettings.UseCustomLed,
                CustomColor = store.LightbarSettingInfo[1].Ds4WinSettings.Led
            };
            CustomLed3 = new CustomLedProxyType()
            {
                IsEnabled = store.LightbarSettingInfo[2].Ds4WinSettings.UseCustomLed,
                CustomColor = store.LightbarSettingInfo[2].Ds4WinSettings.Led
            };
            CustomLed4 = new CustomLedProxyType()
            {
                IsEnabled = store.LightbarSettingInfo[3].Ds4WinSettings.UseCustomLed,
                CustomColor = store.LightbarSettingInfo[3].Ds4WinSettings.Led
            };
            CustomLed5 = new CustomLedProxyType()
            {
                IsEnabled = store.LightbarSettingInfo[4].Ds4WinSettings.UseCustomLed,
                CustomColor = store.LightbarSettingInfo[4].Ds4WinSettings.Led
            };
            CustomLed6 = new CustomLedProxyType()
            {
                IsEnabled = store.LightbarSettingInfo[5].Ds4WinSettings.UseCustomLed,
                CustomColor = store.LightbarSettingInfo[5].Ds4WinSettings.Led
            };
            CustomLed7 = new CustomLedProxyType()
            {
                IsEnabled = store.LightbarSettingInfo[6].Ds4WinSettings.UseCustomLed,
                CustomColor = store.LightbarSettingInfo[6].Ds4WinSettings.Led
            };
            CustomLed8 = new CustomLedProxyType()
            {
                IsEnabled = store.LightbarSettingInfo[7].Ds4WinSettings.UseCustomLed,
                CustomColor = store.LightbarSettingInfo[7].Ds4WinSettings.Led
            };
        }

        /// <summary>
        ///     Injects properties from <see cref="DS4WindowsAppSettings" /> into <see cref="IBackingStore" /> instance.
        /// </summary>
        /// <param name="store">The <see cref="IBackingStore" />.</param>
        public void CopyTo(IBackingStore store)
        {
            store.UseExclusiveMode = UseExclusiveMode;
            store.StartMinimized = StartMinimized;
            store.MinToTaskBar = MinimizeToTaskbar;

            store.FormWidth = FormWidth;
            store.FormHeight = FormHeight;
            store.FormLocationX = Math.Max(FormLocationX, 0);
            store.FormLocationY = Math.Max(FormLocationY, 0);

            for (var i = 0; i < Global.MAX_DS4_CONTROLLER_COUNT; i++)
            {
                var value = (string)GetType().GetProperty($"Controller{i + 1}")?.GetValue(this, null);

                if (string.IsNullOrEmpty(value))
                {
                    store.ProfilePath[i] = store.OlderProfilePath[i] = string.Empty;
                    store.DistanceProfiles[i] = false;
                }
                else
                {
                    store.ProfilePath[i] = value;
                    if (store.ProfilePath[i].ToLower().Contains("distance"))
                        store.DistanceProfiles[i] = true;

                    store.OlderProfilePath[i] = store.ProfilePath[i];
                }
            }

            store.LastChecked = LastChecked;
            store.CheckWhen = CheckWhen;
            store.LastVersionCheckedNumber = string.IsNullOrEmpty(LastVersionChecked)
                ? 0
                : Global.CompileVersionNumberFromString(LastVersionChecked);
            // TODO: replace with Enum
            store.Notifications = Notifications;
            store.DisconnectBluetoothAtStop = DisconnectBTAtStop;
            store.SwipeProfiles = SwipeProfiles;
            store.QuickCharge = QuickCharge;
            store.CloseMini = CloseMinimizes;
            store.UseLang = UseLang;
            store.DownloadLang = DownloadLang;
            store.FlashWhenLate = FlashWhenLate;
            store.FlashWhenLateAt = FlashWhenLateAt;
            store.UseIconChoice = AppIcon;
            store.ThemeChoice = AppTheme;

            store.IsUdpServerEnabled = UseUDPServer;
            store.UdpServerPort = Math.Min(Math.Max(UDPServerPort, 1024), 65535);
            store.UdpServerListenAddress = UDPServerListenAddress;
            store.UseUdpSmoothing = UDPServerSmoothingOptions.UseSmoothing;
            store.UdpSmoothingMincutoff = UDPServerSmoothingOptions.UdpSmoothMinCutoff;
            store.UdpSmoothingBeta = UDPServerSmoothingOptions.UdpSmoothBeta;

            store.UseCustomSteamFolder = UseCustomSteamFolder;
            store.CustomSteamFolder = CustomSteamFolder;

            store.AutoProfileRevertDefaultProfile = AutoProfileRevertDefaultProfile;

            store.DeviceOptions.Ds4DeviceOpts.Enabled = DeviceOptions.DS4SupportSettings.Enabled;
            store.DeviceOptions.DualSenseOpts.Enabled = DeviceOptions.DualSenseSupportSettings.Enabled;
            store.DeviceOptions.SwitchProDeviceOpts.Enabled = DeviceOptions.SwitchProSupportSettings.Enabled;
            store.DeviceOptions.JoyConDeviceOpts.Enabled = DeviceOptions.JoyConSupportSettings.Enabled;
            store.DeviceOptions.JoyConDeviceOpts.LinkedMode = DeviceOptions.JoyConSupportSettings.LinkMode;
            store.DeviceOptions.JoyConDeviceOpts.JoinGyroProv = DeviceOptions.JoyConSupportSettings.JoinedGyroProvider;

            store.LightbarSettingInfo[0].Ds4WinSettings.UseCustomLed = CustomLed1.IsEnabled;
            store.LightbarSettingInfo[0].Ds4WinSettings.Led = CustomLed1.CustomColor;
            store.LightbarSettingInfo[1].Ds4WinSettings.UseCustomLed = CustomLed2.IsEnabled;
            store.LightbarSettingInfo[1].Ds4WinSettings.Led = CustomLed2.CustomColor;
            store.LightbarSettingInfo[2].Ds4WinSettings.UseCustomLed = CustomLed3.IsEnabled;
            store.LightbarSettingInfo[2].Ds4WinSettings.Led = CustomLed3.CustomColor;
            store.LightbarSettingInfo[3].Ds4WinSettings.UseCustomLed = CustomLed4.IsEnabled;
            store.LightbarSettingInfo[3].Ds4WinSettings.Led = CustomLed4.CustomColor;
            store.LightbarSettingInfo[4].Ds4WinSettings.UseCustomLed = CustomLed5.IsEnabled;
            store.LightbarSettingInfo[4].Ds4WinSettings.Led = CustomLed5.CustomColor;
            store.LightbarSettingInfo[5].Ds4WinSettings.UseCustomLed = CustomLed6.IsEnabled;
            store.LightbarSettingInfo[5].Ds4WinSettings.Led = CustomLed6.CustomColor;
            store.LightbarSettingInfo[6].Ds4WinSettings.UseCustomLed = CustomLed7.IsEnabled;
            store.LightbarSettingInfo[6].Ds4WinSettings.Led = CustomLed7.CustomColor;
            store.LightbarSettingInfo[7].Ds4WinSettings.UseCustomLed = CustomLed8.IsEnabled;
            store.LightbarSettingInfo[7].Ds4WinSettings.Led = CustomLed8.CustomColor;
        }
    }
}