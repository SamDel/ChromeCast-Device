using ChromeCast.Classes;
using ChromeCast.Device.Classes;
using ChromeCast.Device.Log.Interfaces;
using ChromeCast.Device.ProtocolBuffer;
using System.Text.Json;
using System;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace ChromeCast.Device.Application
{
    public class DeviceCommunication
    {
        private DeviceState state = DeviceState.Closed;
        private readonly ILogger logger;
        private DateTime playerPlayTime;

        public DeviceCommunication(ILogger loggerIn)
        {
            logger = loggerIn;
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        public void ProcessMessage(DeviceConnection deviceConnection, CastMessage castMessage)
        {
            var options = new JsonSerializerOptions { IncludeFields = true };
            var message = JsonSerializer.Deserialize<PayloadMessageBase>(castMessage.PayloadUtf8, options);
            switch (message.type)
            {
                case "SET_VOLUME":
                    if (castMessage.PayloadUtf8.Contains("muted", System.StringComparison.CurrentCulture))
                    {
                        var volumeMuteMessage = JsonSerializer.Deserialize<MessageVolumeMute>(castMessage.PayloadUtf8, options);
                        SystemCalls.SetMute(volumeMuteMessage.volume.muted);
                        deviceConnection.Write(ChromeCastMessages.MediaStatusMessage(volumeMuteMessage.requestId, state, SecondsPlaying()), state);
                        deviceConnection.Write(ChromeCastMessages.ReceiverStatusMessage(volumeMuteMessage.requestId), state);
                    }
                    else
                    {
                        var volumeMessage = JsonSerializer.Deserialize<MessageVolume>(castMessage.PayloadUtf8, options);
                        SystemCalls.SetVolume(volumeMessage.volume.level);
                        deviceConnection.Write(ChromeCastMessages.MediaStatusMessage(volumeMessage.requestId, state, SecondsPlaying()), state);
                        deviceConnection.Write(ChromeCastMessages.ReceiverStatusMessage(volumeMessage.requestId), state);
                    }
                    break;
                case "CONNECT":
                    state = DeviceState.Connected;
                    break;
                case "CLOSE":
                    state = DeviceState.Closed;
                    var closeMessage = JsonSerializer.Deserialize<MessageStop>(castMessage.PayloadUtf8, options);
                    deviceConnection.Write(ChromeCastMessages.MediaStatusMessage(closeMessage.requestId, state, 0), state);
                    break;
                case "LAUNCH":
                    state = DeviceState.Launching;
                    //var launchMessage = JsonSerializer.Deserialize<MessageLaunch>(castMessage.PayloadUtf8, options);

                    //deviceListener.Write(ChromeCastMessages.ReceiverStatusMessage(launchMessage.requestId), state);
                    break;
                case "LOAD":
                    deviceConnection.OnNewLoad();
                    state = DeviceState.Loading;
                    var loadMessage = JsonSerializer.Deserialize<MessageLoad>(castMessage.PayloadUtf8, options);

                    logger.Log($"[{state}] Start playing: {loadMessage?.media?.contentId}");
                    SystemCalls.StartPlaying(loadMessage.media.contentId);
                    playerPlayTime = DateTime.Now;
                    deviceConnection.Write(ChromeCastMessages.MediaStatusMessage(loadMessage.requestId, state, SecondsPlaying()), state);
                    state = DeviceState.Buffering;
                    Task.Delay(2000).Wait();
                    deviceConnection.Write(ChromeCastMessages.MediaStatusMessage(loadMessage.requestId, state, SecondsPlaying()), state);
                    break;
                case "PAUSE":
                    state = DeviceState.Paused;
                    var pauseMessage = JsonSerializer.Deserialize<MessagePause>(castMessage.PayloadUtf8, options);
                    deviceConnection.Write(ChromeCastMessages.MediaStatusMessage(pauseMessage.requestId, state, SecondsPlaying()), state);
                    break;
                case "PLAY":
                    state = DeviceState.Idle;
                    var playMessage = JsonSerializer.Deserialize<MessagePause>(castMessage.PayloadUtf8, options);
                    deviceConnection.Write(ChromeCastMessages.MediaStatusMessage(playMessage.requestId, state, SecondsPlaying()), state);
                    break;
                case "STOP":
                    state = DeviceState.Idle;
                    var stopMessage = JsonSerializer.Deserialize<MessageStop>(castMessage.PayloadUtf8, options);
                    deviceConnection.Write(ChromeCastMessages.MediaStatusMessage(stopMessage.requestId, state, 0), state);
                    SystemCalls.StopPlaying();
                    break;
                case "PING":
                    break;
                case "PONG":
                    break;
                case "GET_STATUS":
                    var getstatusMessage = JsonSerializer.Deserialize<MessageStatus>(castMessage.PayloadUtf8, options);

                    if (state== DeviceState.Buffering)
                        state = DeviceState.Playing;

                    switch (state)
                    {
                        case DeviceState.Idle:
                        case DeviceState.Closed:
                        case DeviceState.Connected:
                            deviceConnection.Write(ChromeCastMessages.ReceiverStatusMessage(getstatusMessage.requestId), state);
                            break;
                        case DeviceState.Playing:
                            deviceConnection.Write(ChromeCastMessages.MediaStatusMessage(getstatusMessage.requestId, state, SecondsPlaying()), state);
                            break;
                        default:
                            deviceConnection.Write(ChromeCastMessages.ReceiverStatusMessage(getstatusMessage.requestId), state);
                            break;
                    }

                    break;
                default:
                    logger.Log($"in default [{DateTime.Now.ToLongTimeString()}] {message.type} {castMessage.PayloadUtf8}");
                    break;
            }
        }

        public void Stop(DeviceConnection deviceConnection)
        {
            if (state == DeviceState.Playing ||
                state == DeviceState.Launching ||
                state == DeviceState.Loading ||
                state == DeviceState.Buffering)
            {
                state = DeviceState.Paused;
                deviceConnection.Write(ChromeCastMessages.MediaStatusMessage(0, state, 0), state);
            }
        }

        public void SendNewVolume(DeviceConnection deviceConnection)
        {
            deviceConnection.Write(ChromeCastMessages.MediaStatusMessage(0, state, SecondsPlaying()), state);
        }

        private float SecondsPlaying()
        {
            if (state == DeviceState.Playing || state == DeviceState.Loading)
            {
                return (float)(DateTime.Now - playerPlayTime).TotalSeconds;
            }
            else
            {
                return 0;
            }
        }
    }

    public enum DeviceState
    {
        Closed,
        Connected,
        Launching,
        Loading,
        Buffering,
        Playing,
        Paused,
        Idle,
    }
}
