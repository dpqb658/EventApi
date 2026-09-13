using System.Diagnostics;

namespace MiddlewareDemo
{
    /// <summary>
    /// Класс для измерения времени обработки HTTP-запроса.
    /// </summary>
    /// <param name="next">Ссылка на следующий middleware</param>
    public class RequestTimingMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        /// <summary>
        /// Добавляет в Headers время обработки.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            context.Response.OnStarting(() =>
            {
                stopwatch.Stop();
                var elapsed = stopwatch.ElapsedMilliseconds;
                context.Response.Headers["X-Response-Time"] = $"{elapsed}ms";
                return Task.CompletedTask;
            });

            await _next(context);
        }
    }
}
