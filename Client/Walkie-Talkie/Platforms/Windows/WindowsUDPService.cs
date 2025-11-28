using NAudio.Wave;
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
using Walkie_Talkie.Services;

namespace Walkie_Talkie.Platforms.WindowsService
{
    public class WindowsUDPService : AudioBase
    {
        private WaveInEvent? waveIn;
        private BufferedWaveProvider? waveProvider;
        private WaveOutEvent? waveOut;

        public override void AudioInitial()
        {
            UDPReceiver.Client.ReceiveBufferSize = 1024 * 1024;

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
                        RecordFrameQueue.Enqueue(frame);
                        frameBuffer.RemoveRange(0, frameSize);
                    }
                }
            };
            waveIn.StartRecording();

        }


        public override async Task ReceiveLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var result = await UDPReceiver.ReceiveAsync(token);
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

        public override void Dispose()
        {
            waveIn?.StopRecording();
            waveOut?.Stop();
            receiveCts?.Cancel();
            heartbeatCts?.Cancel();
            //注入的实体不能销毁udp对象
            //udpSender.Close();
            //udpReceiver.Close();
            //udpSender.Dispose();
            //udpReceiver.Dispose();
        }
    }
}
