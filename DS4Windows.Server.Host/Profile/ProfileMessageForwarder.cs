﻿using System.Net.WebSockets;
using DS4Windows.Server.Profile;
using DS4Windows.Shared.Configuration.Profiles.Services;

namespace DS4Windows.Server.Host.Profile
{
    public class ProfileMessageForwarder : IProfileMessageForwarder
    {
        private readonly IProfilesService profilesService;
        private WebSocket socket;

        public ProfileMessageForwarder(IProfilesService profilesService)
        {
            this.profilesService = profilesService;
        }

        public async Task StartListening(WebSocket newSocket)
        {
            socket = newSocket;
            var waitSource = new TaskCompletionSource<object>();
            await waitSource.Task;
        }
        
    }
}
