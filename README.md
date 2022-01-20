# Reading PI to OCS Values from Sequential Data Store .Net Sample

**Version:** 1.0

[![Build Status](https://dev.azure.com/osieng/engineering/_apis/build/status/product-readiness/OCS/osisoft.sample-pi-to-ocs-read-only-data-dotnet?repoName=osisoft%2Fsample-pi-to-ocs-read-only-data-dotnet&branchName=main)](https://dev.azure.com/osieng/engineering/_build/latest?definitionId=4507&repoName=osisoft%2Fsample-pi-to-ocs-read-only-data-dotnet&branchName=main)


## Building a Client with the OCS Client Libraries

The sample described in this section makes use of the OSIsoft Cloud Services Client Libraries. When working in .NET, it is recommended that you use the OCS Client Libraries metapackage, OSIsoft.OCSClients. The metapackage is a NuGet package available from [https://api.nuget.org/v3/index.json](https://api.nuget.org/v3/index.json). The libraries offer a framework of classes that make client development easier.

[SDS documentation](https://ocs-docs.osisoft.com/Content_Portal/Documentation/SequentialDataStore/Data_Store_and_SDS.html)

Developed against DotNet 5.0.

## Getting Started

`Note: This sample requires an Id of a PI to OCS stream already created in SDS.`

In this example we assume that you have the dotnet core CLI.

To run this example from the commandline run

```shell
dotnet restore
dotnet run
```

to test this program change directories to the test and run

```shell
dotnet restore
dotnet test
```

## Configure constants for connecting and authentication

The sample is configured using the file [appsettings.placeholder.json](SdsClientLibraries/appsettings.placeholder.json). Before editing, rename this file to `appsettings.json`. This repository's `.gitignore` rules should prevent the file from ever being checked in to any fork or branch, to ensure credentials are not compromised.

The SDS Service is secured here by obtaining authentication tokens using credential clients. Such clients provide a client Id and an associated secret (or key) that are authenticated against OCS. You must replace the placeholders in your `appsettings.json` file with the authentication-related values you received from OSIsoft.

```json
{
  "NamespaceId": "PLACEHOLDER_REPLACE_WITH_NAMESPACE_ID",
  "TenantId": "PLACEHOLDER_REPLACE_WITH_TENANT_ID",
  "Resource": "https://dat-b.osisoft.com",
  "ClientId": "PLACEHOLDER_REPLACE_WITH_CLIENT_IDENTIFIER",
  "ClientSecret": "PLACEHOLDER_REPLACE_WITH_CLIENT_SECRET",
  "StreamId": "PLACEHOLDER_REPLACE_WITH_STREAM_ID"
}
```

The authentication values are provided to the `OSIsoft.Identity.AuthenticationHandler`.
The AuthenticationHandler is a DelegatingHandler that is attached to an HttpClient pipeline.

## Set up SDS clients

The client example works through three client interfaces:

- ISdsMetadataService for SdsStream operations
- ISdsDataService for reading and writing data
- ISdsTableService for reading data in table format

To enable GZip compression, this sample specifies `HttpCompressionMethod.Gzip` in the constructor for the `SdsService`.


## PI Tag Name to OCS Stream Id

Streams in SDS are referred to by their Id rather than by their name as is common with PI tags. To find the PI to OCS stream corresponding to your PI tag name in SDS, you can search in the SDS portal using the following format:

ID:PI_<YOUR_SERVER_NAME>_* AND Name:<PI_TAG_NAME>

The SDS portal can be found by navigating to the [Cloud Portal](http://cloud.osisoft.com) and visiting the *Sequential Data Store* option under the *Data Management* tab on the left hand menu, where you can find the search bar in the top center.

To do this programatically you can use the `GetStreamsAsync` method, for more information see the [Retrieve Streams by Query](#retreive-streams-by-query) section below.

## PI to OCS Stream properties

When ingressing data using PI to OCS, the resulting stream types contain a certain set of PI point attributes as stream type properties to give more information about the data:

| Column         | Description     | 
|--------------|-----------|
| IsQuestionable | The event value is unreliable or the circumstances under which it was recorded are suspect |
| IsSubstituted | The event value has been changed from the original archived value |
| IsAnnotated | An annotation has been made to the event to include further information or commentary |
| SystemStateCode | The system digital state code |
| DigitalStateName | The digital state name |

The amount of information included can be managed by setting the verbosity, which we will show in more detail below.


#### Verbosity

SDS read APIs supports an accept-verbosity header that will set whether verbose output should be excluded. A value is considered verbose if it is the default value for its type, such as false for a boolean, null for a string, etc. The following example output demonstrates responses for the same call using verbose and non-verbose values:

accept-verbosity = verbose
```json
{
  "Timestamp": "2021-12-15T01:39:05Z",
  "Value": 98.30506,
  "IsQuestionable": false,
  "IsSubstituted": true,
  "IsAnnotated": false,
  "SystemStateCode": null,
  "DigitalStateName": null
}
```

accept-verbosity = non-verbose
```json
{
  "Timestamp": "2021-12-15T01:39:05Z",
  "Value": 98.30506,
  "IsSubstituted": true,
}
```

Note that since `IsSubstituted` is True it is still included in both responses while the other values are excluded when not accepting verbose values.

When using the OCS Client libraries, this is configurable by providing a DelegatingHandler to the SdsService constructor. In this sample you can see this being used by created an instance of the [VerbosityHeaderHandler](VerbosityHeaderHandler.cs) and providing it along with the AuthenticationHandler when creating the SdsService:

```c#
AuthenticationHandler authenticationHandler = new AuthenticationHandler(uriResource, clientId, clientSecret);
VerbosityHeaderHandler verbosityHeaderHandler = new VerbosityHeaderHandler();
SdsService sdsService = new SdsService(uriResource, null, HttpCompressionMethod.GZip, authenticationHandler, verbosityHeaderHandler);
```
Verbosity can then be set to True of False depending on your preference:
```c#
verbosityHeaderHandler.Verbose = true;
```

## Retrieve Stream

To run this sample we will need to first retrieve the PI to OCS stream to read values from. This is done by calling the `GetStreamAsync` method providing the Id of the stream to retrieve. The stream Id is configured in the `appsettings.json` file as `StreamId`.

```c#
SdsStream stream = await metadataService.GetStreamAsync(streamId)
```

## Retreive Streams by Query

If you would like to query SDS for multiple streams we can use the `GetStreamsAsync` method providing a query parameter. This could be used to find PI to OCS streams given your PI tag and PI Server names as shown earlier by providing the same query format, ID:PI_<YOUR_SERVER_NAME>_* AND Name:<PI_TAG_NAME>. Following is the function definition:

```c#
IEnumerable<SdsStream> streams = await metadataService.GetStreamsAsync(query, skip: 0, count: 10)
```

## Retrieve Values from a Stream

There are many methods in the SDS REST API allowing for the retrieval of events from a stream, in this sample we will demonstrate reading Window, Range, and Filtered events, as well as using Interpolation.

The following is an example of getting Window events, where the start and end indices are datetime objects expressed as strings
```C#
IEnumerable<PItoOCSType> retrieved =
  await client.GetWindowValuesAsync<PItoOCSType>(stream.Id, startIndex, endIndex);
```

SDS can also retrieve the values in the form of a table

```C#
SdsTable tableEvents = await tableService.GetWindowValuesAsync(stream.Id, startIndex, endIndex);
```

To get Range events we provide a start index and a count to get that amount of values counting from the provided start index 
```C#
IEnumerable<PItoOCSType> rangeValues = await dataService.GetRangeValuesAsync<PItoOCSType>(streamId, startIndex, 10)
```

SDS can retrieve interpolated values where data does not explicitly exist. In this case we are asking for 10 events between the two indices. Suppose that if we only have 5 values stored between these indices, the other 5 values will be interpolated for.

```C#
IEnumerable<PItoOCSType> interpolatedValues = await dataService.GetValuesAsync<PItoOCSType>(streamId, startIndex, endIndex, 10)
```

When retreiving events you can also filter on what is being returned, so that you only get the events you are interested in.

```C#
IEnumerable<PItoOCSType> filteredValues = await dataService.GetWindowFilteredValuesAsync<PItoOCSType>(streamId, startIndex, endIndex, SdsBoundaryType.Exact, $"Value lt 89")
```

## Additional Methods

Note that there are more methods provided in the SdsClient than are discussed in this document, for a complete list of HTTP request URLs refer to the [SDS documentation](https://ocs-docs.osisoft.com/Content_Portal/Documentation/SequentialDataStore/Data_Store_and_SDS.html).

---

Automated test uses DotNet 5.0

For the main PI to OCS read only stream samples page [ReadMe](https://github.com/osisoft/OSI-Samples-OCS/blob/main/docs/PI_TO_OCS_READ_DATA.md)  
For the main OCS samples page [ReadMe](https://github.com/osisoft/OSI-Samples-OCS)
For the main OSIsoft samples page [ReadMe](https://github.com/osisoft/OSI-Samples)