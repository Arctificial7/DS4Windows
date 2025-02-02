﻿using System.Runtime.InteropServices;
using System.ServiceProcess;
using DS4Windows.Server.Controller;
using DS4Windows.Shared.Common.Services;
using DS4Windows.Shared.Devices.HostedServices;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Options;
using PInvoke;
using Serilog;

namespace DS4Windows.Server.Host
{
    public class DS4WindowsServiceLifetime : WindowsServiceLifetime
    {
        private const string SYSTEM = "SYSTEM";
        private readonly ControllerManagerHost controllerHost;
        private readonly IGlobalStateService globalStateService;
        private readonly IControllerMessageForwarder controllerMessageForwarder;

        public DS4WindowsServiceLifetime(
            IHostEnvironment environment, 
            IHostApplicationLifetime applicationLifetime, 
            ILoggerFactory loggerFactory, 
            IOptions<HostOptions> optionsAccessor, 
            IOptions<WindowsServiceLifetimeOptions> windowsServiceOptionsAccessor, 
            ControllerManagerHost controllerHost,
            IGlobalStateService globalStateService,
            IControllerMessageForwarder controllerMessageForwarder) : base(environment, applicationLifetime, loggerFactory, optionsAccessor, windowsServiceOptionsAccessor)
        {
            ControllerManagerHost.IsEnabled = true;
            CanHandleSessionChangeEvent = true;
            CanShutdown = true;
            CanStop = true;
            this.controllerHost = controllerHost;
            this.globalStateService = globalStateService;
            this.controllerMessageForwarder = controllerMessageForwarder;
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
            var currentSession = Kernel32.WTSGetActiveConsoleSessionId();
            if (currentSession != 0)
            {
                var userName = GetUsername((int)currentSession);
                Log.Debug($"On start user session {userName} found");
                if (userName != SYSTEM)
                {
                    Log.Debug("user session is not SYSTEM.  starting controller host");
                    StartHost(userName);
                }
            }
            else
            {
                Log.Debug($"No user session found on start, do not start controller host");
            }
        }

        protected override async void OnStop()
        {
            base.OnStop();
            await StopHost();
        }

        protected override async void OnSessionChange(SessionChangeDescription changeDescription)
        {
            Log.Debug($"lifetime session change {changeDescription.Reason}");
            base.OnSessionChange(changeDescription);

            if (changeDescription.Reason == SessionChangeReason.SessionLogon || changeDescription.Reason == SessionChangeReason.SessionUnlock)
            {
                var userName = GetUsername(changeDescription.SessionId);
                Log.Debug($"found current user {userName}");
                StartHost(userName);

            }
            else if (changeDescription.Reason == SessionChangeReason.SessionLogoff ||
                     changeDescription.Reason == SessionChangeReason.SessionLock)
            {
                await StopHost();
            }
        }
        
        private async void StartHost(string currentUserName)
        {
            if (!controllerHost.IsRunning)
            {
                globalStateService.CurrentUserName = currentUserName;
                Log.Debug("starting controller host");
                await controllerHost.StartAsync();
            }
        }

        private async Task StopHost()
        {
            if (controllerHost.IsRunning)
            {
                Log.Debug("stopping controller host");
                await controllerHost.StopAsync();
            }
        }
        
        private static string GetUsername(int sessionId)
        {
            string username = "SYSTEM";
            if (WTSQuerySessionInformation(IntPtr.Zero, sessionId, WtsInfoClass.WTSUserName, out var buffer, out var strLen) && strLen > 1)
            {
                username = Marshal.PtrToStringAnsi(buffer);
                WTSFreeMemory(buffer);
            }
            return username;
        }

        [DllImport("Wtsapi32.dll")]
        private static extern bool WTSQuerySessionInformation(IntPtr hServer, int sessionId, WtsInfoClass wtsInfoClass, out IntPtr ppBuffer, out int pBytesReturned);
        [DllImport("Wtsapi32.dll")]
        private static extern void WTSFreeMemory(IntPtr pointer);

        private enum WtsInfoClass
        {
            WTSUserName = 5
        }
    }
}
