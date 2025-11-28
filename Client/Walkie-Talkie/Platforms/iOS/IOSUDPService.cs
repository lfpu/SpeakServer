using AVFoundation;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Walkie_Talkie.Models;
using Walkie_Talkie.Services;

namespace Walkie_Talkie.Platforms.iOSService
{
    public class IOSUDPService : AudioBase
    {

        private AVAudioEngine audioEngine;
        private AVAudioInputNode inputNode;
        private AVAudioFormat audioFormat;

        public override void AudioInitial()
        {
            // 初始化音频引擎
            audioEngine = new AVAudioEngine();
            inputNode = audioEngine.InputNode;
            audioFormat = new AVAudioFormat(16000, 1);

            inputNode.InstallTapOnBus(0, 1024, audioFormat, (buffer, when) =>
            {
                if (isRecording)
                {
                    byte[] pcmData = ConvertFloatToPCM16(buffer.FloatChannelData, buffer.FrameLength);
                    frameBuffer.AddRange(pcmData);
                    while (frameBuffer.Count >= frameSize)
                    {
                        var frame = frameBuffer.Take(frameSize).ToArray();
                        RecordFrameQueue.Enqueue(frame);
                        frameBuffer.RemoveRange(0, frameSize);
                    }
                }
            });

            audioEngine.Prepare();
            audioEngine.StartAndReturnError(out var error);
            if (error != null) Console.WriteLine($"AudioEngine error: {error}");
        }

        private byte[] ConvertFloatToPCM16(IntPtr floatData, uint frameLength)
        {
            short[] pcm = new short[frameLength];
            unsafe
            {
                float* ptr = (float*)floatData;
                for (int i = 0; i < frameLength; i++)
                {
                    pcm[i] = (short)(ptr[i] * short.MaxValue);
                }
            }
            byte[] bytes = new byte[pcm.Length * 2];
            Buffer.BlockCopy(pcm, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public override async Task ReceiveLoopAsync(CancellationToken token)
        {
            // 初始化播放引擎
            var playbackEngine = new AVAudioEngine();
            var playerNode = new AVAudioPlayerNode();
            var playbackFormat = new AVAudioFormat(16000, 1); // PCM16 mono @16kHz

            playbackEngine.AttachNode(playerNode);
            playbackEngine.Connect(playerNode, playbackEngine.MainMixerNode, playbackFormat);

            playbackEngine.Prepare();
            playbackEngine.StartAndReturnError(out var error);
            if (error != null)
            {
                Console.WriteLine($"PlaybackEngine error: {error}");
                return;
            }

            playerNode.Play();

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var result = await UDPReceiver.ReceiveAsync(token);
                    byte[] pcmData = result.Buffer;

                    // 将 PCM16 数据封装成 AVAudioPCMBuffer
                    int frameCount = pcmData.Length / 2; // 每帧 2 字节
                    var buffer = new AVAudioPcmBuffer(playbackFormat, (uint)frameCount);
                    buffer.FrameLength = (uint)frameCount;

                    unsafe
                    {
                        short* pcmPtr = (short*)buffer.Int16ChannelData;
                        fixed (byte* srcPtr = pcmData)
                        {
                            short* srcShortPtr = (short*)srcPtr;
                            for (int i = 0; i < frameCount; i++)
                            {
                                pcmPtr[i] = srcShortPtr[i];
                            }
                        }
                    }

                    // 播放音频
                    playerNode.ScheduleBuffer(buffer, null);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"接收失败: {ex.Message}");
                }
            }

            playerNode.Stop();
            playbackEngine.Stop();
        }
        public override void Dispose()
        {
            audioEngine?.Stop();
            receiveCts?.Cancel();
            heartbeatCts?.Cancel();

            audioEngine?.Stop();
            audioEngine?.Dispose();
            audioFormat?.Dispose();
            inputNode?.Dispose();
        }
    }
}