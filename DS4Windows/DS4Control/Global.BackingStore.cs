﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using DS4Windows.InputDevices;
using DS4WinWPF.Properties;

namespace DS4Windows
{
    public partial class Global
    {
        private class BackingStore : IBackingStore
        {
            public const double DEFAULT_UDP_SMOOTH_MINCUTOFF = 0.4;
            public const double DEFAULT_UDP_SMOOTH_BETA = 0.2;

            private readonly int[] _l2OutCurveMode = new int[TEST_PROFILE_ITEM_COUNT] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            private readonly int[] _lsOutCurveMode = new int[TEST_PROFILE_ITEM_COUNT] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            private readonly int[] _r2OutCurveMode = new int[TEST_PROFILE_ITEM_COUNT] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            private readonly int[] _rsOutCurveMode = new int[TEST_PROFILE_ITEM_COUNT] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            private readonly int[] _sxOutCurveMode = new int[TEST_PROFILE_ITEM_COUNT] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            private readonly int[] _szOutCurveMode = new int[TEST_PROFILE_ITEM_COUNT] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            public readonly ControlSettingsGroup[] ds4controlSettings;

            private readonly List<DS4ControlSettings>[] Ds4Settings =
                new List<DS4ControlSettings>[TEST_PROFILE_ITEM_COUNT]
                {
                    new(), new(), new(),
                    new(), new(), new(), new(), new(), new()
                };

            public IList<GyroControlsInfo> GyroControlsInfo { get; set; } = new List<GyroControlsInfo>()
            {
                new(), new(), new(),
                new(), new(), new(),
                new(), new(), new()
            };

            public IList< bool> GyroMouseStickToggle { get; set; } = new List<bool>()
            {
                false, false, false,
                false, false, false, false, false, false
            };

            public IList<bool> GyroMouseStickTriggerTurns { get; set; }= new List<bool>()
                { true, true, true, true, true, true, true, true, true };

            public IList<GyroMouseStickInfo> GyroMouseStickInfo { get; set; }= new List<GyroMouseStickInfo>()
            {
                new(),
                new(),
                new(), new(),
                new(), new(),
                new(), new(),
                new()
            };

            public IList<GyroOutMode> GyroOutputMode { get; set; }= new List<GyroOutMode>()
            {
                DS4Windows.GyroOutMode.Controls, DS4Windows.GyroOutMode.Controls,
                DS4Windows.GyroOutMode.Controls, DS4Windows.GyroOutMode.Controls, DS4Windows.GyroOutMode.Controls,
                DS4Windows.GyroOutMode.Controls,
                DS4Windows.GyroOutMode.Controls, DS4Windows.GyroOutMode.Controls, DS4Windows.GyroOutMode.Controls
            };

            public IList<GyroDirectionalSwipeInfo> GyroSwipeInfo { get; set; } =
                new List<GyroDirectionalSwipeInfo>()
                {
                    new(), new(),
                    new(), new(),
                    new(), new(),
                    new(), new(),
                    new()
                };

            public readonly Dictionary<string, string> linkedProfiles = new();

            protected readonly XmlDocument m_Xdoc = new();

            public readonly int[] profileActionCount = new int[TEST_PROFILE_ITEM_COUNT] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            public readonly Dictionary<string, SpecialAction>[] profileActionDict =
                new Dictionary<string, SpecialAction>[TEST_PROFILE_ITEM_COUNT]
                {
                    new(), new(), new(),
                    new(), new(), new(), new(), new(), new()
                };

            public readonly Dictionary<string, int>[] profileActionIndexDict =
                new Dictionary<string, int>[TEST_PROFILE_ITEM_COUNT]
                {
                    new(), new(), new(),
                    new(), new(), new(), new(), new(), new()
                };

            public IList<int> SAWheelFuzzValues { get; set; } = new List<int>(){
                new(), new(), new(),
                new(), new(), new(), new(), new(), new()
            };

            /// <summary>
            ///     TRUE=AutoProfile reverts to default profile if current foreground process is unknown, FALSE=Leave existing profile
            ///     active when a foreground process is unknown (ie. no matching auto-profile rule)
            /// </summary>
            public bool AutoProfileRevertDefaultProfile { get; set; } = true;

            public int FlashWhenLateAt { get; set; }= 50;

            public string lastVersionChecked = string.Empty;

            public ulong LastVersionCheckedNumber;

            public BackingStore()
            {
                ds4controlSettings = new ControlSettingsGroup[TEST_PROFILE_ITEM_COUNT];

                for (var i = 0; i < TEST_PROFILE_ITEM_COUNT; i++)
                {
                    foreach (DS4Controls dc in Enum.GetValues(typeof(DS4Controls)))
                        if (dc != DS4Controls.None)
                            Ds4Settings[i].Add(new DS4ControlSettings(dc));

                    ds4controlSettings[i] = new ControlSettingsGroup(Ds4Settings[i]);

                    EstablishDefaultSpecialActions(i);
                    CacheExtraProfileInfo(i);
                }

                SetupDefaultColors();
            }

            public bool SwipeProfiles { get; set; } = true;

            public bool UseExclusiveMode { get; set; } // Re-enable Ex Mode

            public bool UseUdpServer { get; set; }

            public IList<int> RumbleAutostopTime { get; } = new List<int>
                { 0, 0, 0, 0, 0, 0, 0, 0, 0 }; // Value in milliseconds (0=autustop timer disabled)

            public IList<LightbarSettingInfo> LightbarSettingInfo { get; set; } = new List<LightbarSettingInfo>
            {
                new(), new(),
                new(), new(),
                new(), new(),
                new(), new(),
                new()
            };

            public IList<bool> SAMouseStickTriggerCond { get; set; } = new List<bool>
                { true, true, true, true, true, true, true, true, true };

            public IList<string> SAMouseStickTriggers { get; set; } = new List<string>
                { "-1", "-1", "-1", "-1", "-1", "-1", "-1", "-1", "-1" };

            public IList<SASteeringWheelEmulationAxisType> SASteeringWheelEmulationAxis { get; set; } =
                new List<SASteeringWheelEmulationAxisType>
                {
                    SASteeringWheelEmulationAxisType.None, SASteeringWheelEmulationAxisType.None,
                    SASteeringWheelEmulationAxisType.None, SASteeringWheelEmulationAxisType.None,
                    SASteeringWheelEmulationAxisType.None, SASteeringWheelEmulationAxisType.None,
                    SASteeringWheelEmulationAxisType.None, SASteeringWheelEmulationAxisType.None,
                    SASteeringWheelEmulationAxisType.None
                };

            public IList<int> SASteeringWheelEmulationRange { get; set; } =
                new List<int> { 360, 360, 360, 360, 360, 360, 360, 360, 360 };

            public IList<bool> SATriggerCond { get; set; } = new List<bool>
                { true, true, true, true, true, true, true, true, true };

            public IList<string> SATriggers { get; set; } = new List<string>
                { "-1", "-1", "-1", "-1", "-1", "-1", "-1", "-1", "-1" };

            public IList<bool> StartTouchpadOff { get; set; } = new List<bool>
                { false, false, false, false, false, false, false, false, false };

            public IList<IList<int>> TouchDisInvertTriggers { get; set; } = new List<IList<int>>
            {
                new int[1] { -1 }, new int[1] { -1 }, new int[1] { -1 },
                new int[1] { -1 }, new int[1] { -1 }, new int[1] { -1 }, new int[1] { -1 }, new int[1] { -1 },
                new int[1] { -1 }
            };

            public IList<TouchpadOutMode> TouchOutMode { get; set; } = new List<TouchpadOutMode>
            {
                TouchpadOutMode.Mouse, TouchpadOutMode.Mouse, TouchpadOutMode.Mouse, TouchpadOutMode.Mouse,
                TouchpadOutMode.Mouse, TouchpadOutMode.Mouse, TouchpadOutMode.Mouse, TouchpadOutMode.Mouse,
                TouchpadOutMode.Mouse
            };

            public string ProfilesPath { get; set; } =
                Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName,
                    Constants.ProfilesFileName);

            public string ActionsPath { get; set; } = Path.Combine(RuntimeAppDataPath, Constants.ActionsFileName);

            public string LinkedProfilesPath { get; set; } =
                Path.Combine(RuntimeAppDataPath, Constants.LinkedProfilesFileName);

            public string ControllerConfigsPath { get; set; } =
                Path.Combine(RuntimeAppDataPath, Constants.ControllerConfigsFileName);

            // ninth (fifth in old builds) value used for options, not last controller
            public IList<ButtonMouseInfo> ButtonMouseInfos { get; set; } = new List<ButtonMouseInfo>
            {
                new(), new(), new(),
                new(), new(), new(),
                new(), new(), new()
            };

            public IList<bool> EnableTouchToggle { get; set; } = new List<bool>
                { true, true, true, true, true, true, true, true, true };

            public IList<int> IdleDisconnectTimeout { get; set; } = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            public IList<bool> EnableOutputDataToDS4 { get; set; } = new List<bool>
                { true, true, true, true, true, true, true, true, true };

            public IList<bool> TouchpadJitterCompensation { get; set; } = new List<bool>
                { true, true, true, true, true, true, true, true, true };

            public IList<bool> LowerRCOn { get; set; } = new List<bool>
                { false, false, false, false, false, false, false, false, false };

            public IList<bool> TouchClickPassthru { get; set; } = new List<bool>
                { false, false, false, false, false, false, false, false, false };

            public IList<string> ProfilePath { get; set; } = new List<string>
            {
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty
            };

            public IList<string> OlderProfilePath { get; set; } = new List<string>
            {
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty
            };

            // Cache properties instead of performing a string comparison every frame
            public IList<bool> DistanceProfiles { get; set; } = new List<bool>
                { false, false, false, false, false, false, false, false, false };

            public IList<byte> RumbleBoost { get; set; } =
                new List<byte> { 100, 100, 100, 100, 100, 100, 100, 100, 100 };

            public IList<byte> TouchSensitivity { get; set; } =
                new List<byte> { 100, 100, 100, 100, 100, 100, 100, 100, 100 };

            public IList<StickDeadZoneInfo> LSModInfo { get; set; } = new List<StickDeadZoneInfo>
            {
                new(), new(),
                new(), new(),
                new(), new(),
                new(), new(),
                new()
            };

            public IList<StickDeadZoneInfo> RSModInfo { get; set; } = new List<StickDeadZoneInfo>
            {
                new(), new(),
                new(), new(),
                new(), new(),
                new(), new(),
                new()
            };

            public IList<TriggerDeadZoneZInfo> L2ModInfo { get; set; } = new List<TriggerDeadZoneZInfo>
            {
                new(), new(),
                new(), new(),
                new(), new(),
                new(), new(),
                new()
            };

            public IList<TriggerDeadZoneZInfo> R2ModInfo { get; set; } = new List<TriggerDeadZoneZInfo>
            {
                new(), new(),
                new(), new(),
                new(), new(),
                new(), new(),
                new()
            };

            public IList<double> LSRotation { get; set; } =
                new List<double> { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };

            public IList<double> RSRotation { get; set; } =
                new List<double> { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };

            public IList<double> SXDeadzone { get; set; } = new List<double>
                { 0.25, 0.25, 0.25, 0.25, 0.25, 0.25, 0.25, 0.25, 0.25 };

            public IList<double> SZDeadzone { get; set; } = new List<double>
                { 0.25, 0.25, 0.25, 0.25, 0.25, 0.25, 0.25, 0.25, 0.25 };

            public IList<double> SXMaxzone { get; set; } =
                new List<double> { 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0 };

            public IList<double> SZMaxzone { get; set; } =
                new List<double> { 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0 };

            public IList<double> SXAntiDeadzone { get; set; } =
                new List<double> { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };

            public IList<double> SZAntiDeadzone { get; set; } =
                new List<double> { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };

            public IList<double> L2Sens { get; set; } =
                new List<double> { 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0 };

            public IList<double> R2Sens { get; set; } =
                new List<double> { 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0 };

            public IList<double> LSSens { get; set; } =
                new List<double> { 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0 };

            public IList<double> RSSens { get; set; } =
                new List<double> { 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0 };

            public IList<double> SXSens { get; set; } =
                new List<double> { 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0 };

            public IList<double> SZSens { get; set; } =
                new List<double> { 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0 };

