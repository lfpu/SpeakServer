

#include <fstream>
#include <vector>
#include <string>
#include <cstdint>
#include <iostream>

class AudioFile
{
public:
    AudioFile(const std::string &name,int rate,int bit,int channel);
    void write(const std::vector<char> &data);
private:
    std::string fileName;
    int sampleRate;
    int bits;
    int channel;

    int byteRate;
    int blockAlign;
    int subchunk2Size;
    int chunksize;

    std::ofstream out;
};