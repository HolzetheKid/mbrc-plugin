﻿using System;
using MusicBeePlugin.AndroidRemote.Entities;
using MusicBeePlugin.AndroidRemote.Interfaces;
using MusicBeePlugin.AndroidRemote.Networking;

namespace MusicBeePlugin.AndroidRemote.Commands.InstaReplies
{
    public class PingReply : ICommand
    {
        public void Execute(IEvent eEvent)
        {
            var message = new SocketMessage(Constants.Pong, string.Empty)
                .ToJsonString();
            SocketServer.Instance.Send(message);
        }
    }
}