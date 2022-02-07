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
                string startIndex = (DateTime.UtcNow - TimeSpan.FromDays(1)).ToString(CultureInfo.InvariantCulture);
                string endIndex = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);

                Console.WriteLine("Step 2. Retrieve stream");
                Console.WriteLine("Getting PI to ADH stream");
                var stream = await metadataService.GetStreamAsync(streamId).ConfigureAwait(false);
                Console.WriteLine($"Found stream: {stream.Id}");

                Console.WriteLine("Step 3. Show verbosity");
                Console.WriteLine("Let\'s first use accept-verbose as True to see the PI point property columns included:");
                Console.WriteLine("Getting latest event of the stream, note how we can see the PI to ADH metadata included:");
                var last = await dataService.GetLastValueAsync<PItoADHFloatType>(streamId).ConfigureAwait(false);
                Console.WriteLine(last);

                Console.WriteLine("Now let\'s use accept-verbosity as False to see the difference:");
                verbosityHeaderHandler.Verbose = false;
                last = await dataService.GetLastValueAsync<PItoADHFloatType>(streamId).ConfigureAwait(false);
                Console.WriteLine(last.ToString());

                Console.WriteLine("Step 4. Retrieve Window events");
                verbosityHeaderHandler.Verbose = true;
                Console.WriteLine("Getting window events with verbosity accepted:");
                var windowEvents = await dataService.GetWindowValuesAsync<PItoADHFloatType>(streamId, startIndex, endIndex).ConfigureAwait(false);
                Console.WriteLine($"Total events found: {windowEvents.Count()}");
                foreach (var value in windowEvents)
                {
                    Console.WriteLine(value.ToString());
                }

                verbosityHeaderHandler.Verbose = false;
                Console.WriteLine("Getting window events with verbosity not accepted:");
                windowEvents = await dataService.GetWindowValuesAsync<PItoADHFloatType>(streamId, startIndex, endIndex).ConfigureAwait(false);
                Console.WriteLine($"Total events found: {windowEvents.Count()}");
                foreach (var value in windowEvents)
                {
                    Console.WriteLine(value.ToString());
                }

                Console.WriteLine("Step 5. Retrieve Window events in table form");
                Console.WriteLine("Getting window events as a table with headers:");
                var windowEventsTable = await tableService.GetWindowValuesAsync(streamId, startIndex, endIndex).ConfigureAwait(false);
                foreach (var value in windowEventsTable.Rows)
                {
                    Console.WriteLine(string.Join(",", value.ToArray()));
                }

                Console.WriteLine("Step 6. Retrieve Range events");
                Console.WriteLine("Getting range events with verbosity accepted:");
                var rangeValues = await dataService.GetRangeValuesAsync<PItoADHFloatType>(streamId, startIndex, 10).ConfigureAwait(false);
                Console.WriteLine($"Total events found: {rangeValues.Count()}");
                foreach (var value in rangeValues)
                {
                    Console.WriteLine(value.ToString());
                }

                Console.WriteLine("Step 7. Retrieve Interpolated events");
                Console.WriteLine("Sds can interpolate or extrapolate data at an index location where data does not explicitly exist:");
                var interpolatedValues = await dataService.GetValuesAsync<PItoADHFloatType>(streamId, startIndex, endIndex, 10).ConfigureAwait(false);
                Console.WriteLine($"Total events found: {interpolatedValues.Count()}");
                foreach (var value in interpolatedValues)
                {
                    Console.WriteLine(value.ToString());
                }

                Console.WriteLine("Step 8. Retrieve Filtered events");
                Console.WriteLine($"To show the filter functionality, we will use the less than operator to show values less than 0. (You can replace the value in the filter statements below to update this)");
                Console.WriteLine($"Getting filtered events for values less than 0");

                var filteredValues = await dataService.GetWindowFilteredValuesAsync<PItoADHFloatType>(streamId, startIndex, endIndex, SdsBoundaryType.Exact, $"Value lt 0").ConfigureAwait(false);
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
                if (!test)
                {
                    Console.ReadKey();
                }
            }

            if (test && _toThrow != null)
            {
                throw _toThrow;
            }

            return _toThrow == null;
        }
    }
}