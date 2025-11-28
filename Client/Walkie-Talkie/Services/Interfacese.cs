using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Walkie_Talkie.Services
{
    public interface IAudioUDPService : IDisposable
    {

        UdpClient UDPSender { get; }
        UdpClient UDPReceiver  { get; }

         CancellationTokenSource? recordCts { get; set; }
         CancellationTokenSource? receiveCts { get; set; }
        CancellationTokenSource? heartbeatCts { get; set; }

        int frameSize { get; set; }

        string User { get; set; }
        string Channel { get; set; }
        bool isRecording { get; set; }
        bool IsConnected { get; set; }
        string SpeakTo { get; set; }
        Task<ServerMessage> ConnectAsync(string username);
        void Start();
        void RecordAndSend();
        void StopRecordSend();
        void SetUser(string username);

        Task<List<UserInfo>?> GetAllUsers();
        Task<List<string>?> GetAllChannels();
        void SetChannel(string channelName);
        ConcurrentQueue<byte[]> RecordFrameQueue { get;  }
    }
}
