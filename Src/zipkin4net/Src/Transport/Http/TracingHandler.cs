﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using zipkin4net.Propagation;

namespace zipkin4net.Transport.Http
{
    public class TracingHandler : DelegatingHandler
    {
        private readonly IInjector<HttpHeaders> _injector;
        private readonly string _serviceName;
        private readonly Func<HttpRequestMessage, string> _getClientTraceRpc;

        public TracingHandler(string serviceName, HttpMessageHandler httpMessageHandler = null, Func<HttpRequestMessage, string> getClientTraceRpc = null)
        : this(Propagations.B3String.Injector<HttpHeaders>((carrier, key, value) => carrier.Add(key, value)), serviceName, httpMessageHandler, getClientTraceRpc)
        { }

        private TracingHandler(IInjector<HttpHeaders> injector, string serviceName, HttpMessageHandler httpMessageHandler = null, Func<HttpRequestMessage, string> getClientTraceRpc = null)
        {
            _injector = injector;
            _serviceName = serviceName;
            _getClientTraceRpc = getClientTraceRpc ?? (request => request.Method.ToString());
            InnerHandler = httpMessageHandler ?? new HttpClientHandler();
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            using (var clientTrace = new ClientTrace(_serviceName, _getClientTraceRpc(request)))
            {
                if (clientTrace.Trace != null)
                {
                    _injector.Inject(clientTrace.Trace.CurrentSpan, request.Headers);
                }
                return await clientTrace.TracedActionAsync(base.SendAsync(request, cancellationToken));
            }
        }
    }
}
