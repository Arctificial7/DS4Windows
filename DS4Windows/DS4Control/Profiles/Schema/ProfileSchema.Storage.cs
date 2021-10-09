﻿using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using DS4WinWPF.DS4Control.Attributes;
using DS4WinWPF.DS4Control.Profiles.Schema.Converters;
using DS4WinWPF.DS4Control.Profiles.Schema.Migrations;
using ExtendedXmlSerializer;
using ExtendedXmlSerializer.Configuration;

namespace DS4WinWPF.DS4Control.Profiles.Schema
{
    public partial class DS4WindowsProfile : XmlSerializable<DS4WindowsProfile>
    {
        public override IExtendedXmlSerializer GetSerializer()
        {
            return new ConfigurationContainer()
                .EnableReferences()
                .EnableMemberExceptionHandling()
                .EnableImplicitTyping(typeof(DS4WindowsProfile))
                .Type<DS4Color>().Register().Converter().Using(DS4ColorConverter.Default)
                .Type<SensitivityProxyType>().Register().Converter().Using(SensitivityConverter.Default)
                .Type<List<int>>().Register().Converter().Using(IntegerListConverterConverter.Default)
                .Type<bool>().Register().Converter().Using(BooleanConverter.Default)
                .Type<BezierCurve>().Register().Converter().Using(BezierCurveConverter.Default)
                .Type<double>().Register().Converter().Using(DoubleConverter.Default)
                .Type<ControlsCollectionEntity>().AddMigration(new ControlsCollectionEntityMigration())
                .Create();
        }

