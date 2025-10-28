#include <iostream>
#include <boost/asio.hpp>
#include "server.hpp"
#include "config_loader.hpp"

int main()
{
    try
    {
        ServerConfig config = loadServerConfig("config.json");

        std::cout << "Loaded config: port=" << config.port << std::endl;

        boost::asio::io_context io_context;
        VoiceServer server(io_context, config);

        server.start();
        std::cout << "Max connections allowed: " << config.max_connections << std::endl;

        // 使用线程池运行 io_context
        std::vector<std::thread> threads;
        for (int i = 0; i < 4; ++i)
        {
            threads.emplace_back([&io_context]()
                                 { io_context.run(); });
        }
        for (auto &t : threads)
        {
            t.join();
        }
        server.stop();
    }
    catch (const std::exception &e)
    {
        std::cerr << e.what() << '\n';
    }
    std::cout << "Press Enter to stop the server..." << std::endl;
    std::cin.get();
    return 0;
}