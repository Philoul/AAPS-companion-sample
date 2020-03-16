using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Xamarin.Forms;
using Samsung.Sap;
using System.Linq;
using System.Diagnostics;
using Tizen.Wearable.CircularUI.Forms;
using System.Text.Json;

namespace AndroidAPS_CompanionXAML
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private Agent agent;
        private Connection connection;
        private Peer peer;
        private string TAG = "AAPS_CompanionXAML";

        public MainPageViewModel()
        {
            ConnectCommand = new Command(Connect);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        string receivedMessage = "Displaying received messages here...";
        double bgValue = 0;

        public string ReceivedMessage
        {
            get => receivedMessage;
            set
            {
                receivedMessage = value;
                var args = new PropertyChangedEventArgs(nameof(ReceivedMessage));
                PropertyChanged?.Invoke(this, args);
            }
        }

        public double BgValue
        {
            get => bgValue;
            set
            {
                bgValue = value;
                var args = new PropertyChangedEventArgs(nameof(BgValue));
                PropertyChanged?.Invoke(this, args);
            }
        }

        public Command ConnectCommand { get; set; }

        private async void Connect()
        {
            try
            {
                agent = await Agent.GetAgent("/sample/hello");
                agent.PeerStatusChanged += PeerStatusChanged;
                var peers = await agent.FindPeers();
                if (peers.Count() > 0)
                {
                    peer = peers.First();
                    connection = peer.Connection;
                    connection.DataReceived -= Connection_DataReceived;
                    connection.DataReceived += Connection_DataReceived;
                    connection.StatusChanged -= Connection_StatusChanged;
                    connection.StatusChanged += Connection_StatusChanged;
                    await connection.Open();
                }
                else
                {
                    ShowMessage("Any peer not found");
                }
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, ex.ToString());
            }
        }

        private void PeerStatusChanged(object sender, PeerStatusEventArgs e)
        {
            if (e.Peer == peer)
            {
                ShowMessage($"Peer Available: {e.Available}, Status: {e.Peer.Status}");
            }
        }

        private void Connection_DataReceived(object sender, Samsung.Sap.DataReceivedEventArgs e)
        {
            //ShowMessage(Encoding.UTF8.GetString(e.Data));
            string s = Encoding.UTF8.GetString(e.Data);
            ReceivedMessage = s;
            BgReading bgReading = JsonSerializer.Deserialize<BgReading>(s);
            BgValue = bgReading.value;
            Tizen.Log.Debug(TAG, s);
            Tizen.Log.Debug(TAG, bgReading.value.ToString());
            Tizen.Log.Debug(TAG, bgReading.time);
            Tizen.Log.Debug(TAG, bgReading.direction);
        }

        private void Connection_StatusChanged(object sender, ConnectionStatusEventArgs e)
        {
            ShowMessage(e.Reason.ToString());

            if (e.Reason == ConnectionStatus.ConnectionClosed ||
                e.Reason == ConnectionStatus.ConnectionLost)
            {
                connection.DataReceived -= Connection_DataReceived;
                connection.StatusChanged -= Connection_StatusChanged;
                connection.Close();
                connection = null;
                peer = null;
                agent = null;
            }
        }

        private void Disconnect()
        {
            if (connection != null)
            {
                connection.Close();
            }
        }

        private void Fetch()
        {
            if (connection != null && agent != null && agent.Channels.Count > 0)
            {
                connection.Send(agent.Channels.First().Value, Encoding.UTF8.GetBytes("Hello Accessory!"));
            }
        }

        private void ShowMessage(string message, string debugLog = null)
        {
            Toast.DisplayText(message, 3000);
            if (debugLog != null)
            {
                debugLog = message;
            }
            Debug.WriteLine("[DEBUG] " + message);
        }

        public class BgReading
        {
            public double value { get; set; }
            public string time { get; set; }
            public string direction { get; set; }
        }
    }
}
