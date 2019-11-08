using AppvNext.Throttlebird.Throttling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace ResilencyDemoPolly
{
    public static class WebApiConfig
    {
        internal static readonly IThrottleStore ThrottleStore = new InMemoryThrottleStore();
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // Implement our custom throttling handler to limit API method calls.
            // Specify the throttle store, max number of allowed requests within specified timespan,
            // and message displayed in the error response when exceeded.

            config.MessageHandlers.Add(new ThrottlingHandler(
                ThrottleStore,
                id => 3,
                TimeSpan.FromSeconds(5),
                "Haz alcanzado el maximo numero de solicitudes permitidas. Espere hasta después del período de cooldown para intentarlo nuevamente."
            ));
        }
    }
}
