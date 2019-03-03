using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MqttClientConnectedEventArgs = MQTTnet.Client.MqttClientConnectedEventArgs;
using MqttClientDisconnectedEventArgs = MQTTnet.Client.MqttClientDisconnectedEventArgs;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x407 dokumentiert.

namespace MemoryLeak
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private IMqttServer _mqttServer;
        private IMqttClient _mqttClient1;
        private IMqttClient _mqttClient2;

        public MainPage()
        {
            this.InitializeComponent();
        }


        private async Task StartServer()
        {
            if (_mqttServer != null)
            {
                return;
            }

            //JsonServerStorage storage = null;
            //if (ServerPersistRetainedMessages.IsChecked == true)
            //{
            //    storage = new JsonServerStorage();

            //    if (ServerClearRetainedMessages.IsChecked == true)
            //    {
            //        storage.Clear();
            //    }
            //}

            _mqttServer = new MqttFactory().CreateMqttServer();

            var options = new MqttServerOptions();
            
            //options.Storage = storage;
            //options.EnablePersistentSessions = ServerAllowPersistentSessions.IsChecked == true;

            await _mqttServer.StartAsync(options);

            _mqttServer.ClientConnected += MqttServerOnClientConnected;
        }

        private void MqttServerOnClientConnected(object sender, MQTTnet.Server.MqttClientConnectedEventArgs mqttClientConnectedEventArgs)
        {
            Debug.WriteLine($"Client {mqttClientConnectedEventArgs.ClientId} connected");
        }

        public async Task ConnectClients()
        {
            var factory = new MqttFactory();
            _mqttClient1 = factory.CreateMqttClient();
            _mqttClient1.ApplicationMessageReceived += OnApplicationMessageReceived;
            _mqttClient1.Connected += OnConnected;
            _mqttClient1.Disconnected += OnDisconnected;

            var options = new MqttClientOptions
            {
                ClientId = "Client1"
            };
            options.ChannelOptions = new MqttClientTcpOptions
            {
                Server = "127.0.0.1",
                Port = 1883,
                
            };

            Debug.WriteLine($"Connect client 1");
            await _mqttClient1.ConnectAsync(options);


            _mqttClient2 = factory.CreateMqttClient();
            _mqttClient2.ApplicationMessageReceived += OnApplicationMessageReceived;
            _mqttClient2.Connected += OnConnected;
            _mqttClient2.Disconnected += OnDisconnected;

            var options2 = new MqttClientOptions
            {
                ClientId = "Client2"
            };
            options2.ChannelOptions = new MqttClientTcpOptions
            {
                Server = "127.0.0.1",
                Port = 1883,

            };

            Debug.WriteLine($"Connect client 2");
            await _mqttClient2.ConnectAsync(options2);
            await _mqttClient2.SubscribeAsync("test", MqttQualityOfServiceLevel.AtMostOnce);
        }


        public async Task StartTest()
        {
            for (int i = 0; i < 5000; i++)
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("test")
                    .WithPayload($"Message {i}")
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
                    .WithRetainFlag(false)
                    .Build();
                await _mqttClient1.PublishAsync(message);
            }
        }


        private void OnDisconnected(object sender, MqttClientDisconnectedEventArgs e)
        {
            Debug.WriteLine($"disconnected");
        }

        private void OnConnected(object sender, MqttClientConnectedEventArgs e)
        {
            Debug.WriteLine($"connected");
        }

        private void OnApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            var payloadString = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
             Debug.WriteLine($"Received {payloadString}");
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            await StartServer();
            await ConnectClients();
            
        }

        private async void ButtonSend_OnClick(object sender, RoutedEventArgs e)
        {
            await StartTest();
        }
    }
}
