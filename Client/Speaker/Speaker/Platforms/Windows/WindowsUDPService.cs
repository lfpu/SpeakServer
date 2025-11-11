using NAudio.Wave;
using Speaker.Models;
using Speaker.Services;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Speaker.Windows
{
    public class WindowsUDPService : IAudioUDPService
    {
        private readonly UdpClient udpSender = new();
        private readonly UdpClient udpReceiver = new(0); // 动态绑定端口
        private WaveInEvent? waveIn;
        private BufferedWaveProvider? waveProvider;
        private WaveOutEvent? waveOut;

        private readonly List<byte> frameBuffer = new();
        private readonly int frameSize = 320; // 20ms PCM16 mono @16kHz
        private CancellationTokenSource? receiveCts;
        private CancellationTokenSource? heartbeatCts;
        private CancellationTokenSource? getUserCts;

        public bool isRecording { get; set; }
        public string User { get; set; } = "Default";
        public bool IsConnected { get; set; }
        public string SpeakTo { get; set; } = "all";

        public async Task<ServerMessage> ConnectAsync()
        {
            ServerMessage serverMsg=new ServerMessage();
            try
            {
                int localPort = ((IPEndPoint)udpReceiver.Client.LocalEndPoint!).Port;
                byte[] handshake = Encoding.UTF8.GetBytes($"handshake:{localPort}");
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
                else
                {
                    // 超时
                    serverMsg.Connected = false;
                    serverMsg.Reason = "连接超时：超过3秒未收到服务器响应";
                    Console.WriteLine("连接超时：超过3秒未收到服务器响应");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"连接失败: {ex.Message}");
            }
            return serverMsg;
        }


        public void Start()
        {
            udpReceiver.Client.ReceiveBufferSize = 1024 * 1024;

            // 初始化播放
            waveProvider = new BufferedWaveProvider(new WaveFormat(16000, 16, 1))
            {
                DiscardOnBufferOverflow = true,
                BufferDuration = TimeSpan.FromMilliseconds(500)
            };
            waveOut = new WaveOutEvent();
            waveOut.Init(waveProvider);
            waveOut.Play();

            // 初始化录音
            waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 16, 1),
                BufferMilliseconds = 100
            };
            waveIn.DataAvailable += (s, e) =>
            {
                if (isRecording)
                {
                    frameBuffer.AddRange(e.Buffer.Take(e.BytesRecorded));
                    while (frameBuffer.Count >= frameSize)
                    {
                        byte[] frame = frameBuffer.Take(frameSize).ToArray();
                        frameBuffer.RemoveRange(0, frameSize);
                        SendAudioData(frame);
                    }
                }
            };
            waveIn.StartRecording();

            // 启动心跳
            heartbeatCts = new CancellationTokenSource();
            _ = Task.Run(() => HeartbeatLoopAsync(heartbeatCts.Token));

            // 启动接收
            receiveCts = new CancellationTokenSource();
            _ = Task.Run(() => ReceiveLoopAsync(receiveCts.Token));

            // 主动发包保持 NAT 映射
            udpReceiver.Send(Encoding.UTF8.GetBytes("receive"), ConfigConstants.ServerIP, ConfigConstants.ServerPort);

            Console.WriteLine($"udpReceiver 正在监听: {udpReceiver.Client.LocalEndPoint}");
        }

        public async Task<List<UserInfo>?> GetAllUsers()
        {
            var byts = Encoding.UTF8.GetBytes("GetUsers");
            try
            {
                await SendAudioDataAsync(byts);
                var res = await udpSender.ReceiveAsync();
                string str = Encoding.UTF8.GetString(res.Buffer);
                System.Diagnostics.Debug.WriteLine(str);
                var users =JsonConvert.DeserializeObject<List<UserInfo>>(str);
                return users;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void RecordAndSend()
        {
            isRecording = true;
            //change user state to speaking
            udpSender.Send(new byte[] { 0x001 }, ConfigConstants.ServerIP, ConfigConstants.ServerPort);
        }
        public void StopRecordSend() { 
            isRecording = false;
            //change user state to not speaking
            udpSender.Send(new byte[] { 0x002 }, ConfigConstants.ServerIP, ConfigConstants.ServerPort);
        }

        private void SendAudioData(byte[] data)
        {
            try
            {
                var prefix = new byte[8];
                var speakToBytes = Encoding.UTF8.GetBytes(SpeakTo);
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

        private async Task HeartbeatLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                byte[] heartbeat = Encoding.UTF8.GetBytes($"heartbeat:{User}");
                await SendAudioDataAsync(heartbeat);
                Console.WriteLine("heart beat sent");
                await Task.Delay(1000, token);
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var result = await udpReceiver.ReceiveAsync(token);
                    waveProvider?.AddSamples(result.Buffer, 0, result.Buffer.Length);

                    if (waveOut?.PlaybackState != PlaybackState.Playing)
                        waveOut?.Play();
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Console.WriteLine($"接收失败: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            waveIn?.StopRecording();
            waveOut?.Stop();
            receiveCts?.Cancel();
            heartbeatCts?.Cancel();
            getUserCts?.Cancel();
            //注入的实体不能销毁udp对象
            //udpSender.Close();
            //udpReceiver.Close();
            //udpSender.Dispose();
            //udpReceiver.Dispose();
        }

        public void SetUser(string username) => User = username;
    }
}
