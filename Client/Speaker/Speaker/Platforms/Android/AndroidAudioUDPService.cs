//using Android.Media;
//using Microsoft.Maui.Controls;
//using Speaker.Android.Service;
//using Speaker.Models;
//using Speaker.Services;
//using System.Net;
//using System.Net.Sockets;
//using System.Threading;

//namespace Speaker.Android.Service
//{

//    public class AndroidAudioUDPService : IAudioUDPService
//    {
//        private AudioRecord? recorder;
//        public bool isRecording { get; set; }
//        private Thread? recordThread;
//        private Thread? receiveThread;
//        private Thread? heartThread;

//        private readonly UdpClient udpSender = new();
//        private readonly UdpClient udpReceiver = new(55250);
//        private readonly List<byte> frameBuffer = new();
//        private readonly int frameSize = 320;
//        public string User { get; set; }
//        private bool isConnected = false;

//        public AndroidAudioUDPService()
//        {
//            this.User = "Defualt";
//        }

//        public bool Connect()
//        {
//            ConnectToServer();
//            return isConnected;
//        }

//        public void Start()
//        {
//            udpReceiver.Client.ReceiveBufferSize = 1024 * 1024;
//            StartHeartbeat();
//            StartRecording();
//            receiveThread = new Thread(async () => await StartReceiving());
//            receiveThread.IsBackground = false;
//            receiveThread.Start();
//            udpReceiver.Send(System.Text.Encoding.UTF8.GetBytes("receive"), ConfigConstants.ServerIP, ConfigConstants.ServerPort);
//        }

//        private async void StartRecording()
//        {
//            var status = await Permissions.RequestAsync<Permissions.Microphone>();
//            if (status != PermissionStatus.Granted)
//            {
//                Console.WriteLine("Microphone permission not granted");
//                return;
//            }

//            int sampleRate = 16000;
//            int bufferSize = AudioRecord.GetMinBufferSize(sampleRate, ChannelIn.Mono, Encoding.Pcm16bit);

//            if (bufferSize <= 0)
//            {
//                Console.WriteLine("Invalid buffer size");
//                return;
//            }

//            recorder = new AudioRecord(AudioSource.Mic, sampleRate, ChannelIn.Mono, Encoding.Pcm16bit, bufferSize);

//            if (recorder.State != State.Initialized)
//            {
//                Console.WriteLine("AudioRecord initialization failed");
//                return;
//            }

//            recorder.StartRecording();

//            recordThread = new Thread(() =>
//            {
//                byte[] buffer = new byte[bufferSize];
//                while (true)
//                {
//                    if (!isRecording) continue;
//                    int read = recorder.Read(buffer, 0, buffer.Length);
//                    if (read > 0)
//                    {
//                        lock (frameBuffer)
//                        {
//                            frameBuffer.AddRange(buffer.Take(read));
//                            while (frameBuffer.Count >= frameSize)
//                            {
//                                byte[] frame = frameBuffer.Take(frameSize).ToArray();
//                                frameBuffer.RemoveRange(0, frameSize);
//                                SendAudioData(frame);
//                            }
//                        }
//                    }
//                }
//            });
//            recordThread.Start();
//        }
//        private void StartHeartbeat()
//        {
//            heartThread = new Thread(() =>
//            {
//                while (true)
//                {
//                    byte[] heartbeat = System.Text.Encoding.UTF8.GetBytes($"heartbeat:{User}");
//                    SendAudioDataAsync(heartbeat);
//                    Console.WriteLine("heart beat sent");
//                    Thread.Sleep(3000);
//                }
//            });
//            heartThread.Start();
//        }

//        private async Task StartReceiving()
//        {
//            System.Diagnostics.Debug.WriteLine($"udpReceiver listening on: {((IPEndPoint)udpReceiver.Client.LocalEndPoint)!}");
//            while (true)
//            {
//                try
//                {
//                    //var result = await udpReceiver.ReceiveAsync();
//                    //System.Diagnostics.Debug.WriteLine($"Received {result.Buffer.Length} bytes at {DateTime.Now}");
//                    Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId},,,,111111111111111111111");
//                    Task.Delay(1000).Wait();
//                    // 可选：播放或处理音频数据
//                }
//                catch (Exception e)
//                {
//                    System.Diagnostics.Debug.WriteLine(e.Message);
//                }
//            }
//        }

//        public void RecordAndSend()
//        {
//            isRecording = true;
//        }
//        public void StopRecordSend()
//        {
//            isRecording = false;
//        }
//        private void SendAudioData(byte[] data)
//        {
//            try
//            {
//                udpSender.Send(data, ConfigConstants.ServerIP, ConfigConstants.ServerPort);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"发送失败: {ex.Message}");
//            }
//        }

