﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DS4Windows;
using DS4Windows.InputDevices;
using DS4WinWPF.DS4Control.HID;
using DS4WinWPF.DS4Library.InputDevices;
using MethodTimer;
using Microsoft.Extensions.Logging;

namespace DS4WinWPF.DS4Control.IoC.Services
{
    internal interface IControllersEnumeratorService
    {
        ReadOnlyObservableCollection<HidDevice> SupportedDevices { get; }

        event Action DeviceListReady;

        event Action<HidDevice> ControllerReady;

        event Action<HidDevice> ControllerRemoved;

        void EnumerateDevices();
    }

    internal class ControllersEnumeratorService : IControllersEnumeratorService
    {
        private const int SonyVid = 0x054C;
        private const int RazerVid = 0x1532;
        private const int NaconVid = 0x146B;
        private const int HoriVid = 0x0F0D;
        private const int NintendoVendorId = 0x57e;
        private const int SwitchProProductId = 0x2009;
        private const int JoyconLProductId = 0x2006;
        private const int JoyconRProductId = 0x2007;

        private const int HidUsageJoystick = 0x04;
        private const int HidUsageGamepad = 0x05;

        private static readonly IEnumerable<VidPidInfo> KnownDevices = new List<VidPidInfo>
        {
            new(SonyVid, 0xBA0, "Sony WA",
                InputDeviceType.DualShock4,
                VidPidFeatureSet.MonitorAudio
            ),
            new(SonyVid, 0x5C4, "DS4 v.1"),
            new(SonyVid, 0x09CC, "DS4 v.2",
                InputDeviceType.DualShock4,
                VidPidFeatureSet.MonitorAudio
            ),
            new(SonyVid, 0x0CE6, "DualSense",
                InputDeviceType.DualSense,
                VidPidFeatureSet.DefaultDS4,
                DualSenseDevice.DetermineConnectionType
            ),
            new(RazerVid, 0x1000, "Razer Raiju PS4"),
            new(NaconVid, 0x0D01, "Nacon Revol Pro v.1",
                InputDeviceType.DualShock4,
                VidPidFeatureSet.NoGyroCalib
            ), // Nacon Revolution Pro v1 and v2 doesn't support DS4 gyro calibration routines
            new(NaconVid, 0x0D02, "Nacon Revol Pro v.2",
                InputDeviceType.DualShock4,
                VidPidFeatureSet.NoGyroCalib | VidPidFeatureSet.MonitorAudio
            ),
            new(HoriVid, 0x00EE, "Hori PS4 Mini",
                InputDeviceType.DualShock4,
                VidPidFeatureSet.NoOutputData | VidPidFeatureSet.NoBatteryReading | VidPidFeatureSet.NoGyroCalib
            ), // Hori PS4 Mini Wired Gamepad
            new(0x7545, 0x0104, "Armor 3 LU Cobra"), // Armor 3 Level Up Cobra
            new(0x2E95, 0x7725, "Scuf Vantage"), // Scuf Vantage gamepad
            new(0x11C0, 0x4001, "PS4 Fun"), // PS4 Fun Controller
            new(0x0C12, 0x0E20, "Brook Mars Controller"), // Brook Mars controller (wired) with DS4 mode
            new(RazerVid, 0x1007, "Razer Raiju TE"), // Razer Raiju Tournament Edition (wired)
            new(RazerVid, 0x100A, "Razer Raiju TE BT",
                InputDeviceType.DualShock4,
                VidPidFeatureSet.OnlyInputData0x01 | VidPidFeatureSet.OnlyOutputData0x05 |
                VidPidFeatureSet.NoBatteryReading |
                VidPidFeatureSet.NoGyroCalib
            ), // Razer Raiju Tournament Edition (BT). Incoming report data is in "ds4 USB format" (32 bytes) in BT. Also, WriteOutput uses "usb" data packet type in BT.
            new(RazerVid, 0x1004, "Razer Raiju UE USB"), // Razer Raiju Ultimate Edition (wired)
            new(RazerVid, 0x1009, "Razer Raiju UE BT", InputDeviceType.DualShock4,
                VidPidFeatureSet.OnlyInputData0x01 | VidPidFeatureSet.OnlyOutputData0x05 |
                VidPidFeatureSet.NoBatteryReading | VidPidFeatureSet.NoGyroCalib), // Razer Raiju Ultimate Edition (BT)
            new(SonyVid, 0x05C5, "CronusMax (PS4 Mode)"), // CronusMax (PS4 Output Mode)
            new(0x0C12, 0x57AB, "Warrior Joypad JS083", InputDeviceType.DualShock4,
                VidPidFeatureSet
                    .NoGyroCalib), // Warrior Joypad JS083 (wired). Custom lightbar color doesn't work, but everything else works OK (except touchpad and gyro because the gamepad doesnt have those).
            new(0x0C12, 0x0E16, "Steel Play MetalTech"), // Steel Play Metaltech P4 (wired)
            new(NaconVid, 0x0D08, "Nacon Revol U Pro"), // Nacon Revolution Unlimited Pro
            new(NaconVid, 0x0D10,
                "Nacon Revol Infinite"), // Nacon Revolution Infinite (sometimes known as Revol Unlimited Pro v2?). Touchpad, gyro, rumble, "led indicator" lightbar.
            new(HoriVid, 0x0084,
                "Hori Fighting Cmd"), // Hori Fighting Commander (special kind of gamepad without touchpad or sticks. There is a hardware switch to alter d-pad type between dpad and LS/RS)
            new(NaconVid, 0x0D13, "Nacon Revol Pro v.3"),
            new(HoriVid, 0x0066, "Horipad FPS Plus", InputDeviceType.DualShock4,
                VidPidFeatureSet
                    .NoGyroCalib), // Horipad FPS Plus (wired only. No light bar, rumble and Gyro/Accel sensor. Cannot Hide "HID-compliant vendor-defined device" in USB Composite Device. Other feature works fine.)
            new(0x9886, 0x0025, "Astro C40", InputDeviceType.DualShock4,
                VidPidFeatureSet
                    .NoGyroCalib), // Astro C40 (wired and BT. Works if Astro specific xinput drivers haven't been installed. Uninstall those to use the pad as dinput device)
            new(0x0E8F, 0x1114, "Gamo2 Divaller", InputDeviceType.DualShock4,
                VidPidFeatureSet
                    .NoGyroCalib), // Gamo2 Divaller (wired only. Light bar not controllable. No touchpad, gyro or rumble)
            new(HoriVid, 0x0101, "Hori Mini Hatsune Miku FT", InputDeviceType.DualShock4,
                VidPidFeatureSet.NoGyroCalib), // Hori Mini Hatsune Miku FT (wired only. No light bar, gyro or rumble)
            new(HoriVid, 0x00C9, "Hori Taiko Controller", InputDeviceType.DualShock4,
                VidPidFeatureSet
                    .NoGyroCalib), // Hori Taiko Controller (wired only. No light bar, touchpad, gyro, rumble, sticks or triggers)
            new(0x0C12, 0x1E1C, "SnakeByte Game:Pad 4S", InputDeviceType.DualShock4,
                VidPidFeatureSet.NoGyroCalib |
                VidPidFeatureSet
                    .NoBatteryReading), // SnakeByte Gamepad for PS4 (wired only. No gyro. No light bar). If it doesn't work then try the latest gamepad firmware from https://mysnakebyte.com/
            new(NintendoVendorId, SwitchProProductId, "Switch Pro", InputDeviceType.SwitchPro,
                VidPidFeatureSet.DefaultDS4, SwitchProDevice.DetermineConnectionType),
            new(NintendoVendorId, JoyconLProductId, "JoyCon (L)", InputDeviceType.JoyConL,
                VidPidFeatureSet.DefaultDS4, JoyConDevice.DetermineConnectionType),
            new(NintendoVendorId, JoyconRProductId, "JoyCon (R)", InputDeviceType.JoyConR,
                VidPidFeatureSet.DefaultDS4, JoyConDevice.DetermineConnectionType),
            new(0x7545, 0x1122, "Gioteck VX4"), // Gioteck VX4 (no real lightbar, only some RGB leds)
            new(0x7331, 0x0001, "DualShock 3 (DS4 Emulation)", InputDeviceType.DualShock4,
                VidPidFeatureSet.NoGyroCalib |
                VidPidFeatureSet
                    .VendorDefinedDevice) // Sony DualShock 3 using DsHidMini driver. DsHidMini uses vendor-defined HID device type when it's emulating DS3 using DS4 button layout
        };

