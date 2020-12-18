using ChromeCast.Classes;
using ChromeCast.Device.Application;
using ChromeCast.Device.ProtocolBuffer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ChromeCast.Device.Classes
{
    /// <summary>
    /// Classes used to send messages to a Chromecast device.
    /// </summary>
    public class ChromeCastMessages
    {
        private const string namespaceConnect = "urn:x-cast:com.google.cast.tp.connection";
        private const string namespaceHeartbeat = "urn:x-cast:com.google.cast.tp.heartbeat";
        private const string namespaceReceiver = "urn:x-cast:com.google.cast.receiver";
        private const string namespaceMedia = "urn:x-cast:com.google.cast.media";

        public static CastMessage GetVolumeSetMessage(Volume volume, int requestId, string sourceId = null, string destinationId = null)
        {
            if (volume == null)
                return null;

            var volumeMessage = new MessageVolume
            {
                type = "SET_VOLUME",
                volume = new SendVolume
                {
                    level = volume.level
                },
                requestId = requestId
            };
            return GetCastMessage(volumeMessage, namespaceReceiver, sourceId, destinationId);
        }

        public static CastMessage GetVolumeMuteMessage(bool muted, int requestId, string sourceId = null, string destinationId = null)
        {
            var volumeMessage = new MessageVolumeMute
            {
                type = "SET_VOLUME",
                volume = new SendVolumeMute
                {
                    muted = muted
                },
                requestId = requestId
            };
            return GetCastMessage(volumeMessage, namespaceReceiver, sourceId, destinationId);
        }

        public static CastMessage MediaStatusMessage(int requestId, DeviceState state, float secondsPlaying)
        {
            var mediaStatusMessage = new MessageMediaStatus
            {
                type = "MEDIA_STATUS",
                requestId = requestId,
                status = new List<MediaStatus>() {
                    new MediaStatus {
                        currentTime = secondsPlaying,
                        playerState = state.ToString().ToUpper(),
                        volume = new Volume
                        {
                            controlType = "master",
                            level = SystemCalls.GetVolume(),
                            muted = SystemCalls.IsMuted(),
                            stepInterval = .01f
                        }
                    }
                },
            };

            return GetCastMessage(mediaStatusMessage, namespaceReceiver);
        }

        public static CastMessage GetConnectMessage(string sourceId = null, string destinationId = null)
        {
            return GetCastMessage(new PayloadMessageBase { type = "CONNECT" }, namespaceConnect, sourceId, destinationId);
        }

        public static CastMessage GetCloseMessage()
        {
            return GetCastMessage(new PayloadMessageBase { type = "CLOSE" }, namespaceConnect, null, null);
        }

        public static CastMessage GetLaunchMessage(int requestId)
        {
            var message = new MessageLaunch { type = "LAUNCH", appId = "CC1AD845", requestId = requestId };
            return GetCastMessage(message, namespaceReceiver);
        }

        public static CastMessage GetLoadMessage(string streamingUrl, string sourceId, string destinationId, int requestId)
        {
            var message = new MessageLoad
            {
                type = "LOAD",
                autoplay = true,
                currentTime = 0,
                activeTrackIds = new List<object>(),
                repeatMode = "REPEAT_OFF",
                media = new Media
                {
                    contentId = streamingUrl,
                    contentType = "audio/wav",
                    streamType = "BUFFERED", // BUFFERED or LIVE
                    metadata = new Metadata
                    {
                        type = 0,
                        metadataType = 0,
                        title = "",
                        images = new List<Image>()
                    },
                },
                requestId = requestId
            };
            return GetCastMessage(message, namespaceMedia, sourceId, destinationId);
        }

        public static CastMessage ReceiverStatusMessage(int requestId)
        {
            var receiverStatusMessage = new MessageReceiverStatus
            {
                type = "RECEIVER_STATUS",
                requestId = requestId,
                status = new ReceiverStatus
                {
                    volume = new Volume
                    {
                        controlType = "master",
                        level = SystemCalls.GetVolume(),
                        muted = SystemCalls.IsMuted(),
                        stepInterval = .01f
                    },
                    applications = new List<Application> { 
                        new Application { 
                            appId = "CC1AD845",
                            sessionId = Guid.NewGuid().ToString(),
                            transportId = Guid.NewGuid().ToString()
                        } 
                    }
                }
            };
            return GetCastMessage(receiverStatusMessage, namespaceMedia);
        }

        public static CastMessage GetPauseMessage(string sessionId, int mediaSessionId, int requestId, string sourceId, string destinationId)
        {
            return GetCastMessage(new MessagePause { type = "PAUSE", sessionId = sessionId, mediaSessionId = mediaSessionId, requestId = requestId }, namespaceMedia, sourceId, destinationId);
        }

        public static CastMessage GetPlayMessage(string sessionId, int mediaSessionId, int requestId, string sourceId, string destinationId)
        {
            return GetCastMessage(new MessagePause { type = "PLAY", sessionId = sessionId, mediaSessionId = mediaSessionId, requestId = requestId }, namespaceMedia, sourceId, destinationId);
        }

        public static CastMessage GetPingMessage()
        {
            return GetCastMessage(new PayloadMessageBase { type = "PING" }, namespaceHeartbeat);
        }

        public static CastMessage GetPongMessage()
        {
            return GetCastMessage(new PayloadMessageBase { type = "PONG" }, namespaceHeartbeat);
        }

        public static CastMessage GetReceiverStatusMessage(int requestId)
        {
            return GetCastMessage(new MessageStatus { type = "GET_STATUS", requestId = requestId }, namespaceReceiver);
        }

        public static CastMessage GetMediaStatusMessage(int requestId, string sourceId, string destinationId)
        {
            return GetCastMessage(new MessageStatus { type = "GET_STATUS", requestId = requestId }, namespaceMedia, sourceId, destinationId);
        }

        public static CastMessage GetStopMessage(string sessionId, int mediaSessionId, int requestId, string sourceId, string destinationId)
        {
            return GetCastMessage(new MessagePause { type = "STOP", sessionId = sessionId, mediaSessionId = mediaSessionId, requestId = requestId }, namespaceMedia, sourceId, destinationId);
        }

        public static CastMessage GetCastMessage(PayloadMessageBase message, string msgNamespace, string sourceId = null, string destinationId = null)
        {
            if (string.IsNullOrWhiteSpace(sourceId)) sourceId = "sender-0";
            if (string.IsNullOrWhiteSpace(destinationId)) destinationId = "receiver-0";

            var jsonMessage = JsonConvert.SerializeObject(message);
            return new CastMessage.Builder
            {
                ProtocolVersion = 0,
                SourceId = sourceId,
                DestinationId = destinationId,
                PayloadType = 0,
                Namespace = msgNamespace,
                PayloadUtf8 = jsonMessage
            }.Build();
        }

        public static byte[] MessageToByteArray(CastMessage message)
        {
            if (message == null)
                return Array.Empty<byte>();

            var messageStream = new MemoryStream();
            message.WriteTo(messageStream);
            var bufMsg = messageStream.ToArray();

            var bufLen = BitConverter.GetBytes(bufMsg.Length);
            bufLen = bufLen.Reverse().ToArray();

            return bufLen.Concat(bufMsg).ToArray();
        }
    }
}
