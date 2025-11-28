using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Walkie_Talkie.Models;

namespace Walkie_Talkie.Services
{

    public abstract class AudioBase : IAudioUDPService
    {
        public bool IsConnected { get; set; }
        public bool isRecording { get; set; }
        public string User { get; set; } = "Default";
        public string Channel { get; set; } = "Lobby";
        public string SpeakTo { get; set; } = "all";
        public ConcurrentQueue<byte[]> RecordFrameQueue { get; set; }
        public List<byte> frameBuffer { get; set; }
        public UdpClient UDPSender { get; set; }

        public UdpClient UDPReceiver { get; set; }

        public CancellationTokenSource? recordCts { get; set; }
        public CancellationTokenSource? receiveCts { get; set; }
        public CancellationTokenSource? heartbeatCts { get; set; }
        public int frameSize { get; set; } = 320;

        public abstract void Dispose();
        public abstract void AudioInitial();
        public abstract Task ReceiveLoopAsync(CancellationToken token);

        public AudioBase()
        {
            RecordFrameQueue = new ConcurrentQueue<byte[]>();
            UDPSender = new UdpClient();
            UDPReceiver = new UdpClient(0);
            receiveCts = new CancellationTokenSource();
            heartbeatCts = new CancellationTokenSource();
            recordCts = new CancellationTokenSource();
            frameBuffer = new List<byte>();
        }

        public virtual void Start()
        {
            AudioInitial();

            // 启动心跳
            heartbeatCts = new CancellationTokenSource();
            _ = Task.Run(() => HeartbeatLoopAsync(heartbeatCts.Token));

            // 启动接收
            receiveCts = new CancellationTokenSource();
            _ = Task.Run(() => ReceiveLoopAsync(receiveCts.Token));

            // 新增发送线程
            _ = Task.Run(() => SendLoopAsync(receiveCts.Token));

            // 主动发包保持 NAT 映射
            UDPReceiver.Send(Encoding.UTF8.GetBytes("receive"), ConfigConstants.ServerIP, ConfigConstants.ServerPort);

            Console.WriteLine($"udpReceiver 正在监听: {UDPReceiver.Client.LocalEndPoint}");
        }

        public virtual async Task<ServerMessage> ConnectAsync(string username)
        {
            ServerMessage serverMsg = new ServerMessage();
            try
            {
                int localPort = ((IPEndPoint)UDPReceiver.Client.LocalEndPoint!).Port;
                byte[] handshake = Encoding.UTF8.GetBytes($"handshake:{localPort}:{username}");
                await UDPSender.SendAsync(handshake, ConfigConstants.ServerIP, ConfigConstants.ServerPort);

                // 创建一个接收任务
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var receiveTask = UDPSender.ReceiveAsync(cts.Token);
                try
                {
                    // 成功接收到消息
                    var result = await receiveTask; // 获取结果
                    string message = Encoding.UTF8.GetString(result.Buffer);
                    serverMsg.Connected = IsConnected = message == "handshake_ack";
                    if (!IsConnected)
                    {
                        serverMsg = JsonConvert.DeserializeObject<ServerMessage>(message);
                    }
                    else
                    {
                        Console.WriteLine($"连接状态: {IsConnected}, 接收端口: {localPort}");
                        serverMsg.Connected = true;
                    }

                }
                catch (OperationCanceledException)
                {
                    // 超时
                    serverMsg.Connected = false;
                    serverMsg.Reason = "连接超时：超过3秒未收到服务器响应";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"连接失败: {ex.Message}");
            }
            return serverMsg;
        }

        public virtual void SetUser(string username) => User = username;
        public virtual void SetChannel(string channelName) => Channel = channelName;
        public virtual void RecordAndSend()
        {
            isRecording = true;
            //change user state to speaking
            UDPSender.Send(new byte[] { 0x001 }, ConfigConstants.ServerIP, ConfigConstants.ServerPort);
        }
        public virtual void StopRecordSend()
        {
            isRecording = false;
            //change user state to not speaking
            UDPSender.Send(new byte[] { 0x002 }, ConfigConstants.ServerIP, ConfigConstants.ServerPort);
        }

        protected  void SendAudioData(byte[] data)
        {
            try
            {
                var prefix = new byte[8];
                var speakToBytes = Encoding.UTF8.GetBytes(SpeakTo);
                Array.Copy(speakToBytes, prefix, Math.Min(speakToBytes.Length, prefix.Length));

                var newdatas = new byte[data.Length + prefix.Length];
                Buffer.BlockCopy(prefix, 0, newdatas, 0, prefix.Length);
                Buffer.BlockCopy(data, 0, newdatas, prefix.Length, data.Length);
                UDPSender.Send(newdatas, ConfigConstants.ServerIP, ConfigConstants.ServerPort);
                newdatas = null;
                prefix = null;
                speakToBytes = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送失败: {ex.Message}");
            }
        }
        protected async Task SendAudioDatasAsync(byte[] data)
        {
            try
            {
                var prefix = new byte[8];
                var speakToBytes = Encoding.UTF8.GetBytes(SpeakTo);
                Array.Copy(speakToBytes, prefix, Math.Min(speakToBytes.Length, prefix.Length));

                var newdatas = new byte[data.Length + prefix.Length];
                Buffer.BlockCopy(prefix, 0, newdatas, 0, prefix.Length);
                Buffer.BlockCopy(data, 0, newdatas, prefix.Length, data.Length);
                await UDPSender.SendAsync(newdatas, ConfigConstants.ServerIP, ConfigConstants.ServerPort);
                newdatas = null;
                prefix = null;
                speakToBytes = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送失败: {ex.Message}");
            }
        }

        protected async Task SendAudioDataAsync(byte[] data)
        {
            try
            {
                await UDPSender.SendAsync(data, ConfigConstants.ServerIP, ConfigConstants.ServerPort);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送失败: {ex.Message}");
            }
        }

        protected async Task HeartbeatLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    byte[] heartbeat = System.Text.Encoding.UTF8.GetBytes($"heartbeat:{User}:{Channel}");
                    await SendAudioDataAsync(heartbeat);
                    await Task.Delay(1000, token);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine($"心跳发送失败: {e.Message}");
                }
            }
        }

        protected async Task SendLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (RecordFrameQueue.TryDequeue(out var frame))
                {
                   await SendAudioDatasAsync(frame);
                }
                else
                {
                    await Task.Delay(5, token);
                }
            }
        }

        public virtual async Task<List<UserInfo>?> GetAllUsers()
        {
            var byts = Encoding.UTF8.GetBytes("GetUsers");
            try
            {
                await SendAudioDataAsync(byts);
                var res = await UDPSender.ReceiveAsync();
                string str = Encoding.UTF8.GetString(res.Buffer, 0, res.Buffer.Length);
                var users = JsonConvert.DeserializeObject<List<UserInfo>>(str);
                return users;
            }
            catch (Exception E)
            {
                return null;
            }
        }

        public virtual async Task<List<string>?> GetAllChannels()
        {
            var byts = Encoding.UTF8.GetBytes("get_channels");
            try
            {
                await SendAudioDataAsync(byts);
                var res = await UDPSender.ReceiveAsync();
                string str = Encoding.UTF8.GetString(res.Buffer, 0, res.Buffer.Length);
                var channels = JsonConvert.DeserializeObject<List<string>>(str);

                return channels;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

}
