using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using MQTTnet.Client;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.TestApp.UWPMemory.Annotations;
using MqttClientConnectedEventArgs = MQTTnet.Client.MqttClientConnectedEventArgs;
using MqttClientDisconnectedEventArgs = MQTTnet.Client.MqttClientDisconnectedEventArgs;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x407 dokumentiert.

namespace MQTTnet.TestApp.UWPMemory
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private IMqttServer _mqttServer;
        private IMqttClient _mqttClient1;
        private IMqttClient _mqttClient2;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private long _duration;

        public long Duration
        {
            get { return _duration; }
            set { _duration = value; OnPropertyChanged();}
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


        public async Task StartTest(int numberOfMessages)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => Duration = 0);
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            for (int i = 0; i < numberOfMessages; i++)
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("test")
                    .WithPayload($"Message {i}")
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
                    .WithRetainFlag(false)
                    .Build();
                //await _mqttClient1.PublishAsync(message).ConfigureAwait(false);

                await _mqttClient1.PublishAsync(message);
            }
            stopwatch.Stop();
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => Duration = stopwatch.ElapsedMilliseconds);
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
            await StartTest(5000);
        }

        private async void ButtonSendLoads_OnClick(object sender, RoutedEventArgs e)
        {
            await StartTest(50000);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