            public IList<byte> TapSensitivity { get; set; } = new List<byte> { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            public IList<bool> DoubleTap { get; set; } = new List<bool>
                { false, false, false, false, false, false, false, false, false };

            public IList<int> ScrollSensitivity { get; set; } = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            public IList<int> TouchPadInvert { get; set; } = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            public IList<int> BluetoothPollRate { get; set; } = new List<int> { 4, 4, 4, 4, 4, 4, 4, 4, 4 };

            public IList<int> GyroMouseDeadZone { get; set; } = new List<int>
            {
                MouseCursor.GYRO_MOUSE_DEADZONE, MouseCursor.GYRO_MOUSE_DEADZONE,
                MouseCursor.GYRO_MOUSE_DEADZONE, MouseCursor.GYRO_MOUSE_DEADZONE,
                MouseCursor.GYRO_MOUSE_DEADZONE, MouseCursor.GYRO_MOUSE_DEADZONE,
                MouseCursor.GYRO_MOUSE_DEADZONE, MouseCursor.GYRO_MOUSE_DEADZONE,
                MouseCursor.GYRO_MOUSE_DEADZONE
            };

            public IList<bool> GyroMouseToggle { get; set; } = new List<bool>
            {
                false, false, false,
                false, false, false, false, false, false
            };

            public IList<SquareStickInfo> SquStickInfo { get; set; } = new List<SquareStickInfo>
            {
                new(), new(),
                new(), new(),
                new(), new(),
                new(), new(),
                new()
            };

            public IList<StickAntiSnapbackInfo> LSAntiSnapbackInfo { get; set; } = new List<StickAntiSnapbackInfo>
            {
                new(), new(),
                new(), new(),
                new(), new(),
                new(), new(),
                new()
            };

            public IList<StickAntiSnapbackInfo> RSAntiSnapbackInfo { get; set; } = new List<StickAntiSnapbackInfo>
            {
                new(), new(),
                new(), new(),
                new(), new(),
                new(), new(),
                new()
            };

            public IList<StickOutputSetting> LSOutputSettings { get; set; } = new List<StickOutputSetting>
            {
                new(), new(), new(),
                new(), new(), new(),
                new(), new(), new()
            };

            public IList<StickOutputSetting> RSOutputSettings { get; set; } = new List<StickOutputSetting>
            {
                new(), new(), new(),
                new(), new(), new(),
                new(), new(), new()
            };

            public IList<TriggerOutputSettings> L2OutputSettings { get; set; } = new List<TriggerOutputSettings>
            {
                new(), new(), new(),
                new(), new(), new(),
                new(), new(), new()
            };

            public IList<TriggerOutputSettings> R2OutputSettings { get; set; } = new List<TriggerOutputSettings>
            {
                new(), new(), new(),
                new(), new(), new(),
                new(), new(), new()
            };

            public IList<SteeringWheelSmoothingInfo> WheelSmoothInfo { get; set; } =
                new List<SteeringWheelSmoothingInfo>
                {
                    new(), new(),
                    new(), new(),
                    new(), new(),
                    new(), new(),
                    new()
                };

            public IList<BezierCurve> LSOutBezierCurveObj { get; set; } = new List<BezierCurve>
                { new(), new(), new(), new(), new(), new(), new(), new(), new() };

            public IList<BezierCurve> RSOutBezierCurveObj { get; set; } = new List<BezierCurve>
                { new(), new(), new(), new(), new(), new(), new(), new(), new() };

            public IList<BezierCurve> L2OutBezierCurveObj { get; set; } = new List<BezierCurve>
                { new(), new(), new(), new(), new(), new(), new(), new(), new() };

            public IList<BezierCurve> R2OutBezierCurveObj { get; set; } = new List<BezierCurve>
                { new(), new(), new(), new(), new(), new(), new(), new(), new() };

            public IList<BezierCurve> SXOutBezierCurveObj { get; set; } = new List<BezierCurve>
                { new(), new(), new(), new(), new(), new(), new(), new(), new() };

            public IList<BezierCurve> SZOutBezierCurveObj { get; set; } = new List<BezierCurve>
                { new(), new(), new(), new(), new(), new(), new(), new(), new() };

            public int getLsOutCurveMode(int index)
            {
                return _lsOutCurveMode[index];
            }

            public void setLsOutCurveMode(int index, int value)
            {
                if (value >= 1)
                    SetOutBezierCurveObjArrayItem(LSOutBezierCurveObj, index, value, BezierCurve.AxisType.LSRS);
                _lsOutCurveMode[index] = value;
            }

            public int getRsOutCurveMode(int index)
            {
                return _rsOutCurveMode[index];
            }

            public void setRsOutCurveMode(int index, int value)
            {
                if (value >= 1)
                    SetOutBezierCurveObjArrayItem(RSOutBezierCurveObj, index, value, BezierCurve.AxisType.LSRS);
                _rsOutCurveMode[index] = value;
            }

            public int getL2OutCurveMode(int index)
            {
                return _l2OutCurveMode[index];
            }

            public void setL2OutCurveMode(int index, int value)
            {
                if (value >= 1)
                    SetOutBezierCurveObjArrayItem(L2OutBezierCurveObj, index, value, BezierCurve.AxisType.L2R2);
                _l2OutCurveMode[index] = value;
            }

            public int getR2OutCurveMode(int index)
            {
                return _r2OutCurveMode[index];
            }

            public void setR2OutCurveMode(int index, int value)
            {
                if (value >= 1)
                    SetOutBezierCurveObjArrayItem(R2OutBezierCurveObj, index, value, BezierCurve.AxisType.L2R2);
                _r2OutCurveMode[index] = value;
            }

            public int getSXOutCurveMode(int index)
            {
                return _sxOutCurveMode[index];
            }

            public void setSXOutCurveMode(int index, int value)
            {
                if (value >= 1)
                    SetOutBezierCurveObjArrayItem(SXOutBezierCurveObj, index, value, BezierCurve.AxisType.SA);
                _sxOutCurveMode[index] = value;
            }

            public int getSZOutCurveMode(int index)
            {
                return _szOutCurveMode[index];
            }

            public void setSZOutCurveMode(int index, int value)
            {
                if (value >= 1)
                    SetOutBezierCurveObjArrayItem(SZOutBezierCurveObj, index, value, BezierCurve.AxisType.SA);
                _szOutCurveMode[index] = value;
            }

            public IList<string> LaunchProgram { get; set; } = new List<string>
            {
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty
            };

            public IList<bool> DirectInputOnly { get; set; } = new List<bool>
                { false, false, false, false, false, false, false, false, false };

            public int FormWidth { get; set; } = 782;

            public int FormHeight { get; set; } = 550;

            public int FormLocationX { get; set; }

            public int FormLocationY { get; set; }

            public bool StartMinimized { get; set; }

            public bool MinToTaskBar { get; set; }

            public DateTime LastChecked { get; set; }

            public int CheckWhen { get; set; } = 24;

            public int Notifications { get; set; } = 2;

            public bool DisconnectBluetoothAtStop { get; set; }

            public bool Ds4Mapping { get; set; }

            public bool QuickCharge { get; set; }

            public bool CloseMini { get; set; }

            public IList<SpecialAction> Actions { get; set; } = new List<SpecialAction>();

            public IList<string>[] ProfileActions { get; set; } =
                { null, null, null, null, null, null, null, null, null };

            public string UseLang { get; set; } = string.Empty;

            public bool DownloadLang { get; set; } = true;

            public TrayIconChoice UseIconChoice { get; set; }

            public bool FlashWhenLate { get; set; } = true;

            public int UdpServerPort { get; set; } = 26760;

            /// <summary>
            ///     127.0.0.1=IPAddress.Loopback (default), 0.0.0.0=IPAddress.Any as all interfaces, x.x.x.x = Specific ipv4 interface
            ///     address or hostname
            /// </summary>
            public string UdpServerListenAddress { get; set; } = "127.0.0.1";

            public bool UseUdpSmoothing { get; set; }

            public double UdpSmoothingMincutoff { get; set; } = DEFAULT_UDP_SMOOTH_MINCUTOFF;

            public double UdpSmoothingBeta { get; set; } = DEFAULT_UDP_SMOOTH_BETA;

            public bool UseCustomSteamFolder { get; set; }

            public string CustomSteamFolder { get; set; }

            public AppThemeChoice ThemeChoice { get; set; }

            public string FakeExeFileName { get; set; } = string.Empty;

            public ControlServiceDeviceOptions DeviceOptions => new();

            // Cache whether profile has custom action
            public IList<bool> ContainsCustomAction { get; set; } = new bool[TEST_PROFILE_ITEM_COUNT]
                { false, false, false, false, false, false, false, false, false };

            // Cache whether profile has custom extras
            public IList<bool> ContainsCustomExtras { get; set; } = new bool[TEST_PROFILE_ITEM_COUNT]
                { false, false, false, false, false, false, false, false, false };

            public IList<int> GyroSensitivity { get; set; } = new int[TEST_PROFILE_ITEM_COUNT]
                { 100, 100, 100, 100, 100, 100, 100, 100, 100 };

            public IList<int> GyroSensVerticalScale { get; set; } = new int[TEST_PROFILE_ITEM_COUNT]
                { 100, 100, 100, 100, 100, 100, 100, 100, 100 };

            public IList<int> GyroInvert { get; set; } = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            public IList<bool> GyroTriggerTurns { get; set; } = new List<bool>
                { true, true, true, true, true, true, true, true, true };

            public IList<GyroMouseInfo> GyroMouseInfo { get; set; } = new List<GyroMouseInfo>
            {
                new(), new(),
                new(), new(),
                new(), new(),
                new(), new(),
                new()
            };

            public IList<int> GyroMouseHorizontalAxis { get; set; } = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            public IList<int> GyroMouseStickHorizontalAxis { get; set; } = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            public IList<bool> TrackballMode { get; set; } = new List<bool>
                { false, false, false, false, false, false, false, false, false };

            public IList<double> TrackballFriction { get; set; } = new List<double>
                { 10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0 };

            public IList<TouchpadAbsMouseSettings> TouchPadAbsMouse { get; set; } =
                new List<TouchpadAbsMouseSettings>
                {
                    new(), new(), new(),
                    new(), new(), new(), new(), new(), new()
                };

            public IList<TouchpadRelMouseSettings> TouchPadRelMouse { get; set; } =
                new List<TouchpadRelMouseSettings>
                {
                    new(), new(), new(), new(),
                    new(), new(), new(), new(), new()
                };

            // Used to hold the controller type desired in a profile
            public IList<OutContType> OutputDeviceType { get; set; } = new List<OutContType>
            {
                OutContType.X360,
                OutContType.X360,
                OutContType.X360,
                OutContType.X360,
                OutContType.X360,
                OutContType.X360,
                OutContType.X360,
                OutContType.X360,
                OutContType.X360
            };

            public void EstablishDefaultSpecialActions(int idx)
            {
                ProfileActions[idx] = new List<string> { "Disconnect Controller" };
                profileActionCount[idx] = ProfileActions[idx].Count;
            }

            public void CacheProfileCustomsFlags(int device)
            {
                var customAct = false;
                ContainsCustomAction[device] = customAct = HasCustomActions(device);
                ContainsCustomExtras[device] = HasCustomExtras(device);

                if (!customAct)
                {
                    customAct = GyroOutputMode[device] == DS4Windows.GyroOutMode.MouseJoystick;
                    customAct = customAct ||
                                SASteeringWheelEmulationAxis[device] >= SASteeringWheelEmulationAxisType.VJoy1X;
                    customAct = customAct || LSOutputSettings[device].mode != StickMode.Controls;
                    customAct = customAct || RSOutputSettings[device].mode != StickMode.Controls;
                    ContainsCustomAction[device] = customAct;
                }
            }

            public void CacheExtraProfileInfo(int device)
            {
                CalculateProfileActionCount(device);
                CalculateProfileActionDicts(device);
                CacheProfileCustomsFlags(device);
            }

            public void CalculateProfileActionCount(int index)
            {
                profileActionCount[index] = ProfileActions[index].Count;
            }

            public void CalculateProfileActionDicts(int device)
            {
                profileActionDict[device].Clear();
                profileActionIndexDict[device].Clear();

                foreach (var actionname in ProfileActions[device])
                {
                    profileActionDict[device][actionname] = GetAction(actionname);
                    profileActionIndexDict[device][actionname] = GetActionIndexOf(actionname);
                }
            }

            public SpecialAction GetAction(string name)
            {
                //foreach (SpecialAction sA in actions)
                for (int i = 0, actionCount = Actions.Count; i < actionCount; i++)
                {
                    var sA = Actions[i];
                    if (sA.Name == name)
                        return sA;
                }

                return new SpecialAction("null", "null", "null", "null");
            }

            public int GetActionIndexOf(string name)
            {
                for (int i = 0, actionCount = Actions.Count; i < actionCount; i++)
                    if (Actions[i].Name == name)
                        return i;

                return -1;
            }

            public void SetSaTriggerCond(int index, string text)
            {
                SATriggerCond[index] = SaTriggerCondValue(text);
            }

            public void SetSaMouseStickTriggerCond(int index, string text)
            {
                SAMouseStickTriggerCond[index] = SaTriggerCondValue(text);
            }

            public void SetGyroMouseDZ(int index, int value, ControlService control)
            {
                GyroMouseDeadZone[index] = value;
                if (index < ControlService.CURRENT_DS4_CONTROLLER_LIMIT && control.touchPad[index] != null)
                    control.touchPad[index].CursorGyroDead = value;
            }

            public void SetGyroControlsToggle(int index, bool value, ControlService control)
            {
                GyroControlsInfo[index].triggerToggle = value;
                if (index < ControlService.CURRENT_DS4_CONTROLLER_LIMIT && control.touchPad[index] != null)
                    control.touchPad[index].ToggleGyroControls = value;
            }

            public void SetGyroMouseToggle(int index, bool value, ControlService control)
            {
                GyroMouseToggle[index] = value;
                if (index < ControlService.CURRENT_DS4_CONTROLLER_LIMIT && control.touchPad[index] != null)
                    control.touchPad[index].ToggleGyroMouse = value;
            }

            public void SetGyroMouseStickToggle(int index, bool value, ControlService control)
            {
                GyroMouseStickToggle[index] = value;
                if (index < ControlService.CURRENT_DS4_CONTROLLER_LIMIT && control.touchPad[index] != null)
                    control.touchPad[index].ToggleGyroStick = value;
            }

            public bool SaveAsNewProfile(int device, string proName)
            {
                var Saved = true;
                ResetProfile(device);
                Saved = SaveProfile(device, proName);
                return Saved;
            }

            public bool SaveProfile(int device, string proName)
            {
                var Saved = true;
                //string path = Global.RuntimeAppDataPath + @"\Profiles\" + Path.GetFileNameWithoutExtension(proName) + ".xml";
                if (proName.EndsWith(XML_EXTENSION)) proName = proName.Remove(proName.LastIndexOf(XML_EXTENSION));

                var path = Path.Combine(
                    RuntimeAppDataPath,
                    Constants.ProfilesSubDirectory,
                    $"{proName}{XML_EXTENSION}"
                );

                try
                {
                    XmlNode tmpNode;
                    var xmlControls = m_Xdoc.SelectSingleNode("/DS4Windows/Control");
                    var xmlShiftControls = m_Xdoc.SelectSingleNode("/DS4Windows/ShiftControl");
                    m_Xdoc.RemoveAll();

                    tmpNode = m_Xdoc.CreateXmlDeclaration("1.0", "utf-8", string.Empty);
                    m_Xdoc.AppendChild(tmpNode);

                    tmpNode = m_Xdoc.CreateComment(string.Format(" DS4Windows Configuration Data. {0} ", DateTime.Now));
                    m_Xdoc.AppendChild(tmpNode);

                    tmpNode = m_Xdoc.CreateComment(string.Format(" Made with DS4Windows version {0} ",
                        ExecutableProductVersion));
                    m_Xdoc.AppendChild(tmpNode);

                    tmpNode = m_Xdoc.CreateWhitespace("\r\n");
                    m_Xdoc.AppendChild(tmpNode);

                    var rootElement = m_Xdoc.CreateElement("DS4Windows", null);
                    rootElement.SetAttribute("app_version", ExecutableProductVersion);
                    rootElement.SetAttribute("config_version", CONFIG_VERSION.ToString());

                    var lightbarSettings = LightbarSettingInfo[device];
                    var lightInfo = lightbarSettings.ds4winSettings;

                    var xmlTouchToggle = m_Xdoc.CreateNode(XmlNodeType.Element, "touchToggle", null);
                    xmlTouchToggle.InnerText = EnableTouchToggle[device].ToString();
                    rootElement.AppendChild(xmlTouchToggle);
                    var xmlIdleDisconnectTimeout =
                        m_Xdoc.CreateNode(XmlNodeType.Element, "idleDisconnectTimeout", null);
                    xmlIdleDisconnectTimeout.InnerText = IdleDisconnectTimeout[device].ToString();
                    rootElement.AppendChild(xmlIdleDisconnectTimeout);
                    var xmlOutputDataToDS4 = m_Xdoc.CreateNode(XmlNodeType.Element, "outputDataToDS4", null);
                    xmlOutputDataToDS4.InnerText = EnableOutputDataToDS4[device].ToString();
                    rootElement.AppendChild(xmlOutputDataToDS4);
                    var xmlColor = m_Xdoc.CreateNode(XmlNodeType.Element, "Color", null);
                    xmlColor.InnerText = lightInfo.m_Led.red + "," + lightInfo.m_Led.green + "," + lightInfo.m_Led.blue;
                    rootElement.AppendChild(xmlColor);
                    var xmlRumbleBoost = m_Xdoc.CreateNode(XmlNodeType.Element, "RumbleBoost", null);
                    xmlRumbleBoost.InnerText = RumbleBoost[device].ToString();
                    rootElement.AppendChild(xmlRumbleBoost);
                    var xmlRumbleAutostopTime = m_Xdoc.CreateNode(XmlNodeType.Element, "RumbleAutostopTime", null);
                    xmlRumbleAutostopTime.InnerText = RumbleAutostopTime[device].ToString();
                    rootElement.AppendChild(xmlRumbleAutostopTime);
                    var xmlLightbarMode = m_Xdoc.CreateNode(XmlNodeType.Element, "LightbarMode", null);
                    xmlLightbarMode.InnerText = GetLightbarModeString(lightbarSettings.mode);
                    rootElement.AppendChild(xmlLightbarMode);
                    var xmlLedAsBatteryIndicator =
                        m_Xdoc.CreateNode(XmlNodeType.Element, "ledAsBatteryIndicator", null);
                    xmlLedAsBatteryIndicator.InnerText = lightInfo.ledAsBattery.ToString();
                    rootElement.AppendChild(xmlLedAsBatteryIndicator);
                    var xmlLowBatteryFlash = m_Xdoc.CreateNode(XmlNodeType.Element, "FlashType", null);
                    xmlLowBatteryFlash.InnerText = lightInfo.flashType.ToString();
                    rootElement.AppendChild(xmlLowBatteryFlash);
                    var xmlFlashBatterAt = m_Xdoc.CreateNode(XmlNodeType.Element, "flashBatteryAt", null);
                    xmlFlashBatterAt.InnerText = lightInfo.flashAt.ToString();
                    rootElement.AppendChild(xmlFlashBatterAt);
                    var xmlTouchSensitivity = m_Xdoc.CreateNode(XmlNodeType.Element, "touchSensitivity", null);
                    xmlTouchSensitivity.InnerText = TouchSensitivity[device].ToString();
                    rootElement.AppendChild(xmlTouchSensitivity);
                    var xmlLowColor = m_Xdoc.CreateNode(XmlNodeType.Element, "LowColor", null);
                    xmlLowColor.InnerText = lightInfo.m_LowLed.red + "," + lightInfo.m_LowLed.green + "," +
                                            lightInfo.m_LowLed.blue;
                    rootElement.AppendChild(xmlLowColor);
                    var xmlChargingColor = m_Xdoc.CreateNode(XmlNodeType.Element, "ChargingColor", null);
                    xmlChargingColor.InnerText = lightInfo.m_ChargingLed.red + "," + lightInfo.m_ChargingLed.green +
                                                 "," + lightInfo.m_ChargingLed.blue;
                    rootElement.AppendChild(xmlChargingColor);
                    var xmlFlashColor = m_Xdoc.CreateNode(XmlNodeType.Element, "FlashColor", null);
                    xmlFlashColor.InnerText = lightInfo.m_FlashLed.red + "," + lightInfo.m_FlashLed.green + "," +
                                              lightInfo.m_FlashLed.blue;
                    rootElement.AppendChild(xmlFlashColor);
                    var xmlTouchpadJitterCompensation =
                        m_Xdoc.CreateNode(XmlNodeType.Element, "touchpadJitterCompensation", null);
                    xmlTouchpadJitterCompensation.InnerText = TouchpadJitterCompensation[device].ToString();
                    rootElement.AppendChild(xmlTouchpadJitterCompensation);
                    var xmlLowerRCOn = m_Xdoc.CreateNode(XmlNodeType.Element, "lowerRCOn", null);
                    xmlLowerRCOn.InnerText = LowerRCOn[device].ToString();
                    rootElement.AppendChild(xmlLowerRCOn);
                    var xmlTapSensitivity = m_Xdoc.CreateNode(XmlNodeType.Element, "tapSensitivity", null);
                    xmlTapSensitivity.InnerText = TapSensitivity[device].ToString();
                    rootElement.AppendChild(xmlTapSensitivity);
                    var xmlDouble = m_Xdoc.CreateNode(XmlNodeType.Element, "doubleTap", null);
                    xmlDouble.InnerText = DoubleTap[device].ToString();
                    rootElement.AppendChild(xmlDouble);
                    var xmlScrollSensitivity = m_Xdoc.CreateNode(XmlNodeType.Element, "scrollSensitivity", null);
                    xmlScrollSensitivity.InnerText = ScrollSensitivity[device].ToString();
                    rootElement.AppendChild(xmlScrollSensitivity);
                    var xmlLeftTriggerMiddle = m_Xdoc.CreateNode(XmlNodeType.Element, "LeftTriggerMiddle", null);
                    xmlLeftTriggerMiddle.InnerText = L2ModInfo[device].deadZone.ToString();
                    rootElement.AppendChild(xmlLeftTriggerMiddle);
                    var xmlRightTriggerMiddle = m_Xdoc.CreateNode(XmlNodeType.Element, "RightTriggerMiddle", null);
                    xmlRightTriggerMiddle.InnerText = R2ModInfo[device].deadZone.ToString();
                    rootElement.AppendChild(xmlRightTriggerMiddle);
                    var xmlTouchpadInvert = m_Xdoc.CreateNode(XmlNodeType.Element, "TouchpadInvert", null);
                    xmlTouchpadInvert.InnerText = TouchPadInvert[device].ToString();
                    rootElement.AppendChild(xmlTouchpadInvert);
                    var xmlTouchClickPasthru = m_Xdoc.CreateNode(XmlNodeType.Element, "TouchpadClickPassthru", null);
                    xmlTouchClickPasthru.InnerText = TouchClickPassthru[device].ToString();
                    rootElement.AppendChild(xmlTouchClickPasthru);

                    var xmlL2AD = m_Xdoc.CreateNode(XmlNodeType.Element, "L2AntiDeadZone", null);
                    xmlL2AD.InnerText = L2ModInfo[device].antiDeadZone.ToString();
                    rootElement.AppendChild(xmlL2AD);
                    var xmlR2AD = m_Xdoc.CreateNode(XmlNodeType.Element, "R2AntiDeadZone", null);
                    xmlR2AD.InnerText = R2ModInfo[device].antiDeadZone.ToString();
                    rootElement.AppendChild(xmlR2AD);
                    var xmlL2Maxzone = m_Xdoc.CreateNode(XmlNodeType.Element, "L2MaxZone", null);
                    xmlL2Maxzone.InnerText = L2ModInfo[device].maxZone.ToString();
                    rootElement.AppendChild(xmlL2Maxzone);
                    var xmlR2Maxzone = m_Xdoc.CreateNode(XmlNodeType.Element, "R2MaxZone", null);
                    xmlR2Maxzone.InnerText = R2ModInfo[device].maxZone.ToString();
                    rootElement.AppendChild(xmlR2Maxzone);
                    var xmlL2MaxOutput = m_Xdoc.CreateNode(XmlNodeType.Element, "L2MaxOutput", null);
                    xmlL2MaxOutput.InnerText = L2ModInfo[device].maxOutput.ToString();
                    rootElement.AppendChild(xmlL2MaxOutput);
                    var xmlR2MaxOutput = m_Xdoc.CreateNode(XmlNodeType.Element, "R2MaxOutput", null);
                    xmlR2MaxOutput.InnerText = R2ModInfo[device].maxOutput.ToString();
                    rootElement.AppendChild(xmlR2MaxOutput);
                    var xmlButtonMouseSensitivity =
                        m_Xdoc.CreateNode(XmlNodeType.Element, "ButtonMouseSensitivity", null);
                    xmlButtonMouseSensitivity.InnerText = ButtonMouseInfos[device].buttonSensitivity.ToString();
                    rootElement.AppendChild(xmlButtonMouseSensitivity);
                    var xmlButtonMouseOffset = m_Xdoc.CreateNode(XmlNodeType.Element, "ButtonMouseOffset", null);
                    xmlButtonMouseOffset.InnerText = ButtonMouseInfos[device].mouseVelocityOffset.ToString();
                    rootElement.AppendChild(xmlButtonMouseOffset);
                    var xmlRainbow = m_Xdoc.CreateNode(XmlNodeType.Element, "Rainbow", null);
                    xmlRainbow.InnerText = lightInfo.rainbow.ToString();
                    rootElement.AppendChild(xmlRainbow);
                    var xmlMaxSatRainbow = m_Xdoc.CreateNode(XmlNodeType.Element, "MaxSatRainbow", null);
                    xmlMaxSatRainbow.InnerText = Convert.ToInt32(lightInfo.maxRainbowSat * 100.0).ToString();
                    rootElement.AppendChild(xmlMaxSatRainbow);
                    var xmlLSD = m_Xdoc.CreateNode(XmlNodeType.Element, "LSDeadZone", null);
                    xmlLSD.InnerText = LSModInfo[device].deadZone.ToString();
                    rootElement.AppendChild(xmlLSD);
                    var xmlRSD = m_Xdoc.CreateNode(XmlNodeType.Element, "RSDeadZone", null);
                    xmlRSD.InnerText = RSModInfo[device].deadZone.ToString();
                    rootElement.AppendChild(xmlRSD);
                    var xmlLSAD = m_Xdoc.CreateNode(XmlNodeType.Element, "LSAntiDeadZone", null);
                    xmlLSAD.InnerText = LSModInfo[device].antiDeadZone.ToString();
                    rootElement.AppendChild(xmlLSAD);
                    var xmlRSAD = m_Xdoc.CreateNode(XmlNodeType.Element, "RSAntiDeadZone", null);
                    xmlRSAD.InnerText = RSModInfo[device].antiDeadZone.ToString();
                    rootElement.AppendChild(xmlRSAD);
                    var xmlLSMaxZone = m_Xdoc.CreateNode(XmlNodeType.Element, "LSMaxZone", null);
                    xmlLSMaxZone.InnerText = LSModInfo[device].maxZone.ToString();
                    rootElement.AppendChild(xmlLSMaxZone);
                    var xmlRSMaxZone = m_Xdoc.CreateNode(XmlNodeType.Element, "RSMaxZone", null);
                    xmlRSMaxZone.InnerText = RSModInfo[device].maxZone.ToString();
                    rootElement.AppendChild(xmlRSMaxZone);
                    var xmlLSVerticalScale = m_Xdoc.CreateNode(XmlNodeType.Element, "LSVerticalScale", null);
                    xmlLSVerticalScale.InnerText = LSModInfo[device].verticalScale.ToString();
                    rootElement.AppendChild(xmlLSVerticalScale);
                    var xmlRSVerticalScale = m_Xdoc.CreateNode(XmlNodeType.Element, "RSVerticalScale", null);
                    xmlRSVerticalScale.InnerText = RSModInfo[device].verticalScale.ToString();
                    rootElement.AppendChild(xmlRSVerticalScale);
                    var xmlLSMaxOutput = m_Xdoc.CreateNode(XmlNodeType.Element, "LSMaxOutput", null);
                    xmlLSMaxOutput.InnerText = LSModInfo[device].maxOutput.ToString();
                    rootElement.AppendChild(xmlLSMaxOutput);
                    var xmlRSMaxOutput = m_Xdoc.CreateNode(XmlNodeType.Element, "RSMaxOutput", null);
                    xmlRSMaxOutput.InnerText = RSModInfo[device].maxOutput.ToString();
                    rootElement.AppendChild(xmlRSMaxOutput);
                    var xmlLSMaxOutputForce = m_Xdoc.CreateNode(XmlNodeType.Element, "LSMaxOutputForce", null);
                    xmlLSMaxOutputForce.InnerText = LSModInfo[device].maxOutputForce.ToString();
                    rootElement.AppendChild(xmlLSMaxOutputForce);
                    var xmlRSMaxOutputForce = m_Xdoc.CreateNode(XmlNodeType.Element, "RSMaxOutputForce", null);
                    xmlRSMaxOutputForce.InnerText = RSModInfo[device].maxOutputForce.ToString();
                    rootElement.AppendChild(xmlRSMaxOutputForce);
                    var xmlLSDeadZoneType = m_Xdoc.CreateNode(XmlNodeType.Element, "LSDeadZoneType", null);
                    xmlLSDeadZoneType.InnerText = LSModInfo[device].deadzoneType.ToString();
                    rootElement.AppendChild(xmlLSDeadZoneType);
                    var xmlRSDeadZoneType = m_Xdoc.CreateNode(XmlNodeType.Element, "RSDeadZoneType", null);
                    xmlRSDeadZoneType.InnerText = RSModInfo[device].deadzoneType.ToString();
                    rootElement.AppendChild(xmlRSDeadZoneType);

                    var xmlLSAxialDeadGroupEl = m_Xdoc.CreateElement("LSAxialDeadOptions");
                    var xmlLSAxialDeadX = m_Xdoc.CreateElement("DeadZoneX");
                    xmlLSAxialDeadX.InnerText = LSModInfo[device].xAxisDeadInfo.deadZone.ToString();
                    xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialDeadX);
                    var xmlLSAxialDeadY = m_Xdoc.CreateElement("DeadZoneY");
                    xmlLSAxialDeadY.InnerText = LSModInfo[device].yAxisDeadInfo.deadZone.ToString();
                    xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialDeadY);
                    var xmlLSAxialMaxX = m_Xdoc.CreateElement("MaxZoneX");
                    xmlLSAxialMaxX.InnerText = LSModInfo[device].xAxisDeadInfo.maxZone.ToString();
                    xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialMaxX);
                    var xmlLSAxialMaxY = m_Xdoc.CreateElement("MaxZoneY");
                    xmlLSAxialMaxY.InnerText = LSModInfo[device].yAxisDeadInfo.maxZone.ToString();
                    xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialMaxY);
                    var xmlLSAxialAntiDeadX = m_Xdoc.CreateElement("AntiDeadZoneX");
                    xmlLSAxialAntiDeadX.InnerText = LSModInfo[device].xAxisDeadInfo.antiDeadZone.ToString();
                    xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialAntiDeadX);
                    var xmlLSAxialAntiDeadY = m_Xdoc.CreateElement("AntiDeadZoneY");
                    xmlLSAxialAntiDeadY.InnerText = LSModInfo[device].yAxisDeadInfo.antiDeadZone.ToString();
                    xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialAntiDeadY);
                    var xmlLSAxialMaxOutputX = m_Xdoc.CreateElement("MaxOutputX");
                    xmlLSAxialMaxOutputX.InnerText = LSModInfo[device].xAxisDeadInfo.maxOutput.ToString();
                    xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialMaxOutputX);
                    var xmlLSAxialMaxOutputY = m_Xdoc.CreateElement("MaxOutputY");
                    xmlLSAxialMaxOutputY.InnerText = LSModInfo[device].yAxisDeadInfo.maxOutput.ToString();
                    xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialMaxOutputY);
                    rootElement.AppendChild(xmlLSAxialDeadGroupEl);

                    var xmlRSAxialDeadGroupEl = m_Xdoc.CreateElement("RSAxialDeadOptions");
                    var xmlRSAxialDeadX = m_Xdoc.CreateElement("DeadZoneX");
                    xmlRSAxialDeadX.InnerText = RSModInfo[device].xAxisDeadInfo.deadZone.ToString();
                    xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialDeadX);
                    var xmlRSAxialDeadY = m_Xdoc.CreateElement("DeadZoneY");
                    xmlRSAxialDeadY.InnerText = RSModInfo[device].yAxisDeadInfo.deadZone.ToString();
                    xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialDeadY);
                    var xmlRSAxialMaxX = m_Xdoc.CreateElement("MaxZoneX");
                    xmlRSAxialMaxX.InnerText = RSModInfo[device].xAxisDeadInfo.maxZone.ToString();
                    xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialMaxX);
                    var xmlRSAxialMaxY = m_Xdoc.CreateElement("MaxZoneY");
                    xmlRSAxialMaxY.InnerText = RSModInfo[device].yAxisDeadInfo.maxZone.ToString();
                    xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialMaxY);
                    var xmlRSAxialAntiDeadX = m_Xdoc.CreateElement("AntiDeadZoneX");
                    xmlRSAxialAntiDeadX.InnerText = RSModInfo[device].xAxisDeadInfo.antiDeadZone.ToString();
                    xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialAntiDeadX);
                    var xmlRSAxialAntiDeadY = m_Xdoc.CreateElement("AntiDeadZoneY");
                    xmlRSAxialAntiDeadY.InnerText = RSModInfo[device].yAxisDeadInfo.antiDeadZone.ToString();
                    xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialAntiDeadY);
                    var xmlRSAxialMaxOutputX = m_Xdoc.CreateElement("MaxOutputX");
                    xmlRSAxialMaxOutputX.InnerText = RSModInfo[device].xAxisDeadInfo.maxOutput.ToString();
                    xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialMaxOutputX);
                    var xmlRSAxialMaxOutputY = m_Xdoc.CreateElement("MaxOutputY");
                    xmlRSAxialMaxOutputY.InnerText = RSModInfo[device].yAxisDeadInfo.maxOutput.ToString();
                    xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialMaxOutputY);
                    rootElement.AppendChild(xmlRSAxialDeadGroupEl);

                    var xmlLSRotation = m_Xdoc.CreateNode(XmlNodeType.Element, "LSRotation", null);
                    xmlLSRotation.InnerText = Convert.ToInt32(LSRotation[device] * 180.0 / Math.PI).ToString();
                    rootElement.AppendChild(xmlLSRotation);
                    var xmlRSRotation = m_Xdoc.CreateNode(XmlNodeType.Element, "RSRotation", null);
                    xmlRSRotation.InnerText = Convert.ToInt32(RSRotation[device] * 180.0 / Math.PI).ToString();
                    rootElement.AppendChild(xmlRSRotation);
                    var xmlLSFuzz = m_Xdoc.CreateNode(XmlNodeType.Element, "LSFuzz", null);
                    xmlLSFuzz.InnerText = LSModInfo[device].fuzz.ToString();
                    rootElement.AppendChild(xmlLSFuzz);
                    var xmlRSFuzz = m_Xdoc.CreateNode(XmlNodeType.Element, "RSFuzz", null);
                    xmlRSFuzz.InnerText = RSModInfo[device].fuzz.ToString();
                    rootElement.AppendChild(xmlRSFuzz);
                    var xmlLSOuterBindDead = m_Xdoc.CreateNode(XmlNodeType.Element, "LSOuterBindDead", null);
                    xmlLSOuterBindDead.InnerText = Convert.ToInt32(LSModInfo[device].outerBindDeadZone).ToString();
                    rootElement.AppendChild(xmlLSOuterBindDead);
                    var xmlRSOuterBindDead = m_Xdoc.CreateNode(XmlNodeType.Element, "RSOuterBindDead", null);
                    xmlRSOuterBindDead.InnerText = Convert.ToInt32(RSModInfo[device].outerBindDeadZone).ToString();
                    rootElement.AppendChild(xmlRSOuterBindDead);
                    var xmlLSOuterBindInvert = m_Xdoc.CreateNode(XmlNodeType.Element, "LSOuterBindInvert", null);
                    xmlLSOuterBindInvert.InnerText = LSModInfo[device].outerBindInvert.ToString();
                    rootElement.AppendChild(xmlLSOuterBindInvert);
                    var xmlRSOuterBindInvert = m_Xdoc.CreateNode(XmlNodeType.Element, "RSOuterBindInvert", null);
                    xmlRSOuterBindInvert.InnerText = RSModInfo[device].outerBindInvert.ToString();
                    rootElement.AppendChild(xmlRSOuterBindInvert);

                    var xmlSXD = m_Xdoc.CreateNode(XmlNodeType.Element, "SXDeadZone", null);
                    xmlSXD.InnerText = SXDeadzone[device].ToString();
                    rootElement.AppendChild(xmlSXD);
                    var xmlSZD = m_Xdoc.CreateNode(XmlNodeType.Element, "SZDeadZone", null);
                    xmlSZD.InnerText = SZDeadzone[device].ToString();
                    rootElement.AppendChild(xmlSZD);

                    var xmlSXMaxzone = m_Xdoc.CreateNode(XmlNodeType.Element, "SXMaxZone", null);
                    xmlSXMaxzone.InnerText = Convert.ToInt32(SXMaxzone[device] * 100.0).ToString();
                    rootElement.AppendChild(xmlSXMaxzone);
                    var xmlSZMaxzone = m_Xdoc.CreateNode(XmlNodeType.Element, "SZMaxZone", null);
                    xmlSZMaxzone.InnerText = Convert.ToInt32(SZMaxzone[device] * 100.0).ToString();
                    rootElement.AppendChild(xmlSZMaxzone);

                    var xmlSXAntiDeadzone = m_Xdoc.CreateNode(XmlNodeType.Element, "SXAntiDeadZone", null);
                    xmlSXAntiDeadzone.InnerText = Convert.ToInt32(SXAntiDeadzone[device] * 100.0).ToString();
                    rootElement.AppendChild(xmlSXAntiDeadzone);
                    var xmlSZAntiDeadzone = m_Xdoc.CreateNode(XmlNodeType.Element, "SZAntiDeadZone", null);
                    xmlSZAntiDeadzone.InnerText = Convert.ToInt32(SZAntiDeadzone[device] * 100.0).ToString();
                    rootElement.AppendChild(xmlSZAntiDeadzone);

                    var xmlSens = m_Xdoc.CreateNode(XmlNodeType.Element, "Sensitivity", null);
                    xmlSens.InnerText =
                        $"{LSSens[device]}|{RSSens[device]}|{L2Sens[device]}|{R2Sens[device]}|{SXSens[device]}|{SZSens[device]}";
                    rootElement.AppendChild(xmlSens);

                    var xmlChargingType = m_Xdoc.CreateNode(XmlNodeType.Element, "ChargingType", null);
                    xmlChargingType.InnerText = lightInfo.chargingType.ToString();
                    rootElement.AppendChild(xmlChargingType);
                    var xmlMouseAccel = m_Xdoc.CreateNode(XmlNodeType.Element, "MouseAcceleration", null);
                    xmlMouseAccel.InnerText = ButtonMouseInfos[device].mouseAccel.ToString();
                    rootElement.AppendChild(xmlMouseAccel);
                    var xmlMouseVerticalScale =
                        m_Xdoc.CreateNode(XmlNodeType.Element, "ButtonMouseVerticalScale", null);
                    xmlMouseVerticalScale.InnerText =
                        Convert.ToInt32(ButtonMouseInfos[device].buttonVerticalScale * 100).ToString();
                    rootElement.AppendChild(xmlMouseVerticalScale);
                    //XmlNode xmlShiftMod = m_Xdoc.CreateNode(XmlNodeType.Element, "ShiftModifier", null); xmlShiftMod.InnerText = shiftModifier[device].ToString(); rootElement.AppendChild(xmlShiftMod);
                    var xmlLaunchProgram = m_Xdoc.CreateNode(XmlNodeType.Element, "LaunchProgram", null);
                    xmlLaunchProgram.InnerText = LaunchProgram[device];
                    rootElement.AppendChild(xmlLaunchProgram);
                    var xmlDinput = m_Xdoc.CreateNode(XmlNodeType.Element, "DinputOnly", null);
                    xmlDinput.InnerText = DirectInputOnly[device].ToString();
                    rootElement.AppendChild(xmlDinput);
                    var xmlStartTouchpadOff = m_Xdoc.CreateNode(XmlNodeType.Element, "StartTouchpadOff", null);
                    xmlStartTouchpadOff.InnerText = StartTouchpadOff[device].ToString();
                    rootElement.AppendChild(xmlStartTouchpadOff);
                    var xmlTouchOutMode = m_Xdoc.CreateNode(XmlNodeType.Element, "TouchpadOutputMode", null);
                    xmlTouchOutMode.InnerText = TouchOutMode[device].ToString();
                    rootElement.AppendChild(xmlTouchOutMode);
                    var xmlSATriggers = m_Xdoc.CreateNode(XmlNodeType.Element, "SATriggers", null);
                    xmlSATriggers.InnerText = SATriggers[device];
                    rootElement.AppendChild(xmlSATriggers);
                    var xmlSATriggerCond = m_Xdoc.CreateNode(XmlNodeType.Element, "SATriggerCond", null);
                    xmlSATriggerCond.InnerText = SaTriggerCondString(SATriggerCond[device]);
                    rootElement.AppendChild(xmlSATriggerCond);
                    var xmlSASteeringWheelEmulationAxis =
                        m_Xdoc.CreateNode(XmlNodeType.Element, "SASteeringWheelEmulationAxis", null);
                    xmlSASteeringWheelEmulationAxis.InnerText = SASteeringWheelEmulationAxis[device].ToString("G");
                    rootElement.AppendChild(xmlSASteeringWheelEmulationAxis);
                    var xmlSASteeringWheelEmulationRange =
                        m_Xdoc.CreateNode(XmlNodeType.Element, "SASteeringWheelEmulationRange", null);
                    xmlSASteeringWheelEmulationRange.InnerText = SASteeringWheelEmulationRange[device].ToString();
                    rootElement.AppendChild(xmlSASteeringWheelEmulationRange);
                    var xmlSASteeringWheelFuzz = m_Xdoc.CreateNode(XmlNodeType.Element, "SASteeringWheelFuzz", null);
                    xmlSASteeringWheelFuzz.InnerText = SAWheelFuzzValues[device].ToString();
                    rootElement.AppendChild(xmlSASteeringWheelFuzz);

                    var xmlSASteeringWheelSmoothingGroupEl = m_Xdoc.CreateElement("SASteeringWheelSmoothingOptions");
                    var xmlSASteeringWheelUseSmoothing = m_Xdoc.CreateElement("SASteeringWheelUseSmoothing");
                    xmlSASteeringWheelUseSmoothing.InnerText = WheelSmoothInfo[device].Enabled.ToString();
                    xmlSASteeringWheelSmoothingGroupEl.AppendChild(xmlSASteeringWheelUseSmoothing);
                    var xmlSASteeringWheelSmoothMinCutoff = m_Xdoc.CreateElement("SASteeringWheelSmoothMinCutoff");
                    xmlSASteeringWheelSmoothMinCutoff.InnerText = WheelSmoothInfo[device].MinCutoff.ToString();
                    xmlSASteeringWheelSmoothingGroupEl.AppendChild(xmlSASteeringWheelSmoothMinCutoff);
                    var xmlSASteeringWheelSmoothBeta = m_Xdoc.CreateElement("SASteeringWheelSmoothBeta");
                    xmlSASteeringWheelSmoothBeta.InnerText = WheelSmoothInfo[device].Beta.ToString();
                    xmlSASteeringWheelSmoothingGroupEl.AppendChild(xmlSASteeringWheelSmoothBeta);
                    rootElement.AppendChild(xmlSASteeringWheelSmoothingGroupEl);

                    //XmlNode xmlSASteeringWheelUseSmoothing = m_Xdoc.CreateNode(XmlNodeType.Element, "SASteeringWheelUseSmoothing", null); xmlSASteeringWheelUseSmoothing.InnerText = wheelSmoothInfo[device].Enabled.ToString(); rootElement.AppendChild(xmlSASteeringWheelUseSmoothing);
                    //XmlNode xmlSASteeringWheelSmoothMinCutoff = m_Xdoc.CreateNode(XmlNodeType.Element, "SASteeringWheelSmoothMinCutoff", null); xmlSASteeringWheelSmoothMinCutoff.InnerText = wheelSmoothInfo[device].MinCutoff.ToString(); rootElement.AppendChild(xmlSASteeringWheelSmoothMinCutoff);
                    //XmlNode xmlSASteeringWheelSmoothBeta = m_Xdoc.CreateNode(XmlNodeType.Element, "SASteeringWheelSmoothBeta", null); xmlSASteeringWheelSmoothBeta.InnerText = wheelSmoothInfo[device].Beta.ToString(); rootElement.AppendChild(xmlSASteeringWheelSmoothBeta);

                    var xmlTouchDisInvTriggers = m_Xdoc.CreateNode(XmlNodeType.Element, "TouchDisInvTriggers", null);
                    var tempTouchDisInv = string.Join(",", TouchDisInvertTriggers[device]);
                    xmlTouchDisInvTriggers.InnerText = tempTouchDisInv;
                    rootElement.AppendChild(xmlTouchDisInvTriggers);

                    var xmlGyroSensitivity = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroSensitivity", null);
                    xmlGyroSensitivity.InnerText = GyroSensitivity[device].ToString();
                    rootElement.AppendChild(xmlGyroSensitivity);
                    var xmlGyroSensVerticalScale =
                        m_Xdoc.CreateNode(XmlNodeType.Element, "GyroSensVerticalScale", null);
                    xmlGyroSensVerticalScale.InnerText = GyroSensVerticalScale[device].ToString();
                    rootElement.AppendChild(xmlGyroSensVerticalScale);
                    var xmlGyroInvert = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroInvert", null);
                    xmlGyroInvert.InnerText = GyroInvert[device].ToString();
                    rootElement.AppendChild(xmlGyroInvert);
                    var xmlGyroTriggerTurns = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroTriggerTurns", null);
                    xmlGyroTriggerTurns.InnerText = GyroTriggerTurns[device].ToString();
                    rootElement.AppendChild(xmlGyroTriggerTurns);
                    /*XmlNode xmlGyroSmoothWeight = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroSmoothingWeight", null); xmlGyroSmoothWeight.InnerText = Convert.ToInt32(gyroSmoothWeight[device] * 100).ToString(); rootElement.AppendChild(xmlGyroSmoothWeight);
                    XmlNode xmlGyroSmoothing = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroSmoothing", null); xmlGyroSmoothing.InnerText = gyroSmoothing[device].ToString(); rootElement.AppendChild(xmlGyroSmoothing);
                    */

                    var xmlGyroControlsSettingsElement = m_Xdoc.CreateElement("GyroControlsSettings");
                    var xmlGyroControlsTriggers = m_Xdoc.CreateNode(XmlNodeType.Element, "Triggers", null);
                    xmlGyroControlsTriggers.InnerText = GyroControlsInfo[device].triggers;
                    xmlGyroControlsSettingsElement.AppendChild(xmlGyroControlsTriggers);
                    var xmlGyroControlsTriggerCond = m_Xdoc.CreateNode(XmlNodeType.Element, "TriggerCond", null);
                    xmlGyroControlsTriggerCond.InnerText = SaTriggerCondString(GyroControlsInfo[device].triggerCond);
                    xmlGyroControlsSettingsElement.AppendChild(xmlGyroControlsTriggerCond);
                    var xmlGyroControlsTriggerTurns = m_Xdoc.CreateNode(XmlNodeType.Element, "TriggerTurns", null);
                    xmlGyroControlsTriggerTurns.InnerText = GyroControlsInfo[device].triggerTurns.ToString();
                    xmlGyroControlsSettingsElement.AppendChild(xmlGyroControlsTriggerTurns);
                    var xmlGyroControlsToggle = m_Xdoc.CreateNode(XmlNodeType.Element, "Toggle", null);
                    xmlGyroControlsToggle.InnerText = GyroControlsInfo[device].triggerToggle.ToString();
                    xmlGyroControlsSettingsElement.AppendChild(xmlGyroControlsToggle);
                    rootElement.AppendChild(xmlGyroControlsSettingsElement);

                    var xmlGyroSmoothingElement = m_Xdoc.CreateElement("GyroMouseSmoothingSettings");
                    var xmlGyroSmoothing = m_Xdoc.CreateNode(XmlNodeType.Element, "UseSmoothing", null);
                    xmlGyroSmoothing.InnerText = GyroMouseInfo[device].enableSmoothing.ToString();
                    xmlGyroSmoothingElement.AppendChild(xmlGyroSmoothing);
                    var xmlGyroSmoothingMethod = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingMethod", null);
                    xmlGyroSmoothingMethod.InnerText = GyroMouseInfo[device].SmoothMethodIdentifier();
                    xmlGyroSmoothingElement.AppendChild(xmlGyroSmoothingMethod);
                    var xmlGyroSmoothWeight = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingWeight", null);
                    xmlGyroSmoothWeight.InnerText =
                        Convert.ToInt32(GyroMouseInfo[device].smoothingWeight * 100).ToString();
                    xmlGyroSmoothingElement.AppendChild(xmlGyroSmoothWeight);
                    var xmlGyroSmoothMincutoff = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingMinCutoff", null);
                    xmlGyroSmoothMincutoff.InnerText = GyroMouseInfo[device].minCutoff.ToString();
                    xmlGyroSmoothingElement.AppendChild(xmlGyroSmoothMincutoff);
                    var xmlGyroSmoothBeta = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingBeta", null);
                    xmlGyroSmoothBeta.InnerText = GyroMouseInfo[device].beta.ToString();
                    xmlGyroSmoothingElement.AppendChild(xmlGyroSmoothBeta);
                    rootElement.AppendChild(xmlGyroSmoothingElement);

                    var xmlGyroMouseHAxis = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseHAxis", null);
                    xmlGyroMouseHAxis.InnerText = GyroMouseHorizontalAxis[device].ToString();
                    rootElement.AppendChild(xmlGyroMouseHAxis);
                    var xmlGyroMouseDZ = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseDeadZone", null);
                    xmlGyroMouseDZ.InnerText = GyroMouseDeadZone[device].ToString();
                    rootElement.AppendChild(xmlGyroMouseDZ);
                    var xmlGyroMinThreshold = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseMinThreshold", null);
                    xmlGyroMinThreshold.InnerText = GyroMouseInfo[device].minThreshold.ToString();
                    rootElement.AppendChild(xmlGyroMinThreshold);
                    var xmlGyroMouseToggle = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseToggle", null);
                    xmlGyroMouseToggle.InnerText = GyroMouseToggle[device].ToString();
                    rootElement.AppendChild(xmlGyroMouseToggle);

                    var xmlGyroOutMode = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroOutputMode", null);
                    xmlGyroOutMode.InnerText = GyroOutputMode[device].ToString();
                    rootElement.AppendChild(xmlGyroOutMode);
                    var xmlGyroMStickTriggers = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickTriggers", null);
                    xmlGyroMStickTriggers.InnerText = SAMouseStickTriggers[device];
                    rootElement.AppendChild(xmlGyroMStickTriggers);
                    var xmlGyroMStickTriggerCond =
                        m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickTriggerCond", null);
                    xmlGyroMStickTriggerCond.InnerText = SaTriggerCondString(SAMouseStickTriggerCond[device]);
                    rootElement.AppendChild(xmlGyroMStickTriggerCond);
                    var xmlGyroMStickTriggerTurns =
                        m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickTriggerTurns", null);
                    xmlGyroMStickTriggerTurns.InnerText = GyroMouseStickTriggerTurns[device].ToString();
                    rootElement.AppendChild(xmlGyroMStickTriggerTurns);
                    var xmlGyroMStickHAxis = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickHAxis", null);
                    xmlGyroMStickHAxis.InnerText = GyroMouseStickHorizontalAxis[device].ToString();
                    rootElement.AppendChild(xmlGyroMStickHAxis);
                    var xmlGyroMStickDZ = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickDeadZone", null);
                    xmlGyroMStickDZ.InnerText = GyroMouseStickInfo[device].deadZone.ToString();
                    rootElement.AppendChild(xmlGyroMStickDZ);
                    var xmlGyroMStickMaxZ = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickMaxZone", null);
                    xmlGyroMStickMaxZ.InnerText = GyroMouseStickInfo[device].maxZone.ToString();
                    rootElement.AppendChild(xmlGyroMStickMaxZ);
                    var xmlGyroMStickOutputStick =
                        m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickOutputStick", null);
                    xmlGyroMStickOutputStick.InnerText = GyroMouseStickInfo[device].outputStick.ToString();
                    rootElement.AppendChild(xmlGyroMStickOutputStick);
                    var xmlGyroMStickOutputStickAxes =
                        m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickOutputStickAxes", null);
                    xmlGyroMStickOutputStickAxes.InnerText = GyroMouseStickInfo[device].outputStickDir.ToString();
                    rootElement.AppendChild(xmlGyroMStickOutputStickAxes);
                    var xmlGyroMStickAntiDX = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickAntiDeadX", null);
                    xmlGyroMStickAntiDX.InnerText = GyroMouseStickInfo[device].antiDeadX.ToString();
                    rootElement.AppendChild(xmlGyroMStickAntiDX);
                    var xmlGyroMStickAntiDY = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickAntiDeadY", null);
                    xmlGyroMStickAntiDY.InnerText = GyroMouseStickInfo[device].antiDeadY.ToString();
                    rootElement.AppendChild(xmlGyroMStickAntiDY);
                    var xmlGyroMStickInvert = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickInvert", null);
                    xmlGyroMStickInvert.InnerText = GyroMouseStickInfo[device].inverted.ToString();
                    rootElement.AppendChild(xmlGyroMStickInvert);
                    var xmlGyroMStickToggle = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickToggle", null);
                    xmlGyroMStickToggle.InnerText = GyroMouseStickToggle[device].ToString();
                    rootElement.AppendChild(xmlGyroMStickToggle);
                    var xmlGyroMStickMaxOutput =
                        m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickMaxOutput", null);
                    xmlGyroMStickMaxOutput.InnerText = GyroMouseStickInfo[device].maxOutput.ToString();
                    rootElement.AppendChild(xmlGyroMStickMaxOutput);
                    var xmlGyroMStickMaxOutputEnabled =
                        m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickMaxOutputEnabled", null);
                    xmlGyroMStickMaxOutputEnabled.InnerText = GyroMouseStickInfo[device].maxOutputEnabled.ToString();
                    rootElement.AppendChild(xmlGyroMStickMaxOutputEnabled);
                    var xmlGyroMStickVerticalScale =
                        m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickVerticalScale", null);
                    xmlGyroMStickVerticalScale.InnerText = GyroMouseStickInfo[device].vertScale.ToString();
                    rootElement.AppendChild(xmlGyroMStickVerticalScale);


                    /*XmlNode xmlGyroMStickSmoothing = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickSmoothing", null); xmlGyroMStickSmoothing.InnerText = gyroMStickInfo[device].useSmoothing.ToString(); rootElement.AppendChild(xmlGyroMStickSmoothing);
                    XmlNode xmlGyroMStickSmoothWeight = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickSmoothingWeight", null); xmlGyroMStickSmoothWeight.InnerText = Convert.ToInt32(gyroMStickInfo[device].smoothWeight * 100).ToString(); rootElement.AppendChild(xmlGyroMStickSmoothWeight);
                    */
                    var xmlGyroMStickSmoothingElement = m_Xdoc.CreateElement("GyroMouseStickSmoothingSettings");
                    var xmlGyroMStickSmoothing = m_Xdoc.CreateNode(XmlNodeType.Element, "UseSmoothing", null);
                    xmlGyroMStickSmoothing.InnerText = GyroMouseStickInfo[device].useSmoothing.ToString();
                    xmlGyroMStickSmoothingElement.AppendChild(xmlGyroMStickSmoothing);
                    var xmlGyroMStickSmoothingMethod = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingMethod", null);
                    xmlGyroMStickSmoothingMethod.InnerText = GyroMouseStickInfo[device].SmoothMethodIdentifier();
                    xmlGyroMStickSmoothingElement.AppendChild(xmlGyroMStickSmoothingMethod);
                    var xmlGyroMStickSmoothWeight = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingWeight", null);
                    xmlGyroMStickSmoothWeight.InnerText =
                        Convert.ToInt32(GyroMouseStickInfo[device].smoothWeight * 100).ToString();
                    xmlGyroMStickSmoothingElement.AppendChild(xmlGyroMStickSmoothWeight);
                    var xmlGyroMStickSmoothMincutoff =
                        m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingMinCutoff", null);
                    xmlGyroMStickSmoothMincutoff.InnerText = GyroMouseStickInfo[device].minCutoff.ToString();
                    xmlGyroMStickSmoothingElement.AppendChild(xmlGyroMStickSmoothMincutoff);
                    var xmlGyroMStickSmoothBeta = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingBeta", null);
                    xmlGyroMStickSmoothBeta.InnerText = GyroMouseStickInfo[device].beta.ToString();
                    xmlGyroMStickSmoothingElement.AppendChild(xmlGyroMStickSmoothBeta);
                    rootElement.AppendChild(xmlGyroMStickSmoothingElement);

                    var xmlGyroSwipeSettingsElement = m_Xdoc.CreateElement("GyroSwipeSettings");
                    var xmlGyroSwipeDeadzoneX = m_Xdoc.CreateNode(XmlNodeType.Element, "DeadZoneX", null);
                    xmlGyroSwipeDeadzoneX.InnerText = GyroSwipeInfo[device].deadzoneX.ToString();
                    xmlGyroSwipeSettingsElement.AppendChild(xmlGyroSwipeDeadzoneX);
                    var xmlGyroSwipeDeadzoneY = m_Xdoc.CreateNode(XmlNodeType.Element, "DeadZoneY", null);
                    xmlGyroSwipeDeadzoneY.InnerText = GyroSwipeInfo[device].deadzoneY.ToString();
                    xmlGyroSwipeSettingsElement.AppendChild(xmlGyroSwipeDeadzoneY);
                    var xmlGyroSwipeTriggers = m_Xdoc.CreateNode(XmlNodeType.Element, "Triggers", null);
                    xmlGyroSwipeTriggers.InnerText = GyroSwipeInfo[device].triggers;
                    xmlGyroSwipeSettingsElement.AppendChild(xmlGyroSwipeTriggers);
                    var xmlGyroSwipeTriggerCond = m_Xdoc.CreateNode(XmlNodeType.Element, "TriggerCond", null);
                    xmlGyroSwipeTriggerCond.InnerText = SaTriggerCondString(GyroSwipeInfo[device].triggerCond);
                    xmlGyroSwipeSettingsElement.AppendChild(xmlGyroSwipeTriggerCond);
                    var xmlGyroSwipeTriggerTurns = m_Xdoc.CreateNode(XmlNodeType.Element, "TriggerTurns", null);
                    xmlGyroSwipeTriggerTurns.InnerText = GyroSwipeInfo[device].triggerTurns.ToString();
                    xmlGyroSwipeSettingsElement.AppendChild(xmlGyroSwipeTriggerTurns);
                    var xmlGyroSwipeXAxis = m_Xdoc.CreateNode(XmlNodeType.Element, "XAxis", null);
                    xmlGyroSwipeXAxis.InnerText = GyroSwipeInfo[device].xAxis.ToString();
                    xmlGyroSwipeSettingsElement.AppendChild(xmlGyroSwipeXAxis);
                    var xmlGyroSwipeDelayTime = m_Xdoc.CreateNode(XmlNodeType.Element, "DelayTime", null);
                    xmlGyroSwipeDelayTime.InnerText = GyroSwipeInfo[device].delayTime.ToString();
                    xmlGyroSwipeSettingsElement.AppendChild(xmlGyroSwipeDelayTime);
                    rootElement.AppendChild(xmlGyroSwipeSettingsElement);

                    var xmlProfileActions = m_Xdoc.CreateNode(XmlNodeType.Element, "ProfileActions", null);
                    xmlProfileActions.InnerText = string.Join("/", ProfileActions[device]);
                    rootElement.AppendChild(xmlProfileActions);
                    var xmlBTPollRate = m_Xdoc.CreateNode(XmlNodeType.Element, "BTPollRate", null);
                    xmlBTPollRate.InnerText = BluetoothPollRate[device].ToString();
                    rootElement.AppendChild(xmlBTPollRate);

                    var xmlLsOutputCurveMode = m_Xdoc.CreateNode(XmlNodeType.Element, "LSOutputCurveMode", null);
                    xmlLsOutputCurveMode.InnerText = stickOutputCurveString(getLsOutCurveMode(device));
                    rootElement.AppendChild(xmlLsOutputCurveMode);
                    var xmlLsOutputCurveCustom = m_Xdoc.CreateNode(XmlNodeType.Element, "LSOutputCurveCustom", null);
                    xmlLsOutputCurveCustom.InnerText = LSOutBezierCurveObj[device].ToString();
                    rootElement.AppendChild(xmlLsOutputCurveCustom);

                    var xmlRsOutputCurveMode = m_Xdoc.CreateNode(XmlNodeType.Element, "RSOutputCurveMode", null);
                    xmlRsOutputCurveMode.InnerText = stickOutputCurveString(getRsOutCurveMode(device));
                    rootElement.AppendChild(xmlRsOutputCurveMode);
                    var xmlRsOutputCurveCustom = m_Xdoc.CreateNode(XmlNodeType.Element, "RSOutputCurveCustom", null);
                    xmlRsOutputCurveCustom.InnerText = RSOutBezierCurveObj[device].ToString();
                    rootElement.AppendChild(xmlRsOutputCurveCustom);

                    var xmlLsSquareStickMode = m_Xdoc.CreateNode(XmlNodeType.Element, "LSSquareStick", null);
                    xmlLsSquareStickMode.InnerText = SquStickInfo[device].lsMode.ToString();
                    rootElement.AppendChild(xmlLsSquareStickMode);
                    var xmlRsSquareStickMode = m_Xdoc.CreateNode(XmlNodeType.Element, "RSSquareStick", null);
                    xmlRsSquareStickMode.InnerText = SquStickInfo[device].rsMode.ToString();
                    rootElement.AppendChild(xmlRsSquareStickMode);

                    var xmlSquareStickRoundness = m_Xdoc.CreateNode(XmlNodeType.Element, "SquareStickRoundness", null);
                    xmlSquareStickRoundness.InnerText = SquStickInfo[device].lsRoundness.ToString();
                    rootElement.AppendChild(xmlSquareStickRoundness);
                    var xmlSquareRStickRoundness =
                        m_Xdoc.CreateNode(XmlNodeType.Element, "SquareRStickRoundness", null);
                    xmlSquareRStickRoundness.InnerText = SquStickInfo[device].rsRoundness.ToString();
                    rootElement.AppendChild(xmlSquareRStickRoundness);

                    var xmlLsAntiSnapbackEnabled = m_Xdoc.CreateNode(XmlNodeType.Element, "LSAntiSnapback", null);
                    xmlLsAntiSnapbackEnabled.InnerText = LSAntiSnapbackInfo[device].enabled.ToString();
                    rootElement.AppendChild(xmlLsAntiSnapbackEnabled);
                    var xmlRsAntiSnapbackEnabled = m_Xdoc.CreateNode(XmlNodeType.Element, "RSAntiSnapback", null);
                    xmlRsAntiSnapbackEnabled.InnerText = RSAntiSnapbackInfo[device].enabled.ToString();
                    rootElement.AppendChild(xmlRsAntiSnapbackEnabled);

                    var xmlLsAntiSnapbackDelta = m_Xdoc.CreateNode(XmlNodeType.Element, "LSAntiSnapbackDelta", null);
                    xmlLsAntiSnapbackDelta.InnerText = LSAntiSnapbackInfo[device].delta.ToString();
                    rootElement.AppendChild(xmlLsAntiSnapbackDelta);
                    var xmlRsAntiSnapbackDelta = m_Xdoc.CreateNode(XmlNodeType.Element, "RSAntiSnapbackDelta", null);
                    xmlRsAntiSnapbackDelta.InnerText = RSAntiSnapbackInfo[device].delta.ToString();
                    rootElement.AppendChild(xmlRsAntiSnapbackDelta);

                    var xmlLsAntiSnapbackTimeout =
                        m_Xdoc.CreateNode(XmlNodeType.Element, "LSAntiSnapbackTimeout", null);
                    xmlLsAntiSnapbackTimeout.InnerText = LSAntiSnapbackInfo[device].timeout.ToString();
                    rootElement.AppendChild(xmlLsAntiSnapbackTimeout);
                    var xmlRsAntiSnapbackTimeout =
                        m_Xdoc.CreateNode(XmlNodeType.Element, "RSAntiSnapbackTimeout", null);
                    xmlRsAntiSnapbackTimeout.InnerText = RSAntiSnapbackInfo[device].timeout.ToString();
                    rootElement.AppendChild(xmlRsAntiSnapbackTimeout);

                    var xmlLsOutputMode = m_Xdoc.CreateNode(XmlNodeType.Element, "LSOutputMode", null);
                    xmlLsOutputMode.InnerText = LSOutputSettings[device].mode.ToString();
                    rootElement.AppendChild(xmlLsOutputMode);
                    var xmlRsOutputMode = m_Xdoc.CreateNode(XmlNodeType.Element, "RSOutputMode", null);
                    xmlRsOutputMode.InnerText = RSOutputSettings[device].mode.ToString();
                    rootElement.AppendChild(xmlRsOutputMode);

                    var xmlLsOutputSettingsElement = m_Xdoc.CreateElement("LSOutputSettings");
                    var xmlLsFlickStickGroupElement = m_Xdoc.CreateElement("FlickStickSettings");
                    xmlLsOutputSettingsElement.AppendChild(xmlLsFlickStickGroupElement);
                    var xmlLsFlickStickRWC = m_Xdoc.CreateNode(XmlNodeType.Element, "RealWorldCalibration", null);
                    xmlLsFlickStickRWC.InnerText = LSOutputSettings[device].outputSettings.flickSettings
                        .realWorldCalibration.ToString();
                    xmlLsFlickStickGroupElement.AppendChild(xmlLsFlickStickRWC);
                    var xmlLsFlickStickThreshold = m_Xdoc.CreateNode(XmlNodeType.Element, "FlickThreshold", null);
                    xmlLsFlickStickThreshold.InnerText = LSOutputSettings[device].outputSettings.flickSettings
                        .flickThreshold.ToString();
                    xmlLsFlickStickGroupElement.AppendChild(xmlLsFlickStickThreshold);
                    var xmlLsFlickStickTime = m_Xdoc.CreateNode(XmlNodeType.Element, "FlickTime", null);
                    xmlLsFlickStickTime.InnerText =
                        LSOutputSettings[device].outputSettings.flickSettings.flickTime.ToString();
                    xmlLsFlickStickGroupElement.AppendChild(xmlLsFlickStickTime);
                    rootElement.AppendChild(xmlLsOutputSettingsElement);

                    var xmlRsOutputSettingsElement = m_Xdoc.CreateElement("RSOutputSettings");
                    var xmlRsFlickStickGroupElement = m_Xdoc.CreateElement("FlickStickSettings");
                    xmlRsOutputSettingsElement.AppendChild(xmlRsFlickStickGroupElement);
                    var xmlRsFlickStickRWC = m_Xdoc.CreateNode(XmlNodeType.Element, "RealWorldCalibration", null);
                    xmlRsFlickStickRWC.InnerText = RSOutputSettings[device].outputSettings.flickSettings
                        .realWorldCalibration.ToString();
                    xmlRsFlickStickGroupElement.AppendChild(xmlRsFlickStickRWC);
                    var xmlRsFlickStickThreshold = m_Xdoc.CreateNode(XmlNodeType.Element, "FlickThreshold", null);
                    xmlRsFlickStickThreshold.InnerText = RSOutputSettings[device].outputSettings.flickSettings
                        .flickThreshold.ToString();
                    xmlRsFlickStickGroupElement.AppendChild(xmlRsFlickStickThreshold);
                    var xmlRsFlickStickTime = m_Xdoc.CreateNode(XmlNodeType.Element, "FlickTime", null);
                    xmlRsFlickStickTime.InnerText =
                        RSOutputSettings[device].outputSettings.flickSettings.flickTime.ToString();
                    xmlRsFlickStickGroupElement.AppendChild(xmlRsFlickStickTime);
                    rootElement.AppendChild(xmlRsOutputSettingsElement);

                    var xmlL2OutputCurveMode = m_Xdoc.CreateNode(XmlNodeType.Element, "L2OutputCurveMode", null);
                    xmlL2OutputCurveMode.InnerText = axisOutputCurveString(getL2OutCurveMode(device));
                    rootElement.AppendChild(xmlL2OutputCurveMode);
                    var xmlL2OutputCurveCustom = m_Xdoc.CreateNode(XmlNodeType.Element, "L2OutputCurveCustom", null);
                    xmlL2OutputCurveCustom.InnerText = L2OutBezierCurveObj[device].ToString();
                    rootElement.AppendChild(xmlL2OutputCurveCustom);

                    var xmlL2TwoStageMode = m_Xdoc.CreateNode(XmlNodeType.Element, "L2TwoStageMode", null);
                    xmlL2TwoStageMode.InnerText = L2OutputSettings[device].twoStageMode.ToString();
                    rootElement.AppendChild(xmlL2TwoStageMode);
                    var xmlR2TwoStageMode = m_Xdoc.CreateNode(XmlNodeType.Element, "R2TwoStageMode", null);
                    xmlR2TwoStageMode.InnerText = R2OutputSettings[device].twoStageMode.ToString();
                    rootElement.AppendChild(xmlR2TwoStageMode);

                    var xmlL2TriggerEffect = m_Xdoc.CreateNode(XmlNodeType.Element, "L2TriggerEffect", null);
                    xmlL2TriggerEffect.InnerText = L2OutputSettings[device].triggerEffect.ToString();
                    rootElement.AppendChild(xmlL2TriggerEffect);
                    var xmlR2TriggerEffect = m_Xdoc.CreateNode(XmlNodeType.Element, "R2TriggerEffect", null);
                    xmlR2TriggerEffect.InnerText = R2OutputSettings[device].triggerEffect.ToString();
                    rootElement.AppendChild(xmlR2TriggerEffect);

                    var xmlR2OutputCurveMode = m_Xdoc.CreateNode(XmlNodeType.Element, "R2OutputCurveMode", null);
                    xmlR2OutputCurveMode.InnerText = axisOutputCurveString(getR2OutCurveMode(device));
                    rootElement.AppendChild(xmlR2OutputCurveMode);
                    var xmlR2OutputCurveCustom = m_Xdoc.CreateNode(XmlNodeType.Element, "R2OutputCurveCustom", null);
                    xmlR2OutputCurveCustom.InnerText = R2OutBezierCurveObj[device].ToString();
                    rootElement.AppendChild(xmlR2OutputCurveCustom);

                    var xmlSXOutputCurveMode = m_Xdoc.CreateNode(XmlNodeType.Element, "SXOutputCurveMode", null);
                    xmlSXOutputCurveMode.InnerText = axisOutputCurveString(getSXOutCurveMode(device));
                    rootElement.AppendChild(xmlSXOutputCurveMode);
                    var xmlSXOutputCurveCustom = m_Xdoc.CreateNode(XmlNodeType.Element, "SXOutputCurveCustom", null);
                    xmlSXOutputCurveCustom.InnerText = SXOutBezierCurveObj[device].ToString();
                    rootElement.AppendChild(xmlSXOutputCurveCustom);

                    var xmlSZOutputCurveMode = m_Xdoc.CreateNode(XmlNodeType.Element, "SZOutputCurveMode", null);
                    xmlSZOutputCurveMode.InnerText = axisOutputCurveString(getSZOutCurveMode(device));
                    rootElement.AppendChild(xmlSZOutputCurveMode);
                    var xmlSZOutputCurveCustom = m_Xdoc.CreateNode(XmlNodeType.Element, "SZOutputCurveCustom", null);
                    xmlSZOutputCurveCustom.InnerText = SZOutBezierCurveObj[device].ToString();
                    rootElement.AppendChild(xmlSZOutputCurveCustom);

                    var xmlTrackBallMode = m_Xdoc.CreateNode(XmlNodeType.Element, "TrackballMode", null);
                    xmlTrackBallMode.InnerText = TrackballMode[device].ToString();
                    rootElement.AppendChild(xmlTrackBallMode);
                    var xmlTrackBallFriction = m_Xdoc.CreateNode(XmlNodeType.Element, "TrackballFriction", null);
                    xmlTrackBallFriction.InnerText = TrackballFriction[device].ToString();
                    rootElement.AppendChild(xmlTrackBallFriction);

                    var xmlTouchRelMouseRotation =
                        m_Xdoc.CreateNode(XmlNodeType.Element, "TouchRelMouseRotation", null);
                    xmlTouchRelMouseRotation.InnerText =
                        Convert.ToInt32(TouchPadRelMouse[device].rotation * 180.0 / Math.PI).ToString();
                    rootElement.AppendChild(xmlTouchRelMouseRotation);
                    var xmlTouchRelMouseMinThreshold =
                        m_Xdoc.CreateNode(XmlNodeType.Element, "TouchRelMouseMinThreshold", null);
                    xmlTouchRelMouseMinThreshold.InnerText = TouchPadRelMouse[device].minThreshold.ToString();
                    rootElement.AppendChild(xmlTouchRelMouseMinThreshold);

                    var xmlTouchAbsMouseGroupEl = m_Xdoc.CreateElement("TouchpadAbsMouseSettings");
                    var xmlTouchAbsMouseMaxZoneX = m_Xdoc.CreateElement("MaxZoneX");
                    xmlTouchAbsMouseMaxZoneX.InnerText = TouchPadAbsMouse[device].maxZoneX.ToString();
                    xmlTouchAbsMouseGroupEl.AppendChild(xmlTouchAbsMouseMaxZoneX);
                    var xmlTouchAbsMouseMaxZoneY = m_Xdoc.CreateElement("MaxZoneY");
                    xmlTouchAbsMouseMaxZoneY.InnerText = TouchPadAbsMouse[device].maxZoneY.ToString();
                    xmlTouchAbsMouseGroupEl.AppendChild(xmlTouchAbsMouseMaxZoneY);
                    var xmlTouchAbsMouseSnapCenter = m_Xdoc.CreateElement("SnapToCenter");
                    xmlTouchAbsMouseSnapCenter.InnerText = TouchPadAbsMouse[device].snapToCenter.ToString();
                    xmlTouchAbsMouseGroupEl.AppendChild(xmlTouchAbsMouseSnapCenter);
                    rootElement.AppendChild(xmlTouchAbsMouseGroupEl);

                    var xmlOutContDevice = m_Xdoc.CreateNode(XmlNodeType.Element, "OutputContDevice", null);
                    xmlOutContDevice.InnerText = OutContDeviceString(OutputDeviceType[device]);
                    rootElement.AppendChild(xmlOutContDevice);

                    var NodeControl = m_Xdoc.CreateNode(XmlNodeType.Element, "Control", null);
                    var Key = m_Xdoc.CreateNode(XmlNodeType.Element, "Key", null);
                    var Macro = m_Xdoc.CreateNode(XmlNodeType.Element, "Macro", null);
                    var KeyType = m_Xdoc.CreateNode(XmlNodeType.Element, "KeyType", null);
                    var Button = m_Xdoc.CreateNode(XmlNodeType.Element, "Button", null);
                    var Extras = m_Xdoc.CreateNode(XmlNodeType.Element, "Extras", null);

                    var NodeShiftControl = m_Xdoc.CreateNode(XmlNodeType.Element, "ShiftControl", null);

                    var ShiftKey = m_Xdoc.CreateNode(XmlNodeType.Element, "Key", null);
                    var ShiftMacro = m_Xdoc.CreateNode(XmlNodeType.Element, "Macro", null);
                    var ShiftKeyType = m_Xdoc.CreateNode(XmlNodeType.Element, "KeyType", null);
                    var ShiftButton = m_Xdoc.CreateNode(XmlNodeType.Element, "Button", null);
                    var ShiftExtras = m_Xdoc.CreateNode(XmlNodeType.Element, "Extras", null);

                    foreach (var dcs in Ds4Settings[device])
                    {
                        if (dcs.ControlActionType != DS4ControlSettings.ActionType.Default)
                        {
                            XmlNode buttonNode;
                            var keyType = string.Empty;

                            if (dcs.ControlActionType == DS4ControlSettings.ActionType.Button &&
                                dcs.ActionData.ActionButton == X360Controls.Unbound)
                                keyType += DS4KeyType.Unbound;

                            if (dcs.KeyType.HasFlag(DS4KeyType.HoldMacro))
                                keyType += DS4KeyType.HoldMacro;
                            else if (dcs.KeyType.HasFlag(DS4KeyType.Macro))
                                keyType += DS4KeyType.Macro;

                            if (dcs.KeyType.HasFlag(DS4KeyType.Toggle))
                                keyType += DS4KeyType.Toggle;
                            if (dcs.KeyType.HasFlag(DS4KeyType.ScanCode))
                                keyType += DS4KeyType.ScanCode;

                            if (keyType != string.Empty)
                            {
                                buttonNode = m_Xdoc.CreateNode(XmlNodeType.Element, dcs.Control.ToString(), null);
                                buttonNode.InnerText = keyType;
                                KeyType.AppendChild(buttonNode);
                            }

                            buttonNode = m_Xdoc.CreateNode(XmlNodeType.Element, dcs.Control.ToString(), null);
                            if (dcs.ControlActionType == DS4ControlSettings.ActionType.Macro)
                            {
                                var ii = dcs.ActionData.ActionMacro;
                                buttonNode.InnerText = string.Join("/", ii);
                                Macro.AppendChild(buttonNode);
                            }
                            else if (dcs.ControlActionType == DS4ControlSettings.ActionType.Key)
                            {
                                buttonNode.InnerText = dcs.ActionData.ActionKey.ToString();
                                Key.AppendChild(buttonNode);
                            }
                            else if (dcs.ControlActionType == DS4ControlSettings.ActionType.Button)
                            {
                                buttonNode.InnerText = GetX360ControlString(dcs.ActionData.ActionButton);
                                Button.AppendChild(buttonNode);
                            }
                        }

                        var hasvalue = false;
                        if (!string.IsNullOrEmpty(dcs.Extras))
                            foreach (var s in dcs.Extras.Split(','))
                                if (s != "0")
                                {
                                    hasvalue = true;
                                    break;
                                }

                        if (hasvalue)
                        {
                            var extraNode = m_Xdoc.CreateNode(XmlNodeType.Element, dcs.Control.ToString(), null);
                            extraNode.InnerText = dcs.Extras;
                            Extras.AppendChild(extraNode);
                        }

                        if (dcs.ShiftActionType != DS4ControlSettings.ActionType.Default && dcs.ShiftTrigger > 0)
                        {
                            XmlElement buttonNode;
                            var keyType = string.Empty;

                            if (dcs.ShiftActionType == DS4ControlSettings.ActionType.Button &&
                                dcs.ShiftAction.ActionButton == X360Controls.Unbound)
                                keyType += DS4KeyType.Unbound;

                            if (dcs.ShiftKeyType.HasFlag(DS4KeyType.HoldMacro))
                                keyType += DS4KeyType.HoldMacro;
                            if (dcs.ShiftKeyType.HasFlag(DS4KeyType.Macro))
                                keyType += DS4KeyType.Macro;
                            if (dcs.ShiftKeyType.HasFlag(DS4KeyType.Toggle))
                                keyType += DS4KeyType.Toggle;
                            if (dcs.ShiftKeyType.HasFlag(DS4KeyType.ScanCode))
                                keyType += DS4KeyType.ScanCode;

                            if (keyType != string.Empty)
                            {
                                buttonNode = m_Xdoc.CreateElement(dcs.Control.ToString());
                                buttonNode.InnerText = keyType;
                                ShiftKeyType.AppendChild(buttonNode);
                            }

                            buttonNode = m_Xdoc.CreateElement(dcs.Control.ToString());
                            buttonNode.SetAttribute("Trigger", dcs.ShiftTrigger.ToString());
                            if (dcs.ShiftActionType == DS4ControlSettings.ActionType.Macro)
                            {
                                var ii = dcs.ShiftAction.ActionMacro;
                                buttonNode.InnerText = string.Join("/", ii);
                                ShiftMacro.AppendChild(buttonNode);
                            }
                            else if (dcs.ShiftActionType == DS4ControlSettings.ActionType.Key)
                            {
                                buttonNode.InnerText = dcs.ShiftAction.ActionKey.ToString();
                                ShiftKey.AppendChild(buttonNode);
                            }
                            else if (dcs.ShiftActionType == DS4ControlSettings.ActionType.Button)
                            {
                                buttonNode.InnerText = dcs.ShiftAction.ActionButton.ToString();
                                ShiftButton.AppendChild(buttonNode);
                            }
                        }

                        hasvalue = false;
                        if (!string.IsNullOrEmpty(dcs.ShiftExtras))
                            foreach (var s in dcs.ShiftExtras.Split(','))
                                if (s != "0")
                                {
                                    hasvalue = true;
                                    break;
                                }

                        if (hasvalue)
                        {
                            var extraNode = m_Xdoc.CreateNode(XmlNodeType.Element, dcs.Control.ToString(), null);
                            extraNode.InnerText = dcs.ShiftExtras;
                            ShiftExtras.AppendChild(extraNode);
                        }
                    }

                    rootElement.AppendChild(NodeControl);
                    if (Button.HasChildNodes)
                        NodeControl.AppendChild(Button);
                    if (Macro.HasChildNodes)
                        NodeControl.AppendChild(Macro);
                    if (Key.HasChildNodes)
                        NodeControl.AppendChild(Key);
                    if (Extras.HasChildNodes)
                        NodeControl.AppendChild(Extras);
                    if (KeyType.HasChildNodes)
                        NodeControl.AppendChild(KeyType);

                    if (NodeControl.HasChildNodes)
                        rootElement.AppendChild(NodeControl);

                    rootElement.AppendChild(NodeShiftControl);
                    if (ShiftButton.HasChildNodes)
                        NodeShiftControl.AppendChild(ShiftButton);
                    if (ShiftMacro.HasChildNodes)
                        NodeShiftControl.AppendChild(ShiftMacro);
                    if (ShiftKey.HasChildNodes)
                        NodeShiftControl.AppendChild(ShiftKey);
                    if (ShiftKeyType.HasChildNodes)
                        NodeShiftControl.AppendChild(ShiftKeyType);
                    if (ShiftExtras.HasChildNodes)
                        NodeShiftControl.AppendChild(ShiftExtras);

                    m_Xdoc.AppendChild(rootElement);
                    m_Xdoc.Save(path);
                }
                catch
                {
                    Saved = false;
                }

                return Saved;
            }

            public DS4Controls GetDs4ControlsByName(string key)
            {
                if (!key.StartsWith("bn"))
                    return (DS4Controls)Enum.Parse(typeof(DS4Controls), key, true);

                switch (key)
                {
                    case "bnShare": return DS4Controls.Share;
                    case "bnL3": return DS4Controls.L3;
                    case "bnR3": return DS4Controls.R3;
                    case "bnOptions": return DS4Controls.Options;
                    case "bnUp": return DS4Controls.DpadUp;
                    case "bnRight": return DS4Controls.DpadRight;
                    case "bnDown": return DS4Controls.DpadDown;
                    case "bnLeft": return DS4Controls.DpadLeft;

                    case "bnL1": return DS4Controls.L1;
                    case "bnR1": return DS4Controls.R1;
                    case "bnTriangle": return DS4Controls.Triangle;
                    case "bnCircle": return DS4Controls.Circle;
                    case "bnCross": return DS4Controls.Cross;
                    case "bnSquare": return DS4Controls.Square;

                    case "bnPS": return DS4Controls.PS;
                    case "bnLSLeft": return DS4Controls.LXNeg;
                    case "bnLSUp": return DS4Controls.LYNeg;
                    case "bnRSLeft": return DS4Controls.RXNeg;
                    case "bnRSUp": return DS4Controls.RYNeg;

                    case "bnLSRight": return DS4Controls.LXPos;
                    case "bnLSDown": return DS4Controls.LYPos;
                    case "bnRSRight": return DS4Controls.RXPos;
                    case "bnRSDown": return DS4Controls.RYPos;
                    case "bnL2": return DS4Controls.L2;
                    case "bnR2": return DS4Controls.R2;

                    case "bnTouchLeft": return DS4Controls.TouchLeft;
                    case "bnTouchMulti": return DS4Controls.TouchMulti;
                    case "bnTouchUpper": return DS4Controls.TouchUpper;
                    case "bnTouchRight": return DS4Controls.TouchRight;
                    case "bnGyroXP": return DS4Controls.GyroXPos;
                    case "bnGyroXN": return DS4Controls.GyroXNeg;
                    case "bnGyroZP": return DS4Controls.GyroZPos;
                    case "bnGyroZN": return DS4Controls.GyroZNeg;

                    case "bnSwipeUp": return DS4Controls.SwipeUp;
                    case "bnSwipeDown": return DS4Controls.SwipeDown;
                    case "bnSwipeLeft": return DS4Controls.SwipeLeft;
                    case "bnSwipeRight": return DS4Controls.SwipeRight;

                    #region OldShiftname

                    case "sbnShare": return DS4Controls.Share;
                    case "sbnL3": return DS4Controls.L3;
                    case "sbnR3": return DS4Controls.R3;
                    case "sbnOptions": return DS4Controls.Options;
                    case "sbnUp": return DS4Controls.DpadUp;
                    case "sbnRight": return DS4Controls.DpadRight;
                    case "sbnDown": return DS4Controls.DpadDown;
                    case "sbnLeft": return DS4Controls.DpadLeft;

                    case "sbnL1": return DS4Controls.L1;
                    case "sbnR1": return DS4Controls.R1;
                    case "sbnTriangle": return DS4Controls.Triangle;
                    case "sbnCircle": return DS4Controls.Circle;
                    case "sbnCross": return DS4Controls.Cross;
                    case "sbnSquare": return DS4Controls.Square;

                    case "sbnPS": return DS4Controls.PS;
                    case "sbnLSLeft": return DS4Controls.LXNeg;
                    case "sbnLSUp": return DS4Controls.LYNeg;
                    case "sbnRSLeft": return DS4Controls.RXNeg;
                    case "sbnRSUp": return DS4Controls.RYNeg;

                    case "sbnLSRight": return DS4Controls.LXPos;
                    case "sbnLSDown": return DS4Controls.LYPos;
                    case "sbnRSRight": return DS4Controls.RXPos;
                    case "sbnRSDown": return DS4Controls.RYPos;
                    case "sbnL2": return DS4Controls.L2;
                    case "sbnR2": return DS4Controls.R2;

                    case "sbnTouchLeft": return DS4Controls.TouchLeft;
                    case "sbnTouchMulti": return DS4Controls.TouchMulti;
                    case "sbnTouchUpper": return DS4Controls.TouchUpper;
                    case "sbnTouchRight": return DS4Controls.TouchRight;
                    case "sbnGsyroXP": return DS4Controls.GyroXPos;
                    case "sbnGyroXN": return DS4Controls.GyroXNeg;
                    case "sbnGyroZP": return DS4Controls.GyroZPos;
                    case "sbnGyroZN": return DS4Controls.GyroZNeg;

                    #endregion

                    case "bnShiftShare": return DS4Controls.Share;
                    case "bnShiftL3": return DS4Controls.L3;
                    case "bnShiftR3": return DS4Controls.R3;
                    case "bnShiftOptions": return DS4Controls.Options;
                    case "bnShiftUp": return DS4Controls.DpadUp;
                    case "bnShiftRight": return DS4Controls.DpadRight;
                    case "bnShiftDown": return DS4Controls.DpadDown;
                    case "bnShiftLeft": return DS4Controls.DpadLeft;

                    case "bnShiftL1": return DS4Controls.L1;
                    case "bnShiftR1": return DS4Controls.R1;
                    case "bnShiftTriangle": return DS4Controls.Triangle;
                    case "bnShiftCircle": return DS4Controls.Circle;
                    case "bnShiftCross": return DS4Controls.Cross;
                    case "bnShiftSquare": return DS4Controls.Square;

                    case "bnShiftPS": return DS4Controls.PS;
                    case "bnShiftLSLeft": return DS4Controls.LXNeg;
                    case "bnShiftLSUp": return DS4Controls.LYNeg;
                    case "bnShiftRSLeft": return DS4Controls.RXNeg;
                    case "bnShiftRSUp": return DS4Controls.RYNeg;

                    case "bnShiftLSRight": return DS4Controls.LXPos;
                    case "bnShiftLSDown": return DS4Controls.LYPos;
                    case "bnShiftRSRight": return DS4Controls.RXPos;
                    case "bnShiftRSDown": return DS4Controls.RYPos;
                    case "bnShiftL2": return DS4Controls.L2;
                    case "bnShiftR2": return DS4Controls.R2;

                    case "bnShiftTouchLeft": return DS4Controls.TouchLeft;
                    case "bnShiftTouchMulti": return DS4Controls.TouchMulti;
                    case "bnShiftTouchUpper": return DS4Controls.TouchUpper;
                    case "bnShiftTouchRight": return DS4Controls.TouchRight;
                    case "bnShiftGyroXP": return DS4Controls.GyroXPos;
                    case "bnShiftGyroXN": return DS4Controls.GyroXNeg;
                    case "bnShiftGyroZP": return DS4Controls.GyroZPos;
                    case "bnShiftGyroZN": return DS4Controls.GyroZNeg;

                    case "bnShiftSwipeUp": return DS4Controls.SwipeUp;
                    case "bnShiftSwipeDown": return DS4Controls.SwipeDown;
                    case "bnShiftSwipeLeft": return DS4Controls.SwipeLeft;
                    case "bnShiftSwipeRight": return DS4Controls.SwipeRight;
                }

                return 0;
            }

            public X360Controls GetX360ControlsByName(string key)
            {
                X360Controls x3c;
                if (Enum.TryParse(key, true, out x3c))
                    return x3c;

                switch (key)
                {
                    case "Back": return X360Controls.Back;
                    case "Left Stick": return X360Controls.LS;
                    case "Right Stick": return X360Controls.RS;
                    case "Start": return X360Controls.Start;
                    case "Up Button": return X360Controls.DpadUp;
                    case "Right Button": return X360Controls.DpadRight;
                    case "Down Button": return X360Controls.DpadDown;
                    case "Left Button": return X360Controls.DpadLeft;

                    case "Left Bumper": return X360Controls.LB;
                    case "Right Bumper": return X360Controls.RB;
                    case "Y Button": return X360Controls.Y;
                    case "B Button": return X360Controls.B;
                    case "A Button": return X360Controls.A;
                    case "X Button": return X360Controls.X;

                    case "Guide": return X360Controls.Guide;
                    case "Left X-Axis-": return X360Controls.LXNeg;
                    case "Left Y-Axis-": return X360Controls.LYNeg;
                    case "Right X-Axis-": return X360Controls.RXNeg;
                    case "Right Y-Axis-": return X360Controls.RYNeg;

                    case "Left X-Axis+": return X360Controls.LXPos;
                    case "Left Y-Axis+": return X360Controls.LYPos;
                    case "Right X-Axis+": return X360Controls.RXPos;
                    case "Right Y-Axis+": return X360Controls.RYPos;
                    case "Left Trigger": return X360Controls.LT;
                    case "Right Trigger": return X360Controls.RT;
                    case "Touchpad Click": return X360Controls.TouchpadClick;

                    case "Left Mouse Button": return X360Controls.LeftMouse;
                    case "Right Mouse Button": return X360Controls.RightMouse;
                    case "Middle Mouse Button": return X360Controls.MiddleMouse;
                    case "4th Mouse Button": return X360Controls.FourthMouse;
                    case "5th Mouse Button": return X360Controls.FifthMouse;
                    case "Mouse Wheel Up": return X360Controls.WUP;
                    case "Mouse Wheel Down": return X360Controls.WDOWN;
                    case "Mouse Up": return X360Controls.MouseUp;
                    case "Mouse Down": return X360Controls.MouseDown;
                    case "Mouse Left": return X360Controls.MouseLeft;
                    case "Mouse Right": return X360Controls.MouseRight;
                    case "Unbound": return X360Controls.Unbound;
                }

                return X360Controls.Unbound;
            }

            public string GetX360ControlString(X360Controls key)
            {
                switch (key)
                {
                    case X360Controls.Back: return "Back";
                    case X360Controls.LS: return "Left Stick";
                    case X360Controls.RS: return "Right Stick";
                    case X360Controls.Start: return "Start";
                    case X360Controls.DpadUp: return "Up Button";
                    case X360Controls.DpadRight: return "Right Button";
                    case X360Controls.DpadDown: return "Down Button";
                    case X360Controls.DpadLeft: return "Left Button";

                    case X360Controls.LB: return "Left Bumper";
                    case X360Controls.RB: return "Right Bumper";
                    case X360Controls.Y: return "Y Button";
                    case X360Controls.B: return "B Button";
                    case X360Controls.A: return "A Button";
                    case X360Controls.X: return "X Button";

                    case X360Controls.Guide: return "Guide";
                    case X360Controls.LXNeg: return "Left X-Axis-";
                    case X360Controls.LYNeg: return "Left Y-Axis-";
                    case X360Controls.RXNeg: return "Right X-Axis-";
                    case X360Controls.RYNeg: return "Right Y-Axis-";

                    case X360Controls.LXPos: return "Left X-Axis+";
                    case X360Controls.LYPos: return "Left Y-Axis+";
                    case X360Controls.RXPos: return "Right X-Axis+";
                    case X360Controls.RYPos: return "Right Y-Axis+";
                    case X360Controls.LT: return "Left Trigger";
                    case X360Controls.RT: return "Right Trigger";
                    case X360Controls.TouchpadClick: return "Touchpad Click";

                    case X360Controls.LeftMouse: return "Left Mouse Button";
                    case X360Controls.RightMouse: return "Right Mouse Button";
                    case X360Controls.MiddleMouse: return "Middle Mouse Button";
                    case X360Controls.FourthMouse: return "4th Mouse Button";
                    case X360Controls.FifthMouse: return "5th Mouse Button";
                    case X360Controls.WUP: return "Mouse Wheel Up";
                    case X360Controls.WDOWN: return "Mouse Wheel Down";
                    case X360Controls.MouseUp: return "Mouse Up";
                    case X360Controls.MouseDown: return "Mouse Down";
                    case X360Controls.MouseLeft: return "Mouse Left";
                    case X360Controls.MouseRight: return "Mouse Right";
                    case X360Controls.Unbound: return "Unbound";
                }

                return "Unbound";
            }

            public bool LoadProfile(int device, bool launchprogram, ControlService control,
                string propath = "", bool xinputChange = true, bool postLoad = true)
            {
                var Loaded = true;
                var customMapKeyTypes = new Dictionary<DS4Controls, DS4KeyType>();
                var customMapKeys = new Dictionary<DS4Controls, ushort>();
                var customMapButtons = new Dictionary<DS4Controls, X360Controls>();
                var customMapMacros = new Dictionary<DS4Controls, string>();
                var customMapExtras = new Dictionary<DS4Controls, string>();
                var shiftCustomMapKeyTypes = new Dictionary<DS4Controls, DS4KeyType>();
                var shiftCustomMapKeys = new Dictionary<DS4Controls, ushort>();
                var shiftCustomMapButtons = new Dictionary<DS4Controls, X360Controls>();
                var shiftCustomMapMacros = new Dictionary<DS4Controls, string>();
                var shiftCustomMapExtras = new Dictionary<DS4Controls, string>();
                var rootname = "DS4Windows";
                var missingSetting = false;
                var migratePerformed = false;
                string profilepath;
                if (string.IsNullOrEmpty(propath))
                    profilepath = Path.Combine(RuntimeAppDataPath, Constants.ProfilesSubDirectory,
                        $"{ProfilePath[device]}{XML_EXTENSION}");
                else
                    profilepath = propath;

                var xinputPlug = false;
                var xinputStatus = false;

                if (File.Exists(profilepath))
                {
                    XmlNode Item;

                    var tmpMigration = new ProfileMigration(profilepath);
                    if (tmpMigration.RequiresMigration())
                    {
                        tmpMigration.Migrate();
                        m_Xdoc.Load(tmpMigration.ProfileReader);
                        migratePerformed = true;
                    }
                    else if (tmpMigration.ProfileReader != null)
                    {
                        m_Xdoc.Load(tmpMigration.ProfileReader);
                        //m_Xdoc.Load(profilepath);
                    }
                    else
                    {
                        Loaded = false;
                    }

                    if (m_Xdoc.SelectSingleNode(rootname) == null)
                    {
                        rootname = "DS4Windows";
                        missingSetting = true;
                    }

                    if (device < MAX_DS4_CONTROLLER_COUNT)
                    {
                        DS4LightBar.forcelight[device] = false;
                        DS4LightBar.forcedFlash[device] = 0;
                    }

                    var oldContType = ActiveOutDevType[device];
                    var lightbarSettings = LightbarSettingInfo[device];
                    var lightInfo = lightbarSettings.ds4winSettings;

                    // Make sure to reset currently set profile values before parsing
                    ResetProfile(device);
                    ResetMouseProperties(device, control);

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/touchToggle");
                        bool.TryParse(Item.InnerText, out var value);
                        EnableTouchToggle[device] = value;
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/idleDisconnectTimeout");
                        if (int.TryParse(Item?.InnerText ?? "", out var temp)) IdleDisconnectTimeout[device] = temp;
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/outputDataToDS4");
                        bool.TryParse(Item.InnerText, out var value);
                        EnableOutputDataToDS4[device] = value;
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LightbarMode");
                        var tempMode = Item.InnerText;
                        lightbarSettings.mode = GetLightbarModeType(tempMode);
                    }
                    catch
                    {
                        lightbarSettings.mode = LightbarMode.DS4Win;
                        missingSetting = true;
                    }

                    //New method for saving color
                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/Color");
                        string[] colors;
                        if (!string.IsNullOrEmpty(Item.InnerText))
                            colors = Item.InnerText.Split(',');
                        else
                            colors = new string[0];

                        lightInfo.m_Led.red = byte.Parse(colors[0]);
                        lightInfo.m_Led.green = byte.Parse(colors[1]);
                        lightInfo.m_Led.blue = byte.Parse(colors[2]);
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    if (m_Xdoc.SelectSingleNode("/" + rootname + "/Color") == null)
                    {
                        //Old method of color saving
                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/" + rootname + "/Red");
                            byte.TryParse(Item.InnerText, out lightInfo.m_Led.red);
                        }
                        catch
                        {
                            missingSetting = true;
                        }

                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/" + rootname + "/Green");
                            byte.TryParse(Item.InnerText, out lightInfo.m_Led.green);
                        }
                        catch
                        {
                            missingSetting = true;
                        }

                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/" + rootname + "/Blue");
                            byte.TryParse(Item.InnerText, out lightInfo.m_Led.blue);
                        }
                        catch
                        {
                            missingSetting = true;
                        }
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RumbleBoost");
                        byte.TryParse(Item.InnerText, out var value);
                        RumbleBoost[device] = value;
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RumbleAutostopTime");
                        int.TryParse(Item.InnerText, out var value);
                        RumbleAutostopTime[device] = value;
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ledAsBatteryIndicator");
                        bool.TryParse(Item.InnerText, out lightInfo.ledAsBattery);
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/FlashType");
                        byte.TryParse(Item.InnerText, out lightInfo.flashType);
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/flashBatteryAt");
                        int.TryParse(Item.InnerText, out lightInfo.flashAt);
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/touchSensitivity");
                        byte.TryParse(Item.InnerText, out var value);
                        TouchSensitivity[device] = value;
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    //New method for saving color
                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LowColor");
                        string[] colors;
                        if (!string.IsNullOrEmpty(Item.InnerText))
                            colors = Item.InnerText.Split(',');
                        else
                            colors = new string[0];

                        lightInfo.m_LowLed.red = byte.Parse(colors[0]);
                        lightInfo.m_LowLed.green = byte.Parse(colors[1]);
                        lightInfo.m_LowLed.blue = byte.Parse(colors[2]);
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    if (m_Xdoc.SelectSingleNode("/" + rootname + "/LowColor") == null)
                    {
                        //Old method of color saving
                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LowRed");
                            byte.TryParse(Item.InnerText, out lightInfo.m_LowLed.red);
                        }
                        catch
                        {
                            missingSetting = true;
                        }

                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LowGreen");
                            byte.TryParse(Item.InnerText, out lightInfo.m_LowLed.green);
                        }
                        catch
                        {
                            missingSetting = true;
                        }

                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LowBlue");
                            byte.TryParse(Item.InnerText, out lightInfo.m_LowLed.blue);
                        }
                        catch
                        {
                            missingSetting = true;
                        }
                    }

                    //New method for saving color
                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ChargingColor");
                        string[] colors;
                        if (!string.IsNullOrEmpty(Item.InnerText))
                            colors = Item.InnerText.Split(',');
                        else
                            colors = new string[0];

                        lightInfo.m_ChargingLed.red = byte.Parse(colors[0]);
                        lightInfo.m_ChargingLed.green = byte.Parse(colors[1]);
                        lightInfo.m_ChargingLed.blue = byte.Parse(colors[2]);
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    if (m_Xdoc.SelectSingleNode("/" + rootname + "/ChargingColor") == null)
                    {
                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ChargingRed");
                            byte.TryParse(Item.InnerText, out lightInfo.m_ChargingLed.red);
                        }
                        catch
                        {
                            missingSetting = true;
                        }

                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ChargingGreen");
                            byte.TryParse(Item.InnerText, out lightInfo.m_ChargingLed.green);
                        }
                        catch
                        {
                            missingSetting = true;
                        }

                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ChargingBlue");
                            byte.TryParse(Item.InnerText, out lightInfo.m_ChargingLed.blue);
                        }
                        catch
                        {
                            missingSetting = true;
                        }
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/FlashColor");
                        string[] colors;
                        if (!string.IsNullOrEmpty(Item.InnerText))
                            colors = Item.InnerText.Split(',');
                        else
                            colors = new string[0];
                        lightInfo.m_FlashLed.red = byte.Parse(colors[0]);
                        lightInfo.m_FlashLed.green = byte.Parse(colors[1]);
                        lightInfo.m_FlashLed.blue = byte.Parse(colors[2]);
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/touchpadJitterCompensation");
                        bool.TryParse(Item.InnerText, out var value);
                        TouchpadJitterCompensation[device] = value;
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/lowerRCOn");
                        bool.TryParse(Item.InnerText, out var value);
                        LowerRCOn[device] = value;
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/tapSensitivity");
                        byte.TryParse(Item.InnerText, out var value);
                        TapSensitivity[device] = value;
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/doubleTap");
                        bool.TryParse(Item.InnerText, out var value);
                        DoubleTap[device] = value;
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/scrollSensitivity");
                        int.TryParse(Item.InnerText, out var value);
                        ScrollSensitivity[device] = value;
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchpadInvert");
                        var temp = 0;
                        int.TryParse(Item.InnerText, out temp);
                        TouchPadInvert[device] = Math.Min(Math.Max(temp, 0), 3);
                    }
                    catch
                    {
                        TouchPadInvert[device] = 0;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchpadClickPassthru");
                        bool.TryParse(Item.InnerText, out var value);
                        TouchClickPassthru[device] = value;
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LeftTriggerMiddle");
                        byte.TryParse(Item.InnerText, out L2ModInfo[device].deadZone);
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RightTriggerMiddle");
                        byte.TryParse(Item.InnerText, out R2ModInfo[device].deadZone);
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2AntiDeadZone");
                        int.TryParse(Item.InnerText, out L2ModInfo[device].antiDeadZone);
                    }
                    catch
                    {
                        L2ModInfo[device].antiDeadZone = 0;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2AntiDeadZone");
                        int.TryParse(Item.InnerText, out R2ModInfo[device].antiDeadZone);
                    }
                    catch
                    {
                        R2ModInfo[device].antiDeadZone = 0;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2MaxZone");
                        var temp = 100;
                        int.TryParse(Item.InnerText, out temp);
                        L2ModInfo[device].maxZone = Math.Min(Math.Max(temp, 0), 100);
                    }
                    catch
                    {
                        L2ModInfo[device].maxZone = 100;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2MaxZone");
                        var temp = 100;
                        int.TryParse(Item.InnerText, out temp);
                        R2ModInfo[device].maxZone = Math.Min(Math.Max(temp, 0), 100);
                    }
                    catch
                    {
                        R2ModInfo[device].maxZone = 100;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2MaxOutput");
                        var temp = 100.0;
                        temp = double.Parse(Item.InnerText);
                        L2ModInfo[device].maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                    }
                    catch
                    {
                        L2ModInfo[device].maxOutput = 100.0;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2MaxOutput");
                        var temp = 100.0;
                        temp = double.Parse(Item.InnerText);
                        R2ModInfo[device].maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                    }
                    catch
                    {
                        R2ModInfo[device].maxOutput = 100.0;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSRotation");
                        var temp = 0;
                        int.TryParse(Item.InnerText, out temp);
                        temp = Math.Min(Math.Max(temp, -180), 180);
                        LSRotation[device] = temp * Math.PI / 180.0;
                    }
                    catch
                    {
                        LSRotation[device] = 0.0;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSRotation");
                        var temp = 0;
                        int.TryParse(Item.InnerText, out temp);
                        temp = Math.Min(Math.Max(temp, -180), 180);
                        RSRotation[device] = temp * Math.PI / 180.0;
                    }
                    catch
                    {
                        RSRotation[device] = 0.0;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSFuzz");
                        var temp = 0;
                        int.TryParse(Item.InnerText, out temp);
                        temp = Math.Min(Math.Max(temp, 0), 100);
                        LSModInfo[device].fuzz = temp;
                    }
                    catch
                    {
                        LSModInfo[device].fuzz = StickDeadZoneInfo.DEFAULT_FUZZ;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSFuzz");
                        var temp = 0;
                        int.TryParse(Item.InnerText, out temp);
                        temp = Math.Min(Math.Max(temp, 0), 100);
                        RSModInfo[device].fuzz = temp;
                    }
                    catch
                    {
                        RSModInfo[device].fuzz = StickDeadZoneInfo.DEFAULT_FUZZ;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ButtonMouseSensitivity");
                        int.TryParse(Item.InnerText, out ButtonMouseInfos[device].buttonSensitivity);
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ButtonMouseOffset");
                        double.TryParse(Item.InnerText, out ButtonMouseInfos[device].mouseVelocityOffset);
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ButtonMouseVerticalScale");
                        int.TryParse(Item.InnerText, out var temp);
                        ButtonMouseInfos[device].buttonVerticalScale = Math.Min(Math.Max(temp, 0), 500) * 0.01;
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/Rainbow");
                        double.TryParse(Item.InnerText, out lightInfo.rainbow);
                    }
                    catch
                    {
                        lightInfo.rainbow = 0;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/MaxSatRainbow");
                        int.TryParse(Item.InnerText, out var temp);
                        lightInfo.maxRainbowSat = Math.Max(0, Math.Min(100, temp)) / 100.0;
                    }
                    catch
                    {
                        lightInfo.maxRainbowSat = 1.0;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSDeadZone");
                        int.TryParse(Item.InnerText, out var temp);
                        temp = Math.Min(Math.Max(temp, 0), 127);
                        LSModInfo[device].deadZone = temp;
                    }
                    catch
                    {
                        LSModInfo[device].deadZone = 10;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSDeadZone");
                        int.TryParse(Item.InnerText, out var temp);
                        temp = Math.Min(Math.Max(temp, 0), 127);
                        RSModInfo[device].deadZone = temp;
                    }
                    catch
                    {
                        RSModInfo[device].deadZone = 10;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSAntiDeadZone");
                        int.TryParse(Item.InnerText, out LSModInfo[device].antiDeadZone);
                    }
                    catch
                    {
                        LSModInfo[device].antiDeadZone = 20;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSAntiDeadZone");
                        int.TryParse(Item.InnerText, out RSModInfo[device].antiDeadZone);
                    }
                    catch
                    {
                        RSModInfo[device].antiDeadZone = 20;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSMaxZone");
                        var temp = 100;
                        int.TryParse(Item.InnerText, out temp);
                        LSModInfo[device].maxZone = Math.Min(Math.Max(temp, 0), 100);
                    }
                    catch
                    {
                        LSModInfo[device].maxZone = 100;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSMaxZone");
                        var temp = 100;
                        int.TryParse(Item.InnerText, out temp);
                        RSModInfo[device].maxZone = Math.Min(Math.Max(temp, 0), 100);
                    }
                    catch
                    {
                        RSModInfo[device].maxZone = 100;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSVerticalScale");
                        if (double.TryParse(Item?.InnerText ?? "", out var temp))
                            LSModInfo[device].verticalScale = Math.Min(Math.Max(temp, 0.0), 200.0);
                    }
                    catch
                    {
                        LSModInfo[device].verticalScale = StickDeadZoneInfo.DEFAULT_VERTICAL_SCALE;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSMaxOutput");
                        var temp = 100.0;
                        temp = double.Parse(Item.InnerText);
                        LSModInfo[device].maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                    }
                    catch
                    {
                        LSModInfo[device].maxOutput = 100.0;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSMaxOutputForce");
                        if (bool.TryParse(Item?.InnerText ?? "", out var temp)) LSModInfo[device].maxOutputForce = temp;
                    }
                    catch
                    {
                        LSModInfo[device].maxOutputForce = StickDeadZoneInfo.DEFAULT_MAXOUTPUT_FORCE;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSMaxOutput");
                        var temp = 100.0;
                        temp = double.Parse(Item.InnerText);
                        RSModInfo[device].maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                    }
                    catch
                    {
                        RSModInfo[device].maxOutput = 100;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSMaxOutputForce");
                        if (bool.TryParse(Item?.InnerText ?? "", out var temp)) RSModInfo[device].maxOutputForce = temp;
                    }
                    catch
                    {
                        RSModInfo[device].maxOutputForce = StickDeadZoneInfo.DEFAULT_MAXOUTPUT_FORCE;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOuterBindDead");
                        if (int.TryParse(Item?.InnerText ?? "", out var temp))
                        {
                            temp = Math.Min(Math.Max(temp, 0), 100);
                            LSModInfo[device].outerBindDeadZone = temp;
                        }
                    }
                    catch
                    {
                        LSModInfo[device].outerBindDeadZone = StickDeadZoneInfo.DEFAULT_OUTER_BIND_DEAD;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOuterBindDead");
                        if (int.TryParse(Item?.InnerText ?? "", out var temp))
                        {
                            temp = Math.Min(Math.Max(temp, 0), 100);
                            RSModInfo[device].outerBindDeadZone = temp;
                        }
                    }
                    catch
                    {
                        RSModInfo[device].outerBindDeadZone = StickDeadZoneInfo.DEFAULT_OUTER_BIND_DEAD;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOuterBindInvert");
                        if (bool.TryParse(Item?.InnerText ?? "", out var temp))
                            LSModInfo[device].outerBindInvert = temp;
                    }
                    catch
                    {
                        LSModInfo[device].outerBindInvert = StickDeadZoneInfo.DEFAULT_OUTER_BIND_INVERT;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOuterBindInvert");
                        if (bool.TryParse(Item?.InnerText ?? "", out var temp))
                            RSModInfo[device].outerBindInvert = temp;
                    }
                    catch
                    {
                        RSModInfo[device].outerBindInvert = StickDeadZoneInfo.DEFAULT_OUTER_BIND_INVERT;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSVerticalScale");
                        if (double.TryParse(Item?.InnerText ?? "", out var temp))
                            RSModInfo[device].verticalScale = Math.Min(Math.Max(temp, 0.0), 200.0);
                    }
                    catch
                    {
                        RSModInfo[device].verticalScale = StickDeadZoneInfo.DEFAULT_VERTICAL_SCALE;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSDeadZoneType");
                        if (Enum.TryParse(Item?.InnerText ?? "", out StickDeadZoneInfo.DeadZoneType temp))
                            LSModInfo[device].deadzoneType = temp;
                    }
                    catch
                    {
                    }

                    var lsAxialDeadGroup = false;
                    var lsAxialDeadElement =
                        m_Xdoc.SelectSingleNode("/" + rootname + "/LSAxialDeadOptions");
                    lsAxialDeadGroup = lsAxialDeadElement != null;

                    if (lsAxialDeadGroup)
                    {
                        try
                        {
                            Item = lsAxialDeadElement.SelectSingleNode("DeadZoneX");
                            int.TryParse(Item.InnerText, out var temp);
                            temp = Math.Min(Math.Max(temp, 0), 127);
                            LSModInfo[device].xAxisDeadInfo.deadZone = temp;
                        }
                        catch
                        {
                        }

                        try
                        {
                            Item = lsAxialDeadElement.SelectSingleNode("DeadZoneY");
                            int.TryParse(Item.InnerText, out var temp);
                            temp = Math.Min(Math.Max(temp, 0), 127);
                            LSModInfo[device].yAxisDeadInfo.deadZone = temp;
                        }
                        catch
                        {
                        }

                        try
                        {
                            Item = lsAxialDeadElement.SelectSingleNode("MaxZoneX");
                            int.TryParse(Item.InnerText, out var temp);
                            LSModInfo[device].xAxisDeadInfo.maxZone = Math.Min(Math.Max(temp, 0), 100);
                        }
                        catch
                        {
                        }

                        try
                        {
                            Item = lsAxialDeadElement.SelectSingleNode("MaxZoneY");
                            int.TryParse(Item.InnerText, out var temp);
                            LSModInfo[device].yAxisDeadInfo.maxZone = Math.Min(Math.Max(temp, 0), 100);
                        }
                        catch
                        {
                        }

                        try
                        {
                            Item = lsAxialDeadElement.SelectSingleNode("AntiDeadZoneX");
                            int.TryParse(Item.InnerText, out var temp);
                            LSModInfo[device].xAxisDeadInfo.antiDeadZone = Math.Min(Math.Max(temp, 0), 100);
                        }
                        catch
                        {
                        }

                        try
                        {
                            Item = lsAxialDeadElement.SelectSingleNode("AntiDeadZoneY");
                            int.TryParse(Item.InnerText, out var temp);
                            LSModInfo[device].yAxisDeadInfo.antiDeadZone = Math.Min(Math.Max(temp, 0), 100);
                        }
                        catch
                        {
                        }

                        try
                        {
                            Item = lsAxialDeadElement.SelectSingleNode("MaxOutputX");
                            double.TryParse(Item.InnerText, out var temp);
                            LSModInfo[device].xAxisDeadInfo.maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                        }
                        catch
                        {
                        }

                        try
                        {
                            Item = lsAxialDeadElement.SelectSingleNode("MaxOutputY");
                            double.TryParse(Item.InnerText, out var temp);
                            LSModInfo[device].yAxisDeadInfo.maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                        }
                        catch
                        {
                        }
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSDeadZoneType");
                        if (Enum.TryParse(Item?.InnerText ?? "", out StickDeadZoneInfo.DeadZoneType temp))
                            RSModInfo[device].deadzoneType = temp;
                    }
                    catch
                    {
                    }

                    var rsAxialDeadGroup = false;
                    var rsAxialDeadElement =
                        m_Xdoc.SelectSingleNode("/" + rootname + "/RSAxialDeadOptions");
                    rsAxialDeadGroup = rsAxialDeadElement != null;

                    if (rsAxialDeadGroup)
                    {
                        try
                        {
                            Item = rsAxialDeadElement.SelectSingleNode("DeadZoneX");
                            int.TryParse(Item.InnerText, out var temp);
                            temp = Math.Min(Math.Max(temp, 0), 127);
                            RSModInfo[device].xAxisDeadInfo.deadZone = temp;
                        }
                        catch
                        {
                        }

                        try
                        {
                            Item = rsAxialDeadElement.SelectSingleNode("DeadZoneY");
                            int.TryParse(Item.InnerText, out var temp);
                            temp = Math.Min(Math.Max(temp, 0), 127);
                            RSModInfo[device].yAxisDeadInfo.deadZone = temp;
                        }
                        catch
                        {
                        }

                        try
                        {
                            Item = rsAxialDeadElement.SelectSingleNode("MaxZoneX");
                            int.TryParse(Item.InnerText, out var temp);
                            RSModInfo[device].xAxisDeadInfo.maxZone = Math.Min(Math.Max(temp, 0), 100);
                        }
                        catch
                        {
                        }

                        try
                        {
                            Item = rsAxialDeadElement.SelectSingleNode("MaxZoneY");
                            int.TryParse(Item.InnerText, out var temp);
                            RSModInfo[device].yAxisDeadInfo.maxZone = Math.Min(Math.Max(temp, 0), 100);
                        }
                        catch
                        {
                        }

                        try
                        {
                            Item = rsAxialDeadElement.SelectSingleNode("AntiDeadZoneX");
                            int.TryParse(Item.InnerText, out var temp);
                            RSModInfo[device].xAxisDeadInfo.antiDeadZone = Math.Min(Math.Max(temp, 0), 100);
                        }
                        catch
                        {
                        }

                        try
                        {
                            Item = rsAxialDeadElement.SelectSingleNode("AntiDeadZoneY");
                            int.TryParse(Item.InnerText, out var temp);
                            RSModInfo[device].yAxisDeadInfo.antiDeadZone = Math.Min(Math.Max(temp, 0), 100);
                        }
                        catch
                        {
                        }

                        try
                        {
                            Item = rsAxialDeadElement.SelectSingleNode("MaxOutputX");
                            double.TryParse(Item.InnerText, out var temp);
                            RSModInfo[device].xAxisDeadInfo.maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                        }
                        catch
                        {
                        }

                        try
                        {
                            Item = rsAxialDeadElement.SelectSingleNode("MaxOutputY");
                            double.TryParse(Item.InnerText, out var temp);
                            RSModInfo[device].yAxisDeadInfo.maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                        }
                        catch
                        {
                        }
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SXDeadZone");
                        double.TryParse(Item.InnerText, out var value);
                        SXDeadzone[device] = value;
                    }
                    catch
                    {
                        SXDeadzone[device] = 0.02;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZDeadZone");
                        double.TryParse(Item.InnerText, out var value);
                        SZDeadzone[device] = value;
                    }
                    catch
                    {
                        SZDeadzone[device] = 0.02;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SXMaxZone");
                        var temp = 0;
                        int.TryParse(Item.InnerText, out temp);
                        SXMaxzone[device] = Math.Min(Math.Max(temp * 0.01, 0.0), 1.0);
                    }
                    catch
                    {
                        SXMaxzone[device] = 1.0;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZMaxZone");
                        var temp = 0;
                        int.TryParse(Item.InnerText, out temp);
                        SZMaxzone[device] = Math.Min(Math.Max(temp * 0.01, 0.0), 1.0);
                    }
                    catch
                    {
                        SZMaxzone[device] = 1.0;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SXAntiDeadZone");
                        var temp = 0;
                        int.TryParse(Item.InnerText, out temp);
                        SXAntiDeadzone[device] = Math.Min(Math.Max(temp * 0.01, 0.0), 1.0);
                    }
                    catch
                    {
                        SXAntiDeadzone[device] = 0.0;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZAntiDeadZone");
                        var temp = 0;
                        int.TryParse(Item.InnerText, out temp);
                        SZAntiDeadzone[device] = Math.Min(Math.Max(temp * 0.01, 0.0), 1.0);
                    }
                    catch
                    {
                        SZAntiDeadzone[device] = 0.0;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/Sensitivity");
                        var s = Item.InnerText.Split('|');
                        if (s.Length == 1)
                            s = Item.InnerText.Split(',');
                        if (!double.TryParse(s[0], out var v1) || v1 < .5f)
                            LSSens[device] = 1;
                        LSSens[device] = v1;
                        if (!double.TryParse(s[1], out var v2) || v2 < .5f)
                            RSSens[device] = 1;
                        RSSens[device] = v2;
                        if (!double.TryParse(s[2], out var v3) || v3 < .1f)
                            L2Sens[device] = 1;
                        L2Sens[device] = v3;
                        if (!double.TryParse(s[3], out var v4) || v4 < .1f)
                            R2Sens[device] = 1;
                        R2Sens[device] = v4;
                        if (!double.TryParse(s[4], out var v5) || v5 < .5f)
                            SXSens[device] = 1;
                        SXSens[device] = v5;
                        if (!double.TryParse(s[5], out var v6) || v6 < .5f)
                            SZSens[device] = 1;
                        SZSens[device] = v6;
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ChargingType");
                        int.TryParse(Item.InnerText, out lightInfo.chargingType);
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/MouseAcceleration");
                        bool.TryParse(Item.InnerText, out ButtonMouseInfos[device].mouseAccel);
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    var shiftM = 0;
                    if (m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftModifier") != null)
                        int.TryParse(m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftModifier").InnerText, out shiftM);

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LaunchProgram");
                        LaunchProgram[device] = Item.InnerText;
                    }
                    catch
                    {
                        LaunchProgram[device] = string.Empty;
                        missingSetting = true;
                    }

                    if (launchprogram && LaunchProgram[device] != string.Empty)
                    {
                        var programPath = LaunchProgram[device];
                        var localAll = Process.GetProcesses();
                        var procFound = false;
                        for (int procInd = 0, procsLen = localAll.Length; !procFound && procInd < procsLen; procInd++)
                            try
                            {
                                var temp = localAll[procInd].MainModule.FileName;
                                if (temp == programPath) procFound = true;
                            }
                            // Ignore any process for which this information
                            // is not exposed
                            catch
                            {
                            }

                        if (!procFound)
                        {
                            var processTask = new Task(() =>
                            {
                                Thread.Sleep(5000);
                                var tempProcess = new Process();
                                tempProcess.StartInfo.FileName = programPath;
                                tempProcess.StartInfo.WorkingDirectory = new FileInfo(programPath).Directory.ToString();
                                //tempProcess.StartInfo.UseShellExecute = false;
                                try
                                {
                                    tempProcess.Start();
                                }
                                catch
                                {
                                }
                            });

                            processTask.Start();
                        }
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/DinputOnly");
                        bool.TryParse(Item.InnerText, out var value);
                        DirectInputOnly[device] = value;
                    }
                    catch
                    {
                        DirectInputOnly[device] = false;
                        missingSetting = true;
                    }

                    var oldUseDInputOnly = UseDirectInputOnly[device];

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/StartTouchpadOff");
                        bool.TryParse(Item.InnerText, out var value);
                        StartTouchpadOff[device] = value;
                        if (StartTouchpadOff[device]) control.StartTPOff(device);
                    }
                    catch
                    {
                        StartTouchpadOff[device] = false;
                        missingSetting = true;
                    }

                    // Fallback lookup if TouchpadOutMode is not set
                    var tpForControlsPresent = false;
                    var xmlUseTPForControlsElement =
                        m_Xdoc.SelectSingleNode("/" + rootname + "/UseTPforControls");
                    tpForControlsPresent = xmlUseTPForControlsElement != null;
                    if (tpForControlsPresent)
                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/" + rootname + "/UseTPforControls");
                            if (bool.TryParse(Item?.InnerText ?? "", out var temp))
                                if (temp)
                                    TouchOutMode[device] = TouchpadOutMode.Controls;
                        }
                        catch
                        {
                            TouchOutMode[device] = TouchpadOutMode.Mouse;
                        }

                    // Fallback lookup if GyroOutMode is not set
                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/UseSAforMouse");
                        if (bool.TryParse(Item?.InnerText ?? "", out var temp))
                            if (temp)
                                GyroOutputMode[device] = DS4Windows.GyroOutMode.Mouse;
                    }
                    catch
                    {
                        GyroOutputMode[device] = DS4Windows.GyroOutMode.Controls;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SATriggers");
                        SATriggers[device] = Item.InnerText;
                    }
                    catch
                    {
                        SATriggers[device] = "-1";
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SATriggerCond");
                        SATriggerCond[device] = SaTriggerCondValue(Item.InnerText);
                    }
                    catch
                    {
                        SATriggerCond[device] = true;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelEmulationAxis");
                        Enum.TryParse(Item.InnerText, out SASteeringWheelEmulationAxisType value);
                        SASteeringWheelEmulationAxis[device] = value;
                    }
                    catch
                    {
                        SASteeringWheelEmulationAxis[device] = SASteeringWheelEmulationAxisType.None;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelEmulationRange");
                        int.TryParse(Item.InnerText, out var value);
                        SASteeringWheelEmulationRange[device] = value;
                    }
                    catch
                    {
                        SASteeringWheelEmulationRange[device] = 360;
                        missingSetting = true;
                    }

                    var sASteeringWheelSmoothingGroup = false;
                    var xmlSASteeringWheelSmoothElement =
                        m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelSmoothingOptions");
                    sASteeringWheelSmoothingGroup = xmlSASteeringWheelSmoothElement != null;

                    if (sASteeringWheelSmoothingGroup)
                    {
                        try
                        {
                            Item = xmlSASteeringWheelSmoothElement.SelectSingleNode("SASteeringWheelUseSmoothing");
                            //Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelUseSmoothing");
                            bool.TryParse(Item.InnerText, out var temp);
                            WheelSmoothInfo[device].Enabled = temp;
                        }
                        catch
                        {
                            WheelSmoothInfo[device].Reset();
                            missingSetting = true;
                        }

                        try
                        {
                            Item = xmlSASteeringWheelSmoothElement.SelectSingleNode("SASteeringWheelSmoothMinCutoff");
                            //Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelSmoothMinCutoff");
                            double.TryParse(Item.InnerText, out var temp);
                            WheelSmoothInfo[device].MinCutoff = temp;
                        }
                        catch
                        {
                            WheelSmoothInfo[device].MinCutoff = OneEuroFilterPair.DEFAULT_WHEEL_CUTOFF;
                            missingSetting = true;
                        }

                        try
                        {
                            Item = xmlSASteeringWheelSmoothElement.SelectSingleNode("SASteeringWheelSmoothBeta");
                            //Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelSmoothBeta");
                            double.TryParse(Item.InnerText, out var temp);
                            WheelSmoothInfo[device].Beta = temp;
                        }
                        catch
                        {
                            WheelSmoothInfo[device].Beta = OneEuroFilterPair.DEFAULT_WHEEL_BETA;
                            missingSetting = true;
                        }
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelFuzz");
                        int.TryParse(Item.InnerText, out var temp);
                        SAWheelFuzzValues[device] = temp >= 0 && temp <= 100 ? temp : 0;
                    }
                    catch
                    {
                        SAWheelFuzzValues[device] = 0;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroOutputMode");
                        var tempMode = Item.InnerText;
                        //gyroOutMode[device] = GetGyroOutModeType(tempMode);
                        Enum.TryParse(tempMode, out GyroOutMode value);
                        GyroOutputMode[device] = value;
                    }
                    catch
                    {
                        PortOldGyroSettings(device);
                        missingSetting = true;
                    }

                    var xmlGyroControlsElement =
                        m_Xdoc.SelectSingleNode("/" + rootname + "/GyroControlsSettings");
                    if (xmlGyroControlsElement != null)
                    {
                        try
                        {
                            Item = xmlGyroControlsElement.SelectSingleNode("Triggers");
                            if (Item != null) GyroControlsInfo[device].triggers = Item.InnerText;
                        }
                        catch
                        {
                        }

                        try
                        {
                            Item = xmlGyroControlsElement.SelectSingleNode("TriggerCond");
                            if (Item != null) GyroControlsInfo[device].triggerCond = SaTriggerCondValue(Item.InnerText);
                        }
                        catch
                        {
                        }

                        try
                        {
                            Item = xmlGyroControlsElement.SelectSingleNode("TriggerTurns");
                            if (bool.TryParse(Item?.InnerText ?? "", out var tempTurns))
                                GyroControlsInfo[device].triggerTurns = tempTurns;
                        }
                        catch
                        {
                        }

                        try
                        {
                            Item = xmlGyroControlsElement.SelectSingleNode("Toggle");
                            if (bool.TryParse(Item?.InnerText ?? "", out var tempToggle))
                                SetGyroControlsToggle(device, tempToggle, control);
                        }
                        catch
                        {
                        }
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickTriggers");
                        SAMouseStickTriggers[device] = Item.InnerText;
                    }
                    catch
                    {
                        SAMouseStickTriggers[device] = "-1";
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickTriggerCond");
                        SAMouseStickTriggerCond[device] = SaTriggerCondValue(Item.InnerText);
                    }
                    catch
                    {
                        SAMouseStickTriggerCond[device] = true;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickTriggerTurns");
                        bool.TryParse(Item.InnerText, out var value);
                        GyroMouseStickTriggerTurns[device] = value;
                    }
                    catch
                    {
                        GyroMouseStickTriggerTurns[device] = true;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickHAxis");
                        var temp = 0;
                        int.TryParse(Item.InnerText, out temp);
                        GyroMouseStickHorizontalAxis[device] = Math.Min(Math.Max(0, temp), 1);
                    }
                    catch
                    {
                        GyroMouseStickHorizontalAxis[device] = 0;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickDeadZone");
                        int.TryParse(Item.InnerText, out var temp);
                        GyroMouseStickInfo[device].deadZone = temp;
                    }
                    catch
                    {
                        GyroMouseStickInfo[device].deadZone = 30;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickMaxZone");
                        int.TryParse(Item.InnerText, out var temp);
                        GyroMouseStickInfo[device].maxZone = Math.Max(temp, 1);
                    }
                    catch
                    {
                        GyroMouseStickInfo[device].maxZone = 830;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickOutputStick");
                        if (Enum.TryParse(Item?.InnerText ?? "", out GyroMouseStickInfo.OutputStick temp))
                            GyroMouseStickInfo[device].outputStick = temp;
                    }
                    catch
                    {
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickOutputStickAxes");
                        if (Enum.TryParse(Item?.InnerText ?? "", out GyroMouseStickInfo.OutputStickAxes temp))
                            GyroMouseStickInfo[device].outputStickDir = temp;
                    }
                    catch
                    {
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickAntiDeadX");
                        double.TryParse(Item.InnerText, out var temp);
                        GyroMouseStickInfo[device].antiDeadX = temp;
                    }
                    catch
                    {
                        GyroMouseStickInfo[device].antiDeadX = 0.4;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickAntiDeadY");
                        double.TryParse(Item.InnerText, out var temp);
                        GyroMouseStickInfo[device].antiDeadY = temp;
                    }
                    catch
                    {
                        GyroMouseStickInfo[device].antiDeadY = 0.4;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickInvert");
                        uint.TryParse(Item.InnerText, out GyroMouseStickInfo[device].inverted);
                    }
                    catch
                    {
                        GyroMouseStickInfo[device].inverted = 0;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickToggle");
                        bool.TryParse(Item.InnerText, out var temp);
                        SetGyroMouseStickToggle(device, temp, control);
                    }
                    catch
                    {
                        SetGyroMouseStickToggle(device, false, control);
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickMaxOutput");
                        var temp = 100.0;
                        temp = double.Parse(Item.InnerText);
                        GyroMouseStickInfo[device].maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                    }
                    catch
                    {
                        GyroMouseStickInfo[device].maxOutput = 100.0;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickMaxOutputEnabled");
                        bool.TryParse(Item.InnerText, out var temp);
                        GyroMouseStickInfo[device].maxOutputEnabled = temp;
                    }
                    catch
                    {
                        GyroMouseStickInfo[device].maxOutputEnabled = false;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickVerticalScale");
                        int.TryParse(Item.InnerText, out GyroMouseStickInfo[device].vertScale);
                    }
                    catch
                    {
                        GyroMouseStickInfo[device].vertScale = 100;
                        missingSetting = true;
                    }

                    var gyroMStickSmoothingGroup = false;
                    var xmlGyroMStickSmoothingElement =
                        m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickSmoothingSettings");
                    gyroMStickSmoothingGroup = xmlGyroMStickSmoothingElement != null;

                    if (gyroMStickSmoothingGroup)
                    {
                        try
                        {
                            Item = xmlGyroMStickSmoothingElement.SelectSingleNode("UseSmoothing");
                            bool.TryParse(Item.InnerText, out var tempSmoothing);
                            GyroMouseStickInfo[device].useSmoothing = tempSmoothing;
                        }
                        catch
                        {
                            GyroMouseStickInfo[device].useSmoothing = false;
                            missingSetting = true;
                        }

                        try
                        {
                            Item = xmlGyroMStickSmoothingElement.SelectSingleNode("SmoothingMethod");
                            var temp = Item?.InnerText ?? DS4Windows.GyroMouseStickInfo.DEFAULT_SMOOTH_TECHNIQUE;
                            GyroMouseStickInfo[device].DetermineSmoothMethod(temp);
                        }
                        catch
                        {
                            GyroMouseStickInfo[device].ResetSmoothing();
                            missingSetting = true;
                        }

                        try
                        {
                            Item = xmlGyroMStickSmoothingElement.SelectSingleNode("SmoothingWeight");
                            var temp = 0;
                            int.TryParse(Item.InnerText, out temp);
                            GyroMouseStickInfo[device].smoothWeight =
                                Math.Min(Math.Max(0.0, Convert.ToDouble(temp * 0.01)), 1.0);
                        }
                        catch
                        {
                            GyroMouseStickInfo[device].smoothWeight = 0.5;
                            missingSetting = true;
                        }

                        try
                        {
                            Item = xmlGyroMStickSmoothingElement.SelectSingleNode("SmoothingMinCutoff");
                            double.TryParse(Item.InnerText, out var temp);
                            GyroMouseStickInfo[device].minCutoff = Math.Min(Math.Max(0.0, temp), 100.0);
                        }
                        catch
                        {
                            GyroMouseStickInfo[device].minCutoff = DS4Windows.GyroMouseStickInfo.DEFAULT_MINCUTOFF;
                            missingSetting = true;
                        }

                        try
                        {
                            Item = xmlGyroMStickSmoothingElement.SelectSingleNode("SmoothingBeta");
                            double.TryParse(Item.InnerText, out var temp);
                            GyroMouseStickInfo[device].beta = Math.Min(Math.Max(0.0, temp), 1.0);
                        }
                        catch
                        {
                            GyroMouseStickInfo[device].beta = DS4Windows.GyroMouseStickInfo.DEFAULT_BETA;
                            missingSetting = true;
                        }
                    }
                    else
                    {
                        missingSetting = true;
                    }

                    var xmlGyroSwipeElement =
                        m_Xdoc.SelectSingleNode("/" + rootname + "/GyroSwipeSettings");
                    if (xmlGyroSwipeElement != null)
                    {
                        try
                        {
                            Item = xmlGyroSwipeElement.SelectSingleNode("DeadZoneX");
                            if (int.TryParse(Item?.InnerText ?? "", out var tempDead))
                                GyroSwipeInfo[device].deadzoneX = tempDead;
                        }
                        catch
                        {
                        }

                        try
                        {
                            Item = xmlGyroSwipeElement.SelectSingleNode("DeadZoneY");
                            if (int.TryParse(Item?.InnerText ?? "", out var tempDead))
                                GyroSwipeInfo[device].deadzoneY = tempDead;
                        }
                        catch
                        {
                        }

                        try
                        {
                            Item = xmlGyroSwipeElement.SelectSingleNode("Triggers");
                            if (Item != null) GyroSwipeInfo[device].triggers = Item.InnerText;
                        }
                        catch
                        {
                        }

                        try
                        {
                            Item = xmlGyroSwipeElement.SelectSingleNode("TriggerCond");
                            if (Item != null) GyroSwipeInfo[device].triggerCond = SaTriggerCondValue(Item.InnerText);
                        }
                        catch
                        {
                        }

                        try
                        {
                            Item = xmlGyroSwipeElement.SelectSingleNode("TriggerTurns");
                            if (bool.TryParse(Item?.InnerText ?? "", out var tempTurns))
                                GyroSwipeInfo[device].triggerTurns = tempTurns;
                        }
                        catch
                        {
                        }

                        try
                        {
                            Item = xmlGyroSwipeElement.SelectSingleNode("XAxis");
                            if (Enum.TryParse(Item?.InnerText ?? "", out GyroDirectionalSwipeInfo.XAxisSwipe tempX))
                                GyroSwipeInfo[device].xAxis = tempX;
                        }
                        catch
                        {
                        }

                        try
                        {
                            Item = xmlGyroSwipeElement.SelectSingleNode("DelayTime");
                            if (int.TryParse(Item?.InnerText ?? "", out var tempDelay))
                                GyroSwipeInfo[device].delayTime = tempDelay;
                        }
                        catch
                        {
                        }
                    }

                    // Check for TouchpadOutputMode if UseTPforControls is not present in profile
                    if (!tpForControlsPresent)
                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchpadOutputMode");
                            var tempMode = Item.InnerText;
                            Enum.TryParse(tempMode, out TouchpadOutMode value);
                            TouchOutMode[device] = value;
                        }
                        catch
                        {
                            TouchOutMode[device] = TouchpadOutMode.Mouse;
                            missingSetting = true;
                        }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchDisInvTriggers");
                        var triggers = Item.InnerText.Split(',');
                        var temp = -1;
                        var tempIntList = new List<int>();
                        for (int i = 0, arlen = triggers.Length; i < arlen; i++)
                            if (int.TryParse(triggers[i], out temp))
                                tempIntList.Add(temp);

                        TouchDisInvertTriggers[device] = tempIntList.ToArray();
                    }
                    catch
                    {
                        TouchDisInvertTriggers[device] = new int[1] { -1 };
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroSensitivity");
                        int.TryParse(Item.InnerText, out var value);
                        GyroSensitivity[device] = value;
                    }
                    catch
                    {
                        GyroSensitivity[device] = 100;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroSensVerticalScale");
                        int.TryParse(Item.InnerText, out var value);
                        GyroSensVerticalScale[device] = value;
                    }
                    catch
                    {
                        GyroSensVerticalScale[device] = 100;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroInvert");
                        int.TryParse(Item.InnerText, out var value);
                        GyroInvert[device] = value;
                    }
                    catch
                    {
                        GyroInvert[device] = 0;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroTriggerTurns");
                        bool.TryParse(Item.InnerText, out var value);
                        GyroTriggerTurns[device] = value;
                    }
                    catch
                    {
                        GyroTriggerTurns[device] = true;
                        missingSetting = true;
                    }

                    /*try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroSmoothing"); bool.TryParse(Item.InnerText, out gyroSmoothing[device]); }
                    catch { gyroSmoothing[device] = false; missingSetting = true; }

                    try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroSmoothingWeight"); int temp = 0; int.TryParse(Item.InnerText, out temp); gyroSmoothWeight[device] = Math.Min(Math.Max(0.0, Convert.ToDouble(temp * 0.01)), 1.0); }
                    catch { gyroSmoothWeight[device] = 0.5; missingSetting = true; }
                    */

                    var gyroSmoothingGroup = false;
                    var xmlGyroSmoothingElement =
                        m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseSmoothingSettings");
                    gyroSmoothingGroup = xmlGyroSmoothingElement != null;

                    if (gyroSmoothingGroup)
                    {
                        try
                        {
                            Item = xmlGyroSmoothingElement.SelectSingleNode("UseSmoothing");
                            bool.TryParse(Item.InnerText, out var temp);
                            GyroMouseInfo[device].enableSmoothing = temp;
                        }
                        catch
                        {
                            GyroMouseInfo[device].enableSmoothing = false;
                            missingSetting = true;
                        }

                        try
                        {
                            Item = xmlGyroSmoothingElement.SelectSingleNode("SmoothingMethod");
                            var temp = Item?.InnerText ?? DS4Windows.GyroMouseInfo.DEFAULT_SMOOTH_TECHNIQUE;
                            GyroMouseInfo[device].DetermineSmoothMethod(temp);
                        }
                        catch
                        {
                            GyroMouseInfo[device].ResetSmoothing();
                            missingSetting = true;
                        }

                        try
                        {
                            Item = xmlGyroSmoothingElement.SelectSingleNode("SmoothingWeight");
                            int.TryParse(Item.InnerText, out var temp);
                            GyroMouseInfo[device].smoothingWeight =
                                Math.Min(Math.Max(0.0, Convert.ToDouble(temp * 0.01)), 1.0);
                        }
                        catch
                        {
                            GyroMouseInfo[device].smoothingWeight = 0.5;
                            missingSetting = true;
                        }

                        try
                        {
                            Item = xmlGyroSmoothingElement.SelectSingleNode("SmoothingMinCutoff");
                            double.TryParse(Item.InnerText, out var temp);
                            GyroMouseInfo[device].minCutoff = Math.Min(Math.Max(0.0, temp), 100.0);
                        }
                        catch
                        {
                            GyroMouseInfo[device].minCutoff = DS4Windows.GyroMouseInfo.DEFAULT_MINCUTOFF;
                            missingSetting = true;
                        }

                        try
                        {
                            Item = xmlGyroSmoothingElement.SelectSingleNode("SmoothingBeta");
                            double.TryParse(Item.InnerText, out var temp);
                            GyroMouseInfo[device].beta = Math.Min(Math.Max(0.0, temp), 1.0);
                        }
                        catch
                        {
                            GyroMouseInfo[device].beta = DS4Windows.GyroMouseInfo.DEFAULT_BETA;
                            missingSetting = true;
                        }
                    }
                    else
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseHAxis");
                        var temp = 0;
                        int.TryParse(Item.InnerText, out temp);
                        GyroMouseHorizontalAxis[device] = Math.Min(Math.Max(0, temp), 1);
                    }
                    catch
                    {
                        GyroMouseHorizontalAxis[device] = 0;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseDeadZone");
                        int.TryParse(Item.InnerText, out var temp);
                        SetGyroMouseDZ(device, temp, control);
                    }
                    catch
                    {
                        SetGyroMouseDZ(device, MouseCursor.GYRO_MOUSE_DEADZONE, control);
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseMinThreshold");
                        double.TryParse(Item.InnerText, out var temp);
                        temp = Math.Min(Math.Max(temp, 1.0), 40.0);
                        GyroMouseInfo[device].minThreshold = temp;
                    }
                    catch
                    {
                        GyroMouseInfo[device].minThreshold = DS4Windows.GyroMouseInfo.DEFAULT_MIN_THRESHOLD;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseToggle");
                        bool.TryParse(Item.InnerText, out var temp);
                        SetGyroMouseToggle(device, temp, control);
                    }
                    catch
                    {
                        SetGyroMouseToggle(device, false, control);
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/BTPollRate");
                        var temp = 0;
                        int.TryParse(Item.InnerText, out temp);
                        BluetoothPollRate[device] = temp >= 0 && temp <= 16 ? temp : 4;
                    }
                    catch
                    {
                        BluetoothPollRate[device] = 4;
                        missingSetting = true;
                    }

                    // Note! xxOutputCurveCustom property needs to be read before xxOutputCurveMode property in case the curveMode is value 6
                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOutputCurveCustom");
                        LSOutBezierCurveObj[device].CustomDefinition = Item.InnerText;
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOutputCurveMode");
                        setLsOutCurveMode(device, stickOutputCurveId(Item.InnerText));
                    }
                    catch
                    {
                        setLsOutCurveMode(device, 0);
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOutputCurveCustom");
                        RSOutBezierCurveObj[device].CustomDefinition = Item.InnerText;
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOutputCurveMode");
                        setRsOutCurveMode(device, stickOutputCurveId(Item.InnerText));
                    }
                    catch
                    {
                        setRsOutCurveMode(device, 0);
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSSquareStick");
                        bool.TryParse(Item.InnerText, out SquStickInfo[device].lsMode);
                    }
                    catch
                    {
                        SquStickInfo[device].lsMode = false;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SquareStickRoundness");
                        double.TryParse(Item.InnerText, out SquStickInfo[device].lsRoundness);
                    }
                    catch
                    {
                        SquStickInfo[device].lsRoundness = 5.0;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SquareRStickRoundness");
                        double.TryParse(Item.InnerText, out SquStickInfo[device].rsRoundness);
                    }
                    catch
                    {
                        SquStickInfo[device].rsRoundness = 5.0;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSSquareStick");
                        bool.TryParse(Item.InnerText, out SquStickInfo[device].rsMode);
                    }
                    catch
                    {
                        SquStickInfo[device].rsMode = false;
                        missingSetting = true;
                    }


                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSAntiSnapback");
                        bool.TryParse(Item.InnerText, out LSAntiSnapbackInfo[device].enabled);
                    }
                    catch
                    {
                        LSAntiSnapbackInfo[device].enabled = StickAntiSnapbackInfo.DEFAULT_ENABLED;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSAntiSnapback");
                        bool.TryParse(Item.InnerText, out RSAntiSnapbackInfo[device].enabled);
                    }
                    catch
                    {
                        RSAntiSnapbackInfo[device].enabled = StickAntiSnapbackInfo.DEFAULT_ENABLED;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSAntiSnapbackDelta");
                        double.TryParse(Item.InnerText, out LSAntiSnapbackInfo[device].delta);
                    }
                    catch
                    {
                        LSAntiSnapbackInfo[device].delta = StickAntiSnapbackInfo.DEFAULT_DELTA;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSAntiSnapbackDelta");
                        double.TryParse(Item.InnerText, out RSAntiSnapbackInfo[device].delta);
                    }
                    catch
                    {
                        RSAntiSnapbackInfo[device].delta = StickAntiSnapbackInfo.DEFAULT_DELTA;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSAntiSnapbackTimeout");
                        int.TryParse(Item.InnerText, out LSAntiSnapbackInfo[device].timeout);
                    }
                    catch
                    {
                        LSAntiSnapbackInfo[device].timeout = StickAntiSnapbackInfo.DEFAULT_TIMEOUT;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSAntiSnapbackTimeout");
                        int.TryParse(Item.InnerText, out RSAntiSnapbackInfo[device].timeout);
                    }
                    catch
                    {
                        RSAntiSnapbackInfo[device].timeout = StickAntiSnapbackInfo.DEFAULT_TIMEOUT;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOutputMode");
                        Enum.TryParse(Item.InnerText, out LSOutputSettings[device].mode);
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOutputMode");
                        Enum.TryParse(Item.InnerText, out RSOutputSettings[device].mode);
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    var xmlLSOutputSettingsElement =
                        m_Xdoc.SelectSingleNode("/" + rootname + "/LSOutputSettings");
                    var lsOutputGroup = xmlLSOutputSettingsElement != null;
                    if (lsOutputGroup)
                    {
                        var flickStickLSGroup = false;
                        var xmlFlickStickLSElement =
                            xmlLSOutputSettingsElement.SelectSingleNode("FlickStickSettings");
                        flickStickLSGroup = xmlFlickStickLSElement != null;

                        if (flickStickLSGroup)
                        {
                            try
                            {
                                Item = xmlFlickStickLSElement.SelectSingleNode("RealWorldCalibration");
                                double.TryParse(Item.InnerText, out var temp);
                                LSOutputSettings[device].outputSettings.flickSettings.realWorldCalibration = temp;
                            }
                            catch
                            {
                                missingSetting = true;
                            }

                            try
                            {
                                Item = xmlFlickStickLSElement.SelectSingleNode("FlickThreshold");
                                double.TryParse(Item.InnerText, out var temp);
                                LSOutputSettings[device].outputSettings.flickSettings.flickThreshold = temp;
                            }
                            catch
                            {
                                missingSetting = true;
                            }

                            try
                            {
                                Item = xmlFlickStickLSElement.SelectSingleNode("FlickTime");
                                double.TryParse(Item.InnerText, out var temp);
                                LSOutputSettings[device].outputSettings.flickSettings.flickTime = temp;
                            }
                            catch
                            {
                                missingSetting = true;
                            }
                        }
                        else
                        {
                            missingSetting = true;
                        }
                    }
                    else
                    {
                        missingSetting = true;
                    }

                    var xmlRSOutputSettingsElement =
                        m_Xdoc.SelectSingleNode("/" + rootname + "/RSOutputSettings");
                    var rsOutputGroup = xmlRSOutputSettingsElement != null;
                    if (rsOutputGroup)
                    {
                        var flickStickRSGroup = false;
                        var xmlFlickStickRSElement =
                            xmlRSOutputSettingsElement.SelectSingleNode("FlickStickSettings");
                        flickStickRSGroup = xmlFlickStickRSElement != null;

                        if (flickStickRSGroup)
                        {
                            try
                            {
                                Item = xmlFlickStickRSElement.SelectSingleNode("RealWorldCalibration");
                                double.TryParse(Item.InnerText, out var temp);
                                RSOutputSettings[device].outputSettings.flickSettings.realWorldCalibration = temp;
                            }
                            catch
                            {
                                missingSetting = true;
                            }

                            try
                            {
                                Item = xmlFlickStickRSElement.SelectSingleNode("FlickThreshold");
                                double.TryParse(Item.InnerText, out var temp);
                                RSOutputSettings[device].outputSettings.flickSettings.flickThreshold = temp;
                            }
                            catch
                            {
                                missingSetting = true;
                            }

                            try
                            {
                                Item = xmlFlickStickRSElement.SelectSingleNode("FlickTime");
                                double.TryParse(Item.InnerText, out var temp);
                                RSOutputSettings[device].outputSettings.flickSettings.flickTime = temp;
                            }
                            catch
                            {
                                missingSetting = true;
                            }
                        }
                        else
                        {
                            missingSetting = true;
                        }
                    }
                    else
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2OutputCurveCustom");
                        L2OutBezierCurveObj[device].CustomDefinition = Item.InnerText;
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2OutputCurveMode");
                        setL2OutCurveMode(device, axisOutputCurveId(Item.InnerText));
                    }
                    catch
                    {
                        setL2OutCurveMode(device, 0);
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2TwoStageMode");
                        if (Enum.TryParse(Item?.InnerText, out TwoStageTriggerMode tempMode))
                            L2OutputSettings[device].TwoStageMode = tempMode;
                    }
                    catch
                    {
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2HipFireDelay");
                        if (int.TryParse(Item?.InnerText, out var temp))
                            L2OutputSettings[device].hipFireMS = Math.Max(Math.Min(0, temp), 5000);
                    }
                    catch
                    {
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2TriggerEffect");
                        if (Enum.TryParse(Item?.InnerText, out TriggerEffects tempMode))
                            L2OutputSettings[device].TriggerEffect = tempMode;
                    }
                    catch
                    {
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2OutputCurveCustom");
                        R2OutBezierCurveObj[device].CustomDefinition = Item.InnerText;
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2OutputCurveMode");
                        setR2OutCurveMode(device, axisOutputCurveId(Item.InnerText));
                    }
                    catch
                    {
                        setR2OutCurveMode(device, 0);
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2TwoStageMode");
                        if (Enum.TryParse(Item?.InnerText, out TwoStageTriggerMode tempMode))
                            R2OutputSettings[device].TwoStageMode = tempMode;
                    }
                    catch
                    {
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2HipFireDelay");
                        if (int.TryParse(Item?.InnerText, out var temp))
                            R2OutputSettings[device].hipFireMS = Math.Max(Math.Min(0, temp), 5000);
                    }
                    catch
                    {
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2TriggerEffect");
                        if (Enum.TryParse(Item?.InnerText, out TriggerEffects tempMode))
                            R2OutputSettings[device].TriggerEffect = tempMode;
                    }
                    catch
                    {
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SXOutputCurveCustom");
                        SXOutBezierCurveObj[device].CustomDefinition = Item.InnerText;
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SXOutputCurveMode");
                        setSXOutCurveMode(device, axisOutputCurveId(Item.InnerText));
                    }
                    catch
                    {
                        setSXOutCurveMode(device, 0);
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZOutputCurveCustom");
                        SZOutBezierCurveObj[device].CustomDefinition = Item.InnerText;
                    }
                    catch
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZOutputCurveMode");
                        setSZOutCurveMode(device, axisOutputCurveId(Item.InnerText));
                    }
                    catch
                    {
                        setSZOutCurveMode(device, 0);
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TrackballMode");
                        bool.TryParse(Item.InnerText, out var value);
                        TrackballMode[device] = value;
                    }
                    catch
                    {
                        TrackballMode[device] = false;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TrackballFriction");
                        double.TryParse(Item.InnerText, out var value);
                        TrackballFriction[device] = value;
                    }
                    catch
                    {
                        TrackballFriction[device] = 10.0;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchRelMouseRotation");
                        int.TryParse(Item.InnerText, out var temp);
                        temp = Math.Min(Math.Max(temp, -180), 180);
                        TouchPadRelMouse[device].rotation = temp * Math.PI / 180.0;
                    }
                    catch
                    {
                        TouchPadRelMouse[device].rotation = 0.0;
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchRelMouseMinThreshold");
                        double.TryParse(Item.InnerText, out var temp);
                        temp = Math.Min(Math.Max(temp, 1.0), 40.0);
                        TouchPadRelMouse[device].minThreshold = temp;
                    }
                    catch
                    {
                        TouchPadRelMouse[device].minThreshold = TouchpadRelMouseSettings.DEFAULT_MIN_THRESHOLD;
                        missingSetting = true;
                    }


                    var touchpadAbsMouseGroup = false;
                    var touchpadAbsMouseElement =
                        m_Xdoc.SelectSingleNode("/" + rootname + "/TouchpadAbsMouseSettings");
                    touchpadAbsMouseGroup = touchpadAbsMouseElement != null;

                    if (touchpadAbsMouseGroup)
                    {
                        try
                        {
                            Item = touchpadAbsMouseElement.SelectSingleNode("MaxZoneX");
                            int.TryParse(Item.InnerText, out var temp);
                            TouchPadAbsMouse[device].maxZoneX = temp;
                        }
                        catch
                        {
                            TouchPadAbsMouse[device].maxZoneX = TouchpadAbsMouseSettings.DEFAULT_MAXZONE_X;
                            missingSetting = true;
                        }

                        try
                        {
                            Item = touchpadAbsMouseElement.SelectSingleNode("MaxZoneY");
                            int.TryParse(Item.InnerText, out var temp);
                            TouchPadAbsMouse[device].maxZoneY = temp;
                        }
                        catch
                        {
                            TouchPadAbsMouse[device].maxZoneY = TouchpadAbsMouseSettings.DEFAULT_MAXZONE_Y;
                            missingSetting = true;
                        }

                        try
                        {
                            Item = touchpadAbsMouseElement.SelectSingleNode("SnapToCenter");
                            bool.TryParse(Item.InnerText, out var temp);
                            TouchPadAbsMouse[device].snapToCenter = temp;
                        }
                        catch
                        {
                            TouchPadAbsMouse[device].snapToCenter = TouchpadAbsMouseSettings.DEFAULT_SNAP_CENTER;
                            missingSetting = true;
                        }
                    }
                    else
                    {
                        missingSetting = true;
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/OutputContDevice");
                        OutputDeviceType[device] = OutContDeviceId(Item.InnerText);
                    }
                    catch
                    {
                        OutputDeviceType[device] = OutContType.X360;
                        missingSetting = true;
                    }

                    // Only change xinput devices under certain conditions. Avoid
                    // performing this upon program startup before loading devices.
                    if (xinputChange && device < ControlService.CURRENT_DS4_CONTROLLER_LIMIT)
                        CheckOldDeviceStatus(device, control, oldContType,
                            out xinputPlug, out xinputStatus);

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ProfileActions");
                        ProfileActions[device].Clear();
                        if (!string.IsNullOrEmpty(Item.InnerText))
                        {
                            var actionNames = Item.InnerText.Split('/');
                            for (int actIndex = 0, actLen = actionNames.Length; actIndex < actLen; actIndex++)
                            {
                                var tempActionName = actionNames[actIndex];
                                if (!ProfileActions[device].Contains(tempActionName))
                                    ProfileActions[device].Add(tempActionName);
                            }
                        }
                    }
                    catch
                    {
                        ProfileActions[device].Clear();
                        missingSetting = true;
                    }

                    foreach (var dcs in Ds4Settings[device])
                        dcs.Reset();

                    ContainsCustomAction[device] = false;
                    ContainsCustomExtras[device] = false;
                    profileActionCount[device] = ProfileActions[device].Count;
                    profileActionDict[device].Clear();
                    profileActionIndexDict[device].Clear();
                    foreach (var actionname in ProfileActions[device])
                    {
                        profileActionDict[device][actionname] = Instance.GetAction(actionname);
                        profileActionIndexDict[device][actionname] = Instance.GetActionIndexOf(actionname);
                    }

                    DS4KeyType keyType;
                    ushort wvk;

                    {
                        var ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/Control/Button");
                        if (ParentItem != null)
                            foreach (XmlNode item in ParentItem.ChildNodes)
                                if (Enum.TryParse(item.Name, out DS4Controls currentControl))
                                {
                                    UpdateDs4ControllerSetting(device, item.Name, false,
                                        GetX360ControlsByName(item.InnerText), "", DS4KeyType.None);
                                    customMapButtons.Add(GetDs4ControlsByName(item.Name),
                                        GetX360ControlsByName(item.InnerText));
                                }

                        ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/Control/Macro");
                        if (ParentItem != null)
                            foreach (XmlNode item in ParentItem.ChildNodes)
                            {
                                customMapMacros.Add(GetDs4ControlsByName(item.Name), item.InnerText);
                                string[] skeys;
                                int[] keys;
                                if (!string.IsNullOrEmpty(item.InnerText))
                                {
                                    skeys = item.InnerText.Split('/');
                                    keys = new int[skeys.Length];
                                }
                                else
                                {
                                    skeys = new string[0];
                                    keys = new int[0];
                                }

                                for (int i = 0, keylen = keys.Length; i < keylen; i++)
                                    keys[i] = int.Parse(skeys[i]);

                                if (Enum.TryParse(item.Name, out DS4Controls currentControl))
                                    UpdateDs4ControllerSetting(device, item.Name, false, keys, "", DS4KeyType.None);
                            }

                        ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/Control/Key");
                        if (ParentItem != null)
                            foreach (XmlNode item in ParentItem.ChildNodes)
                                if (ushort.TryParse(item.InnerText, out wvk) &&
                                    Enum.TryParse(item.Name, out DS4Controls currentControl))
                                {
                                    UpdateDs4ControllerSetting(device, item.Name, false, wvk, "", DS4KeyType.None);
                                    customMapKeys.Add(GetDs4ControlsByName(item.Name), wvk);
                                }

                        ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/Control/Extras");
                        if (ParentItem != null)
                            foreach (XmlNode item in ParentItem.ChildNodes)
                                if (item.InnerText != string.Empty &&
                                    Enum.TryParse(item.Name, out DS4Controls currentControl))
                                {
                                    UpdateDs4ControllerExtra(device, item.Name, false, item.InnerText);
                                    customMapExtras.Add(GetDs4ControlsByName(item.Name), item.InnerText);
                                }
                                else
                                {
                                    ParentItem.RemoveChild(item);
                                }

                        ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/Control/KeyType");
                        if (ParentItem != null)
                            foreach (XmlNode item in ParentItem.ChildNodes)
                                if (item != null)
                                {
                                    keyType = DS4KeyType.None;
                                    if (item.InnerText.Contains(DS4KeyType.ScanCode.ToString()))
                                        keyType |= DS4KeyType.ScanCode;
                                    if (item.InnerText.Contains(DS4KeyType.Toggle.ToString()))
                                        keyType |= DS4KeyType.Toggle;
                                    if (item.InnerText.Contains(DS4KeyType.Macro.ToString()))
                                        keyType |= DS4KeyType.Macro;
                                    if (item.InnerText.Contains(DS4KeyType.HoldMacro.ToString()))
                                        keyType |= DS4KeyType.HoldMacro;
                                    if (item.InnerText.Contains(DS4KeyType.Unbound.ToString()))
                                        keyType |= DS4KeyType.Unbound;

                                    if (keyType != DS4KeyType.None &&
                                        Enum.TryParse(item.Name, out DS4Controls currentControl))
                                    {
                                        UpdateDs4ControllerKeyType(device, item.Name, false, keyType);
                                        customMapKeyTypes.Add(GetDs4ControlsByName(item.Name), keyType);
                                    }
                                }

                        ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftControl/Button");
                        if (ParentItem != null)
                            foreach (XmlElement item in ParentItem.ChildNodes)
                            {
                                var shiftT = shiftM;
                                if (item.HasAttribute("Trigger"))
                                    int.TryParse(item.Attributes["Trigger"].Value, out shiftT);

                                if (Enum.TryParse(item.Name, out DS4Controls currentControl))
                                {
                                    UpdateDs4ControllerSetting(device, item.Name, true,
                                        GetX360ControlsByName(item.InnerText), "", DS4KeyType.None, shiftT);
                                    shiftCustomMapButtons.Add(GetDs4ControlsByName(item.Name),
                                        GetX360ControlsByName(item.InnerText));
                                }
                            }

                        ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftControl/Macro");
                        if (ParentItem != null)
                            foreach (XmlElement item in ParentItem.ChildNodes)
                            {
                                shiftCustomMapMacros.Add(GetDs4ControlsByName(item.Name), item.InnerText);
                                string[] skeys;
                                int[] keys;
                                if (!string.IsNullOrEmpty(item.InnerText))
                                {
                                    skeys = item.InnerText.Split('/');
                                    keys = new int[skeys.Length];
                                }
                                else
                                {
                                    skeys = new string[0];
                                    keys = new int[0];
                                }

                                for (int i = 0, keylen = keys.Length; i < keylen; i++)
                                    keys[i] = int.Parse(skeys[i]);

                                var shiftT = shiftM;
                                if (item.HasAttribute("Trigger"))
                                    int.TryParse(item.Attributes["Trigger"].Value, out shiftT);

                                if (Enum.TryParse(item.Name, out DS4Controls currentControl))
                                    UpdateDs4ControllerSetting(device, item.Name, true, keys, "", DS4KeyType.None,
                                        shiftT);
                            }

                        ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftControl/Key");
                        if (ParentItem != null)
                            foreach (XmlElement item in ParentItem.ChildNodes)
                                if (ushort.TryParse(item.InnerText, out wvk))
                                {
                                    var shiftT = shiftM;
                                    if (item.HasAttribute("Trigger"))
                                        int.TryParse(item.Attributes["Trigger"].Value, out shiftT);

                                    if (Enum.TryParse(item.Name, out DS4Controls currentControl))
                                    {
                                        UpdateDs4ControllerSetting(device, item.Name, true, wvk, "", DS4KeyType.None,
                                            shiftT);
                                        shiftCustomMapKeys.Add(GetDs4ControlsByName(item.Name), wvk);
                                    }
                                }

                        ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftControl/Extras");
                        if (ParentItem != null)
                            foreach (XmlElement item in ParentItem.ChildNodes)
                                if (item.InnerText != string.Empty)
                                {
                                    if (Enum.TryParse(item.Name, out DS4Controls currentControl))
                                    {
                                        UpdateDs4ControllerExtra(device, item.Name, true, item.InnerText);
                                        shiftCustomMapExtras.Add(GetDs4ControlsByName(item.Name), item.InnerText);
                                    }
                                }
                                else
                                {
                                    ParentItem.RemoveChild(item);
                                }

                        ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftControl/KeyType");
                        if (ParentItem != null)
                            foreach (XmlElement item in ParentItem.ChildNodes)
                                if (item != null)
                                {
                                    keyType = DS4KeyType.None;
                                    if (item.InnerText.Contains(DS4KeyType.ScanCode.ToString()))
                                        keyType |= DS4KeyType.ScanCode;
                                    if (item.InnerText.Contains(DS4KeyType.Toggle.ToString()))
                                        keyType |= DS4KeyType.Toggle;
                                    if (item.InnerText.Contains(DS4KeyType.Macro.ToString()))
                                        keyType |= DS4KeyType.Macro;
                                    if (item.InnerText.Contains(DS4KeyType.HoldMacro.ToString()))
                                        keyType |= DS4KeyType.HoldMacro;
                                    if (item.InnerText.Contains(DS4KeyType.Unbound.ToString()))
                                        keyType |= DS4KeyType.Unbound;

                                    if (keyType != DS4KeyType.None &&
                                        Enum.TryParse(item.Name, out DS4Controls currentControl))
                                    {
                                        UpdateDs4ControllerKeyType(device, item.Name, true, keyType);
                                        shiftCustomMapKeyTypes.Add(GetDs4ControlsByName(item.Name), keyType);
                                    }
                                }
                    }
                }
                else
                {
                    Loaded = false;
                    ResetProfile(device);
                    ResetMouseProperties(device, control);

                    // Unplug existing output device if requested profile does not exist
                    var tempOutDev = device < ControlService.CURRENT_DS4_CONTROLLER_LIMIT
                        ? control.outputDevices[device]
                        : null;
                    if (tempOutDev != null)
                    {
                        tempOutDev = null;
                        //Global.ActiveOutDevType[device] = OutContType.None;
                        var tempDev = control.DS4Controllers[device];
                        if (tempDev != null)
                            tempDev.queueEvent(() => { control.UnplugOutDev(device, tempDev); });
                    }
                }

                // Only add missing settings if the actual load was graceful
                if ((missingSetting || migratePerformed) && Loaded) // && buttons != null)
                {
                    var proName = Path.GetFileName(profilepath);
                    SaveProfile(device, proName);
                }

                if (Loaded)
                {
                    CacheProfileCustomsFlags(device);
                    ButtonMouseInfos[device].activeButtonSensitivity =
                        ButtonMouseInfos[device].buttonSensitivity;

                    //if (device < Global.MAX_DS4_CONTROLLER_COUNT && control.touchPad[device] != null)
                    //{
                    //    control.touchPad[device]?.ResetToggleGyroModes();
                    //    GyroOutMode currentGyro = gyroOutMode[device];
                    //    if (currentGyro == GyroOutMode.Mouse)
                    //    {
                    //        control.touchPad[device].ToggleGyroMouse =
                    //            gyroMouseToggle[device];
                    //    }
                    //    else if (currentGyro == GyroOutMode.MouseJoystick)
                    //    {
                    //        control.touchPad[device].ToggleGyroMouse =
                    //            gyroMouseStickToggle[device];
                    //    }
                    //}

                    // If a device exists, make sure to transfer relevant profile device
                    // options to device instance
                    if (postLoad && device < MAX_DS4_CONTROLLER_COUNT)
                        PostLoadSnippet(device, control, xinputStatus, xinputPlug);
                }

                return Loaded;
            }

            public bool Load()
            {
                var Loaded = true;
                var missingSetting = false;

                try
                {
                    if (File.Exists(ProfilesPath))
                    {
                        XmlNode Item;

                        m_Xdoc.Load(ProfilesPath);

                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/Profile/useExclusiveMode");
                            bool.TryParse(Item.InnerText, out var value);
                            UseExclusiveMode = value;
                        } // Ex Mode
                        catch
                        {
                            missingSetting = true;
                        } // Ex Mode

                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/Profile/startMinimized");
                            bool.TryParse(Item.InnerText, out var value);
                            StartMinimized = value;
                        }
                        catch
                        {
                            missingSetting = true;
                        }

                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/Profile/minimizeToTaskbar");
                            bool.TryParse(Item.InnerText, out var value);
                            MinToTaskBar = value;
                        }
                        catch
                        {
                            missingSetting = true;
                        }

                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/Profile/formWidth");
                            int.TryParse(Item.InnerText, out var value);
                            FormWidth = value;
                        }
                        catch
                        {
                            missingSetting = true;
                        }

                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/Profile/formHeight");
                            int.TryParse(Item.InnerText, out var value);
                            FormHeight = value;
                        }
                        catch
                        {
                            missingSetting = true;
                        }

                        try
                        {
                            var temp = 0;
                            Item = m_Xdoc.SelectSingleNode("/Profile/formLocationX");
                            int.TryParse(Item.InnerText, out temp);
                            FormLocationX = Math.Max(temp, 0);
                        }
                        catch
                        {
                            missingSetting = true;
                        }

                        try
                        {
                            var temp = 0;
                            Item = m_Xdoc.SelectSingleNode("/Profile/formLocationY");
                            int.TryParse(Item.InnerText, out temp);
                            FormLocationY = Math.Max(temp, 0);
                        }
                        catch
                        {
                            missingSetting = true;
                        }

                        for (var i = 0; i < MAX_DS4_CONTROLLER_COUNT; i++)
                        {
                            var contTag = $"/Profile/Controller{i + 1}";
                            try
                            {
                                Item = m_Xdoc.SelectSingleNode(contTag);
                                ProfilePath[i] = Item?.InnerText ?? string.Empty;
                                if (ProfilePath[i].ToLower().Contains("distance")) DistanceProfiles[i] = true;

                                OlderProfilePath[i] = ProfilePath[i];
                            }
                            catch
                            {
                                ProfilePath[i] = OlderProfilePath[i] = string.Empty;
                                DistanceProfiles[i] = false;
                            }
                        }

                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/Profile/LastChecked");
                            DateTime.TryParse(Item.InnerText, out var lc);
                            LastChecked = lc;
                        }
                        catch
                        {
                            missingSetting = true;
                        }

                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/Profile/CheckWhen");
                            int.TryParse(Item.InnerText, out var cw);
                            CheckWhen = cw;
                        }
                        catch
                        {
                            missingSetting = true;
                        }

                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/Profile/LastVersionChecked");
                            var tempVer = Item?.InnerText ?? string.Empty;
                            if (!string.IsNullOrEmpty(tempVer))
                            {
                                LastVersionCheckedNumber = CompileVersionNumberFromString(tempVer);
                                if (LastVersionCheckedNumber > 0) lastVersionChecked = tempVer;
                            }
                        }
                        catch
                        {
                            missingSetting = true;
                        }

                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/Profile/Notifications");
                            if (!int.TryParse(Item.InnerText, out var not))
                                Notifications = bool.Parse(Item.InnerText) ? 2 : 0;

                            Notifications = not;
                        }
                        catch
                        {
                            missingSetting = true;
                        }

                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/Profile/DisconnectBTAtStop");
                            bool.TryParse(Item.InnerText, out var dc);
                            DisconnectBluetoothAtStop = dc;
                        }
                        catch
                        {
                            missingSetting = true;
                        }

                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/Profile/SwipeProfiles");
                            bool.TryParse(Item.InnerText, out var value);
                            SwipeProfiles = value;
                        }
                        catch
                        {
                            missingSetting = true;
                        }

                        //try { Item = m_Xdoc.SelectSingleNode("/Profile/UseDS4ForMapping"); Boolean.TryParse(Item.InnerText, out ds4Mapping); }
                        //catch { missingSetting = true; }
                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/Profile/QuickCharge");
                            bool.TryParse(Item.InnerText, out var value);
                            QuickCharge = value;
                        }
                        catch
                        {
                            missingSetting = true;
                        }

                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/Profile/CloseMinimizes");
                            bool.TryParse(Item.InnerText, out var value);
                            CloseMini = value;
                        }
                        catch
                        {
                            missingSetting = true;
                        }

                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/Profile/UseLang");
                            UseLang = Item.InnerText;
                        }
                        catch
                        {
                            missingSetting = true;
                        }

                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/Profile/DownloadLang");
                            bool.TryParse(Item.InnerText, out var value);
                            DownloadLang = value;
                        }
                        catch
                        {
                            missingSetting = true;
                        }

                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/Profile/FlashWhenLate");
                            bool.TryParse(Item.InnerText, out var value);
                            FlashWhenLate = value;
                        }
                        catch
                        {
                            missingSetting = true;
                        }

                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/Profile/FlashWhenLateAt");
                            int.TryParse(Item.InnerText, out var value);
                            FlashWhenLateAt = value;
                        }
                        catch
                        {
                            missingSetting = true;
                        }

                        Item = m_Xdoc.SelectSingleNode("/Profile/AppIcon");
                        var hasIconChoice = Item != null;
                        if (hasIconChoice)
                        {
                            hasIconChoice = Enum.TryParse(Item.InnerText ?? "", out TrayIconChoice ch);
                            UseIconChoice = ch;
                        }

                        if (!hasIconChoice)
                        {
                            missingSetting = true;

                            try
                            {
                                Item = m_Xdoc.SelectSingleNode("/Profile/WhiteIcon");
                                if (bool.TryParse(Item?.InnerText ?? "", out var temp))
                                    UseIconChoice = temp ? TrayIconChoice.White : TrayIconChoice.Default;
                            }
                            catch
                            {
                                missingSetting = true;
                            }
                        }

                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/Profile/AppTheme");
                            var temp = Item.InnerText;
                            Enum.TryParse(temp, out AppThemeChoice choice);
                            ThemeChoice = choice;
                        }
                        catch
                        {
                            missingSetting = true;
                            ThemeChoice = AppThemeChoice.Default;
                        }

                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/Profile/UseUDPServer");
                            bool.TryParse(Item.InnerText, out var value);
                            UseUdpServer = value;
                        }
                        catch
                        {
                            missingSetting = true;
                        }

                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/Profile/UDPServerPort");
                            int temp;
                            int.TryParse(Item.InnerText, out temp);
                            UdpServerPort = Math.Min(Math.Max(temp, 1024), 65535);
                        }
                        catch
                        {
                            missingSetting = true;
                        }

                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/Profile/UDPServerListenAddress");
                            UdpServerListenAddress = Item.InnerText;
                        }
                        catch
                        {
                            missingSetting = true;
                        }

                        var udpServerSmoothingGroup = false;
                        var xmlUdpServerSmoothElement =
                            m_Xdoc.SelectSingleNode("/Profile/UDPServerSmoothingOptions");
                        udpServerSmoothingGroup = xmlUdpServerSmoothElement != null;
                        if (udpServerSmoothingGroup)
                        {
                            try
                            {
                                Item = xmlUdpServerSmoothElement.SelectSingleNode("UseSmoothing");
                                bool.TryParse(Item.InnerText, out var temp);
                                UseUdpSmoothing = temp;
                            }
                            catch
                            {
                                missingSetting = true;
                            }

                            try
                            {
                                Item = xmlUdpServerSmoothElement.SelectSingleNode("UdpSmoothMinCutoff");
                                double.TryParse(Item.InnerText, out var temp);
                                temp = Math.Min(Math.Max(temp, 0.00001), 100.0);
                                UdpSmoothingMincutoff = temp;
                            }
                            catch
                            {
                                missingSetting = true;
                            }

                            try
                            {
                                Item = xmlUdpServerSmoothElement.SelectSingleNode("UdpSmoothBeta");
                                double.TryParse(Item.InnerText, out var temp);
                                temp = Math.Min(Math.Max(temp, 0.0), 1.0);
                                UdpSmoothingBeta = temp;
                            }
                            catch
                            {
                                missingSetting = true;
                            }
                        }

                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/Profile/UseCustomSteamFolder");
                            bool.TryParse(Item.InnerText, out var value);
                            UseCustomSteamFolder = value;
                        }
                        catch
                        {
                            missingSetting = true;
                        }

                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/Profile/CustomSteamFolder");
                            CustomSteamFolder = Item.InnerText;
                        }
                        catch
                        {
                            missingSetting = true;
                        }

                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/Profile/AutoProfileRevertDefaultProfile");
                            bool.TryParse(Item.InnerText, out var value);
                            AutoProfileRevertDefaultProfile = value;
                        }
                        catch
                        {
                            missingSetting = true;
                        }


                        var xmlDeviceOptions = m_Xdoc.SelectSingleNode("/Profile/DeviceOptions");
                        if (xmlDeviceOptions != null)
                        {
                            var xmlDS4Support = xmlDeviceOptions.SelectSingleNode("DS4SupportSettings");
                            if (xmlDS4Support != null)
                                try
                                {
                                    var item = xmlDS4Support.SelectSingleNode("Enabled");
                                    if (bool.TryParse(item?.InnerText ?? "", out var temp))
                                        DeviceOptions.DS4DeviceOpts.Enabled = temp;
                                }
                                catch
                                {
                                }

                            var xmlDualSenseSupport = xmlDeviceOptions.SelectSingleNode("DualSenseSupportSettings");
                            if (xmlDualSenseSupport != null)
                                try
                                {
                                    var item = xmlDualSenseSupport.SelectSingleNode("Enabled");
                                    if (bool.TryParse(item?.InnerText ?? "", out var temp))
                                        DeviceOptions.DualSenseOpts.Enabled = temp;
                                }
                                catch
                                {
                                }

                            var xmlSwitchProSupport = xmlDeviceOptions.SelectSingleNode("SwitchProSupportSettings");
                            if (xmlSwitchProSupport != null)
                                try
                                {
                                    var item = xmlSwitchProSupport.SelectSingleNode("Enabled");
                                    if (bool.TryParse(item?.InnerText ?? "", out var temp))
                                        DeviceOptions.SwitchProDeviceOpts.Enabled = temp;
                                }
                                catch
                                {
                                }

                            var xmlJoyConSupport = xmlDeviceOptions.SelectSingleNode("JoyConSupportSettings");
                            if (xmlJoyConSupport != null)
                            {
                                try
                                {
                                    var item = xmlJoyConSupport.SelectSingleNode("Enabled");
                                    if (bool.TryParse(item?.InnerText ?? "", out var temp))
                                        DeviceOptions.JoyConDeviceOpts.Enabled = temp;
                                }
                                catch
                                {
                                }

                                try
                                {
                                    var item = xmlJoyConSupport.SelectSingleNode("LinkMode");
                                    if (Enum.TryParse(item?.InnerText ?? "", out JoyConDeviceOptions.LinkMode temp))
                                        DeviceOptions.JoyConDeviceOpts.LinkedMode = temp;
                                }
                                catch
                                {
                                }

                                try
                                {
                                    var item = xmlJoyConSupport.SelectSingleNode("JoinedGyroProvider");
                                    if (Enum.TryParse(item?.InnerText ?? "",
                                        out JoyConDeviceOptions.JoinedGyroProvider temp))
                                        DeviceOptions.JoyConDeviceOpts.JoinGyroProv = temp;
                                }
                                catch
                                {
                                }
                            }
                        }

                        for (var i = 0; i < MAX_DS4_CONTROLLER_COUNT; i++)
                            try
                            {
                                Item = m_Xdoc.SelectSingleNode("/Profile/CustomLed" + (i + 1));
                                var ss = Item.InnerText.Split(':');
                                bool.TryParse(ss[0], out LightbarSettingInfo[i].ds4winSettings.useCustomLed);
                                DS4Color.TryParse(ss[1], ref LightbarSettingInfo[i].ds4winSettings.m_CustomLed);
                            }
                            catch
                            {
                                LightbarSettingInfo[i].ds4winSettings.useCustomLed = false;
                                LightbarSettingInfo[i].ds4winSettings.m_CustomLed = new DS4Color(Color.Blue);
                                missingSetting = true;
                            }
                    }
                }
                catch
                {
                }

                if (missingSetting)
                    Save();

                if (Loaded)
                {
                    var custom_exe_name_path = Path.Combine(ExecutableDirectory, CUSTOM_EXE_CONFIG_FILENAME);
                    var fakeExeFileExists = File.Exists(custom_exe_name_path);
                    if (fakeExeFileExists)
                    {
                        var fake_exe_name = File.ReadAllText(custom_exe_name_path).Trim();
                        var valid = !(fake_exe_name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0);
                        if (valid) FakeExeFileName = fake_exe_name;
                    }
                }

                return Loaded;
            }

            public bool Save()
            {
                var Saved = true;

                XmlNode Node;

                m_Xdoc.RemoveAll();

                Node = m_Xdoc.CreateXmlDeclaration("1.0", "utf-8", string.Empty);
                m_Xdoc.AppendChild(Node);

                Node = m_Xdoc.CreateComment(string.Format(" Profile Configuration Data. {0} ", DateTime.Now));
                m_Xdoc.AppendChild(Node);

                Node = m_Xdoc.CreateComment(string.Format(" Made with DS4Windows version {0} ",
                    ExecutableProductVersion));
                m_Xdoc.AppendChild(Node);

                Node = m_Xdoc.CreateWhitespace("\r\n");
                m_Xdoc.AppendChild(Node);

                var rootElement = m_Xdoc.CreateElement("Profile", null);
                rootElement.SetAttribute("app_version", ExecutableProductVersion);
                rootElement.SetAttribute("config_version", APP_CONFIG_VERSION.ToString());

                // Ex Mode (+1 line)
                var xmlUseExclNode = m_Xdoc.CreateNode(XmlNodeType.Element, "useExclusiveMode", null);
                xmlUseExclNode.InnerText = UseExclusiveMode.ToString();
                rootElement.AppendChild(xmlUseExclNode);
                var xmlStartMinimized = m_Xdoc.CreateNode(XmlNodeType.Element, "startMinimized", null);
                xmlStartMinimized.InnerText = StartMinimized.ToString();
                rootElement.AppendChild(xmlStartMinimized);
                var xmlminToTaskbar = m_Xdoc.CreateNode(XmlNodeType.Element, "minimizeToTaskbar", null);
                xmlminToTaskbar.InnerText = MinToTaskBar.ToString();
                rootElement.AppendChild(xmlminToTaskbar);
                var xmlFormWidth = m_Xdoc.CreateNode(XmlNodeType.Element, "formWidth", null);
                xmlFormWidth.InnerText = FormWidth.ToString();
                rootElement.AppendChild(xmlFormWidth);
                var xmlFormHeight = m_Xdoc.CreateNode(XmlNodeType.Element, "formHeight", null);
                xmlFormHeight.InnerText = FormHeight.ToString();
                rootElement.AppendChild(xmlFormHeight);
                var xmlFormLocationX = m_Xdoc.CreateNode(XmlNodeType.Element, "formLocationX", null);
                xmlFormLocationX.InnerText = FormLocationX.ToString();
                rootElement.AppendChild(xmlFormLocationX);
                var xmlFormLocationY = m_Xdoc.CreateNode(XmlNodeType.Element, "formLocationY", null);
                xmlFormLocationY.InnerText = FormLocationY.ToString();
                rootElement.AppendChild(xmlFormLocationY);

                for (var i = 0; i < MAX_DS4_CONTROLLER_COUNT; i++)
                {
                    var contTagName = $"Controller{i + 1}";
                    var xmlControllerNode = m_Xdoc.CreateNode(XmlNodeType.Element, contTagName, null);
                    xmlControllerNode.InnerText = !LinkedProfileCheck[i] ? ProfilePath[i] : OlderProfilePath[i];
                    if (!string.IsNullOrEmpty(xmlControllerNode.InnerText)) rootElement.AppendChild(xmlControllerNode);
                }

                var xmlLastChecked = m_Xdoc.CreateNode(XmlNodeType.Element, "LastChecked", null);
                xmlLastChecked.InnerText = LastChecked.ToString();
                rootElement.AppendChild(xmlLastChecked);
                var xmlCheckWhen = m_Xdoc.CreateNode(XmlNodeType.Element, "CheckWhen", null);
                xmlCheckWhen.InnerText = CheckWhen.ToString();
                rootElement.AppendChild(xmlCheckWhen);
                if (!string.IsNullOrEmpty(lastVersionChecked))
                {
                    var xmlLastVersionChecked = m_Xdoc.CreateNode(XmlNodeType.Element, "LastVersionChecked", null);
                    xmlLastVersionChecked.InnerText = lastVersionChecked;
                    rootElement.AppendChild(xmlLastVersionChecked);
                }

                var xmlNotifications = m_Xdoc.CreateNode(XmlNodeType.Element, "Notifications", null);
                xmlNotifications.InnerText = Notifications.ToString();
                rootElement.AppendChild(xmlNotifications);
                var xmlDisconnectBT = m_Xdoc.CreateNode(XmlNodeType.Element, "DisconnectBTAtStop", null);
                xmlDisconnectBT.InnerText = DisconnectBluetoothAtStop.ToString();
                rootElement.AppendChild(xmlDisconnectBT);
                var xmlSwipeProfiles = m_Xdoc.CreateNode(XmlNodeType.Element, "SwipeProfiles", null);
                xmlSwipeProfiles.InnerText = SwipeProfiles.ToString();
                rootElement.AppendChild(xmlSwipeProfiles);
                //XmlNode xmlDS4Mapping = m_Xdoc.CreateNode(XmlNodeType.Element, "UseDS4ForMapping", null); xmlDS4Mapping.InnerText = ds4Mapping.ToString(); rootElement.AppendChild(xmlDS4Mapping);
                var xmlQuickCharge = m_Xdoc.CreateNode(XmlNodeType.Element, "QuickCharge", null);
                xmlQuickCharge.InnerText = QuickCharge.ToString();
                rootElement.AppendChild(xmlQuickCharge);
                var xmlCloseMini = m_Xdoc.CreateNode(XmlNodeType.Element, "CloseMinimizes", null);
                xmlCloseMini.InnerText = CloseMini.ToString();
                rootElement.AppendChild(xmlCloseMini);
                var xmlUseLang = m_Xdoc.CreateNode(XmlNodeType.Element, "UseLang", null);
                xmlUseLang.InnerText = UseLang;
                rootElement.AppendChild(xmlUseLang);
                var xmlDownloadLang = m_Xdoc.CreateNode(XmlNodeType.Element, "DownloadLang", null);
                xmlDownloadLang.InnerText = DownloadLang.ToString();
                rootElement.AppendChild(xmlDownloadLang);
                var xmlFlashWhenLate = m_Xdoc.CreateNode(XmlNodeType.Element, "FlashWhenLate", null);
                xmlFlashWhenLate.InnerText = FlashWhenLate.ToString();
                rootElement.AppendChild(xmlFlashWhenLate);
                var xmlFlashWhenLateAt = m_Xdoc.CreateNode(XmlNodeType.Element, "FlashWhenLateAt", null);
                xmlFlashWhenLateAt.InnerText = FlashWhenLateAt.ToString();
                rootElement.AppendChild(xmlFlashWhenLateAt);
                var xmlAppIconChoice = m_Xdoc.CreateNode(XmlNodeType.Element, "AppIcon", null);
                xmlAppIconChoice.InnerText = UseIconChoice.ToString();
                rootElement.AppendChild(xmlAppIconChoice);
                var xmlAppThemeChoice = m_Xdoc.CreateNode(XmlNodeType.Element, "AppTheme", null);
                xmlAppThemeChoice.InnerText = ThemeChoice.ToString();
                rootElement.AppendChild(xmlAppThemeChoice);
                var xmlUseUDPServ = m_Xdoc.CreateNode(XmlNodeType.Element, "UseUDPServer", null);
                xmlUseUDPServ.InnerText = UseUdpServer.ToString();
                rootElement.AppendChild(xmlUseUDPServ);
                var xmlUDPServPort = m_Xdoc.CreateNode(XmlNodeType.Element, "UDPServerPort", null);
                xmlUDPServPort.InnerText = UdpServerPort.ToString();
                rootElement.AppendChild(xmlUDPServPort);
                var xmlUDPServListenAddress = m_Xdoc.CreateNode(XmlNodeType.Element, "UDPServerListenAddress", null);
                xmlUDPServListenAddress.InnerText = UdpServerListenAddress;
                rootElement.AppendChild(xmlUDPServListenAddress);

                var xmlUdpServerSmoothElement = m_Xdoc.CreateElement("UDPServerSmoothingOptions");
                var xmlUDPUseSmoothing = m_Xdoc.CreateElement("UseSmoothing");
                xmlUDPUseSmoothing.InnerText = UseUdpSmoothing.ToString();
                xmlUdpServerSmoothElement.AppendChild(xmlUDPUseSmoothing);
                var xmlUDPSmoothMinCutoff = m_Xdoc.CreateElement("UdpSmoothMinCutoff");
                xmlUDPSmoothMinCutoff.InnerText = UdpSmoothingMincutoff.ToString();
                xmlUdpServerSmoothElement.AppendChild(xmlUDPSmoothMinCutoff);
                var xmlUDPSmoothBeta = m_Xdoc.CreateElement("UdpSmoothBeta");
                xmlUDPSmoothBeta.InnerText = UdpSmoothingBeta.ToString();
                xmlUdpServerSmoothElement.AppendChild(xmlUDPSmoothBeta);
                rootElement.AppendChild(xmlUdpServerSmoothElement);

                var xmlUseCustomSteamFolder = m_Xdoc.CreateNode(XmlNodeType.Element, "UseCustomSteamFolder", null);
                xmlUseCustomSteamFolder.InnerText = UseCustomSteamFolder.ToString();
                rootElement.AppendChild(xmlUseCustomSteamFolder);
                var xmlCustomSteamFolder = m_Xdoc.CreateNode(XmlNodeType.Element, "CustomSteamFolder", null);
                xmlCustomSteamFolder.InnerText = CustomSteamFolder;
                rootElement.AppendChild(xmlCustomSteamFolder);
                var xmlAutoProfileRevertDefaultProfile =
                    m_Xdoc.CreateNode(XmlNodeType.Element, "AutoProfileRevertDefaultProfile", null);
                xmlAutoProfileRevertDefaultProfile.InnerText = AutoProfileRevertDefaultProfile.ToString();
                rootElement.AppendChild(xmlAutoProfileRevertDefaultProfile);

                var xmlDeviceOptions = m_Xdoc.CreateElement("DeviceOptions", null);
                var xmlDS4Support = m_Xdoc.CreateElement("DS4SupportSettings", null);
                var xmlDS4Enabled = m_Xdoc.CreateElement("Enabled", null);
                xmlDS4Enabled.InnerText = DeviceOptions.DS4DeviceOpts.Enabled.ToString();
                xmlDS4Support.AppendChild(xmlDS4Enabled);
                xmlDeviceOptions.AppendChild(xmlDS4Support);

                var xmlDualSenseSupport = m_Xdoc.CreateElement("DualSenseSupportSettings", null);
                var xmlDualSenseEnabled = m_Xdoc.CreateElement("Enabled", null);
                xmlDualSenseEnabled.InnerText = DeviceOptions.DualSenseOpts.Enabled.ToString();
                xmlDualSenseSupport.AppendChild(xmlDualSenseEnabled);

                xmlDeviceOptions.AppendChild(xmlDualSenseSupport);

                var xmlSwitchProSupport = m_Xdoc.CreateElement("SwitchProSupportSettings", null);
                var xmlSwitchProEnabled = m_Xdoc.CreateElement("Enabled", null);
                xmlSwitchProEnabled.InnerText = DeviceOptions.SwitchProDeviceOpts.Enabled.ToString();
                xmlSwitchProSupport.AppendChild(xmlSwitchProEnabled);

                xmlDeviceOptions.AppendChild(xmlSwitchProSupport);

                var xmlJoyConSupport = m_Xdoc.CreateElement("JoyConSupportSettings", null);
                var xmlJoyconEnabled = m_Xdoc.CreateElement("Enabled", null);
                xmlJoyconEnabled.InnerText = DeviceOptions.JoyConDeviceOpts.Enabled.ToString();
                xmlJoyConSupport.AppendChild(xmlJoyconEnabled);
                var xmlJoyconLinkMode = m_Xdoc.CreateElement("LinkMode", null);
                xmlJoyconLinkMode.InnerText = DeviceOptions.JoyConDeviceOpts.LinkedMode.ToString();
                xmlJoyConSupport.AppendChild(xmlJoyconLinkMode);
                var xmlJoyconUnionGyro = m_Xdoc.CreateElement("JoinedGyroProvider", null);
                xmlJoyconUnionGyro.InnerText = DeviceOptions.JoyConDeviceOpts.JoinGyroProv.ToString();
                xmlJoyConSupport.AppendChild(xmlJoyconUnionGyro);

                xmlDeviceOptions.AppendChild(xmlJoyConSupport);

                rootElement.AppendChild(xmlDeviceOptions);

                for (var i = 0; i < MAX_DS4_CONTROLLER_COUNT; i++)
                {
                    var xmlCustomLed = m_Xdoc.CreateNode(XmlNodeType.Element, "CustomLed" + (1 + i), null);
                    xmlCustomLed.InnerText = LightbarSettingInfo[i].ds4winSettings.useCustomLed + ":" +
                                             LightbarSettingInfo[i].ds4winSettings.m_CustomLed.red + "," +
                                             LightbarSettingInfo[i].ds4winSettings.m_CustomLed.green + "," +
                                             LightbarSettingInfo[i].ds4winSettings.m_CustomLed.blue;
                    rootElement.AppendChild(xmlCustomLed);
                }

                m_Xdoc.AppendChild(rootElement);

                try
                {
                    m_Xdoc.Save(ProfilesPath);
                }
                catch (UnauthorizedAccessException)
                {
                    Saved = false;
                }

                var adminNeeded = IsAdminNeeded;
                if (Saved &&
                    (!adminNeeded || adminNeeded && IsAdministrator))
                {
                    var custom_exe_name_path = Path.Combine(ExecutableDirectory, CUSTOM_EXE_CONFIG_FILENAME);
                    var fakeExeFileExists = File.Exists(custom_exe_name_path);
                    if (!string.IsNullOrEmpty(FakeExeFileName) || fakeExeFileExists)
                        File.WriteAllText(custom_exe_name_path, FakeExeFileName);
                }

                return Saved;
            }

            public bool SaveAction(string name, string controls, int mode, string details, bool edit,
                string extras = "")
            {
                var saved = true;
                if (!File.Exists(ActionsPath))
                    CreateAction();

                try
                {
                    m_Xdoc.Load(ActionsPath);
                }
                catch (XmlException)
                {
                    // XML file has become corrupt. Start from scratch
                    AppLogger.LogToGui(Resources.XMLActionsCorrupt, true);
                    m_Xdoc.RemoveAll();
                    PrepareActionsXml(m_Xdoc);
                }

                XmlNode Node;

                Node = m_Xdoc.CreateComment(string.Format(" Special Actions Configuration Data. {0} ", DateTime.Now));
                foreach (XmlNode node in m_Xdoc.SelectNodes("//comment()"))
                    node.ParentNode.ReplaceChild(Node, node);

                Node = m_Xdoc.SelectSingleNode("Actions");
                var el = m_Xdoc.CreateElement("Action");
                el.SetAttribute("Name", name);
                el.AppendChild(m_Xdoc.CreateElement("Trigger")).InnerText = controls;
                switch (mode)
                {
                    case 1:
                        el.AppendChild(m_Xdoc.CreateElement("Type")).InnerText = "Macro";
                        el.AppendChild(m_Xdoc.CreateElement("Details")).InnerText = details;
                        if (extras != string.Empty)
                            el.AppendChild(m_Xdoc.CreateElement("Extras")).InnerText = extras;
                        break;
                    case 2:
                        el.AppendChild(m_Xdoc.CreateElement("Type")).InnerText = "Program";
                        el.AppendChild(m_Xdoc.CreateElement("Details")).InnerText = details.Split('?')[0];
                        el.AppendChild(m_Xdoc.CreateElement("Arguements")).InnerText = extras;
                        el.AppendChild(m_Xdoc.CreateElement("Delay")).InnerText = details.Split('?')[1];
                        break;
                    case 3:
                        el.AppendChild(m_Xdoc.CreateElement("Type")).InnerText = "Profile";
                        el.AppendChild(m_Xdoc.CreateElement("Details")).InnerText = details;
                        el.AppendChild(m_Xdoc.CreateElement("UnloadTrigger")).InnerText = extras;
                        break;
                    case 4:
                        el.AppendChild(m_Xdoc.CreateElement("Type")).InnerText = "Key";
                        el.AppendChild(m_Xdoc.CreateElement("Details")).InnerText = details;
                        if (!string.IsNullOrEmpty(extras))
                        {
                            var exts = extras.Split('\n');
                            el.AppendChild(m_Xdoc.CreateElement("UnloadTrigger")).InnerText = exts[1];
                            el.AppendChild(m_Xdoc.CreateElement("UnloadStyle")).InnerText = exts[0];
                        }

                        break;
                    case 5:
                        el.AppendChild(m_Xdoc.CreateElement("Type")).InnerText = "DisconnectBT";
                        el.AppendChild(m_Xdoc.CreateElement("Details")).InnerText = details;
                        break;
                    case 6:
                        el.AppendChild(m_Xdoc.CreateElement("Type")).InnerText = "BatteryCheck";
                        el.AppendChild(m_Xdoc.CreateElement("Details")).InnerText = details;
                        break;
                    case 7:
                        el.AppendChild(m_Xdoc.CreateElement("Type")).InnerText = "MultiAction";
                        el.AppendChild(m_Xdoc.CreateElement("Details")).InnerText = details;
                        break;
                    case 8:
                        el.AppendChild(m_Xdoc.CreateElement("Type")).InnerText = "SASteeringWheelEmulationCalibrate";
                        el.AppendChild(m_Xdoc.CreateElement("Details")).InnerText = details;
                        break;
                }

                if (edit)
                {
                    var oldxmlprocess = m_Xdoc.SelectSingleNode("/Actions/Action[@Name=\"" + name + "\"]");
                    Node.ReplaceChild(el, oldxmlprocess);
                }
                else
                {
                    Node.AppendChild(el);
                }

                m_Xdoc.AppendChild(Node);
                try
                {
                    m_Xdoc.Save(ActionsPath);
                }
                catch
                {
                    saved = false;
                }

                LoadActions();
                return saved;
            }

            public void RemoveAction(string name)
            {
                m_Xdoc.Load(ActionsPath);
                var Node = m_Xdoc.SelectSingleNode("Actions");
                var Item = m_Xdoc.SelectSingleNode("/Actions/Action[@Name=\"" + name + "\"]");
                if (Item != null)
                    Node.RemoveChild(Item);

                m_Xdoc.AppendChild(Node);
                m_Xdoc.Save(ActionsPath);
                LoadActions();
            }

            public bool LoadActions()
            {
                var saved = true;
                if (!File.Exists(Path.Combine(RuntimeAppDataPath, Constants.ActionsFileName)))
                {
                    SaveAction("Disconnect Controller", "PS/Options", 5, "0", false);
                    saved = false;
                }

                try
                {
                    Actions.Clear();
                    var doc = new XmlDocument();
                    doc.Load(Path.Combine(RuntimeAppDataPath, Constants.ActionsFileName));
                    var actionslist = doc.SelectNodes("Actions/Action");
                    string name, controls, type, details, extras, extras2;
                    Mapping.actionDone.Clear();
                    foreach (XmlNode x in actionslist)
                    {
                        name = x.Attributes["Name"].Value;
                        controls = x.ChildNodes[0].InnerText;
                        type = x.ChildNodes[1].InnerText;
                        details = x.ChildNodes[2].InnerText;
                        Mapping.actionDone.Add(new Mapping.ActionState());
                        if (type == "Profile")
                        {
                            extras = x.ChildNodes[3].InnerText;
                            Actions.Add(new SpecialAction(name, controls, type, details, 0, extras));
                        }
                        else if (type == "Macro")
                        {
                            if (x.ChildNodes[3] != null) extras = x.ChildNodes[3].InnerText;
                            else extras = string.Empty;
                            Actions.Add(new SpecialAction(name, controls, type, details, 0, extras));
                        }
                        else if (type == "Key")
                        {
                            if (x.ChildNodes[3] != null)
                            {
                                extras = x.ChildNodes[3].InnerText;
                                extras2 = x.ChildNodes[4].InnerText;
                            }
                            else
                            {
                                extras = string.Empty;
                                extras2 = string.Empty;
                            }

                            if (!string.IsNullOrEmpty(extras))
                                Actions.Add(
                                    new SpecialAction(name, controls, type, details, 0, extras2 + '\n' + extras));
                            else
                                Actions.Add(new SpecialAction(name, controls, type, details));
                        }
                        else if (type == "DisconnectBT")
                        {
                            double doub;
                            if (double.TryParse(details, NumberStyles.Float, ConfigFileDecimalCulture, out doub))
                                Actions.Add(new SpecialAction(name, controls, type, "", doub));
                            else
                                Actions.Add(new SpecialAction(name, controls, type, ""));
                        }
                        else if (type == "BatteryCheck")
                        {
                            double doub;
                            if (double.TryParse(details.Split('|')[0], NumberStyles.Float, ConfigFileDecimalCulture,
                                out doub))
                                Actions.Add(new SpecialAction(name, controls, type, details, doub));
                            else if (double.TryParse(details.Split(',')[0], NumberStyles.Float,
                                ConfigFileDecimalCulture, out doub))
                                Actions.Add(new SpecialAction(name, controls, type, details, doub));
                            else
                                Actions.Add(new SpecialAction(name, controls, type, details));
                        }
                        else if (type == "Program")
                        {
                            double doub;
                            if (x.ChildNodes[3] != null)
                            {
                                extras = x.ChildNodes[3].InnerText;
                                if (double.TryParse(x.ChildNodes[4].InnerText, NumberStyles.Float,
                                    ConfigFileDecimalCulture, out doub))
                                    Actions.Add(new SpecialAction(name, controls, type, details, doub, extras));
                                else
                                    Actions.Add(new SpecialAction(name, controls, type, details, 0, extras));
                            }
                            else
                            {
                                Actions.Add(new SpecialAction(name, controls, type, details));
                            }
                        }
                        else if (type == "XboxGameDVR" || type == "MultiAction")
                        {
                            Actions.Add(new SpecialAction(name, controls, type, details));
                        }
                        else if (type == "SASteeringWheelEmulationCalibrate")
                        {
                            double doub;
                            if (double.TryParse(details, NumberStyles.Float, ConfigFileDecimalCulture, out doub))
                                Actions.Add(new SpecialAction(name, controls, type, "", doub));
                            else
                                Actions.Add(new SpecialAction(name, controls, type, ""));
                        }
                    }
                }
                catch
                {
                    saved = false;
                }

                return saved;
            }

            public bool CreateLinkedProfiles()
            {
                var saved = true;
                var m_Xdoc = new XmlDocument();
                XmlNode Node;

                Node = m_Xdoc.CreateXmlDeclaration("1.0", "utf-8", string.Empty);
                m_Xdoc.AppendChild(Node);

                Node = m_Xdoc.CreateComment(string.Format(" Mac Address and Profile Linking Data. {0} ", DateTime.Now));
                m_Xdoc.AppendChild(Node);

                Node = m_Xdoc.CreateWhitespace("\r\n");
                m_Xdoc.AppendChild(Node);

                Node = m_Xdoc.CreateNode(XmlNodeType.Element, "LinkedControllers", "");
                m_Xdoc.AppendChild(Node);

                try
                {
                    m_Xdoc.Save(LinkedProfilesPath);
                }
                catch (UnauthorizedAccessException)
                {
                    AppLogger.LogToGui("Unauthorized Access - Save failed to path: " + LinkedProfilesPath, false);
                    saved = false;
                }

                return saved;
            }

            public bool LoadLinkedProfiles()
            {
                var loaded = true;
                if (File.Exists(LinkedProfilesPath))
                {
                    var linkedXdoc = new XmlDocument();
                    XmlNode Node;
                    linkedXdoc.Load(LinkedProfilesPath);
                    linkedProfiles.Clear();

                    try
                    {
                        Node = linkedXdoc.SelectSingleNode("/LinkedControllers");
                        var links = Node.ChildNodes;
                        for (int i = 0, listLen = links.Count; i < listLen; i++)
                        {
                            var current = links[i];
                            var serial = current.Name.Replace("MAC", string.Empty);
                            var profile = current.InnerText;
                            linkedProfiles[serial] = profile;
                        }
                    }
                    catch
                    {
                        loaded = false;
                    }
                }
                else
                {
                    AppLogger.LogToGui("LinkedProfiles.xml can't be found.", false);
                    loaded = false;
                }

                return loaded;
            }

            public bool SaveLinkedProfiles()
            {
                var saved = true;
                if (File.Exists(LinkedProfilesPath))
                {
                    var linkedXdoc = new XmlDocument();
                    XmlNode Node;

                    Node = linkedXdoc.CreateXmlDeclaration("1.0", "utf-8", string.Empty);
                    linkedXdoc.AppendChild(Node);

                    Node = linkedXdoc.CreateComment(string.Format(" Mac Address and Profile Linking Data. {0} ",
                        DateTime.Now));
                    linkedXdoc.AppendChild(Node);

                    Node = linkedXdoc.CreateWhitespace("\r\n");
                    linkedXdoc.AppendChild(Node);

                    Node = linkedXdoc.CreateNode(XmlNodeType.Element, "LinkedControllers", "");
                    linkedXdoc.AppendChild(Node);

                    var serials = linkedProfiles.Keys;
                    //for (int i = 0, itemCount = linkedProfiles.Count; i < itemCount; i++)
                    for (var serialEnum = serials.GetEnumerator(); serialEnum.MoveNext();)
                    {
                        //string serial = serials.ElementAt(i);
                        var serial = serialEnum.Current;
                        var profile = linkedProfiles[serial];
                        var link = linkedXdoc.CreateElement("MAC" + serial);
                        link.InnerText = profile;
                        Node.AppendChild(link);
                    }

                    try
                    {
                        linkedXdoc.Save(LinkedProfilesPath);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        AppLogger.LogToGui("Unauthorized Access - Save failed to path: " + LinkedProfilesPath, false);
                        saved = false;
                    }
                }
                else
                {
                    saved = CreateLinkedProfiles();
                    saved = saved && SaveLinkedProfiles();
                }

                return saved;
            }

            public bool CreateControllerConfigs()
            {
                var saved = true;
                var configXdoc = new XmlDocument();
                XmlNode Node;

                Node = configXdoc.CreateXmlDeclaration("1.0", "utf-8", string.Empty);
                configXdoc.AppendChild(Node);

                Node = configXdoc.CreateComment(string.Format(" Controller config data. {0} ", DateTime.Now));
                configXdoc.AppendChild(Node);

                Node = configXdoc.CreateWhitespace("\r\n");
                configXdoc.AppendChild(Node);

                Node = configXdoc.CreateNode(XmlNodeType.Element, "Controllers", "");
                configXdoc.AppendChild(Node);

                try
                {
                    configXdoc.Save(ControllerConfigsPath);
                }
                catch (UnauthorizedAccessException)
                {
                    AppLogger.LogToGui("Unauthorized Access - Save failed to path: " + ControllerConfigsPath, false);
                    saved = false;
                }

                return saved;
            }

            public bool LoadControllerConfigsForDevice(DS4Device device)
            {
                var loaded = false;

                if (device == null) return false;
                if (!File.Exists(ControllerConfigsPath)) CreateControllerConfigs();

                try
                {
                    var xmlDoc = new XmlDocument();
                    xmlDoc.Load(ControllerConfigsPath);

                    var node = xmlDoc.SelectSingleNode("/Controllers/Controller[@Mac=\"" + device.getMacAddress() +
                                                       "\"]");
                    if (node != null)
                    {
                        int intValue;
                        if (int.TryParse(node["wheelCenterPoint"]?.InnerText.Split(',')[0] ?? "", out intValue))
                            device.wheelCenterPoint.X = intValue;
                        if (int.TryParse(node["wheelCenterPoint"]?.InnerText.Split(',')[1] ?? "", out intValue))
                            device.wheelCenterPoint.Y = intValue;
                        if (int.TryParse(node["wheel90DegPointLeft"]?.InnerText.Split(',')[0] ?? "", out intValue))
                            device.wheel90DegPointLeft.X = intValue;
                        if (int.TryParse(node["wheel90DegPointLeft"]?.InnerText.Split(',')[1] ?? "", out intValue))
                            device.wheel90DegPointLeft.Y = intValue;
                        if (int.TryParse(node["wheel90DegPointRight"]?.InnerText.Split(',')[0] ?? "", out intValue))
                            device.wheel90DegPointRight.X = intValue;
                        if (int.TryParse(node["wheel90DegPointRight"]?.InnerText.Split(',')[1] ?? "", out intValue))
                            device.wheel90DegPointRight.Y = intValue;

                        device.optionsStore.LoadSettings(xmlDoc, node);

                        loaded = true;
                    }
                }
                catch
                {
                    AppLogger.LogToGui("ControllerConfigs.xml can't be found.", false);
                    loaded = false;
                }

                return loaded;
            }

            public bool SaveControllerConfigsForDevice(DS4Device device)
            {
                var saved = true;

                if (device == null) return false;
                if (!File.Exists(ControllerConfigsPath)) CreateControllerConfigs();

                try
                {
                    //XmlNode node = null;
                    var xmlDoc = new XmlDocument();
                    xmlDoc.Load(ControllerConfigsPath);

                    var node = xmlDoc.SelectSingleNode("/Controllers/Controller[@Mac=\"" + device.getMacAddress() +
                                                       "\"]");
                    var xmlControllersNode = xmlDoc.SelectSingleNode("/Controllers");
                    if (node == null)
                    {
                        var el = xmlDoc.CreateElement("Controller");
                        node = xmlControllersNode.AppendChild(el);
                    }
                    else
                    {
                        node.RemoveAll();
                    }

                    var macAttr = xmlDoc.CreateAttribute("Mac");
                    macAttr.Value = device.getMacAddress();
                    node.Attributes.Append(macAttr);

                    var contTypeAttr = xmlDoc.CreateAttribute("ControllerType");
                    contTypeAttr.Value = device.DeviceType.ToString();
                    node.Attributes.Append(contTypeAttr);

                    if (!device.wheelCenterPoint.IsEmpty)
                    {
                        var wheelCenterEl = xmlDoc.CreateElement("wheelCenterPoint");
                        wheelCenterEl.InnerText = $"{device.wheelCenterPoint.X},{device.wheelCenterPoint.Y}";
                        node.AppendChild(wheelCenterEl);

                        var wheel90DegPointLeftEl = xmlDoc.CreateElement("wheel90DegPointLeft");
                        wheel90DegPointLeftEl.InnerText =
                            $"{device.wheel90DegPointLeft.X},{device.wheel90DegPointLeft.Y}";
                        node.AppendChild(wheel90DegPointLeftEl);

                        var wheel90DegPointRightEl = xmlDoc.CreateElement("wheel90DegPointRight");
                        wheel90DegPointRightEl.InnerText =
                            $"{device.wheel90DegPointRight.X},{device.wheel90DegPointRight.Y}";
                        node.AppendChild(wheel90DegPointRightEl);
                    }

                    device.optionsStore.PersistSettings(xmlDoc, node);

                    // Remove old elements
                    xmlDoc.RemoveAll();

                    XmlNode Node;
                    Node = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", string.Empty);
                    xmlDoc.AppendChild(Node);

                    Node = xmlDoc.CreateComment(string.Format(" Controller config data. {0} ", DateTime.Now));
                    xmlDoc.AppendChild(Node);

                    Node = xmlDoc.CreateWhitespace("\r\n");
                    xmlDoc.AppendChild(Node);

                    // Write old Controllers node back in
                    xmlDoc.AppendChild(xmlControllersNode);

                    // Save XML to file
                    xmlDoc.Save(ControllerConfigsPath);
                }
                catch (UnauthorizedAccessException)
                {
                    AppLogger.LogToGui("Unauthorized Access - Save failed to path: " + ControllerConfigsPath, false);
                    saved = false;
                }

                return saved;
            }

            public void UpdateDs4ControllerSetting(int deviceNum, string buttonName, bool shift, object action,
                string exts,
                DS4KeyType kt, int trigger = 0)
            {
                DS4Controls dc;
                if (buttonName.StartsWith("bn"))
                    dc = GetDs4ControlsByName(buttonName);
                else
                    dc = (DS4Controls)Enum.Parse(typeof(DS4Controls), buttonName, true);

                var temp = (int)dc;
                if (temp > 0)
                {
                    var index = temp - 1;
                    var dcs = Ds4Settings[deviceNum][index];
                    dcs.UpdateSettings(shift, action, exts, kt, trigger);
                    RefreshActionAlias(dcs, shift);
                }
            }

            public void UpdateDs4ControllerExtra(int deviceNum, string buttonName, bool shift, string exts)
            {
                DS4Controls dc;
                if (buttonName.StartsWith("bn"))
                    dc = GetDs4ControlsByName(buttonName);
                else
                    dc = (DS4Controls)Enum.Parse(typeof(DS4Controls), buttonName, true);

                var temp = (int)dc;
                if (temp > 0)
                {
                    var index = temp - 1;
                    var dcs = Ds4Settings[deviceNum][index];
                    if (shift)
                        dcs.ShiftExtras = exts;
                    else
                        dcs.Extras = exts;
                }
            }

            public ControlActionData GetDs4Action(int deviceNum, string buttonName, bool shift)
            {
                DS4Controls dc;
                if (buttonName.StartsWith("bn"))
                    dc = GetDs4ControlsByName(buttonName);
                else
                    dc = (DS4Controls)Enum.Parse(typeof(DS4Controls), buttonName, true);

                var temp = (int)dc;
                if (temp > 0)
                {
                    var index = temp - 1;
                    var dcs = Ds4Settings[deviceNum][index];
                    if (shift)
                        return dcs.ShiftAction;
                    return dcs.ActionData;
                }

                return null;
            }

            public ControlActionData GetDs4Action(int deviceNum, DS4Controls dc, bool shift)
            {
                var temp = (int)dc;
                if (temp > 0)
                {
                    var index = temp - 1;
                    var dcs = Ds4Settings[deviceNum][index];
                    if (shift)
                        return dcs.ShiftAction;
                    return dcs.ActionData;
                }

                return null;
            }

            public string GetDs4Extra(int deviceNum, string buttonName, bool shift)
            {
                DS4Controls dc;
                if (buttonName.StartsWith("bn"))
                    dc = GetDs4ControlsByName(buttonName);
                else
                    dc = (DS4Controls)Enum.Parse(typeof(DS4Controls), buttonName, true);

                var temp = (int)dc;
                if (temp > 0)
                {
                    var index = temp - 1;
                    var dcs = Ds4Settings[deviceNum][index];
                    if (shift)
                        return dcs.ShiftExtras;
                    return dcs.Extras;
                }

                return null;
            }

            public DS4KeyType GetDs4KeyType(int deviceNum, string buttonName, bool shift)
            {
                DS4Controls dc;
                if (buttonName.StartsWith("bn"))
                    dc = GetDs4ControlsByName(buttonName);
                else
                    dc = (DS4Controls)Enum.Parse(typeof(DS4Controls), buttonName, true);

                var temp = (int)dc;
                if (temp > 0)
                {
                    var index = temp - 1;
                    var dcs = Ds4Settings[deviceNum][index];
                    if (shift)
                        return dcs.ShiftKeyType;
                    return dcs.KeyType;
                }

                return DS4KeyType.None;
            }

            public int GetDs4STrigger(int deviceNum, string buttonName)
            {
                DS4Controls dc;
                if (buttonName.StartsWith("bn"))
                    dc = GetDs4ControlsByName(buttonName);
                else
                    dc = (DS4Controls)Enum.Parse(typeof(DS4Controls), buttonName, true);

                var temp = (int)dc;
                if (temp > 0)
                {
                    var index = temp - 1;
                    var dcs = Ds4Settings[deviceNum][index];
                    return dcs.ShiftTrigger;
                }

                return 0;
            }

            public int GetDs4STrigger(int deviceNum, DS4Controls dc)
            {
                var temp = (int)dc;
                if (temp > 0)
                {
                    var index = temp - 1;
                    var dcs = Ds4Settings[deviceNum][index];
                    return dcs.ShiftTrigger;
                }

                return 0;
            }

            public DS4ControlSettings GetDs4ControllerSetting(int deviceNum, string buttonName)
            {
                DS4Controls dc;
                if (buttonName.StartsWith("bn"))
                    dc = GetDs4ControlsByName(buttonName);
                else
                    dc = (DS4Controls)Enum.Parse(typeof(DS4Controls), buttonName, true);

                var temp = (int)dc;
                if (temp > 0)
                {
                    var index = temp - 1;
                    var dcs = Ds4Settings[deviceNum][index];
                    return dcs;
                }

                return null;
            }

            public DS4ControlSettings GetDs4ControllerSetting(int deviceNum, DS4Controls dc)
            {
                var temp = (int)dc;
                if (temp > 0)
                {
                    var index = temp - 1;
                    var dcs = Ds4Settings[deviceNum][index];
                    return dcs;
                }

                return null;
            }

            public bool HasCustomActions(int deviceNum)
            {
                var ds4settingsList = Ds4Settings[deviceNum];
                for (int i = 0, settingsLen = ds4settingsList.Count; i < settingsLen; i++)
                {
                    var dcs = ds4settingsList[i];
                    if (dcs.ControlActionType != DS4ControlSettings.ActionType.Default ||
                        dcs.ShiftActionType != DS4ControlSettings.ActionType.Default)
                        return true;
                }

                return false;
            }

            public bool HasCustomExtras(int deviceNum)
            {
                var ds4settingsList = Ds4Settings[deviceNum];
                for (int i = 0, settingsLen = ds4settingsList.Count; i < settingsLen; i++)
                {
                    var dcs = ds4settingsList[i];
                    if (dcs.Extras != null || dcs.ShiftExtras != null)
                        return true;
                }

                return false;
            }

            public void LoadBlankDs4Profile(int device, bool launchprogram, ControlService control,
                string propath = "", bool xinputChange = true, bool postLoad = true)
            {
                PrepareBlankingProfile(device, control, out var xinputPlug, out var xinputStatus, xinputChange);

                var lsInfo = LSModInfo[device];
                lsInfo.deadZone = (int)(0.00 * 127);
                lsInfo.antiDeadZone = 0;
                lsInfo.maxZone = 100;

                var rsInfo = RSModInfo[device];
                rsInfo.deadZone = (int)(0.00 * 127);
                rsInfo.antiDeadZone = 0;
                rsInfo.maxZone = 100;

                var l2Info = L2ModInfo[device];
                l2Info.deadZone = (byte)(0.00 * 255);

                var r2Info = R2ModInfo[device];
                r2Info.deadZone = (byte)(0.00 * 255);

                OutputDeviceType[device] = OutContType.DS4;

                // If a device exists, make sure to transfer relevant profile device
                // options to device instance
                if (postLoad && device < MAX_DS4_CONTROLLER_COUNT)
                    PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }

            public void LoadBlankProfile(int device, bool launchprogram, ControlService control,
                string propath = "", bool xinputChange = true, bool postLoad = true)
            {
                var xinputPlug = false;
                var xinputStatus = false;

                var oldContType = ActiveOutDevType[device];

                // Make sure to reset currently set profile values before parsing
                ResetProfile(device);
                ResetMouseProperties(device, control);

                // Only change xinput devices under certain conditions. Avoid
                // performing this upon program startup before loading devices.
                if (xinputChange)
                    CheckOldDeviceStatus(device, control, oldContType,
                        out xinputPlug, out xinputStatus);

                foreach (var dcs in Ds4Settings[device])
                    dcs.Reset();

                ProfileActions[device].Clear();
                ContainsCustomAction[device] = false;
                ContainsCustomExtras[device] = false;

                // If a device exists, make sure to transfer relevant profile device
                // options to device instance
                if (postLoad && device < MAX_DS4_CONTROLLER_COUNT)
                    PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }

            public void LoadDefaultGamepadGyroProfile(int device, bool launchprogram, ControlService control,
                string propath = "", bool xinputChange = true, bool postLoad = true)
            {
                var xinputPlug = false;
                var xinputStatus = false;

                var oldContType = ActiveOutDevType[device];

                // Make sure to reset currently set profile values before parsing
                ResetProfile(device);
                ResetMouseProperties(device, control);

                // Only change xinput devices under certain conditions. Avoid
                // performing this upon program startup before loading devices.
                if (xinputChange)
                    CheckOldDeviceStatus(device, control, oldContType,
                        out xinputPlug, out xinputStatus);

                foreach (var dcs in Ds4Settings[device])
                    dcs.Reset();

                ProfileActions[device].Clear();
                ContainsCustomAction[device] = false;
                ContainsCustomExtras[device] = false;

                GyroOutputMode[device] = DS4Windows.GyroOutMode.MouseJoystick;
                SAMouseStickTriggers[device] = "4";
                SAMouseStickTriggerCond[device] = true;
                GyroMouseStickTriggerTurns[device] = false;
                GyroMouseStickInfo[device].useSmoothing = true;
                GyroMouseStickInfo[device].smoothingMethod = DS4Windows.GyroMouseStickInfo.SmoothingMethod.OneEuro;

                // If a device exists, make sure to transfer relevant profile device
                // options to device instance
                if (postLoad && device < MAX_DS4_CONTROLLER_COUNT)
                    PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }

            public void LoadDefaultDS4GamepadGyroProfile(int device, bool launchprogram, ControlService control,
                string propath = "", bool xinputChange = true, bool postLoad = true)
            {
                PrepareBlankingProfile(device, control, out var xinputPlug, out var xinputStatus, xinputChange);

                var lsInfo = LSModInfo[device];
                lsInfo.deadZone = (int)(0.00 * 127);
                lsInfo.antiDeadZone = 0;
                lsInfo.maxZone = 100;

                var rsInfo = RSModInfo[device];
                rsInfo.deadZone = (int)(0.00 * 127);
                rsInfo.antiDeadZone = 0;
                rsInfo.maxZone = 100;

                var l2Info = L2ModInfo[device];
                l2Info.deadZone = (byte)(0.00 * 255);

                var r2Info = R2ModInfo[device];
                r2Info.deadZone = (byte)(0.00 * 255);

                GyroOutputMode[device] = DS4Windows.GyroOutMode.MouseJoystick;
                SAMouseStickTriggers[device] = "4";
                SAMouseStickTriggerCond[device] = true;
                GyroMouseStickTriggerTurns[device] = false;
                GyroMouseStickInfo[device].useSmoothing = true;
                GyroMouseStickInfo[device].smoothingMethod = DS4Windows.GyroMouseStickInfo.SmoothingMethod.OneEuro;

                OutputDeviceType[device] = OutContType.DS4;

                // If a device exists, make sure to transfer relevant profile device
                // options to device instance
                if (postLoad && device < MAX_DS4_CONTROLLER_COUNT)
                    PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }

            public void LoadDefaultMixedGyroMouseProfile(int device, bool launchprogram, ControlService control,
                string propath = "", bool xinputChange = true, bool postLoad = true)
            {
                var xinputPlug = false;
                var xinputStatus = false;

                var oldContType = ActiveOutDevType[device];

                // Make sure to reset currently set profile values before parsing
                ResetProfile(device);
                ResetMouseProperties(device, control);

                // Only change xinput devices under certain conditions. Avoid
                // performing this upon program startup before loading devices.
                if (xinputChange)
                    CheckOldDeviceStatus(device, control, oldContType,
                        out xinputPlug, out xinputStatus);

                foreach (var dcs in Ds4Settings[device])
                    dcs.Reset();

                ProfileActions[device].Clear();
                ContainsCustomAction[device] = false;
                ContainsCustomExtras[device] = false;

                GyroOutputMode[device] = DS4Windows.GyroOutMode.Mouse;
                SATriggers[device] = "4";
                SATriggerCond[device] = true;
                GyroTriggerTurns[device] = false;
                GyroMouseInfo[device].enableSmoothing = true;
                GyroMouseInfo[device].smoothingMethod = DS4Windows.GyroMouseInfo.SmoothingMethod.OneEuro;

                var rsInfo = RSModInfo[device];
                rsInfo.deadZone = (int)(0.10 * 127);
                rsInfo.antiDeadZone = 0;
                rsInfo.maxZone = 90;

                // If a device exists, make sure to transfer relevant profile device
                // options to device instance
                if (postLoad && device < MAX_DS4_CONTROLLER_COUNT)
                    PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }

            public void LoadDefaultDs4MixedGyroMouseProfile(int device, bool launchprogram, ControlService control,
                string propath = "", bool xinputChange = true, bool postLoad = true)
            {
                PrepareBlankingProfile(device, control, out var xinputPlug, out var xinputStatus, xinputChange);

                var lsInfo = LSModInfo[device];
                lsInfo.deadZone = (int)(0.00 * 127);
                lsInfo.antiDeadZone = 0;
                lsInfo.maxZone = 100;

                var rsInfo = RSModInfo[device];
                rsInfo.deadZone = (int)(0.10 * 127);
                rsInfo.antiDeadZone = 0;
                rsInfo.maxZone = 100;

                var l2Info = L2ModInfo[device];
                l2Info.deadZone = (byte)(0.00 * 255);

                var r2Info = R2ModInfo[device];
                r2Info.deadZone = (byte)(0.00 * 255);

                GyroOutputMode[device] = DS4Windows.GyroOutMode.Mouse;
                SATriggers[device] = "4";
                SATriggerCond[device] = true;
                GyroTriggerTurns[device] = false;
                GyroMouseInfo[device].enableSmoothing = true;
                GyroMouseInfo[device].smoothingMethod = DS4Windows.GyroMouseInfo.SmoothingMethod.OneEuro;

                OutputDeviceType[device] = OutContType.DS4;

                // If a device exists, make sure to transfer relevant profile device
                // options to device instance
                if (postLoad && device < MAX_DS4_CONTROLLER_COUNT)
                    PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }

            public void LoadDefaultDS4MixedControlsProfile(int device, bool launchprogram, ControlService control,
                string propath = "", bool xinputChange = true, bool postLoad = true)
            {
                PrepareBlankingProfile(device, control, out var xinputPlug, out var xinputStatus, xinputChange);

                var setting = GetDs4ControllerSetting(device, DS4Controls.RYNeg);
                setting.UpdateSettings(false, X360Controls.MouseUp, "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.RYPos);
                setting.UpdateSettings(false, X360Controls.MouseDown, "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.RXNeg);
                setting.UpdateSettings(false, X360Controls.MouseLeft, "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.RXPos);
                setting.UpdateSettings(false, X360Controls.MouseRight, "", DS4KeyType.None);

                var rsInfo = RSModInfo[device];
                rsInfo.deadZone = (int)(0.035 * 127);
                rsInfo.antiDeadZone = 0;
                rsInfo.maxZone = 90;

                OutputDeviceType[device] = OutContType.DS4;

                // If a device exists, make sure to transfer relevant profile device
                // options to device instance
                if (postLoad && device < MAX_DS4_CONTROLLER_COUNT)
                    PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }

            public void LoadDefaultMixedControlsProfile(int device, bool launchprogram, ControlService control,
                string propath = "", bool xinputChange = true, bool postLoad = true)
            {
                var xinputPlug = false;
                var xinputStatus = false;

                var oldContType = ActiveOutDevType[device];

                // Make sure to reset currently set profile values before parsing
                ResetProfile(device);
                ResetMouseProperties(device, control);

                // Only change xinput devices under certain conditions. Avoid
                // performing this upon program startup before loading devices.
                if (xinputChange)
                    CheckOldDeviceStatus(device, control, oldContType,
                        out xinputPlug, out xinputStatus);

                foreach (var dcs in Ds4Settings[device])
                    dcs.Reset();

                ProfileActions[device].Clear();
                ContainsCustomAction[device] = false;
                ContainsCustomExtras[device] = false;

                var setting = GetDs4ControllerSetting(device, DS4Controls.RYNeg);
                setting.UpdateSettings(false, X360Controls.MouseUp, "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.RYPos);
                setting.UpdateSettings(false, X360Controls.MouseDown, "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.RXNeg);
                setting.UpdateSettings(false, X360Controls.MouseLeft, "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.RXPos);
                setting.UpdateSettings(false, X360Controls.MouseRight, "", DS4KeyType.None);

                var rsInfo = RSModInfo[device];
                rsInfo.deadZone = (int)(0.035 * 127);
                rsInfo.antiDeadZone = 0;
                rsInfo.maxZone = 90;

                // If a device exists, make sure to transfer relevant profile device
                // options to device instance
                if (postLoad && device < MAX_DS4_CONTROLLER_COUNT)
                    PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }

            public void LoadDefaultKBMProfile(int device, bool launchprogram, ControlService control,
                string propath = "", bool xinputChange = true, bool postLoad = true)
            {
                var xinputPlug = false;
                var xinputStatus = false;

                var oldContType = ActiveOutDevType[device];

                // Make sure to reset currently set profile values before parsing
                ResetProfile(device);
                ResetMouseProperties(device, control);

                // Only change xinput devices under certain conditions. Avoid
                // performing this upon program startup before loading devices.
                if (xinputChange)
                    CheckOldDeviceStatus(device, control, oldContType,
                        out xinputPlug, out xinputStatus);

                foreach (var dcs in Ds4Settings[device])
                    dcs.Reset();

                ProfileActions[device].Clear();
                ContainsCustomAction[device] = false;
                ContainsCustomExtras[device] = false;

                var lsInfo = LSModInfo[device];
                lsInfo.antiDeadZone = 0;

                var rsInfo = RSModInfo[device];
                rsInfo.deadZone = (int)(0.035 * 127);
                rsInfo.antiDeadZone = 0;
                rsInfo.maxZone = 90;

                var l2Info = L2ModInfo[device];
                l2Info.deadZone = (byte)(0.20 * 255);

                var r2Info = R2ModInfo[device];
                r2Info.deadZone = (byte)(0.20 * 255);

                // Flag to unplug virtual controller
                DirectInputOnly[device] = true;

                var setting = GetDs4ControllerSetting(device, DS4Controls.LYNeg);
                setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.W), "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.LXNeg);
                setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.A), "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.LYPos);
                setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.S), "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.LXPos);
                setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.D), "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.L3);
                setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.LeftShift), "", DS4KeyType.None);

                setting = GetDs4ControllerSetting(device, DS4Controls.RYNeg);
                setting.UpdateSettings(false, X360Controls.MouseUp, "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.RYPos);
                setting.UpdateSettings(false, X360Controls.MouseDown, "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.RXNeg);
                setting.UpdateSettings(false, X360Controls.MouseLeft, "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.RXPos);
                setting.UpdateSettings(false, X360Controls.MouseRight, "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.R3);
                setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.LeftCtrl), "", DS4KeyType.None);

                setting = GetDs4ControllerSetting(device, DS4Controls.DpadUp);
                setting.UpdateSettings(false, X360Controls.Unbound, "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.DpadRight);
                setting.UpdateSettings(false, X360Controls.WDOWN, "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.DpadDown);
                setting.UpdateSettings(false, X360Controls.Unbound, "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.DpadLeft);
                setting.UpdateSettings(false, X360Controls.WUP, "", DS4KeyType.None);

                setting = GetDs4ControllerSetting(device, DS4Controls.Cross);
                setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.Space), "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.Square);
                setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.F), "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.Triangle);
                setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.E), "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.Circle);
                setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.C), "", DS4KeyType.None);

                setting = GetDs4ControllerSetting(device, DS4Controls.L1);
                setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.Q), "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.L2);
                setting.UpdateSettings(false, X360Controls.RightMouse, "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.R1);
                setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.R), "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.R2);
                setting.UpdateSettings(false, X360Controls.LeftMouse, "", DS4KeyType.None);

                setting = GetDs4ControllerSetting(device, DS4Controls.Share);
                setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.Tab), "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.Options);
                setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.Escape), "", DS4KeyType.None);

                // If a device exists, make sure to transfer relevant profile device
                // options to device instance
                if (postLoad && device < MAX_DS4_CONTROLLER_COUNT)
                    PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }

            public void LoadDefaultKBMGyroMouseProfile(int device, bool launchprogram, ControlService control,
                string propath = "", bool xinputChange = true, bool postLoad = true)
            {
                var xinputPlug = false;
                var xinputStatus = false;

                var oldContType = ActiveOutDevType[device];

                // Make sure to reset currently set profile values before parsing
                ResetProfile(device);
                ResetMouseProperties(device, control);

                // Only change xinput devices under certain conditions. Avoid
                // performing this upon program startup before loading devices.
                if (xinputChange)
                    CheckOldDeviceStatus(device, control, oldContType,
                        out xinputPlug, out xinputStatus);

                foreach (var dcs in Ds4Settings[device])
                    dcs.Reset();

                ProfileActions[device].Clear();
                ContainsCustomAction[device] = false;
                ContainsCustomExtras[device] = false;

                var lsInfo = LSModInfo[device];
                lsInfo.antiDeadZone = 0;

                var rsInfo = RSModInfo[device];
                rsInfo.deadZone = (int)(0.105 * 127);
                rsInfo.antiDeadZone = 0;
                rsInfo.maxZone = 90;

                var l2Info = L2ModInfo[device];
                l2Info.deadZone = (byte)(0.20 * 255);

                var r2Info = R2ModInfo[device];
                r2Info.deadZone = (byte)(0.20 * 255);

                GyroOutputMode[device] = DS4Windows.GyroOutMode.Mouse;
                SATriggers[device] = "4";
                SATriggerCond[device] = true;
                GyroTriggerTurns[device] = false;
                GyroMouseInfo[device].enableSmoothing = true;
                GyroMouseInfo[device].smoothingMethod = DS4Windows.GyroMouseInfo.SmoothingMethod.OneEuro;

                // Flag to unplug virtual controller
                DirectInputOnly[device] = true;

                var setting = GetDs4ControllerSetting(device, DS4Controls.LYNeg);
                setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.W), "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.LXNeg);
                setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.A), "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.LYPos);
                setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.S), "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.LXPos);
                setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.D), "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.L3);
                setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.LeftShift), "", DS4KeyType.None);

                setting = GetDs4ControllerSetting(device, DS4Controls.RYNeg);
                setting.UpdateSettings(false, X360Controls.MouseUp, "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.RYPos);
                setting.UpdateSettings(false, X360Controls.MouseDown, "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.RXNeg);
                setting.UpdateSettings(false, X360Controls.MouseLeft, "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.RXPos);
                setting.UpdateSettings(false, X360Controls.MouseRight, "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.R3);
                setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.LeftCtrl), "", DS4KeyType.None);

                setting = GetDs4ControllerSetting(device, DS4Controls.DpadUp);
                setting.UpdateSettings(false, X360Controls.Unbound, "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.DpadRight);
                setting.UpdateSettings(false, X360Controls.WDOWN, "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.DpadDown);
                setting.UpdateSettings(false, X360Controls.Unbound, "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.DpadLeft);
                setting.UpdateSettings(false, X360Controls.WUP, "", DS4KeyType.None);

                setting = GetDs4ControllerSetting(device, DS4Controls.Cross);
                setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.Space), "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.Square);
                setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.F), "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.Triangle);
                setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.E), "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.Circle);
                setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.C), "", DS4KeyType.None);

                setting = GetDs4ControllerSetting(device, DS4Controls.L1);
                //setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.Q), "", DS4KeyType.None);
                setting.UpdateSettings(false, X360Controls.Unbound, "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.L2);
                setting.UpdateSettings(false, X360Controls.RightMouse, "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.R1);
                setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.R), "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.R2);
                setting.UpdateSettings(false, X360Controls.LeftMouse, "", DS4KeyType.None);

                setting = GetDs4ControllerSetting(device, DS4Controls.Share);
                setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.Tab), "", DS4KeyType.None);
                setting = GetDs4ControllerSetting(device, DS4Controls.Options);
                setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.Escape), "", DS4KeyType.None);

                // If a device exists, make sure to transfer relevant profile device
                // options to device instance
                if (postLoad && device < MAX_DS4_CONTROLLER_COUNT)
                    PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }

            private void SetOutBezierCurveObjArrayItem(IList<BezierCurve> bezierCurveArray, int device,
                int curveOptionValue, BezierCurve.AxisType axisType)
            {
                // Set bezier curve obj of axis. 0=Linear (no curve mapping), 1-5=Pre-defined curves, 6=User supplied custom curve string value of a profile (comma separated list of 4 decimal numbers)
                switch (curveOptionValue)
                {
                    // Commented out case 1..5 because Mapping.cs:SetCurveAndDeadzone function has the original IF-THEN-ELSE code logic for those original 1..5 output curve mappings (ie. no need to initialize the lookup result table).
                    // Only the new bezier custom curve option 6 uses the lookup result table (initialized in BezierCurve class based on an input curve definition).
                    //case 1: bezierCurveArray[device].InitBezierCurve(99.0, 91.0, 0.00, 0.00, axisType); break;  // Enhanced Precision (hard-coded curve) (almost the same curve as bezier 0.70, 0.28, 1.00, 1.00)
                    //case 2: bezierCurveArray[device].InitBezierCurve(99.0, 92.0, 0.00, 0.00, axisType); break;  // Quadric
                    //case 3: bezierCurveArray[device].InitBezierCurve(99.0, 93.0, 0.00, 0.00, axisType); break;  // Cubic
                    //case 4: bezierCurveArray[device].InitBezierCurve(99.0, 94.0, 0.00, 0.00, axisType); break;  // Easeout Quad
                    //case 5: bezierCurveArray[device].InitBezierCurve(99.0, 95.0, 0.00, 0.00, axisType); break;  // Easeout Cubic
                    case 6:
                        bezierCurveArray[device].InitBezierCurve(bezierCurveArray[device].CustomDefinition, axisType);
                        break; // Custom output curve
                }
            }

            private void SetupDefaultColors()
            {
                LightbarSettingInfo[0].ds4winSettings.m_Led = new DS4Color(Color.Blue);
                LightbarSettingInfo[1].ds4winSettings.m_Led = new DS4Color(Color.Red);
                LightbarSettingInfo[2].ds4winSettings.m_Led = new DS4Color(Color.Green);
                LightbarSettingInfo[3].ds4winSettings.m_Led = new DS4Color(Color.Pink);
                LightbarSettingInfo[4].ds4winSettings.m_Led = new DS4Color(Color.Blue);
                LightbarSettingInfo[5].ds4winSettings.m_Led = new DS4Color(Color.Red);
                LightbarSettingInfo[6].ds4winSettings.m_Led = new DS4Color(Color.Green);
                LightbarSettingInfo[7].ds4winSettings.m_Led = new DS4Color(Color.Pink);
                LightbarSettingInfo[8].ds4winSettings.m_Led = new DS4Color(Color.White);
            }

            private string stickOutputCurveString(int id)
            {
                var result = "linear";
                switch (id)
                {
                    case 0: break;
                    case 1:
                        result = "enhanced-precision";
                        break;
                    case 2:
                        result = "quadratic";
                        break;
                    case 3:
                        result = "cubic";
                        break;
                    case 4:
                        result = "easeout-quad";
                        break;
                    case 5:
                        result = "easeout-cubic";
                        break;
                    case 6:
                        result = "custom";
                        break;
                }

                return result;
            }

            private int stickOutputCurveId(string name)
            {
                var id = 0;
                switch (name)
                {
                    case "linear":
                        id = 0;
                        break;
                    case "enhanced-precision":
                        id = 1;
                        break;
                    case "quadratic":
                        id = 2;
                        break;
                    case "cubic":
                        id = 3;
                        break;
                    case "easeout-quad":
                        id = 4;
                        break;
                    case "easeout-cubic":
                        id = 5;
                        break;
                    case "custom":
                        id = 6;
                        break;
                }

                return id;
            }

            private string axisOutputCurveString(int id)
            {
                return stickOutputCurveString(id);
            }

            private int axisOutputCurveId(string name)
            {
                return stickOutputCurveId(name);
            }

            private bool SaTriggerCondValue(string text)
            {
                var result = true;
                switch (text)
                {
                    case "and":
                        result = true;
                        break;
                    case "or":
                        result = false;
                        break;
                    default:
                        result = true;
                        break;
                }

                return result;
            }

            private string SaTriggerCondString(bool value)
            {
                var result = value ? "and" : "or";
                return result;
            }

            private string OutContDeviceString(OutContType id)
            {
                var result = "X360";
                switch (id)
                {
                    case OutContType.None:
                    case OutContType.X360:
                        result = "X360";
                        break;
                    case OutContType.DS4:
                        result = "DS4";
                        break;
                }

                return result;
            }

            private OutContType OutContDeviceId(string name)
            {
                var id = OutContType.X360;
                switch (name)
                {
                    case "None":
                    case "X360":
                        id = OutContType.X360;
                        break;
                    case "DS4":
                        id = OutContType.DS4;
                        break;
                }

                return id;
            }

            private void PortOldGyroSettings(int device)
            {
                if (GyroOutputMode[device] == DS4Windows.GyroOutMode.None)
                    GyroOutputMode[device] = DS4Windows.GyroOutMode.Controls;
            }

            private string GetGyroOutModeString(GyroOutMode mode)
            {
                var result = "None";
                switch (mode)
                {
                    case DS4Windows.GyroOutMode.Controls:
                        result = "Controls";
                        break;
                    case DS4Windows.GyroOutMode.Mouse:
                        result = "Mouse";
                        break;
                    case DS4Windows.GyroOutMode.MouseJoystick:
                        result = "MouseJoystick";
                        break;
                    case DS4Windows.GyroOutMode.Passthru:
                        result = "Passthru";
                        break;
                }

                return result;
            }

            private GyroOutMode GetGyroOutModeType(string modeString)
            {
                var result = DS4Windows.GyroOutMode.None;
                switch (modeString)
                {
                    case "Controls":
                        result = DS4Windows.GyroOutMode.Controls;
                        break;
                    case "Mouse":
                        result = DS4Windows.GyroOutMode.Mouse;
                        break;
                    case "MouseJoystick":
                        result = DS4Windows.GyroOutMode.MouseJoystick;
                        break;
                    case "Passthru":
                        result = DS4Windows.GyroOutMode.Passthru;
                        break;
                }

                return result;
            }

            private string GetLightbarModeString(LightbarMode mode)
            {
                var result = "DS4Win";
                switch (mode)
                {
                    case LightbarMode.DS4Win:
                        result = "DS4Win";
                        break;
                    case LightbarMode.Passthru:
                        result = "Passthru";
                        break;
                }

                return result;
            }

            private LightbarMode GetLightbarModeType(string modeString)
            {
                var result = LightbarMode.DS4Win;
                switch (modeString)
                {
                    case "DS4Win":
                        result = LightbarMode.DS4Win;
                        break;
                    case "Passthru":
                        result = LightbarMode.Passthru;
                        break;
                }

                return result;
            }

            private void CreateAction()
            {
                var m_Xdoc = new XmlDocument();
                PrepareActionsXml(m_Xdoc);
                m_Xdoc.Save(ActionsPath);
            }

            private void PrepareActionsXml(XmlDocument xmlDoc)
            {
                XmlNode Node;

                Node = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", string.Empty);
                xmlDoc.AppendChild(Node);

                Node = xmlDoc.CreateComment(string.Format(" Special Actions Configuration Data. {0} ", DateTime.Now));
                xmlDoc.AppendChild(Node);

                Node = xmlDoc.CreateWhitespace("\r\n");
                xmlDoc.AppendChild(Node);

                Node = xmlDoc.CreateNode(XmlNodeType.Element, "Actions", "");
                xmlDoc.AppendChild(Node);
            }

            private void UpdateDs4ControllerKeyType(int deviceNum, string buttonName, bool shift, DS4KeyType keyType)
            {
                DS4Controls dc;
                if (buttonName.StartsWith("bn"))
                    dc = GetDs4ControlsByName(buttonName);
                else
                    dc = (DS4Controls)Enum.Parse(typeof(DS4Controls), buttonName, true);

                var temp = (int)dc;
                if (temp > 0)
                {
                    var index = temp - 1;
                    var dcs = Ds4Settings[deviceNum][index];
                    if (shift)
                        dcs.ShiftKeyType = keyType;
                    else
                        dcs.KeyType = keyType;
                }
            }

            private void ResetMouseProperties(int device, ControlService control)
            {
                if (device < MAX_DS4_CONTROLLER_COUNT &&
                    control.touchPad[device] != null)
                    control.touchPad[device]?.ResetToggleGyroModes();
            }

            private void ResetProfile(int device)
            {
                ButtonMouseInfos[device].Reset();
                GyroControlsInfo[device].Reset();

                EnableTouchToggle[device] = true;
                IdleDisconnectTimeout[device] = 0;
                EnableOutputDataToDS4[device] = true;
                TouchpadJitterCompensation[device] = true;
                LowerRCOn[device] = false;
                TouchClickPassthru[device] = false;

                RumbleBoost[device] = 100;
                RumbleAutostopTime[device] = 0;
                TouchSensitivity[device] = 100;

                LSModInfo[device].Reset();
                RSModInfo[device].Reset();
                LSModInfo[device].deadZone = RSModInfo[device].deadZone = 10;
                LSModInfo[device].antiDeadZone = RSModInfo[device].antiDeadZone = 20;
                LSModInfo[device].maxZone = RSModInfo[device].maxZone = 100;
                LSModInfo[device].maxOutput = RSModInfo[device].maxOutput = 100.0;
                LSModInfo[device].fuzz = RSModInfo[device].fuzz = StickDeadZoneInfo.DEFAULT_FUZZ;

                //l2ModInfo[device].deadZone = r2ModInfo[device].deadZone = 0;
                //l2ModInfo[device].antiDeadZone = r2ModInfo[device].antiDeadZone = 0;
                //l2ModInfo[device].maxZone = r2ModInfo[device].maxZone = 100;
                //l2ModInfo[device].maxOutput = r2ModInfo[device].maxOutput = 100.0;
                L2ModInfo[device].Reset();
                R2ModInfo[device].Reset();

                LSRotation[device] = 0.0;
                RSRotation[device] = 0.0;
                SXDeadzone[device] = SZDeadzone[device] = 0.25;
                SXMaxzone[device] = SZMaxzone[device] = 1.0;
                SXAntiDeadzone[device] = SZAntiDeadzone[device] = 0.0;
                L2Sens[device] = R2Sens[device] = 1;
                LSSens[device] = RSSens[device] = 1;
                SXSens[device] = SZSens[device] = 1;
                TapSensitivity[device] = 0;
                DoubleTap[device] = false;
                ScrollSensitivity[device] = 0;
                TouchPadInvert[device] = 0;
                BluetoothPollRate[device] = 4;

                LSOutputSettings[device].ResetSettings();
                RSOutputSettings[device].ResetSettings();
                L2OutputSettings[device].ResetSettings();
                R2OutputSettings[device].ResetSettings();

                var lightbarSettings = LightbarSettingInfo[device];
                var lightInfo = lightbarSettings.ds4winSettings;
                lightbarSettings.Mode = LightbarMode.DS4Win;
                lightInfo.m_LowLed = new DS4Color(Color.Black);
                //m_LowLeds[device] = new DS4Color(Color.Black);

                var tempColor = Color.Blue;
                switch (device)
                {
                    case 0:
                        tempColor = Color.Blue;
                        break;
                    case 1:
                        tempColor = Color.Red;
                        break;
                    case 2:
                        tempColor = Color.Green;
                        break;
                    case 3:
                        tempColor = Color.Pink;
                        break;
                    case 4:
                        tempColor = Color.Blue;
                        break;
                    case 5:
                        tempColor = Color.Red;
                        break;
                    case 6:
                        tempColor = Color.Green;
                        break;
                    case 7:
                        tempColor = Color.Pink;
                        break;
                    case 8:
                        tempColor = Color.White;
                        break;
                    default:
                        tempColor = Color.Blue;
                        break;
                }

                lightInfo.m_Led = new DS4Color(tempColor);
                lightInfo.m_ChargingLed = new DS4Color(Color.Black);
                lightInfo.m_FlashLed = new DS4Color(Color.Black);
                lightInfo.flashAt = 0;
                lightInfo.flashType = 0;
                lightInfo.chargingType = 0;
                lightInfo.rainbow = 0;
                lightInfo.maxRainbowSat = 1.0;
                lightInfo.ledAsBattery = false;

                LaunchProgram[device] = string.Empty;
                DirectInputOnly[device] = false;
                StartTouchpadOff[device] = false;
                TouchOutMode[device] = TouchpadOutMode.Mouse;
                SATriggers[device] = "-1";
                SATriggerCond[device] = true;
                GyroOutputMode[device] = DS4Windows.GyroOutMode.Controls;
                SAMouseStickTriggers[device] = "-1";
                SAMouseStickTriggerCond[device] = true;

                GyroMouseStickInfo[device].Reset();
                GyroSwipeInfo[device].Reset();

                GyroMouseStickToggle[device] = false;
                GyroMouseStickTriggerTurns[device] = true;
                SASteeringWheelEmulationAxis[device] = SASteeringWheelEmulationAxisType.None;
                SASteeringWheelEmulationRange[device] = 360;
                SAWheelFuzzValues[device] = 0;
                WheelSmoothInfo[device].Reset();
                TouchDisInvertTriggers[device] = new int[1] { -1 };
                GyroSensitivity[device] = 100;
                GyroSensVerticalScale[device] = 100;
                GyroInvert[device] = 0;
                GyroTriggerTurns[device] = true;
                GyroMouseInfo[device].Reset();

                GyroMouseHorizontalAxis[device] = 0;
                GyroMouseToggle[device] = false;
                SquStickInfo[device].lsMode = false;
                SquStickInfo[device].rsMode = false;
                SquStickInfo[device].lsRoundness = 5.0;
                SquStickInfo[device].rsRoundness = 5.0;
                LSAntiSnapbackInfo[device].timeout = StickAntiSnapbackInfo.DEFAULT_TIMEOUT;
                LSAntiSnapbackInfo[device].delta = StickAntiSnapbackInfo.DEFAULT_DELTA;
                LSAntiSnapbackInfo[device].enabled = StickAntiSnapbackInfo.DEFAULT_ENABLED;
                setLsOutCurveMode(device, 0);
                setRsOutCurveMode(device, 0);
                setL2OutCurveMode(device, 0);
                setR2OutCurveMode(device, 0);
                setSXOutCurveMode(device, 0);
                setSZOutCurveMode(device, 0);
                TrackballMode[device] = false;
                TrackballFriction[device] = 10.0;
                TouchPadAbsMouse[device].Reset();
                TouchPadRelMouse[device].Reset();
                OutputDeviceType[device] = OutContType.X360;
                Ds4Mapping = false;
            }

            private void PrepareBlankingProfile(int device, ControlService control, out bool xinputPlug,
                out bool xinputStatus, bool xinputChange = true)
            {
                xinputPlug = false;
                xinputStatus = false;

                var oldContType = ActiveOutDevType[device];

                // Make sure to reset currently set profile values before parsing
                ResetProfile(device);
                ResetMouseProperties(device, control);

                // Only change xinput devices under certain conditions. Avoid
                // performing this upon program startup before loading devices.
                if (xinputChange)
                    CheckOldDeviceStatus(device, control, oldContType,
                        out xinputPlug, out xinputStatus);

                foreach (var dcs in Ds4Settings[device])
                    dcs.Reset();

                ProfileActions[device].Clear();
                ContainsCustomAction[device] = false;
                ContainsCustomExtras[device] = false;
            }

            private void CheckOldDeviceStatus(int device, ControlService control,
                OutContType oldContType, out bool xinputPlug, out bool xinputStatus)
            {
                xinputPlug = false;
                xinputStatus = false;

                if (device < ControlService.CURRENT_DS4_CONTROLLER_LIMIT)
                {
                    var oldUseDInputOnly = UseDirectInputOnly[device];
                    var tempDevice = control.DS4Controllers[device];
                    var exists = tempDevice != null;
                    var synced = exists ? tempDevice.isSynced() : false;
                    var isAlive = exists ? tempDevice.IsAlive() : false;
                    if (DirectInputOnly[device] != oldUseDInputOnly)
                    {
                        if (DirectInputOnly[device])
                        {
                            xinputPlug = false;
                            xinputStatus = true;
                        }
                        else if (synced && isAlive)
                        {
                            xinputPlug = true;
                            xinputStatus = true;
                        }
                    }
                    else if (!DirectInputOnly[device] &&
                             oldContType != OutputDeviceType[device])
                    {
                        xinputPlug = true;
                        xinputStatus = true;
                    }
                }
            }

            private void PostLoadSnippet(int device, ControlService control, bool xinputStatus, bool xinputPlug)
            {
                var tempDev = control.DS4Controllers[device];
                if (tempDev != null && tempDev.isSynced())
                    tempDev.queueEvent(() =>
                    {
                        //tempDev.setIdleTimeout(idleDisconnectTimeout[device]);
                        //tempDev.setBTPollRate(btPollRate[device]);
                        if (xinputStatus && tempDev.PrimaryDevice)
                        {
                            if (xinputPlug)
                            {
                                var tempOutDev = control.outputDevices[device];
                                if (tempOutDev != null)
                                {
                                    tempOutDev = null;
                                    //Global.ActiveOutDevType[device] = OutContType.None;
                                    control.UnplugOutDev(device, tempDev);
                                }

                                var tempContType = OutputDeviceType[device];
                                control.PluginOutDev(device, tempDev);
                                //Global.UseDirectInputOnly[device] = false;
                            }
                            else
                            {
                                //Global.ActiveOutDevType[device] = OutContType.None;
                                control.UnplugOutDev(device, tempDev);
                            }
                        }

                        //tempDev.RumbleAutostopTime = rumbleAutostopTime[device];
                        //tempDev.setRumble(0, 0);
                        //tempDev.LightBarColor = Global.getMainColor(device);
                        control.CheckProfileOptions(device, tempDev, true);
                    });

                //Program.rootHub.touchPad[device]?.ResetTrackAccel(trackballFriction[device]);
            }
        }
    }
}