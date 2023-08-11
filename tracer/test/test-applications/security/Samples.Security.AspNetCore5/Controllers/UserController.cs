using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Samples.Security.AspNetCore5.Models;
using System;
using System.Diagnostics;
using System.Linq;
using Datadog.Trace;

namespace Samples.Security.AspNetCore5.Controllers
{
    public class UserController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ITracer _tracer;

        public UserController(ILogger<HomeController> logger)
        {
            _logger = logger;
            _tracer = TracerProviderBuilder.Create().Build().GetTracer();
        }

        public IActionResult Index(string userId = null)
        {
            var userDetails = new UserDetails
            {
                Id = userId ?? "user3"
            };
            _tracer.ActiveScope?.Span.SetUser(userDetails);

            return View();
        }
    }
}
