using ConsoleClient.OutputHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleClient.Scenarios
{
    /// <summary>
    /// No utiliza ningun Policy. Demuestra el comportamiento de un "servidor fallando" al cual realizamos requests.
    /// </summary>
    public class NoPolicyAsync : AsyncBase
    {
        private int totalRequests;
        private int eventualSuccesses;
        private int retries;
        private int eventualFailures;

        public override string Description => "Esta demo demuestra como se comporta nuestro servidor con fallas, sin Polly polices funcionando.";

        public override async Task ExecuteAsync(CancellationToken cancellationToken, IProgress<Progress> progress)
        {
            if (cancellationToken == null) throw new ArgumentNullException(nameof(cancellationToken));
            if (progress == null) throw new ArgumentNullException(nameof(progress));

            eventualSuccesses = 0;
            retries = 0;
            eventualFailures = 0;

            progress.Report(ProgressWithMessage(typeof(NoPolicyAsync).Name));
            progress.Report(ProgressWithMessage("======"));
            progress.Report(ProgressWithMessage(string.Empty));

            using (var client = new HttpClient())
            {
                bool internalCancel = false;
                totalRequests = 0;
                // Realiza lo siguiente hasta que se presiona una tecla
                while (!internalCancel && !cancellationToken.IsCancellationRequested)
                {
                    totalRequests++;

                    try
                    {
                        // Realiza una peticion y obtiene una respuesta
                        string msg = await client.GetStringAsync(Configuration.WEB_API_ROOT + "/api/values/" + totalRequests);

                        // Muestra la respuesta en la consola
                        progress.Report(ProgressWithMessage("Respuesta : " + msg, Color.Green));
                        eventualSuccesses++;
                    }
                    catch (Exception e)
                    {
                        progress.Report(ProgressWithMessage("Request " + totalRequests + " eventualmente falló por: " + e.Message, Color.Red));
                        eventualFailures++;
                    }

                    // Espera medio segundo
                    await Task.Delay(TimeSpan.FromSeconds(0.5), cancellationToken);

                    internalCancel = TerminateDemosByKeyPress && Console.KeyAvailable;
                }
            }
        }

        public override Statistic[] LatestStatistics => new[]
        {
            new Statistic("Total de solicitudes realizadas", totalRequests),
            new Statistic("Solicitudes que finalmente tuvieron éxito", eventualSuccesses, Color.Green),
            new Statistic("Reintentos realizados para ayudar a lograr el éxito", retries, Color.Yellow),
            new Statistic("Solicitudes que finalmente fallaron", eventualFailures, Color.Red),
        };

    }
}
