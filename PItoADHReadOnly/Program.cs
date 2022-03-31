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
            var tenantId = _configuration["TenantId"];
            var namespaceId = _configuration["NamespaceId"];
            var resource = _configuration["Resource"];
            var clientId = _configuration["ClientId"];
            var clientSecret = _configuration["ClientSecret"];
            var streamId = _configuration["StreamId"];
            var uriResource = new Uri(resource);

            // Get Sds Services to communicate with server
            AuthenticationHandler authenticationHandler = new AuthenticationHandler(uriResource, clientId, clientSecret);
            VerbosityHeaderHandler verbosityHeaderHandler = new VerbosityHeaderHandler();
            SdsService sdsService = new SdsService(uriResource, null, HttpCompressionMethod.GZip, authenticationHandler, verbosityHeaderHandler);

            var metadataService = sdsService.GetMetadataService(tenantId, namespaceId);
            var dataService = sdsService.GetDataService(tenantId, namespaceId);
            var tableService = sdsService.GetTableService(tenantId, namespaceId);

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
                var currentTime = DateTime.UtcNow;

                // Format indices with "O" to use ISO 8601 standard timestamps
                var startIndex = (currentTime - TimeSpan.FromDays(1)).ToString("O", CultureInfo.InvariantCulture);
                var endIndex = currentTime.ToString("O", CultureInfo.InvariantCulture);

                Console.WriteLine("Step 2. Retrieve stream");
                var stream = await metadataService.GetStreamAsync(streamId).ConfigureAwait(false);
                Console.WriteLine($"Found stream: {stream.Id}");

                Console.WriteLine("Step 3. Retrieve Window events");
                var windowEvents = await dataService.GetWindowValuesAsync<PItoADHEvent>(streamId, startIndex, endIndex).ConfigureAwait(false);
                Console.WriteLine($"Total events found: {windowEvents.Count()}");
                foreach (var value in windowEvents)
                {
                    Console.WriteLine(value.ToString());
                }

                Console.WriteLine("Step 4. Retrieve Window events in table form");
                var windowEventsTable = await tableService.GetWindowValuesAsync(streamId, startIndex, endIndex).ConfigureAwait(false);
                foreach (var value in windowEventsTable.Rows)
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

                    foreach (var value in resultPage.Results)
                    {
                        Console.WriteLine(value.ToString());
                    }

                    continuationToken = resultPage.ContinuationToken;
                } 
                while (!string.IsNullOrEmpty(continuationToken));

                Console.WriteLine("Step 6. Retrieve Range events");
                var rangeValues = await dataService.GetRangeValuesAsync<PItoADHEvent>(streamId, startIndex, 10).ConfigureAwait(false);
                Console.WriteLine($"Total events found: {rangeValues.Count()}");
                foreach (var value in rangeValues)
                {
                    Console.WriteLine(value.ToString());
                }

                Console.WriteLine("Step 7. Retrieve Interpolated events");
                Console.WriteLine("Sds can interpolate or extrapolate data at an index location where data does not explicitly exist:");
                var interpolatedValues = await dataService.GetValuesAsync<PItoADHEvent>(streamId, startIndex, endIndex, 10).ConfigureAwait(false);
                Console.WriteLine($"Total events found: {interpolatedValues.Count()}");
                foreach (var value in interpolatedValues)
                {
                    Console.WriteLine(value.ToString());
                }

                Console.WriteLine("Step 8. Retrieve Filtered events");
                Console.WriteLine($"To show the filter functionality, we will use the less than operator to show values less than 0. (This value can be replaced in the filter statement below to better fit the data set)");
                var filteredValues = await dataService.GetWindowFilteredValuesAsync<PItoADHEvent>(streamId, startIndex, endIndex, SdsBoundaryType.Exact, $"Value lt 0").ConfigureAwait(false);
                Console.WriteLine($"Total events found: {filteredValues.Count()}");
                foreach (var value in filteredValues)
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
