using ConsoleClient.OutputHelpers;
using Polly;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.Wrap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleClient.Scenarios
{
    /// <summary>
    /// Demuestra el uso de PolicyWrap, incluye dos Fallback Policies (para diferentes excepciones), WaitAndRetry y CircuitBreaker.
    /// Utiliza Fallback policies para proveer valores substitutos para el usuario cuando las peticiones fallan.
    ///  
    /// Ejecuta bucles a través de una serie de peticiones HTTP, manteniendo el tracking de cada peticion
    /// item e informa fallas del servidor cuando encuentra excepciones.
    ///  
    /// fallback policies proveen un buen mensaje de substitución si las solicitudes siguen fallando
    /// onFallback delegate captura las estadisticas que fueron capturadas en try/catches
    /// tambien demuestra como utilizar el mismo tipo de policy (Fallback en este caso) dos veces (o más) en un wrap.
    /// </summary>
    public class Wrap_Fallback_WaitAndRetry_CircuitBreaker_Async : AsyncBase
    {
        private int totalRequests;
        private int eventualSuccesses;
        private int retries;
        private int eventualFailuresDueToCircuitBreaking;
        private int eventualFailuresForOtherReasons;

        public override string Description => "Esta demo utiliza Fallback: se puede proveer un mensaje que sea apropiado para el usuario final debido a la falla general";

        public override async Task ExecuteAsync(CancellationToken cancellationToken, IProgress<Progress> progress)
        {
            if (cancellationToken == null) throw new ArgumentNullException(nameof(cancellationToken));
            if (progress == null) throw new ArgumentNullException(nameof(progress));

            // Llama el servicio web de la API para hacer peticiones repetitivas al servidor. 
            // El servicio esta programado para fallar despues de 3 peticiones en 5 segundos.

            eventualSuccesses = 0;
            retries = 0;
            eventualFailuresDueToCircuitBreaking = 0;
            eventualFailuresForOtherReasons = 0;

            progress.Report(ProgressWithMessage(typeof(Wrap_Fallback_WaitAndRetry_CircuitBreaker_Async).Name));
            progress.Report(ProgressWithMessage("======"));
            progress.Report(ProgressWithMessage(string.Empty));

            Stopwatch watch = null;

            // Definimos nuestro waitAndRetry policy: sigue intentando con intervalos de 200ms.
            var waitAndRetryPolicy = Policy
                .Handle<Exception>(e => !(e is BrokenCircuitException)) // Filtrado de excepción! No reintentamos si el circuit-breaker determina que el sistema invocado está fuera de servicio!
                .WaitAndRetryForeverAsync(
                attempt => TimeSpan.FromMilliseconds(200),
                (exception, calculatedWaitDuration) =>
                {
                    progress.Report(ProgressWithMessage(".Log,vuelva e intentar: " + exception.Message, Color.Yellow));
                    retries++;
                });

            // Definimos nuestro CircuitBreaker policy: cortar si la acción falla 4 veces seguidas.
            var circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 4,
                    durationOfBreak: TimeSpan.FromSeconds(3),
                    onBreak: (ex, breakDelay) =>
                    {
                        progress.Report(ProgressWithMessage(".Breaker logging: Cortando el circuito por " + breakDelay.TotalMilliseconds + "ms!", Color.Magenta));
                        progress.Report(ProgressWithMessage("..debido a: " + ex.Message, Color.Magenta));
                    },
                    onReset: () => progress.Report(ProgressWithMessage(".Breaker logging: Llamado ok! Se cierra el circuito nuevamente!", Color.Magenta)),
                    onHalfOpen: () => progress.Report(ProgressWithMessage(".Breaker logging: Half-open: el proximo llamado es de prueba!", Color.Magenta))
                );

            // Definimos un fallback policy: provee un buen mensaje de reemplazo para el usuario, si encontramos que el circuito estaba cortado.
            AsyncFallbackPolicy<string> fallbackForCircuitBreaker = Policy<string>
                .Handle<BrokenCircuitException>()
                .FallbackAsync(
                    fallbackValue: "Por favor intente mas tarde [mensaje substituido por fallback policy]",
                    onFallbackAsync: async b =>
                    {
                        await Task.FromResult(true);
                        watch.Stop();
                        progress.Report(ProgressWithMessage("Fallback capto llamada fallida por: " + b.Exception.Message
                            + " (despues de " + watch.ElapsedMilliseconds + "ms)", Color.Red));
                        eventualFailuresDueToCircuitBreaking++;
                    }
                );

            // Definimos un fallback policy: provee un buen mensaje substituto para el usuario, para cualquier excepcion.
            AsyncFallbackPolicy<string> fallbackForAnyException = Policy<string>
                .Handle<Exception>()
                .FallbackAsync(
                    fallbackAction: async ct =>
                    {
                        await Task.FromResult(true);
                        /* logica extra que se desee aquí */
                        return "Por favor intente mas tarde [Fallback para cualquier excepción]";
                    },
                    onFallbackAsync: async e =>
                    {
                        await Task.FromResult(true);
                        watch.Stop();
                        progress.Report(ProgressWithMessage("Fallback captura eventualmented fallido por: " + e.Exception.Message
                            + " (despues de " + watch.ElapsedMilliseconds + "ms)", Color.Red));
                        eventualFailuresForOtherReasons++;
                    }
                );

            // Combinamos el waitAndRetryPolicy y circuitBreakerPolicy en un PolicyWrap
            AsyncPolicyWrap myResilienceStrategy = Policy.WrapAsync(waitAndRetryPolicy, circuitBreakerPolicy);

            // Envuelve los dos fallback policies en el frente del wrap existente. Demuestra el hecho de que el PolicyWrap myResilienceStrategy de arriba es solo otro Policy, el cual puede ser envuelto también.
            // Con este patron, se puede construir una estrategia general programaticamente, reusando algunas partes en común (ej. Policy Wrap myResilienceStrategy) pero variendo otras partes (ej. Fallback) individualmente para diferentes llamados.
            AsyncPolicyWrap<string> policyWrap = fallbackForAnyException.WrapAsync(fallbackForCircuitBreaker.WrapAsync(myResilienceStrategy));
            // Para info: equivalente a: AsyncPolicyWrap<string> policyWrap = Policy.WrapAsync(fallbackForAnyException, fallbackForCircuitBreaker, waitAndRetryPolicy, circuitBreakerPolicy);

            totalRequests = 0;

            using (var client = new HttpClient())
            {
                bool internalCancel = false;
                // Hacer lo siguiente hasta que una tecla sea presionada
                while (!internalCancel && !cancellationToken.IsCancellationRequested)
                {
                    totalRequests++;
                    watch = new Stopwatch();
                    watch.Start();

                    try
                    {
                        // Maneja el llamado acorde al policy wrap
                        string response = await policyWrap.ExecuteAsync(ct =>
                                        client.GetStringAsync(Configuration.WEB_API_ROOT + "/api/values/" + totalRequests), cancellationToken);

                        watch.Stop();

                        // Muestra la respuesta en la consola
                        progress.Report(ProgressWithMessage("Respuesta : " + response + " (despues de " + watch.ElapsedMilliseconds + "ms)", Color.Green));

                        eventualSuccesses++;
                    }
                    catch (Exception e) // try-catch innecesario ahora que tenemos un Fallback.Handle<Exception>. Sólo está aquí para demostrar que nunca llega hasta este codigo.
                    {
                        throw new InvalidOperationException("Nunca debería llegar hasta aquí.  Uso de fallbackForAnyException debería proveer un buen mensaje devuelta al usuario para cualquier excepción.", e);
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
            new Statistic("Solicitudes fallidas de manera temprana por circuito cortado", eventualFailuresDueToCircuitBreaking, Color.Magenta),
            new Statistic("Solicitudes que fallaron después de un delay largo", eventualFailuresForOtherReasons, Color.Red),
        };

    }
}
