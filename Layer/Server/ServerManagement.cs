﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LabFusion.Network;
using LabFusion.Preferences;
using LabFusion.Representation;
using LabFusion.Senders;
using Riptide;
using Unity.Collections;
using LabFusion.Riptide.Utilities;
using RiptideNetworkLayer.Preferences;
using LabFusion.Utilities;
using System.Net;
using static RiptideNetworkLayer.Layer.ClientManagement;
using System.Threading;
using LabFusion.Voice;
using Steamworks;

namespace RiptideNetworkLayer.Layer
{
    public class ServerManagement
    {
        public static Server CurrentServer = new();

        public static void StartServer()
        {
            if (ClientManagement.IsConnecting)
            {
                FusionNotifier.Send(new FusionNotification()
                {
                    showTitleOnPopup = false,
                    message = $"Cannot start server whilst connecting to a server! Either wait for the connection to fail, or stop connecting to a server!",
                    isMenuItem = false,
                    isPopup = true,
                    popupLength = 5f,
                    type = NotificationType.WARNING
                });
                return;
            }

            if (CurrentServer.IsRunning)
                CurrentServer.Stop();

            RiptideNetworkLayer.Instance.ChangeBroadcastingData(new RiptideNetworkLayer.LANData(PlayerIdManager.LocalUsername, RiptidePreferences.LocalServerSettings.ServerPort.GetValue(), true));

            CurrentServer.Start(RiptidePreferences.LocalServerSettings.ServerPort.GetValue(), 256, 0, false);

            CurrentClient.Connected += OnConnectToP2PServer;
            CurrentClient.Connect(
                $"127.0.0.1:{RiptidePreferences.LocalServerSettings.ServerPort.GetValue()}", 5, 0, null, false);
        }

        private static void OnConnectToP2PServer(object sender, EventArgs e)
        {
            CurrentClient.Connected -= OnConnectToP2PServer;

            CurrentClient.TimeoutTime = 60000;
            CurrentClient.Connection.CanQualityDisconnect = false;
            
            PlayerIdManager.SetLongId(CurrentClient.Id);
            
            // Call server setup
            InternalServerHelpers.OnStartServer();

            InternalLayerHelpers.OnUpdateLobby();
        }

        // Hooks
        public static void OnClientDisconnect(object sender, ServerDisconnectedEventArgs e)
        {
            ushort id = e.Client.Id;
            
            if (id != PlayerIdManager.LocalLongId)
            {
                if (PlayerIdManager.HasPlayerId(id))
                {
                    // Update the mod so it knows this user has left
                    InternalServerHelpers.OnUserLeave(id);

                    // Send disconnect notif to everyone
                    ConnectionSender.SendDisconnect(id);
                }
            } else
            {
                RiptideNetworkLayer.Instance.ChangeBroadcastingData(new RiptideNetworkLayer.LANData(PlayerIdManager.LocalUsername, RiptidePreferences.LocalServerSettings.ServerPort.GetValue(), false));
            }
        }

        public static void OnClientConnect(object obj, ServerConnectedEventArgs args)
        {
            args.Client.TimeoutTime = 60000;
            args.Client.CanQualityDisconnect = false;
        }

        /// <summary>
        /// Calls a handler when a message is received based on its MessageId.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="args"></param>
        public static void OnMessageReceived(object sender, MessageReceivedEventArgs message)
        {
            FusionMessageHandler.ReadMessage(message.Message.GetBytes(), true);
        }
    }
}
