using ChromeCast.Classes;
using ChromeCast.Device.Classes;
using ChromeCast.Device.Log.Interfaces;
using ChromeCast.Device.ProtocolBuffer;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

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

        public void ProcessMessage(DeviceListener deviceListener, CastMessage castMessage)
        {
            var message = JsonConvert.DeserializeObject<PayloadMessageBase>(castMessage.PayloadUtf8);
            switch (message.type)
            {
                case "SET_VOLUME":
                    if (castMessage.PayloadUtf8.Contains("muted", System.StringComparison.CurrentCulture))
                    {
                        var volumeMuteMessage = JsonConvert.DeserializeObject<MessageVolumeMute>(castMessage.PayloadUtf8);
                        SystemCalls.SetMute(volumeMuteMessage.volume.muted);
                        deviceListener.Write(ChromeCastMessages.MediaStatusMessage(volumeMuteMessage.requestId, state, SecondsPlaying()), state);
                        deviceListener.Write(ChromeCastMessages.ReceiverStatusMessage(volumeMuteMessage.requestId), state);
                    }
                    else
                    {
                        var volumeMessage = JsonConvert.DeserializeObject<MessageVolume>(castMessage.PayloadUtf8);
                        SystemCalls.SetVolume(volumeMessage.volume.level);
                        deviceListener.Write(ChromeCastMessages.MediaStatusMessage(volumeMessage.requestId, state, SecondsPlaying()), state);
                        deviceListener.Write(ChromeCastMessages.ReceiverStatusMessage(volumeMessage.requestId), state);
                    }
                    break;
                case "CONNECT":
                    state = DeviceState.Connected;
                    break;
                case "CLOSE":
                    state = DeviceState.Closed;
                    var closeMessage = JsonConvert.DeserializeObject<MessageStop>(castMessage.PayloadUtf8);
                    deviceListener.Write(ChromeCastMessages.MediaStatusMessage(closeMessage.requestId, state, 0), state);
                    break;
                case "LAUNCH":
                    state = DeviceState.Launching;
                    var launchMessage = JsonConvert.DeserializeObject<MessageLaunch>(castMessage.PayloadUtf8);

                    deviceListener.Write(ChromeCastMessages.ReceiverStatusMessage(launchMessage.requestId), state);
                    break;
                case "LOAD":
                    state = DeviceState.Loading;
                    var loadMessage = JsonConvert.DeserializeObject<MessageLoad>(castMessage.PayloadUtf8);

                    logger.Log($"[{state}] Start playing: {loadMessage?.media?.contentId}");
                    SystemCalls.StartPlaying(loadMessage.media.contentId);
                    playerPlayTime = DateTime.Now;
                    deviceListener.Write(ChromeCastMessages.MediaStatusMessage(loadMessage.requestId, state, SecondsPlaying()), state);
                    state = DeviceState.Buffering;
                    Task.Delay(2000).Wait();
                    deviceListener.Write(ChromeCastMessages.MediaStatusMessage(loadMessage.requestId, state, SecondsPlaying()), state);
                    break;
                case "PAUSE":
                    state = DeviceState.Paused;
                    var pauseMessage = JsonConvert.DeserializeObject<MessagePause>(castMessage.PayloadUtf8);
                    deviceListener.Write(ChromeCastMessages.MediaStatusMessage(pauseMessage.requestId, state, SecondsPlaying()), state);
                    break;
                case "PLAY":
                    break;
                case "STOP":
                    state = DeviceState.Idle;
                    var stopMessage = JsonConvert.DeserializeObject<MessageStop>(castMessage.PayloadUtf8);
                    SystemCalls.StopPlaying();
                    deviceListener.Write(ChromeCastMessages.MediaStatusMessage(stopMessage.requestId, state, 0), state);
                    break;
                case "PING":
                    break;
                case "PONG":
                    break;
                case "GET_STATUS":
                    var getstatusMessage = JsonConvert.DeserializeObject<MessageStatus>(castMessage.PayloadUtf8);

                    if (state== DeviceState.Buffering)
                        state = DeviceState.Playing;

                    switch (state)
                    {
                        case DeviceState.Idle:
                        case DeviceState.Closed:
                        case DeviceState.Connected:
                            deviceListener.Write(ChromeCastMessages.ReceiverStatusMessage(getstatusMessage.requestId), state);
                            break;
                        case DeviceState.Playing:
                            deviceListener.Write(ChromeCastMessages.MediaStatusMessage(getstatusMessage.requestId, state, SecondsPlaying()), state);
                            break;
                        default:
                            deviceListener.Write(ChromeCastMessages.ReceiverStatusMessage(getstatusMessage.requestId), state);
                            break;
                    }

                    break;
                default:
                    break;
            }
        }

        public void SendNewVolume(float level, DeviceListener deviceListener)
        {
            deviceListener.Write(ChromeCastMessages.MediaStatusMessage(0, state, SecondsPlaying()), state);
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