        /// <summary>
        ///     Converts properties from <see cref="IBackingStore" /> for a specified device index to this
        ///     <see cref="Schema.DS4WindowsProfile" /> instance.
        /// </summary>
        /// <param name="store">The <see cref="IBackingStore" />.</param>
        /// <param name="device">The zero-based device index to copy.</param>
        [IntermediateSolution]
        public void CopyFrom(IBackingStore store, int device)
        {
            var light = store.LightbarSettingInfo[device];

            TouchToggle = store.EnableTouchToggle[device];
            IdleDisconnectTimeout = store.IdleDisconnectTimeout[device];
            OutputDataToDS4 = store.EnableOutputDataToDS4[device];
            Color = light.Ds4WinSettings.Led;
            RumbleBoost = store.RumbleBoost[device];
            RumbleAutostopTime = store.RumbleAutostopTime[device];
            LightbarMode = store.LightbarSettingInfo[device].Mode;
            LedAsBatteryIndicator = light.Ds4WinSettings.LedAsBattery;
            FlashType = light.Ds4WinSettings.FlashType;
            FlashBatteryAt = light.Ds4WinSettings.FlashAt;
            TouchSensitivity = store.TouchSensitivity[device];

            LowColor = light.Ds4WinSettings.LowLed;
            ChargingColor = light.Ds4WinSettings.ChargingLed;
            FlashColor = light.Ds4WinSettings.FlashLed;

            TouchpadJitterCompensation = store.TouchpadJitterCompensation[device];
            LowerRCOn = store.LowerRCOn[device];
            TapSensitivity = store.TapSensitivity[device];
            DoubleTap = store.DoubleTap[device];
            ScrollSensitivity = store.ScrollSensitivity[device];

            LeftTriggerMiddle = store.L2ModInfo[device].deadZone;
            RightTriggerMiddle = store.R2ModInfo[device].deadZone;

            TouchpadInvert = store.TouchPadInvert[device];
            TouchpadClickPassthru = store.TouchClickPassthru[device];

            L2AntiDeadZone = store.L2ModInfo[device].AntiDeadZone;
            R2AntiDeadZone = store.R2ModInfo[device].AntiDeadZone;

            L2MaxZone = store.L2ModInfo[device].maxZone;
            R2MaxZone = store.R2ModInfo[device].maxZone;

            L2MaxOutput = store.L2ModInfo[device].maxOutput;
            R2MaxOutput = store.R2ModInfo[device].maxOutput;

            ButtonMouseSensitivity = store.ButtonMouseInfos[device].buttonSensitivity;
            ButtonMouseOffset = store.ButtonMouseInfos[device].mouseVelocityOffset;

            Rainbow = light.Ds4WinSettings.Rainbow;
            MaxSatRainbow = Convert.ToInt32(light.Ds4WinSettings.MaxRainbowSaturation * 100.0);

            LSDeadZone = store.LSModInfo[device].DeadZone;
            RSDeadZone = store.RSModInfo[device].DeadZone;

            LSAntiDeadZone = store.LSModInfo[device].AntiDeadZone;
            RSAntiDeadZone = store.RSModInfo[device].AntiDeadZone;

            LSMaxZone = store.LSModInfo[device].MaxZone;
            RSMaxZone = store.RSModInfo[device].MaxZone;

            LSVerticalScale = store.LSModInfo[device].VerticalScale;
            RSVerticalScale = store.RSModInfo[device].VerticalScale;

            LSMaxOutput = store.LSModInfo[device].MaxOutput;
            RSMaxOutput = store.RSModInfo[device].MaxOutput;

            LSMaxOutputForce = store.LSModInfo[device].MaxOutputForce;
            RSMaxOutputForce = store.RSModInfo[device].MaxOutputForce;

            LSDeadZoneType = store.LSModInfo[device].DZType;
            RSDeadZoneType = store.RSModInfo[device].DZType;

            LSAxialDeadOptions.DeadZoneX = store.LSModInfo[device].XAxisDeadInfo.DeadZone;
            LSAxialDeadOptions.DeadZoneY = store.LSModInfo[device].YAxisDeadInfo.DeadZone;
            LSAxialDeadOptions.MaxZoneX = store.LSModInfo[device].XAxisDeadInfo.MaxZone;
            LSAxialDeadOptions.MaxZoneY = store.LSModInfo[device].YAxisDeadInfo.MaxZone;
            LSAxialDeadOptions.AntiDeadZoneX = store.LSModInfo[device].XAxisDeadInfo.AntiDeadZone;
            LSAxialDeadOptions.AntiDeadZoneY = store.LSModInfo[device].YAxisDeadInfo.AntiDeadZone;
            LSAxialDeadOptions.MaxOutputX = store.LSModInfo[device].XAxisDeadInfo.MaxOutput;
            LSAxialDeadOptions.MaxOutputY = store.LSModInfo[device].YAxisDeadInfo.MaxOutput;

            RSAxialDeadOptions.DeadZoneX = store.RSModInfo[device].XAxisDeadInfo.DeadZone;
            RSAxialDeadOptions.DeadZoneY = store.RSModInfo[device].YAxisDeadInfo.DeadZone;
            RSAxialDeadOptions.MaxZoneX = store.RSModInfo[device].XAxisDeadInfo.MaxZone;
            RSAxialDeadOptions.MaxZoneY = store.RSModInfo[device].YAxisDeadInfo.MaxZone;
            RSAxialDeadOptions.AntiDeadZoneX = store.RSModInfo[device].XAxisDeadInfo.AntiDeadZone;
            RSAxialDeadOptions.AntiDeadZoneY = store.RSModInfo[device].YAxisDeadInfo.AntiDeadZone;
            RSAxialDeadOptions.MaxOutputX = store.RSModInfo[device].XAxisDeadInfo.MaxOutput;
            RSAxialDeadOptions.MaxOutputY = store.RSModInfo[device].YAxisDeadInfo.MaxOutput;

            LSRotation = Convert.ToInt32(store.LSRotation[device] * 180.0 / Math.PI);
            RSRotation = Convert.ToInt32(store.RSRotation[device] * 180.0 / Math.PI);

            LSFuzz = store.LSModInfo[device].Fuzz;
            RSFuzz = store.RSModInfo[device].Fuzz;

            LSOuterBindDead = Convert.ToInt32(store.LSModInfo[device].OuterBindDeadZone);
            RSOuterBindDead = Convert.ToInt32(store.RSModInfo[device].OuterBindDeadZone);

            LSOuterBindInvert = store.LSModInfo[device].OuterBindInvert;
            RSOuterBindInvert = store.RSModInfo[device].OuterBindInvert;

            SXDeadZone = store.SXDeadzone[device];
            SZDeadZone = store.SZDeadzone[device];

            SXMaxZone = Convert.ToInt32(store.SXMaxzone[device] * 100.0);
            SZMaxZone = Convert.ToInt32(store.SZMaxzone[device] * 100.0);

            SXAntiDeadZone = Convert.ToInt32(store.SXAntiDeadzone[device] * 100.0);
            SZAntiDeadZone = Convert.ToInt32(store.SZAntiDeadzone[device] * 100.0);

            Sensitivity = new SensitivityProxyType
            {
                LSSens = store.LSSens[device],
                RSSens = store.RSSens[device],
                L2Sens = store.L2Sens[device],
                R2Sens = store.R2Sens[device],
                SXSens = store.SXSens[device],
                SZSens = store.SZSens[device]
            };

            ChargingType = light.Ds4WinSettings.ChargingType;

            MouseAcceleration = store.ButtonMouseInfos[device].mouseAccel;
            ButtonMouseVerticalScale = Convert.ToInt32(store.ButtonMouseInfos[device].buttonVerticalScale * 100);

            LaunchProgram = store.LaunchProgram[device];
            DinputOnly = store.DirectInputOnly[device];
            StartTouchpadOff = store.StartTouchpadOff[device];
            TouchpadOutputMode = store.TouchOutMode[device];
            SATriggers = store.SATriggers[device];
            SATriggerCond = store.SaTriggerCondString(store.SATriggerCondition[device]);
            SASteeringWheelEmulationAxis = store.SASteeringWheelEmulationAxis[device];
            SASteeringWheelEmulationRange = store.SASteeringWheelEmulationRange[device];
            SASteeringWheelFuzz = store.SAWheelFuzzValues[device];

            SASteeringWheelSmoothingOptions.SASteeringWheelUseSmoothing = store.WheelSmoothInfo[device].Enabled;
            SASteeringWheelSmoothingOptions.SASteeringWheelSmoothMinCutoff = store.WheelSmoothInfo[device].MinCutoff;
            SASteeringWheelSmoothingOptions.SASteeringWheelSmoothBeta = store.WheelSmoothInfo[device].Beta;

            TouchDisInvTriggers = store.TouchDisInvertTriggers[device].ToList();

            GyroSensitivity = store.GyroSensitivity[device];
            GyroSensVerticalScale = store.GyroSensVerticalScale[device];
            GyroInvert = store.GyroInvert[device];
            GyroTriggerTurns = store.GyroTriggerTurns[device];

            GyroControlsSettings.Triggers = store.GyroControlsInfo[device].Triggers;
            GyroControlsSettings.TriggerCond = store.SaTriggerCondString(store.GyroControlsInfo[device].TriggerCond);
            GyroControlsSettings.TriggerTurns = store.GyroControlsInfo[device].TriggerTurns;
            GyroControlsSettings.Toggle = store.GyroControlsInfo[device].TriggerToggle;

            GyroMouseSmoothingSettings.UseSmoothing = store.GyroMouseInfo[device].enableSmoothing;
            GyroMouseSmoothingSettings.SmoothingMethod = store.GyroMouseInfo[device].SmoothMethodIdentifier();
            GyroMouseSmoothingSettings.SmoothingWeight =
                Convert.ToInt32(store.GyroMouseInfo[device].smoothingWeight * 100);
            GyroMouseSmoothingSettings.SmoothingMinCutoff = store.GyroMouseInfo[device].minCutoff;
            GyroMouseSmoothingSettings.SmoothingBeta = store.GyroMouseInfo[device].beta;

            GyroMouseHAxis = store.GyroMouseHorizontalAxis[device];
            GyroMouseDeadZone = store.GyroMouseDeadZone[device];
            GyroMouseMinThreshold = store.GyroMouseInfo[device].minThreshold;
            GyroMouseToggle = store.GyroMouseToggle[device];
            GyroOutputMode = store.GyroOutputMode[device];
            GyroMouseStickTriggers = store.SAMouseStickTriggers[device];
            GyroMouseStickTriggerCond = store.SaTriggerCondString(store.SAMouseStickTriggerCond[device]);
            GyroMouseStickTriggerTurns = store.GyroMouseStickTriggerTurns[device];
            GyroMouseStickHAxis = store.GyroMouseStickHorizontalAxis[device];
            GyroMouseStickDeadZone = store.GyroMouseStickInfo[device].DeadZone;
            GyroMouseStickMaxZone = store.GyroMouseStickInfo[device].MaxZone;
            GyroMouseStickOutputStick = store.GyroMouseStickInfo[device].outputStick;
            GyroMouseStickOutputStickAxes = store.GyroMouseStickInfo[device].outputStickDir;
            GyroMouseStickAntiDeadX = store.GyroMouseStickInfo[device].AntiDeadX;
            GyroMouseStickAntiDeadY = store.GyroMouseStickInfo[device].AntiDeadY;
            GyroMouseStickInvert = store.GyroMouseStickInfo[device].Inverted;
            GyroMouseStickToggle = store.GyroMouseStickToggle[device];
            GyroMouseStickMaxOutput = store.GyroMouseStickInfo[device].MaxOutput;
            GyroMouseStickMaxOutputEnabled = store.GyroMouseStickInfo[device].MaxOutputEnabled;
            GyroMouseStickVerticalScale = store.GyroMouseStickInfo[device].VertScale;

            GyroMouseStickSmoothingSettings.UseSmoothing = store.GyroMouseStickInfo[device].UseSmoothing;
            GyroMouseStickSmoothingSettings.SmoothingMethod = store.GyroMouseStickInfo[device].SmoothMethodIdentifier();
            GyroMouseStickSmoothingSettings.SmoothingWeight =
                Convert.ToInt32(store.GyroMouseStickInfo[device].SmoothWeight * 100);
            GyroMouseStickSmoothingSettings.SmoothingMinCutoff = store.GyroMouseStickInfo[device].minCutoff;
            GyroMouseStickSmoothingSettings.SmoothingBeta = store.GyroMouseStickInfo[device].beta;

            GyroSwipeSettings.DeadZoneX = store.GyroSwipeInfo[device].deadzoneX;
            GyroSwipeSettings.DeadZoneY = store.GyroSwipeInfo[device].deadzoneY;
            GyroSwipeSettings.Triggers = store.GyroSwipeInfo[device].triggers;
            GyroSwipeSettings.TriggerCond = store.SaTriggerCondString(store.GyroSwipeInfo[device].triggerCond);
            GyroSwipeSettings.TriggerTurns = store.GyroSwipeInfo[device].triggerTurns;
            GyroSwipeSettings.XAxis = store.GyroSwipeInfo[device].xAxis;
            GyroSwipeSettings.DelayTime = store.GyroSwipeInfo[device].delayTime;

            ProfileActions = string.Join("/", store.ProfileActions[device]);
            BTPollRate = store.BluetoothPollRate[device];

            LSOutputCurveMode = store.StickOutputCurveString(store.GetLsOutCurveMode(device));
            LSOutputCurveCustom = store.LSOutBezierCurveObj[device];

            RSOutputCurveMode = store.StickOutputCurveString(store.GetRsOutCurveMode(device));
            RSOutputCurveCustom = store.RSOutBezierCurveObj[device];

            LSSquareStick = store.SquStickInfo[device].LSMode;
            RSSquareStick = store.SquStickInfo[device].RSMode;

            SquareStickRoundness = store.SquStickInfo[device].LSRoundness;
            SquareRStickRoundness = store.SquStickInfo[device].RSRoundness;

            LSAntiSnapback = store.LSAntiSnapbackInfo[device].Enabled;
            RSAntiSnapback = store.RSAntiSnapbackInfo[device].Enabled;

            LSAntiSnapbackDelta = store.LSAntiSnapbackInfo[device].Delta;
            RSAntiSnapbackDelta = store.RSAntiSnapbackInfo[device].Delta;

            LSAntiSnapbackTimeout = store.LSAntiSnapbackInfo[device].Timeout;
            RSAntiSnapbackTimeout = store.RSAntiSnapbackInfo[device].Timeout;

            LSOutputMode = store.LSOutputSettings[device].Mode;
            RSOutputMode = store.RSOutputSettings[device].Mode;

            LSOutputSettings.FlickStickSettings.RealWorldCalibration = store
                .LSOutputSettings[device]
                .OutputSettings
                .flickSettings
                .realWorldCalibration;
            LSOutputSettings.FlickStickSettings.FlickThreshold = store
                .LSOutputSettings[device]
                .OutputSettings
                .flickSettings
                .flickThreshold;
            LSOutputSettings.FlickStickSettings.FlickTime = store
                .LSOutputSettings[device]
                .OutputSettings
                .flickSettings
                .flickTime;
            LSOutputSettings.FlickStickSettings.MinAngleThreshold = store
                .LSOutputSettings[device]
                .OutputSettings
                .flickSettings
                .minAngleThreshold;

            RSOutputSettings.FlickStickSettings.RealWorldCalibration = store
                .RSOutputSettings[device]
                .OutputSettings
                .flickSettings
                .realWorldCalibration;
            RSOutputSettings.FlickStickSettings.FlickThreshold = store
                .RSOutputSettings[device]
                .OutputSettings
                .flickSettings
                .flickThreshold;
            RSOutputSettings.FlickStickSettings.FlickTime = store
                .RSOutputSettings[device]
                .OutputSettings
                .flickSettings
                .flickTime;
            RSOutputSettings.FlickStickSettings.MinAngleThreshold = store
                .RSOutputSettings[device]
                .OutputSettings
                .flickSettings
                .minAngleThreshold;

            L2OutputCurveMode = store.AxisOutputCurveString(store.GetL2OutCurveMode(device));
            L2OutputCurveCustom = store.L2OutBezierCurveObj[device];

            L2TwoStageMode = store.L2OutputSettings[device].twoStageMode;
            R2TwoStageMode = store.R2OutputSettings[device].twoStageMode;

            L2HipFireTime = store.L2OutputSettings[device].hipFireMS;
            R2HipFireTime = store.R2OutputSettings[device].hipFireMS;

            L2TriggerEffect = store.L2OutputSettings[device].triggerEffect;
            R2TriggerEffect = store.R2OutputSettings[device].triggerEffect;

            R2OutputCurveMode = store.AxisOutputCurveString(store.GetR2OutCurveMode(device));
            R2OutputCurveCustom = store.R2OutBezierCurveObj[device];

            SXOutputCurveMode = store.AxisOutputCurveString(store.GetSXOutCurveMode(device));
            SXOutputCurveCustom = store.SXOutBezierCurveObj[device];

            SZOutputCurveMode = store.AxisOutputCurveString(store.GetSZOutCurveMode(device));
            SZOutputCurveCustom = store.SZOutBezierCurveObj[device];

            TrackballMode = store.TrackballMode[device];
            TrackballFriction = store.TrackballFriction[device];

            TouchRelMouseRotation = Convert.ToInt32(store.TouchPadRelMouse[device].Rotation * 180.0 / Math.PI);
            TouchRelMouseMinThreshold = store.TouchPadRelMouse[device].MinThreshold;

            TouchpadAbsMouseSettings.MaxZoneX = store.TouchPadAbsMouse[device].MaxZoneX;
            TouchpadAbsMouseSettings.MaxZoneY = store.TouchPadAbsMouse[device].MaxZoneY;
            TouchpadAbsMouseSettings.SnapToCenter = store.TouchPadAbsMouse[device].SnapToCenter;

            OutputContDevice = store.OutputDeviceType[device];

            //
            // TODO: to be continued
            // 
        }