//        private async void SendAudioDataAsync(byte[] data)
//        {
//            try
//            {
//                await udpSender.SendAsync(data, ConfigConstants.ServerIP, ConfigConstants.ServerPort);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"发送失败: {ex.Message}");
//            }
//        }

//        private void ConnectToServer()
//        {
//            try
//            {
//                int localPort = ((IPEndPoint)udpReceiver.Client.LocalEndPoint!).Port;
//                byte[] testData = System.Text.Encoding.UTF8.GetBytes($"handshake:{localPort}");
//                udpSender.Send(testData, ConfigConstants.ServerIP, ConfigConstants.ServerPort);
//                Console.WriteLine($"正在连接服务器：{ConfigConstants.ServerIP}:{ConfigConstants.ServerPort}...");

//                udpSender.ReceiveAsync().ContinueWith(receiveTask =>
//                {
//                    var receivedResult = receiveTask.Result;
//                    string message = System.Text.Encoding.UTF8.GetString(receivedResult.Buffer);
//                    Console.WriteLine($"收到服务器消息: {message}");
//                    if (message == "handshake_ack")
//                    {
//                        isConnected = true;
//                        Console.WriteLine($"已连接到服务器。接收端口：{localPort}");
//                    }
//                });
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"连接失败: {ex.Message}");
//            }
//        }

//        public void Dispose()
//        {
//            isRecording = false;
//            recorder?.Stop();
//            recorder?.Release();
//            udpSender.Close();
//            udpReceiver.Close();
//            udpSender.Dispose();
//            udpReceiver.Dispose();
//        }

//        public void SetUser(string username)
//        {
//            User = username;
//        }
//    }

//}

