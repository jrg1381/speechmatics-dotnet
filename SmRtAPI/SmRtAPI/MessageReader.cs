using System;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Speechmatics.Realtime.Client.Messages;
using Speechmatics.Realtime.Client.Interfaces;

namespace Speechmatics.Realtime.Client
{
    internal class MessageReader
    {
        private string _lastPartial;
        private int _ackedSequenceNumbers;
        private readonly ClientWebSocket _wsClient;
        private readonly AutoResetEvent _resetEvent;
        private readonly AutoResetEvent _recognitionStarted;
        private readonly ISmRtApi _api;

        internal MessageReader(ISmRtApi smRtApi, ClientWebSocket client, AutoResetEvent resetEvent, AutoResetEvent recognitionStarted)
        {
            _api = smRtApi;
            _wsClient = client;
            _resetEvent = resetEvent;
            _recognitionStarted = recognitionStarted;
        }

        internal async Task Start()
        {
            var receiveBuffer = new ArraySegment<byte>(new byte[32768]);
            var messageBuilder = new StringBuilder();

            while (true)
            {
                var haveCompleteMessage = false;
                messageBuilder.Clear();

                /* Because ProcessMessage is async now, we can't do the message building there, we must do it before launching the message */
                while (!haveCompleteMessage)
                {
                    var webSocketReceiveResult = await _wsClient.ReceiveAsync(receiveBuffer, _api.CancelToken);

                    var subset = new ArraySegment<byte>(receiveBuffer.Array, 0, webSocketReceiveResult.Count);
                    messageBuilder.Append(Encoding.UTF8.GetString(subset.ToArray()));
                    haveCompleteMessage = webSocketReceiveResult.EndOfMessage;
                    if (webSocketReceiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        // Assuming that a Close message has no information we need in it
                        _resetEvent.Set();
                    }
                }

                if(_resetEvent.WaitOne(0)) {
                    break;
                }

                // should consider a limit on how many are in flight
                ProcessMessage(messageBuilder.ToString());
            }
        }

        private async void ProcessMessage(string message)
        {
            var jsonObject = JObject.Parse(message);

            Trace.WriteLine("ProcessMessage: " + message);

            switch (jsonObject.Value<string>("message"))
            {
                case "RecognitionStarted":
                {
                    Trace.WriteLine("Recognition started");
                    _recognitionStarted.Set();
                    break;
                }
                case "AudioAdded":
                {
                    // Log the ack
                    Interlocked.Increment(ref _ackedSequenceNumbers);
                    break;
                }
                case "AddTranscript":
                {
                    string transcript = jsonObject["metadata"]["transcript"].Value<string>();
                    _api.Configuration.AddTranscriptMessageCallback?.Invoke(
                        JsonConvert.DeserializeObject<AddTranscriptMessage>(message));
                    _api.Configuration.AddTranscriptCallback?.Invoke(transcript);
                    // Example of artificially slowing down this processing
                    await Task.Delay(15000);
                    Console.WriteLine("AddTranscript slow process completed");
                    break;
                }
                case "AddPartialTranscript":
                {
                    _lastPartial = jsonObject["metadata"]["transcript"].Value<string>();
                    _api.Configuration.AddPartialTranscriptMessageCallback?.Invoke(JsonConvert.DeserializeObject<AddPartialTranscriptMessage>(message));
                    _api.Configuration.AddPartialTranscriptCallback?.Invoke(_lastPartial);
                    break;
                 }
                case "AddTranslation":
                {
                    _api.Configuration.AddTranslationMessageCallback?.Invoke(
                        JsonConvert.DeserializeObject<AddTranslationMessage>(message));
                    break;
                }
                case "AddPartialTranslation":
                {
                    _api.Configuration.AddPartialTranslationMessageCallback?.Invoke(JsonConvert.DeserializeObject<AddPartialTranslationMessage>(message));
                    break;
                 }
                case "EndOfTranscript":
                {
                    // Sometimes there is a partial without a corresponding transcript, let's pretend it was a transcript here.
                    _api.Configuration.AddTranscriptCallback?.Invoke(_lastPartial);
                    _api.Configuration.EndOfTranscriptCallback?.Invoke();
                    _resetEvent.Set();
                    break;
                }
                case "Error":
                {
                    _api.Configuration.ErrorMessageCallback?.Invoke(JsonConvert.DeserializeObject<ErrorMessage>(message));
                    _resetEvent.Set();
                    break;
                }
                case "Warning":
                {
                    _api.Configuration.WarningMessageCallback?.Invoke(JsonConvert.DeserializeObject<WarningMessage>(message));
                    break;
                }
                default:
                {
                    Trace.WriteLine(message);
                    break;
                }
            }
        }
    }
}