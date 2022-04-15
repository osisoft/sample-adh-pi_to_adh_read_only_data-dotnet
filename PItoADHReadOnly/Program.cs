using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OSIsoft.Data;
using OSIsoft.Data.Http;
using OSIsoft.Identity;

namespace PItoADHReadOnly
{
    public static class Program
    {
        private static IConfiguration _configuration;

        private static Exception _toThrow;

        public static void Main() => MainAsync().GetAwaiter().GetResult();

        public static async Task<bool> MainAsync(bool test = false)
        {
            Console.WriteLine("Step 1. Authenticate against OCS");
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.test.json", optional: true);
            _configuration = builder.Build();

            // ==== Client constants ====
            string tenantId = _configuration["TenantId"];
            string namespaceId = _configuration["NamespaceId"];
            string resource = _configuration["Resource"];
            string clientId = _configuration["ClientId"];
            string clientSecret = _configuration["ClientSecret"];
            string streamId = _configuration["StreamId"];
            Uri uriResource = new (resource);

            // Get Sds Services to communicate with server
            AuthenticationHandler authenticationHandler = new (uriResource, clientId, clientSecret);
            VerbosityHeaderHandler verbosityHeaderHandler = new ();
            SdsService sdsService = new (uriResource, null, HttpCompressionMethod.GZip, authenticationHandler, verbosityHeaderHandler);

            ISdsMetadataService metadataService = sdsService.GetMetadataService(tenantId, namespaceId);
            ISdsDataService dataService = sdsService.GetDataService(tenantId, namespaceId);
            ISdsTableService tableService = sdsService.GetTableService(tenantId, namespaceId);

            Console.WriteLine(@"---------------------------------------------------------------------------------");
            Console.WriteLine(@"██████╗░██╗████████╗░█████╗░░█████╗░██████╗░██╗░░██╗░░░███╗░░██╗███████╗████████╗");
            Console.WriteLine(@"██╔══██╗██║╚══██╔══╝██╔══██╗██╔══██╗██╔══██╗██║░░██║░░░████╗░██║██╔════╝╚══██╔══╝");
            Console.WriteLine(@"██████╔╝██║░░░██║░░░██║░░██║███████║██║░░██║███████║░░░██╔██╗██║█████╗░░░░░██║░░░");
            Console.WriteLine(@"██╔═══╝░██║░░░██║░░░██║░░██║██╔══██║██║░░██║██╔══██║░░░██║╚████║██╔══╝░░░░░██║░░░");
            Console.WriteLine(@"██║░░░░░██║░░░██║░░░╚█████╔╝██║░░██║██████╔╝██║░░██║██╗██║░╚███║███████╗░░░██║░░░");
            Console.WriteLine(@"╚═╝░░░░░╚═╝░░░╚═╝░░░░╚════╝░╚═╝░░╚═╝╚═════╝░╚═╝░░╚═╝╚═╝╚═╝░░╚══╝╚══════╝░░░╚═╝░░░");
            Console.WriteLine(@"---------------------------------------------------------------------------------");
            Console.WriteLine($"SDS endpoint at {resource}");

            try
            {
                // Get time indices for the last day
                DateTime currentTime = DateTime.UtcNow;

                // Format indices with "O" to use ISO 8601 standard timestamps
                string startIndex = (currentTime - TimeSpan.FromDays(1)).ToString("O");
                string endIndex = currentTime.ToString("O");

                Console.WriteLine("Step 2. Retrieve stream");
                SdsStream stream = await metadataService.GetStreamAsync(streamId).ConfigureAwait(false);
                Console.WriteLine($"Found stream: {stream.Id}");

                Console.WriteLine("Step 3. Retrieve Window events");
                System.Collections.Generic.IEnumerable<PItoADHEvent> windowEvents = await dataService.GetWindowValuesAsync<PItoADHEvent>(streamId, startIndex, endIndex).ConfigureAwait(false);
                Console.WriteLine($"Total events found: {windowEvents.Count()}");
                foreach (PItoADHEvent value in windowEvents)
                {
                    Console.WriteLine(value.ToString());
                }

                Console.WriteLine("Step 4. Retrieve Window events in table form");
                SdsTable windowEventsTable = await tableService.GetWindowValuesAsync(streamId, startIndex, endIndex).ConfigureAwait(false);
                foreach (System.Collections.Generic.IList<object> value in windowEventsTable.Rows)
                {
                    Console.WriteLine(string.Join(",", value.ToArray()));
                }

                Console.WriteLine("Step 5. Retrieve Paged events");
                Console.WriteLine("Sds has a limit of 250,000 events returned per data call, paging can be used to circumvent this by reading data one page at a time:");
                int eventsPerPage = 2;
                string continuationToken = string.Empty;

                do
                {
                    SdsResultPage<PItoADHEvent> resultPage = await dataService.GetWindowValuesAsync<PItoADHEvent>(streamId, startIndex, endIndex, SdsBoundaryType.Inside, eventsPerPage, continuationToken: continuationToken).ConfigureAwait(false);

                    foreach (PItoADHEvent value in resultPage.Results)
                    {
                        Console.WriteLine(value.ToString());
                    }

                    continuationToken = resultPage.ContinuationToken;
                } 
                while (!string.IsNullOrEmpty(continuationToken));

                Console.WriteLine("Step 6. Retrieve Range events");
                System.Collections.Generic.IEnumerable<PItoADHEvent> rangeValues = await dataService.GetRangeValuesAsync<PItoADHEvent>(streamId, startIndex, 10).ConfigureAwait(false);
                Console.WriteLine($"Total events found: {rangeValues.Count()}");
                foreach (PItoADHEvent value in rangeValues)
                {
                    Console.WriteLine(value.ToString());
                }

                Console.WriteLine("Step 7. Retrieve Interpolated events");
                Console.WriteLine("Sds can interpolate or extrapolate data at an index location where data does not explicitly exist:");
                System.Collections.Generic.IEnumerable<PItoADHEvent> interpolatedValues = await dataService.GetValuesAsync<PItoADHEvent>(streamId, startIndex, endIndex, 10).ConfigureAwait(false);
                Console.WriteLine($"Total events found: {interpolatedValues.Count()}");
                foreach (PItoADHEvent value in interpolatedValues)
                {
                    Console.WriteLine(value.ToString());
                }

                Console.WriteLine("Step 8. Retrieve Filtered events");
                Console.WriteLine($"To show the filter functionality, we will use the less than operator to show values less than 0. (This value can be replaced in the filter statement below to better fit the data set)");
                System.Collections.Generic.IEnumerable<PItoADHEvent> filteredValues = await dataService.GetWindowFilteredValuesAsync<PItoADHEvent>(streamId, startIndex, endIndex, SdsBoundaryType.Exact, $"Value lt 0").ConfigureAwait(false);
                Console.WriteLine($"Total events found: {filteredValues.Count()}");
                foreach (PItoADHEvent value in filteredValues)
                {
                    Console.WriteLine(value.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _toThrow = ex;
            }
            finally
            {
                Console.WriteLine("Complete!");
            }

            if (test && _toThrow != null)
            {
                throw _toThrow;
            }

            return _toThrow == null;
        }
    }
}
