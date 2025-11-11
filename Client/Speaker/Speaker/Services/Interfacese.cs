using System;
using System.Collections.Generic;
using System.Text;

namespace Speaker.Services
{

    public interface IAudioUDPService : IDisposable
    {
        string User { get; set; }
        bool isRecording { get; set; }
        bool IsConnected { get; set; }
        string SpeakTo { get; set; }
        Task<bool> ConnectAsync();
        void Start();
        void RecordAndSend();
        void StopRecordSend();
        void SetUser(string username);

        Task<List<UserInfo>?> GetAllUsers();
    }

}
