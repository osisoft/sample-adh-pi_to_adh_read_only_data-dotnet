using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PItoADHReadOnly
{
    public class VerbosityHeaderHandler : DelegatingHandler
    {
        public VerbosityHeaderHandler(bool verbose = true)
        {
            Verbose = verbose;
        }

        public bool Verbose { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // If the handler is set to non-verbose, set the accept-verbosity header to non-verbose to prevent verbose values from being returned from Cds
            if (Verbose)
            {
                request?.Headers.Add("accept-verbosity", "verbose");
            }
            else
            {
                request?.Headers.Add("accept-verbosity", "non-verbose");
            }

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
