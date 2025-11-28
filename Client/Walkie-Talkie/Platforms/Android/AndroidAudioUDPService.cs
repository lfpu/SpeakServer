
using Android.Content;
using Android.Media;
using Microsoft.Maui.Controls;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Walkie_Talkie.Models;
using Walkie_Talkie.Services;

namespace Walkie_Talkie.Platforms.AndroidService
{
    public class AndroidAudioUDPService : AudioBase
    {
        private AudioRecord? recorder;
        private AudioTrack? player;


        public override void AudioInitial()
        {
            UDPReceiver.Client.ReceiveBufferSize = 1024 * 1024;

            _ = StartRecordingAsync();
            _ = StartPlaybackAsync();
        }


        public override async Task ReceiveLoopAsync(CancellationToken token)
        {
            Console.WriteLine($"udpReceiver listening on: {((IPEndPoint)UDPReceiver.Client.LocalEndPoint)!}");
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var result = await UDPReceiver.ReceiveAsync(token);
                    if (result.Buffer.Length > 0 && player != null)
                    {
                        Console.WriteLine(result.Buffer.Length);
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

        public override void Dispose()
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
                System.Diagnostics.Debug.WriteLine("Microphone permission not granted");
                return;
            }

            int sampleRate = 16000;
            int bufferSize = AudioRecord.GetMinBufferSize(sampleRate, ChannelIn.Mono, Encoding.Pcm16bit);
            if (bufferSize <= 0)
            {
                System.Diagnostics.Debug.WriteLine("Invalid buffer size");
                return;
            }

            recorder = new AudioRecord(AudioSource.Mic, sampleRate, ChannelIn.Mono, Encoding.Pcm16bit, bufferSize);
            if (recorder.State != State.Initialized)
            {
                System.Diagnostics.Debug.WriteLine("AudioRecord initialization failed");
                return;
            }

            recorder.StartRecording();
            var audioManager = (AudioManager)Android.App.Application.Context.GetSystemService(Context.AudioService);
            Console.WriteLine($"Mode: {audioManager.Mode}, MicMute: {audioManager.MicrophoneMute}");
            await Task.Run(() => RecordLoopAsync(bufferSize, recordCts.Token));
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
            int warmupFrames = 5;
            int silentCount = 0;

            //// 丢弃启动后的前几帧
            //while (warmupFrames-- > 0 && !token.IsCancellationRequested)
            //{
            //    recorder!.Read(buffer, 0, buffer.Length);
            //}

            while (!token.IsCancellationRequested)
            {
                if (!isRecording)
                {
                    await Task.Delay(10, token);
                    continue;
                }
                
                int read = recorder!.Read(buffer, 0, buffer.Length);
                if (read > 0)
                {
                    //bool allZero = buffer.Take(read).All(b => b == 0);
                    //if (allZero)
                    //{
                    //    silentCount++;
                    //    if (silentCount > 10) // 连续10次静音
                    //    {
                    //        RestartAudioRecord(bufferSize);
                    //        silentCount = 0;
                    //    }
                    //    continue;
                    //}
                    //silentCount = 0;

                    var buffs = buffer.Take(read);
                    frameBuffer.AddRange(buffs);

                    while (frameBuffer.Count >= frameSize)
                    {
                        byte[] frame = frameBuffer.Take(frameSize).ToArray();
                        RecordFrameQueue.Enqueue(frame);
                        frameBuffer.RemoveRange(0, frameSize);
                    }
                }
            }
        }


        private void RestartAudioRecord(int bufferSize)
        {
            recorder?.Stop();
            recorder?.Release();
            recorder = new AudioRecord(AudioSource.VoiceRecognition, 16000, ChannelIn.Mono, Encoding.Pcm16bit, bufferSize);
            recorder.StartRecording();
        }

    }
}
