#include <unordered_map>
#include <string>
#include <memory>
#include <vector>
#include <set>
#include <nlohmann/json.hpp>
#include "client_session.hpp"
#include <boost/asio.hpp>

std::string getDistinctUserNames(
    const std::unordered_map<std::string, std::shared_ptr<ClientSession>> &client_sessions,const std::string& channel);
std::vector<std::string> splitByChar(const std::string &str, char delimiter);
std::string getDistinctChannels(const std::unordered_map<std::string, std::shared_ptr<ClientSession>> &client_sessions);