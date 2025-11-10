#include <unordered_map>
#include <string>
#include <memory>
#include <vector>
#include <set>
#include <nlohmann/json.hpp>
#include "client_session.hpp"
#include <boost/asio.hpp>

boost::asio::const_buffer getDistinctUserNamesJson(
    const std::unordered_map<std::string, std::shared_ptr<ClientSession>> &client_sessions);