using Android.Media;
using Microsoft.Maui.Controls;
using Newtonsoft.Json;
using Speaker.Models;
using Speaker.Services;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Speaker.AndroidPlatform.Service
{
    public class AndroidAudioUDPService : IAudioUDPService
    {
        private AudioRecord? recorder;
        private AudioTrack? player;

        private CancellationTokenSource? recordCts;
        private CancellationTokenSource? receiveCts;
        private CancellationTokenSource? heartbeatCts;

        private readonly UdpClient udpSender = new(0);
        private readonly UdpClient udpReceiver = new(0);
        private readonly List<byte> frameBuffer = new();
        private readonly int frameSize = 320;

        public bool IsConnected { get; set; }
        public string User { get; set; } = "Default";
        public bool isRecording { get; set; } = false;
        public string SpeakTo { get; set; } = "all";

        public async Task<bool> ConnectAsync()
        {
            try
            {
                int localPort = ((IPEndPoint)udpReceiver.Client.LocalEndPoint!).Port;
                byte[] handshake = System.Text.Encoding.UTF8.GetBytes($"handshake:{localPort}");
                await udpSender.SendAsync(handshake, ConfigConstants.ServerIP, ConfigConstants.ServerPort);
                // 创建一个接收任务
                var receiveTask = udpSender.ReceiveAsync();

                // 创建一个超时任务（3秒）
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(3));

                // 等待任意一个任务完成
                var completedTask = await Task.WhenAny(receiveTask, timeoutTask);
                if (completedTask == receiveTask)
                {
                    // 成功接收到消息
                    var result = await receiveTask; // 获取结果
                    string message = System.Text.Encoding.UTF8.GetString(result.Buffer);
                    IsConnected = message == "handshake_ack";
                    Console.WriteLine($"连接状态: {IsConnected}, 接收端口: {localPort}");
                }
                else
                {
                    // 超时
                    throw new TimeoutException("连接超时：未收到服务器响应");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"连接失败: {ex.Message}");
            }
            return IsConnected;
        }

        public void Start()
        {
            udpReceiver.Client.ReceiveBufferSize = 1024 * 1024;

            heartbeatCts = new CancellationTokenSource();
            _ = Task.Run(() => HeartbeatLoopAsync(heartbeatCts.Token));

            _ = StartRecordingAsync();
            _ = StartPlaybackAsync();

            receiveCts = new CancellationTokenSource();
            _ = Task.Run(() => ReceiveLoopAsync(receiveCts.Token));
        }

        public void RecordAndSend()
        {
            isRecording = true;
            //change user state to speaking
            udpSender.Send(new byte[] { 0x001 }, ConfigConstants.ServerIP, ConfigConstants.ServerPort);
        }
        public void StopRecordSend()
        {
            isRecording = false;
            //change user state to not speaking
            udpSender.Send(new byte[] { 0x002 }, ConfigConstants.ServerIP, ConfigConstants.ServerPort);
        }
        public void SetUser(string username) => User = username;

        public void Dispose()
        {
            receiveCts?.Cancel();
            heartbeatCts?.Cancel();
            recordCts?.Cancel();

            isRecording = false;
            recorder?.Stop();
            recorder?.Release();
            player?.Stop();
            player?.Release();

            //udpSender.Close();
            //udpReceiver.Close();
            //udpSender.Dispose();
            //udpReceiver.Dispose();
        }

        private async Task StartRecordingAsync()
        {
            var status = await Permissions.RequestAsync<Permissions.Microphone>();
            if (status != PermissionStatus.Granted)
            {
                Console.WriteLine("Microphone permission not granted");
                return;
            }

            int sampleRate = 16000;
            int bufferSize = AudioRecord.GetMinBufferSize(sampleRate, ChannelIn.Mono, Encoding.Pcm16bit);
            if (bufferSize <= 0)
            {
                Console.WriteLine("Invalid buffer size");
                return;
            }

            recorder = new AudioRecord(AudioSource.Mic, sampleRate, ChannelIn.Mono, Encoding.Pcm16bit, bufferSize);
            if (recorder.State != State.Initialized)
            {
                Console.WriteLine("AudioRecord initialization failed");
                return;
            }

            recorder.StartRecording();
            recordCts = new CancellationTokenSource();
            _ = Task.Run(() => RecordLoopAsync(bufferSize, recordCts.Token));
        }

        private async Task StartPlaybackAsync()
        {
            int sampleRate = 16000;
            int bufferSize = AudioTrack.GetMinBufferSize(sampleRate, ChannelOut.Mono, Encoding.Pcm16bit);
            player = new AudioTrack(

                Android.Media.Stream.Music,
                sampleRate,
                ChannelOut.Mono,
                Encoding.Pcm16bit,
                bufferSize,
                AudioTrackMode.Stream
            );
            player.Play();
        }

        private async Task RecordLoopAsync(int bufferSize, CancellationToken token)
        {
            byte[] buffer = new byte[bufferSize];
            while (!token.IsCancellationRequested)
            {
                if (!isRecording)
                {
                    await Task.Delay(10, token); // 防止空转
                    continue;
                }

                int read = recorder!.Read(buffer, 0, buffer.Length);
                if (read > 0)
                {
                    lock (frameBuffer)
                    {
                        frameBuffer.AddRange(buffer.Take(read));
                        while (frameBuffer.Count >= frameSize)
                        {
                            byte[] frame = frameBuffer.Take(frameSize).ToArray();
                            frameBuffer.RemoveRange(0, frameSize);
                            SendAudioData(frame);
                        }
                    }
                }
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            Console.WriteLine($"udpReceiver listening on: {((IPEndPoint)udpReceiver.Client.LocalEndPoint)!}");
            udpReceiver.Send(System.Text.Encoding.UTF8.GetBytes("receive"), ConfigConstants.ServerIP, ConfigConstants.ServerPort);
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var result = await udpReceiver.ReceiveAsync(token);
                    if (result.Buffer.Length > 0 && player != null)
                    {
                        player.Write(result.Buffer, 0, result.Buffer.Length);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Console.WriteLine($"接收失败: {ex.Message}");
                }
            }
        }

        private async Task HeartbeatLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                byte[] heartbeat = System.Text.Encoding.UTF8.GetBytes($"heartbeat:{User}");
                await SendAudioDataAsync(heartbeat);
                Console.WriteLine("heart beat sent");
                await Task.Delay(1000, token);
            }
        }

        private void SendAudioData(byte[] data)
        {
            try
            {
                var prefix = new byte[8];
                var speakToBytes = System.Text.Encoding.UTF8.GetBytes(SpeakTo);
                Array.Copy(speakToBytes, prefix, Math.Min(speakToBytes.Length, prefix.Length));

                var newdatas = new byte[data.Length + prefix.Length];
                Buffer.BlockCopy(prefix, 0, newdatas, 0, prefix.Length);
                Buffer.BlockCopy(data, 0, newdatas, prefix.Length, data.Length);
                udpSender.Send(newdatas, ConfigConstants.ServerIP, ConfigConstants.ServerPort);
                newdatas = null;
                prefix = null;
                speakToBytes = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送失败: {ex.Message}");
            }
        }

        private async Task SendAudioDataAsync(byte[] data)
        {
            try
            {
                await udpSender.SendAsync(data, ConfigConstants.ServerIP, ConfigConstants.ServerPort);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送失败: {ex.Message}");
            }
        }
        public async Task<List<UserInfo>?> GetAllUsers()
        {
            var byts = System.Text.Encoding.UTF8.GetBytes("GetUsers");
            try
            {
                await SendAudioDataAsync(byts);
                var res = await udpSender.ReceiveAsync();
                string str = System.Text.Encoding.UTF8.GetString(res.Buffer);
                System.Diagnostics.Debug.WriteLine(str);
                var users = JsonConvert.DeserializeObject<List<UserInfo>>(str);
                return users;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