        /// <summary>
        ///     Injects properties from <see cref="DS4WindowsProfile" /> for a specified device index into
        ///     <see cref="IBackingStore" /> instance.
        /// </summary>
        /// <param name="store">The <see cref="IBackingStore" />.</param>
        /// <param name="device">The zero-based device index to copy.</param>
        [IntermediateSolution]
        public void CopyTo(IBackingStore store, int device)
        {
            var lightbarSettings = store.LightbarSettingInfo[device];
            var lightInfo = lightbarSettings.Ds4WinSettings;

            store.EnableTouchToggle[device] = TouchToggle;
            store.IdleDisconnectTimeout[device] = IdleDisconnectTimeout;
            store.EnableOutputDataToDS4[device] = OutputDataToDS4;
            lightbarSettings.Mode = LightbarMode;
            lightInfo.Led = Color;
            store.RumbleBoost[device] = RumbleBoost;
            store.RumbleAutostopTime[device] = RumbleAutostopTime;
            lightInfo.LedAsBattery = LedAsBatteryIndicator;
            lightInfo.FlashType = FlashType;
            lightInfo.FlashAt = FlashBatteryAt;
            store.TouchSensitivity[device] = TouchSensitivity;
            lightInfo.LowLed = LowColor;
            lightInfo.ChargingLed = ChargingColor;
            lightInfo.FlashLed = FlashColor;
            store.TouchpadJitterCompensation[device] = TouchpadJitterCompensation;
            store.LowerRCOn[device] = LowerRCOn;
            store.TapSensitivity[device] = TapSensitivity;
            store.DoubleTap[device] = DoubleTap;
            store.ScrollSensitivity[device] = ScrollSensitivity;
            store.TouchPadInvert[device] = Math.Min(Math.Max(TouchpadInvert, 0), 3);
            store.TouchClickPassthru[device] = TouchpadClickPassthru;
            store.L2ModInfo[device].deadZone = LeftTriggerMiddle;
            store.R2ModInfo[device].deadZone = RightTriggerMiddle;
            store.L2ModInfo[device].AntiDeadZone = L2AntiDeadZone;
            store.R2ModInfo[device].AntiDeadZone = R2AntiDeadZone;
            store.L2ModInfo[device].maxZone = Math.Min(Math.Max(L2MaxZone, 0), 100);
            store.R2ModInfo[device].maxZone = Math.Min(Math.Max(R2MaxZone, 0), 100);
            store.L2ModInfo[device].maxOutput = Math.Min(Math.Max(L2MaxOutput, 0.0), 100.0);
            ;
            store.R2ModInfo[device].maxOutput = Math.Min(Math.Max(R2MaxOutput, 0.0), 100.0);
            ;
            store.LSRotation[device] = Math.Min(Math.Max(LSRotation, -180), 180) * Math.PI / 180.0;
            store.RSRotation[device] = Math.Min(Math.Max(RSRotation, -180), 180) * Math.PI / 180.0;
            store.LSModInfo[device].Fuzz = Math.Min(Math.Max(LSFuzz, 0), 100);
            store.RSModInfo[device].Fuzz = Math.Min(Math.Max(RSFuzz, 0), 100);
            store.ButtonMouseInfos[device].buttonSensitivity = ButtonMouseSensitivity;
            store.ButtonMouseInfos[device].mouseVelocityOffset = ButtonMouseOffset;
            store.ButtonMouseInfos[device].buttonVerticalScale =
                Math.Min(Math.Max(ButtonMouseVerticalScale, 0), 500) * 0.01;
            lightInfo.Rainbow = Rainbow;
            lightInfo.MaxRainbowSaturation = Math.Max(0, Math.Min(100, MaxSatRainbow)) / 100.0;
            store.LSModInfo[device].DeadZone = Math.Min(Math.Max(LSDeadZone, 0), 127);
            store.RSModInfo[device].DeadZone = Math.Min(Math.Max(RSDeadZone, 0), 127);
            store.LSModInfo[device].AntiDeadZone = LSAntiDeadZone;
            store.RSModInfo[device].AntiDeadZone = RSAntiDeadZone;
            store.LSModInfo[device].MaxZone = Math.Min(Math.Max(LSMaxZone, 0), 100);
            store.RSModInfo[device].MaxZone = Math.Min(Math.Max(RSMaxZone, 0), 100);
            store.LSModInfo[device].VerticalScale = Math.Min(Math.Max(LSVerticalScale, 0.0), 200.0);
            store.RSModInfo[device].VerticalScale = Math.Min(Math.Max(RSVerticalScale, 0.0), 200.0);
            store.LSModInfo[device].MaxOutput = Math.Min(Math.Max(LSMaxOutput, 0.0), 100.0);
            store.RSModInfo[device].MaxOutput = Math.Min(Math.Max(RSMaxOutput, 0.0), 100.0);
            store.LSModInfo[device].MaxOutputForce = LSMaxOutputForce;
            store.RSModInfo[device].MaxOutputForce = RSMaxOutputForce;
            store.LSModInfo[device].OuterBindDeadZone = Math.Min(Math.Max(LSOuterBindDead, 0), 100);
            store.RSModInfo[device].OuterBindDeadZone = Math.Min(Math.Max(RSOuterBindDead, 0), 100);
            store.LSModInfo[device].OuterBindInvert = LSOuterBindInvert;
            store.RSModInfo[device].OuterBindInvert = RSOuterBindInvert;
            store.LSModInfo[device].DZType = LSDeadZoneType;
            store.RSModInfo[device].DZType = RSDeadZoneType;
            store.LSModInfo[device].XAxisDeadInfo.DeadZone = Math.Min(Math.Max(LSAxialDeadOptions.DeadZoneX, 0), 127);
            store.LSModInfo[device].YAxisDeadInfo.DeadZone = Math.Min(Math.Max(LSAxialDeadOptions.DeadZoneY, 0), 127);
            store.LSModInfo[device].XAxisDeadInfo.MaxZone = Math.Min(Math.Max(LSAxialDeadOptions.MaxZoneX, 0), 100);
            store.LSModInfo[device].YAxisDeadInfo.MaxZone = Math.Min(Math.Max(LSAxialDeadOptions.MaxZoneY, 0), 100);
            store.LSModInfo[device].XAxisDeadInfo.AntiDeadZone =
                Math.Min(Math.Max(LSAxialDeadOptions.AntiDeadZoneX, 0), 100);
            store.LSModInfo[device].YAxisDeadInfo.AntiDeadZone =
                Math.Min(Math.Max(LSAxialDeadOptions.AntiDeadZoneY, 0), 100);
            store.LSModInfo[device].XAxisDeadInfo.MaxOutput =
                Math.Min(Math.Max(LSAxialDeadOptions.MaxOutputX, 0.0), 100.0);
            store.LSModInfo[device].YAxisDeadInfo.MaxOutput =
                Math.Min(Math.Max(LSAxialDeadOptions.MaxOutputY, 0.0), 100.0);
            store.RSModInfo[device].XAxisDeadInfo.DeadZone = Math.Min(Math.Max(RSAxialDeadOptions.DeadZoneX, 0), 127);
            store.RSModInfo[device].YAxisDeadInfo.DeadZone = Math.Min(Math.Max(RSAxialDeadOptions.DeadZoneY, 0), 127);
            store.RSModInfo[device].XAxisDeadInfo.MaxZone = Math.Min(Math.Max(RSAxialDeadOptions.MaxZoneX, 0), 100);
            store.RSModInfo[device].YAxisDeadInfo.MaxZone = Math.Min(Math.Max(RSAxialDeadOptions.MaxZoneY, 0), 100);
            store.RSModInfo[device].XAxisDeadInfo.AntiDeadZone =
                Math.Min(Math.Max(RSAxialDeadOptions.AntiDeadZoneX, 0), 100);
            store.RSModInfo[device].YAxisDeadInfo.AntiDeadZone =
                Math.Min(Math.Max(RSAxialDeadOptions.AntiDeadZoneY, 0), 100);
            store.RSModInfo[device].XAxisDeadInfo.MaxOutput =
                Math.Min(Math.Max(RSAxialDeadOptions.MaxOutputX, 0.0), 100.0);
            store.RSModInfo[device].YAxisDeadInfo.MaxOutput =
                Math.Min(Math.Max(RSAxialDeadOptions.MaxOutputY, 0.0), 100.0);
            store.SXDeadzone[device] = SXDeadZone;
            store.SZDeadzone[device] = SZDeadZone;
            store.SXMaxzone[device] = Math.Min(Math.Max(SXMaxZone * 0.01, 0.0), 1.0);
            store.SZMaxzone[device] = Math.Min(Math.Max(SZMaxZone * 0.01, 0.0), 1.0);
            store.SXAntiDeadzone[device] = Math.Min(Math.Max(SXAntiDeadZone * 0.01, 0.0), 1.0);
            store.SZAntiDeadzone[device] = Math.Min(Math.Max(SZAntiDeadZone * 0.01, 0.0), 1.0);

            store.LSSens[device] = Sensitivity.LSSens;
            store.RSSens[device] = Sensitivity.RSSens;
            store.L2Sens[device] = Sensitivity.L2Sens;
            store.R2Sens[device] = Sensitivity.R2Sens;
            store.SXSens[device] = Sensitivity.SXSens;
            store.SZSens[device] = Sensitivity.SZSens;

            lightInfo.ChargingType = ChargingType;
            store.ButtonMouseInfos[device].mouseAccel = MouseAcceleration;
            //ShiftModifier
            store.LaunchProgram[device] = LaunchProgram;
            store.DirectInputOnly[device] = DinputOnly;
            store.StartTouchpadOff[device] = StartTouchpadOff;

            store.SATriggers[device] = SATriggers;
            store.SATriggerCondition[device] = store.SaTriggerCondValue(SATriggerCond);
            store.SASteeringWheelEmulationAxis[device] = SASteeringWheelEmulationAxis;
            store.SASteeringWheelEmulationRange[device] = SASteeringWheelEmulationRange;

            store.WheelSmoothInfo[device].Enabled = SASteeringWheelSmoothingOptions.SASteeringWheelUseSmoothing;
            store.WheelSmoothInfo[device].MinCutoff = SASteeringWheelSmoothingOptions.SASteeringWheelSmoothMinCutoff;
            store.WheelSmoothInfo[device].Beta = SASteeringWheelSmoothingOptions.SASteeringWheelSmoothBeta;

            store.SAWheelFuzzValues[device] = SASteeringWheelFuzz is >= 0 and <= 100 ? SASteeringWheelFuzz : 0;

            store.GyroOutputMode[device] = GyroOutputMode;

            store.GyroControlsInfo[device].Triggers = GyroControlsSettings.Triggers;
            store.GyroControlsInfo[device].TriggerCond = store.SaTriggerCondValue(GyroControlsSettings.TriggerCond);
            store.GyroControlsInfo[device].TriggerTurns = GyroControlsSettings.TriggerTurns;
            store.GyroControlsInfo[device].TriggerToggle = GyroControlsSettings.Toggle;

            store.SAMouseStickTriggers[device] = GyroMouseStickTriggers;
            store.SAMouseStickTriggerCond[device] = store.SaTriggerCondValue(GyroMouseStickTriggerCond);
            store.GyroMouseStickTriggerTurns[device] = GyroMouseStickTriggerTurns;
            store.GyroMouseStickHorizontalAxis[device] = Math.Min(Math.Max(0, GyroMouseStickHAxis), 1);
            store.GyroMouseStickInfo[device].DeadZone = GyroMouseStickDeadZone;
            store.GyroMouseStickInfo[device].MaxZone = Math.Max(GyroMouseStickMaxZone, 1);
            store.GyroMouseStickInfo[device].outputStick = GyroMouseStickOutputStick;
            store.GyroMouseStickInfo[device].outputStickDir = GyroMouseStickOutputStickAxes;
            store.GyroMouseStickInfo[device].AntiDeadX = GyroMouseStickAntiDeadX;
            store.GyroMouseStickInfo[device].AntiDeadY = GyroMouseStickAntiDeadY;
            store.GyroMouseStickInfo[device].Inverted = GyroMouseStickInvert;
            //store.SetGyroMouseStickToggle(device, GyroMouseStickToggle, control)
            store.GyroMouseStickInfo[device].MaxOutput = Math.Min(Math.Max(GyroMouseStickMaxOutput, 0.0), 100.0);
            store.GyroMouseStickInfo[device].MaxOutputEnabled = GyroMouseStickMaxOutputEnabled;
            store.GyroMouseStickInfo[device].VertScale = GyroMouseStickVerticalScale;
            store.GyroMouseStickInfo[device].UseSmoothing = GyroMouseStickSmoothingSettings.UseSmoothing;
            store.GyroMouseStickInfo[device].DetermineSmoothMethod(GyroMouseStickSmoothingSettings.SmoothingMethod);
            store.GyroMouseStickInfo[device].SmoothWeight = Math.Min(
                Math.Max(0.0, Convert.ToDouble(GyroMouseStickSmoothingSettings.SmoothingWeight * 0.01)), 1.0);
            store.GyroMouseStickInfo[device].minCutoff =
                Math.Min(Math.Max(0.0, GyroMouseStickSmoothingSettings.SmoothingMinCutoff), 100.0);
            store.GyroMouseStickInfo[device].beta =
                Math.Min(Math.Max(0.0, GyroMouseStickSmoothingSettings.SmoothingBeta), 1.0);

            store.GyroSwipeInfo[device].deadzoneX = GyroSwipeSettings.DeadZoneX;
            store.GyroSwipeInfo[device].deadzoneY = GyroSwipeSettings.DeadZoneY;
            store.GyroSwipeInfo[device].triggers = GyroSwipeSettings.Triggers;
            store.GyroSwipeInfo[device].triggerCond = store.SaTriggerCondValue(GyroSwipeSettings.TriggerCond);
            store.GyroSwipeInfo[device].triggerTurns = GyroSwipeSettings.TriggerTurns;
            store.GyroSwipeInfo[device].xAxis = GyroSwipeSettings.XAxis;
            store.GyroSwipeInfo[device].delayTime = GyroSwipeSettings.DelayTime;

            store.TouchOutMode[device] = TouchpadOutputMode;
            store.TouchDisInvertTriggers[device] = TouchDisInvTriggers;
            store.GyroSensitivity[device] = GyroSensitivity;
            store.GyroSensVerticalScale[device] = GyroSensVerticalScale;
            store.GyroInvert[device] = GyroInvert;
            store.GyroTriggerTurns[device] = GyroTriggerTurns;

            store.GyroMouseInfo[device].enableSmoothing = GyroMouseSmoothingSettings.UseSmoothing;
            store.GyroMouseInfo[device].DetermineSmoothMethod(GyroMouseSmoothingSettings.SmoothingMethod);
            store.GyroMouseInfo[device].smoothingWeight =
                Math.Min(Math.Max(0.0, Convert.ToDouble(GyroMouseSmoothingSettings.SmoothingWeight * 0.01)), 1.0);
            store.GyroMouseInfo[device].minCutoff =
                Math.Min(Math.Max(0.0, GyroMouseSmoothingSettings.SmoothingMinCutoff), 100.0);
            store.GyroMouseInfo[device].beta = Math.Min(Math.Max(0.0, GyroMouseSmoothingSettings.SmoothingBeta), 1.0);

            store.GyroMouseHorizontalAxis[device] = Math.Min(Math.Max(0, GyroMouseHAxis), 1);
            //store.SetGyroMouseDZ(device, temp, control);
            store.GyroMouseInfo[device].minThreshold = Math.Min(Math.Max(GyroMouseMinThreshold, 1.0), 40.0);
            //SetGyroMouseToggle(device, temp, control);
            store.BluetoothPollRate[device] = BTPollRate is >= 0 and <= 16 ? BTPollRate : 4;

            store.LSOutBezierCurveObj[device] = LSOutputCurveCustom;
            store.SetLsOutCurveMode(device, store.StickOutputCurveId(LSOutputCurveMode));
            store.RSOutBezierCurveObj[device] = RSOutputCurveCustom;
            store.SetRsOutCurveMode(device, store.StickOutputCurveId(RSOutputCurveMode));

            store.SquStickInfo[device].LSMode = LSSquareStick;
            store.SquStickInfo[device].LSRoundness = SquareStickRoundness;
            store.SquStickInfo[device].RSRoundness = SquareRStickRoundness;
            store.SquStickInfo[device].RSMode = RSSquareStick;
            store.LSAntiSnapbackInfo[device].Enabled = LSAntiSnapback;
            store.RSAntiSnapbackInfo[device].Enabled = RSAntiSnapback;
            store.LSAntiSnapbackInfo[device].Delta = LSAntiSnapbackDelta;
            store.RSAntiSnapbackInfo[device].Delta = RSAntiSnapbackDelta;
            store.LSAntiSnapbackInfo[device].Timeout = LSAntiSnapbackTimeout;
            store.RSAntiSnapbackInfo[device].Timeout = RSAntiSnapbackTimeout;
            store.LSOutputSettings[device].Mode = LSOutputMode;
            store.RSOutputSettings[device].Mode = RSOutputMode;

            store.LSOutputSettings[device].OutputSettings.flickSettings.realWorldCalibration =
                LSOutputSettings.FlickStickSettings.RealWorldCalibration;
            store.LSOutputSettings[device].OutputSettings.flickSettings.flickThreshold =
                LSOutputSettings.FlickStickSettings.FlickThreshold;
            store.LSOutputSettings[device].OutputSettings.flickSettings.flickTime =
                LSOutputSettings.FlickStickSettings.FlickTime;
            store.LSOutputSettings[device].OutputSettings.flickSettings.minAngleThreshold =
                LSOutputSettings.FlickStickSettings.MinAngleThreshold;
            store.RSOutputSettings[device].OutputSettings.flickSettings.realWorldCalibration =
                RSOutputSettings.FlickStickSettings.RealWorldCalibration;
            store.RSOutputSettings[device].OutputSettings.flickSettings.flickThreshold =
                RSOutputSettings.FlickStickSettings.FlickThreshold;
            store.RSOutputSettings[device].OutputSettings.flickSettings.flickTime =
                RSOutputSettings.FlickStickSettings.FlickTime;
            store.RSOutputSettings[device].OutputSettings.flickSettings.minAngleThreshold =
                RSOutputSettings.FlickStickSettings.MinAngleThreshold;

            store.L2OutBezierCurveObj[device] = L2OutputCurveCustom;
            store.SetL2OutCurveMode(device, store.StickOutputCurveId(L2OutputCurveMode));
            store.L2OutputSettings[device].TwoStageMode = L2TwoStageMode;
            store.L2OutputSettings[device].hipFireMS = Math.Min(Math.Max(0, L2HipFireTime), 5000);
            store.L2OutputSettings[device].TriggerEffect = L2TriggerEffect;

            store.R2OutBezierCurveObj[device] = R2OutputCurveCustom;
            store.SetR2OutCurveMode(device, store.StickOutputCurveId(R2OutputCurveMode));
            store.R2OutputSettings[device].TwoStageMode = R2TwoStageMode;
            store.R2OutputSettings[device].TriggerEffect = R2TriggerEffect;
            store.R2OutputSettings[device].hipFireMS = Math.Min(Math.Max(0, R2HipFireTime), 5000);

            store.SXOutBezierCurveObj[device] = SXOutputCurveCustom;
            store.SetSXOutCurveMode(device, store.StickOutputCurveId(SXOutputCurveMode));
            store.SZOutBezierCurveObj[device] = SZOutputCurveCustom;
            store.SetSZOutCurveMode(device, store.StickOutputCurveId(SZOutputCurveMode));
            store.TrackballMode[device] = TrackballMode;
            store.TrackballFriction[device] = TrackballFriction;

            store.TouchPadRelMouse[device].Rotation =
                Math.Min(Math.Max(TouchRelMouseRotation, -180), 180) * Math.PI / 180.0;
            store.TouchPadRelMouse[device].MinThreshold = Math.Min(Math.Max(TouchRelMouseMinThreshold, 1.0), 40.0);

            store.TouchPadAbsMouse[device].MaxZoneX = TouchpadAbsMouseSettings.MaxZoneX;
            store.TouchPadAbsMouse[device].MaxZoneY = TouchpadAbsMouseSettings.MaxZoneY;
            store.TouchPadAbsMouse[device].SnapToCenter = TouchpadAbsMouseSettings.SnapToCenter;

            store.OutputDeviceType[device] = OutputContDevice;

            //
            // TODO: to be continued
            // 
        }
    }
}