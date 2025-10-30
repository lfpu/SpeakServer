#include "audio_File.hpp"

AudioFile::AudioFile(const std::string &name, int rate, int bit, int cha)
    : fileName(name), sampleRate(rate), bits(bit), channel(cha),out(fileName, std::ios::binary | std::ios::trunc)
{

    byteRate = sampleRate * channel * bits / 8;
    blockAlign = channel * bits / 8;
    chunksize = 36; // 初始为 36 + 0
    subchunk2Size = 0;

    if (!out)
    {
        throw std::runtime_error("无法打开文件写入");
    }
    // RIFF header
    out.write("RIFF", 4);
    out.write(reinterpret_cast<const char *>(&chunksize), 4);
    out.write("WAVE", 4);

    // 写 fmt 子块
    out.write("fmt ", 4);
    int subchunk1Size = 16;
    int16_t audioFormat = 1; // PCM
    out.write(reinterpret_cast<const char *>(&subchunk1Size), 4);
    out.write(reinterpret_cast<const char *>(&audioFormat), 2);
    out.write(reinterpret_cast<const char *>(&channel), 2);
    out.write(reinterpret_cast<const char *>(&sampleRate), 4);
    out.write(reinterpret_cast<const char *>(&byteRate), 4);
    out.write(reinterpret_cast<const char *>(&blockAlign), 2);
    out.write(reinterpret_cast<const char *>(&bits), 2);
    // 写 data 子块
    out.write("data", 4);
    out.write(reinterpret_cast<const char*>(&subchunk2Size), 4); // 初始为 
    std::cout<<"file init"<< std::endl;
};

void AudioFile::write(const std::vector<char> &data)
{

    if (!out) return;

    // 追加数据
    out.seekp(0, std::ios::end);
    out.write(data.data(), data.size());

    // 更新数据长度
    subchunk2Size += static_cast<int>(data.size());
    chunksize = 36 + subchunk2Size;

    // 更新头部信息
    out.seekp(4, std::ios::beg);
    out.write(reinterpret_cast<const char*>(&chunksize), 4);
    out.seekp(40, std::ios::beg);
    out.write(reinterpret_cast<const char*>(&subchunk2Size), 4);

    // 回到末尾，准备下次写入
    out.seekp(0, std::ios::end);

    std::cout << "save size: " << data.size() << std::endl;

}