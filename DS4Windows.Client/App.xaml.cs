﻿using DS4Windows.Client.Core;
using DS4Windows.Client.Modules.Main;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Windows;
using DS4Windows.Client.Core.ViewModel;
using DS4Windows.Client.Modules.Controllers.Utils;
using DS4Windows.Client.Modules.Profiles.Utils;
using DS4Windows.Client.Modules.Settings;
using DS4Windows.Client.ServiceClients;
using DS4Windows.Shared.Common;
using DS4Windows.Shared.Common.Tracing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DS4Windows.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            await ApplicationStartup.Start(
                new []
                {
                    typeof(ViewModelRegistrar),
                    typeof(ProfilesModuleRegistrar),
                    typeof(ControllersModuleRegistrar),
                    typeof(MainModuleRegistrar),
                    typeof(SettingsModuleRegistrar),
                    typeof(ServiceClientsRegistrar),
                    typeof(OpenTelemetryRegistrar),
                    typeof(CommonRegistrar)
                },
                StartMainView);
        }

        private async Task StartMainView(IServiceScope scope)
        {
            var controllerService = scope.ServiceProvider.GetService<IControllerServiceClient>();
            await controllerService.WaitForService();
            var client = scope.ServiceProvider.GetService<IProfileServiceClient>();
            await client.Initialize();

            var viewModelFactory = scope.ServiceProvider.GetRequiredService<IViewModelFactory>();
            var viewModel = await viewModelFactory.Create<IMainViewModel, IMainView>();
            if (viewModel.MainView is Window windowViewModel)
            {
                windowViewModel.Show();
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await ApplicationStartup.Shutdown();
            base.OnExit(e);
            Process.GetCurrentProcess().Kill();
        }
    }
}
