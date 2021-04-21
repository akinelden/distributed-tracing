#include <iostream>
#include <ctime>
//#include <yaml-cpp/yaml.h>

#include <jaegertracing/Tracer.h>

#include "httplib.h"



namespace {

void setUpTracer(const char* configFilePath)
{
    // auto configYAML = YAML::LoadFile(configFilePath);
    // auto config = jaegertracing::Config::parse(configYAML);
    auto conf = jaegertracing::Config(
        false,
        false,
        jaegertracing::samplers::Config("const", 1),
        jaegertracing::reporters::Config(
            100,
            jaegertracing::reporters::Config::defaultBufferFlushInterval(),
            false,
            "127.0.0.1:6831"),
        jaegertracing::propagation::HeadersConfig(),
        jaegertracing::baggage::RestrictionsConfig(),
        "cpp-service",
        std::vector<jaegertracing::Tag>(),
        jaegertracing::propagation::W3C);
    auto tracer = jaegertracing::Tracer::make("cpp-service", conf);
    opentracing::Tracer::InitGlobal(
        std::static_pointer_cast<opentracing::Tracer>(tracer));
}

class RequestReader : public opentracing::HTTPHeadersReader {
  public:
    explicit RequestReader(const httplib::Request& request)
        : _request(request)
    {
    }

    opentracing::expected<void> ForeachKey(
        std::function<opentracing::expected<void>(opentracing::string_view,
                                                  opentracing::string_view)> f)
        const override
    {
        for (auto&& header : _request.headers) {
            const auto result = f(header.first, header.second);
            if (!result) {
                return result;
            }
        }
        return opentracing::make_expected();
    }

  private:
    const httplib::Request& _request;
};


}  // anonymous namespace

int main(int argc, char* argv[])
{
    using namespace httplib;
    using namespace std::chrono_literals;
    using namespace opentracing;
    /*if (argc < 2) {
        std::cerr << "usage: " << argv[0] << " <config-yaml-path>\n";
        return 1;
    }*/
    setUpTracer(argv[1]);

    Server svr;
    
    svr.Get("/instantweather", [](const Request& req, Response& res) {
        RequestReader reader(req);
        auto ctxResult = Tracer::Global()->Extract(reader);
        auto span = Tracer::Global()->StartSpan(
            "instantweather", { opentracing::ChildOf(ctxResult->release()) });
        span->SetTag("type", "instant");
        res.set_content("Weather is great!", "text/plain");
        span->Log({ { "event", "instant request completed" } });
        // span->Finish();
    });

    svr.Get("/delayedweather", [](const Request& req, Response& res) {
        RequestReader reader(req);
        auto ctxResult = Tracer::Global()->Extract(reader);
        auto span = Tracer::Global()->StartSpan(
            "delayedweather", { opentracing::ChildOf(ctxResult->release()) });
        span->SetTag("type", "delayed");
        auto child = Tracer::Global()->StartSpan(
            "wait", { opentracing::ChildOf(&span->context()) });
        child->SetTag("stime", "2000ms");
        std::this_thread::sleep_for(2000ms);
        child->Finish();
        res.set_content("Weather is rainy!", "text/plain");
        span->Log({ { "event", "delayed request completed" } });
        // span->Finish();
    });

    std::cout << "Application started..";

    svr.listen("localhost", 2345);

    //tracedFunction();
    // Not stricly necessary to close tracer, but might flush any buffered
    // spans. See more details in opentracing::Tracer::Close() documentation.
    opentracing::Tracer::Global()->Close();
    return 0;
}