        private readonly IHidDeviceEnumeratorService enumeratorService;
        private readonly ILogger<ControllersEnumeratorService> logger;

        private readonly ObservableCollection<HidDevice> supportedDevices;

        public ControllersEnumeratorService(ILogger<ControllersEnumeratorService> logger,
            IHidDeviceEnumeratorService enumeratorService)
        {
            this.logger = logger;
            this.enumeratorService = enumeratorService;

            enumeratorService.DeviceArrived += EnumeratorServiceOnDeviceArrived;
            enumeratorService.DeviceRemoved += EnumeratorServiceOnDeviceRemoved;

            supportedDevices = new ObservableCollection<HidDevice>();

            SupportedDevices = new ReadOnlyObservableCollection<HidDevice>(supportedDevices);
        }

        public ReadOnlyObservableCollection<HidDevice> SupportedDevices { get; }

        public event Action DeviceListReady;

        public event Action<HidDevice> ControllerReady;

        public event Action<HidDevice> ControllerRemoved;

        [Time]
        public void EnumerateDevices()
        {
            enumeratorService.EnumerateDevices();

            var hidDevices = enumeratorService.ConnectedDevices;

            //
            // Filter for supported devices
            // 
            var filtered = from hidDevice in hidDevices
                let known =
                    KnownDevices.FirstOrDefault(d =>
                        d.Vid == hidDevice.Attributes.VendorId && d.Pid == hidDevice.Attributes.ProductId)
                where known is not null
                where (hidDevice.Capabilities.Usage is HidUsageGamepad or HidUsageJoystick ||
                       known.FeatureSet.HasFlag(VidPidFeatureSet.VendorDefinedDevice)) &&
                      !hidDevice.IsVirtual
                select hidDevice;

            supportedDevices.Clear();

            foreach (var device in filtered)
            {
                logger.LogInformation("Adding supported input device {Device} to cache",
                    device);

                supportedDevices.Add(device);

                //
                // Notify compatible device found and ready
                // 
                ControllerReady?.Invoke(device);
            }

            //
            // Notify list is built
            // 
            DeviceListReady?.Invoke();
        }

        private void EnumeratorServiceOnDeviceArrived(HidDevice hidDevice)
        {
            var known = KnownDevices.FirstOrDefault(d =>
                d.Vid == hidDevice.Attributes.VendorId && d.Pid == hidDevice.Attributes.ProductId);

            if (known is null) return;

            if (hidDevice.Capabilities.Usage is not (HidUsageGamepad or HidUsageJoystick) &&
                !known.FeatureSet.HasFlag(VidPidFeatureSet.VendorDefinedDevice)
                || hidDevice.IsVirtual) return;

            if (!supportedDevices.Contains(hidDevice))
                supportedDevices.Add(hidDevice);
        }

        private void EnumeratorServiceOnDeviceRemoved(HidDevice hidDevice)
        {
            if (supportedDevices.Contains(hidDevice))
                supportedDevices.Remove(hidDevice);

            ControllerRemoved?.Invoke(hidDevice);
        }
    }
}