using Funq;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;
using System.Net;
using MusicBeePlugin.AndroidRemote.Persistance;
using ServiceStack.Api.Swagger;
using ServiceStack.Common;

namespace MusicBeePlugin.Rest
{
    public class AppHost : AppHostHttpListenerBase
    {
        private readonly SettingsController _controller;

        public AppHost(SettingsController controller)
            : base("MusicBee Remote", typeof(AppHost).Assembly)
        {
            _controller = controller;
        }

        public override void Configure(Container container)
        {
            SetConfig(new EndpointHostConfig()
            {
                EnableFeatures = Feature.All.Remove(Feature.Csv |
                                                    Feature.Jsv |
                                                    Feature.Soap |
                                                    Feature.Soap11 |
                                                    Feature.Soap12 |
                                                    Feature.Xml)
            });   

            Plugins.Add(new SwaggerFeature());
            RequestFilters.Add((req, res, requestDto) =>
            {
                var address = req.RemoteIp;
                if (!_controller.CheckIfAddressIsAllowed(address))
                {
                    res.RedirectToUrl("/", HttpStatusCode.Forbidden);
                }

            });
        }
    }
}