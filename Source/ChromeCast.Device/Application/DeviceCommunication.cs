using ChromeCast.Classes;
using ChromeCast.Device.Classes;
using ChromeCast.Device.Log.Interfaces;
using ChromeCast.Device.ProtocolBuffer;
using Newtonsoft.Json;
using System;

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
                        deviceListener.Write(ChromeCastMessages.MediaStatusMessage(volumeMuteMessage.requestId, state, SecondsPlaying()));
                        deviceListener.Write(ChromeCastMessages.ReceiverStatusMessage(volumeMuteMessage.requestId));
                    }
                    else
                    {
                        var volumeMessage = JsonConvert.DeserializeObject<MessageVolume>(castMessage.PayloadUtf8);
                        SystemCalls.SetVolume(volumeMessage.volume.level);
                        deviceListener.Write(ChromeCastMessages.MediaStatusMessage(volumeMessage.requestId, state, SecondsPlaying()));
                        deviceListener.Write(ChromeCastMessages.ReceiverStatusMessage(volumeMessage.requestId));
                    }
                    break;
                case "CONNECT":
                    state = DeviceState.Connected;
                    break;
                case "CLOSE":
                    state = DeviceState.Closed;
                    break;
                case "LAUNCH":
                    state = DeviceState.Launching;
                    var launchMessage = JsonConvert.DeserializeObject<MessageLaunch>(castMessage.PayloadUtf8);

                    deviceListener.Write(ChromeCastMessages.ReceiverStatusMessage(launchMessage.requestId));
                    break;
                case "LOAD":
                    state = DeviceState.Loading;
                    var loadMessage = JsonConvert.DeserializeObject<MessageLoad>(castMessage.PayloadUtf8);

                    logger.Log($"[{state}] Start playing: {loadMessage?.media?.contentId}");
                    SystemCalls.StartPlaying(loadMessage.media.contentId);
                    playerPlayTime = DateTime.Now;
                    deviceListener.Write(ChromeCastMessages.MediaStatusMessage(loadMessage.requestId, state, SecondsPlaying()));
                    state = DeviceState.Buffering;
                    break;
                case "PAUSE":
                    state = DeviceState.Paused;
                    var pauseMessage = JsonConvert.DeserializeObject<MessagePause>(castMessage.PayloadUtf8);
                    deviceListener.Write(ChromeCastMessages.MediaStatusMessage(pauseMessage.requestId, state, SecondsPlaying()));
                    break;
                case "PLAY":
                    break;
                case "STOP":
                    state = DeviceState.Idle;
                    var stopMessage = JsonConvert.DeserializeObject<MessageStop>(castMessage.PayloadUtf8);
                    SystemCalls.StopPlaying();
                    deviceListener.Write(ChromeCastMessages.MediaStatusMessage(stopMessage.requestId, state, SecondsPlaying()));
                    break;
                case "PING":
                    break;
                case "PONG":
                    break;
                case "GET_STATUS":
                    var getstatusMessage = JsonConvert.DeserializeObject<MessageStatus>(castMessage.PayloadUtf8);

                    if (state== DeviceState.Buffering)
                        state = DeviceState.Playing;

                    if (state == DeviceState.Playing)
                        deviceListener.Write(ChromeCastMessages.MediaStatusMessage(getstatusMessage.requestId, state, SecondsPlaying()));
                    else
                        deviceListener.Write(ChromeCastMessages.ReceiverStatusMessage(getstatusMessage.requestId));

                    break;
                default:
                    break;
            }
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
