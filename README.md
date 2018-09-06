# smrtapi.net
C# client for Speechmatics real time API

## Installation
```powershell
Install-Package Speechmatics.Realtime.Client
```

## Sample code
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Speechmatics.Realtime.Client;
using Newtonsoft.Json;

namespace DemoApp
{
    public class Program
    {
        private const string SampleAudio = "2013-8-british-soccer-football-commentary-alex-warner.mp3";

        private static string ToJson(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        private static string RtUrl
        {
            get
            {
                var host = Environment.GetEnvironmentVariable("TEST_HOST") ?? "api.rt.speechmatics.io";
                return host.StartsWith("wss://") ? host : $"wss://{host}:9000/";
            }
        }

        // ReSharper disable once UnusedParameter.Local
        public static void Main(string[] args)
        {
            var builder = new StringBuilder();

            using (var stream = File.Open(SampleAudio, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    /*
                     * The API constructor is passed the websockets URL, callbacks for the messages it might receive,
                     * the language to transcribe (as a .NET CultureInfo object) and stream to read data from.
                     */
                    var config = new SmRtApiConfig("en")
                    {
                        AddTranscriptCallback = s => builder.Append(s),
                        AddTranscriptMessageCallback = s => Console.WriteLine(ToJson(s.words)),
                        AddPartialTranscriptMessageCallback = s => Console.WriteLine(ToJson(s)),
                        ErrorMessageCallback = s => Console.WriteLine(ToJson(s)),
                        WarningMessageCallback = s => Console.WriteLine(ToJson(s)),
                        CustomDictionaryPlainWords = new[] {"speechmagic"},
                        CustomDictionarySoundsLikes = new Dictionary<string, IEnumerable<string>>(),
                        Insecure = true
                    };

                    // We can do this here, or earlier. It's not used until .Run() is called on the API object.
                    config.CustomDictionarySoundsLikes["gnocchi"] = new[] {"nokey", "noki"};

                    var api = new SmRtApi(RtUrl,
                        stream,
                        config
                    );
                    // Run() will block until the transcription is complete.
                    Console.WriteLine($"Connecting to {RtUrl}");
                    api.Run();
                    Console.WriteLine(builder.ToString());
                }
                catch (AggregateException e)
                {
                    Console.WriteLine(e);
                }
            }

            Console.ReadLine();
        }
    }
}
```

# Sample Dockerfile

```
FROM microsoft/dotnet as build-env
WORKDIR /app

RUN git clone https://github.com/jrg1381/smrtapi.net.git
WORKDIR /app/smrtapi.net/SmRtAPI/DemoAppNetCore
RUN sed -i 's/<SignAssembly>true<\/SignAssembly>/<SignAssembly>false<\/SignAssembly>/' ../SmRtAPI/SpeechmaticsAPI.csproj
RUN dotnet build && dotnet publish -c Release -o out
RUN cp ../DemoApp/*.mp3 ./out

FROM microsoft/dotnet:runtime
WORKDIR /app
COPY --from=build-env /app/smrtapi.net/SmRtAPI/DemoAppNetCore/out ./
ENTRYPOINT ["dotnet", "DemoAppNetCore.dll"]
```
