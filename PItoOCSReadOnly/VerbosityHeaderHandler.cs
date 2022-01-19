using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PItoOCSReadOnly
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
            // If the handler is set to non-verbose, set the accept-verbosity header to non-verbose to prevent null values from being returned from OCS
            if (!Verbose)
            {
                request?.Headers.Add("accept-verbosity", "non-verbose");
            }
            else
            {
                request?.Headers.Add("accept-verbosity", "verbose");
            }

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
