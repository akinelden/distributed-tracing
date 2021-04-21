import { context, getSpan, setSpan } from '@opentelemetry/api';
import { ConsoleSpanExporter, SimpleSpanProcessor, BatchSpanProcessor } from '@opentelemetry/tracing';
import { WebTracerProvider } from '@opentelemetry/web';
import { XMLHttpRequestInstrumentation } from '@opentelemetry/instrumentation-xml-http-request';
import { ZoneContextManager } from '@opentelemetry/context-zone';
import { CollectorTraceExporter } from '@opentelemetry/exporter-collector';
import { registerInstrumentations } from '@opentelemetry/instrumentation';
import { HttpTraceContext, TraceIdRatioBasedSampler } from "@opentelemetry/core";

const providerWithZone = new WebTracerProvider({
  sampler: new TraceIdRatioBasedSampler(1),
});

registerInstrumentations({
  instrumentations: [
    new XMLHttpRequestInstrumentation({
      ignoreUrls: [/localhost:8090\/sockjs-node/],
      propagateTraceHeaderCorsUrls: /.+/, // required if host domain is different
      // propagateTraceHeaderCorsUrls: [
      //   'localhost:5000',
      // ],
    }),
  ],
  tracerProvider: providerWithZone,
});

// simple console exporter
providerWithZone.addSpanProcessor(new SimpleSpanProcessor(new ConsoleSpanExporter()));

const collectorOptions = {
  serviceName: "js-client",
  url: 'http://localhost:55681/v1/trace', // url is optional and can be omitted - default is http://localhost:55681/v1/trace
  // headers: {}, // an optional object containing custom headers to be sent with each request
  concurrencyLimit: 10, // an optional limit on pending requests
};
const exporter = new CollectorTraceExporter(collectorOptions);
providerWithZone.addSpanProcessor(new BatchSpanProcessor(exporter, {
  // The maximum queue size. After the size is reached spans are dropped.
  maxQueueSize: 100,
  // The maximum batch size of every export. It must be smaller or equal to maxQueueSize.
  maxExportBatchSize: 10,
  // The interval between two consecutive exports
  scheduledDelayMillis: 1000,
  // How long the export can run before it is cancelled
  exportTimeoutMillis: 30000,
}));

providerWithZone.register({
  contextManager: new ZoneContextManager(),
  // propagator: new HttpTraceContext(),
});

const webTracerWithZone = providerWithZone.getTracer('example-tracer-web');

// const getData = (url) => new Promise((resolve, reject) => {
//   // eslint-disable-next-line no-undef
//   const req = new XMLHttpRequest();
//   req.open('GET', url, true);
//   req.setRequestHeader('Content-Type', 'application/json');
//   req.setRequestHeader('Accept', 'application/json');
//   req.onload = () => {
//     resolve();
//   };
//   req.onerror = () => {
//     reject();
//   };
//   req.send();
// });

// example of keeping track of context between async operations
const prepareClickEvent = (btnId, url) => {
  const url1 = url;

  const element = document.getElementById(btnId);
  const weatherDiv = document.getElementById("weatherDiv");
  const onClick = () => {
    const span1 = webTracerWithZone.startSpan(`WeatherRequest`);
    weatherDiv.innerHTML = "";
    context.with(setSpan(context.active(), span1), () => {
      $.get(url1, function (data) {
        getSpan(context.active()).addEvent('Get Weather request completed');
        weatherDiv.innerHTML = JSON.stringify(data);
      })
        .fail(function () {
          getSpan(context.active()).addEvent('Get Weather request error').setAttribute("error", true);
        })
        .always(function () {
          span1.end();
        });
      // getData(url1).then((_data) => {
      //   getSpan(context.active()).addEvent('Get Weather request completed');
      //   span1.end();
      // }, ()=> {
      //   span1.end();
      // });
    });
  };
  element.addEventListener('click', onClick);
};

const winloadCallback = () => {
  prepareClickEvent("button1", "http://localhost:5000/instantweather");
  prepareClickEvent("button2", "http://localhost:5000/delayedweather");
}

window.addEventListener('load', winloadCallback);
