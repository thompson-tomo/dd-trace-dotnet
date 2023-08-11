using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Datadog.Trace;

namespace Samples.Security.WebForms
{
    public partial class User : Page
    {
        private readonly ITracer _tracer = TracerProviderBuilder.Create().Build().GetTracer();

        protected void Page_Load(object sender, EventArgs e)
        {
            var userId = "user3";

            var userDetails = new UserDetails()
            {
                Id = userId,
            };
            _tracer.ActiveScope?.Span.SetUser(userDetails);
        }
    }
}
