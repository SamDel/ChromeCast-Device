using ChromeCast.Device.Application.Interfaces;
using ChromeCast.Device.Classes;
using ChromeCast.Device.Log.Interfaces;
using ChromeCast.Device.ProtocolBuffer;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ChromeCast.Device.Application
{
    public class Device : IDisposable
    {
        private DeviceListener DeviceListener { get; set; }
        private IBaseListener DeviceEurekaInfoListener { get; set; }
        private DeviceCommunication DeviceCommunication { get; set; }
        private readonly ILogger logger;
        private readonly TasksToCancel taskList;
        private readonly IPAddress ipAddress;
        private readonly string DeviceName;
        private MdnsAdvertise MdnsAdvertise { get; set; }

        public Device(bool log, string deviceNameIn)
        {
            DeviceName = deviceNameIn;
            logger = new Log.Logger(log);
            logger.SetCallback(Console.WriteLine);
            logger.Log("Initializing...");
            taskList = new TasksToCancel();
            DeviceListener = new DeviceListener();
            DeviceCommunication = new DeviceCommunication(logger);
            DeviceEurekaInfoListener = new DeviceEurekaInfoListener();
            MdnsAdvertise = new MdnsAdvertise(logger, DeviceName);
            MdnsAdvertise.Advertise();
            ipAddress = Network.GetIp4Address();
            if (ipAddress == null)
            {
                logger.Log(Strings.Message_NoIPAddress);
                return;
            }

            StartTask(() =>
            {
                DeviceListener.StartListening(ipAddress, 8009, OnReceive, logger);
            });
            StartTask(() =>
            {
                DeviceEurekaInfoListener.StartListening(ipAddress, 8008, OnReceiveEureka, logger);
            });
            StartTask(() =>
            {
                new VolumeHook().Start(this);
            });
        }

        private void OnReceiveEureka(Socket socket, string message)
        {
            logger.Log($"in [{DateTime.Now.ToLongTimeString()}] [{ipAddress}:8008] {((IPEndPoint)socket.RemoteEndPoint).Address}");

            //TODO: For now always send the same response.
            var json = "{\"audio\":{\"digital\":false},\"build_info\":{\"build_type\":2,\"cast_build_revision\":\"1.36.145856\",\"cast_control_version\":1,\"preview_channel_state\":0,\"release_track\":\"stable-channel\",\"system_build_number\":\"12\"},\"detail\":{\"icon_list\":[{\"depth\":32,\"height\":55,\"mimetype\":\"image/png\",\"url\":\"/setup/icon.png\",\"width\":98}],\"locale\":{\"display_string\":\"\"},\"timezone\":{\"display_string\":\"\",\"offset\":60}},\"device_info\":{\"4k_blocked\":0,\"capabilities\":{\"audio_hdr_supported\":false,\"audio_surround_mode_supported\":false,\"ble_supported\":true,\"cloudcast_supported\":true,\"display_supported\":false,\"fdr_supported\":false,\"hdmi_prefer_50hz_supported\":false,\"hdmi_prefer_high_fps_supported\":false,\"hotspot_supported\":true,\"https_setup_supported\":true,\"input_management_supported\":true,\"keep_hotspot_until_connected_supported\":true,\"multichannel_group_supported\":true,\"multizone_supported\":true,\"opencast_supported\":false,\"reboot_supported\":true,\"setup_supported\":true,\"stats_supported\":true,\"system_sound_effects_supported\":true,\"wifi_auto_save_supported\":true,\"wifi_regulatory_domain_locked\":true,\"wifi_supported\":true},\"cloud_device_id\":\"B4D627DB9FF5175F84C88E008C302F9F\",\"factory_country_code\":\"US\",\"hotspot_bssid\":\"00:09:B0:70:03:87\",\"local_authorization_token_hash\":\"Et1RC23nh7c+0nlAcyPpWqv3KmKqlta9+4s4Jk3poZ8=\",\"mac_address\":\"00:09:AA:AA:AA:AA\",\"manufacturer\":\"\",\"model_name\":\"\",\"product_name\":\"\",\"public_key\":\"MIIBCgKCAQEAmN6likKI//DxCsrLZiDqvS8layshFWkVBkhc0Un4i7CTCuGz9SKipS7KY51cbiGa2zqE5g3fdww5y7CpR+l7JW8gQeS1v9V8svvKpKjlrVtgv5wzQwdy1p9+r4IK4iw2GmcFJ4zivPZua3P7DGMlQ6ZiYdGJ5xiwqYrNsp1h3scWWjGkdJppmNblvKv18Y6UvfjK/skJtsBjlKpxAmmW44uVI6EYFhHMtJvhXqbx2KLatQU/EaoRTPBcy7NNNVWfJQvfrpjvtF7k0iAjd5odkgvKbr3KMSIG769tnoti4nQKK3EmUeHL1pVdYCWzmCgmoPqSddVa4vcUC0qFte93jwIDAQAB\",\"ssdp_udn\":\"87747bec-48c7-cba1-d7e6-8565711c9979\",\"uptime\":1809470.041184},\"multizone\":{\"audio_output_delay\":0.0,\"audio_output_delay_hdmi\":0.0,\"audio_output_delay_oem\":5000.0,\"aux_in_group\":\"\",\"groups\":[],\"multichannel_status\":0},\"name\":\"{0}\",\"net\":{\"ethernet_connected\":false,\"ethernet_oui\":\"00:09:B0\",\"ip_address\":\"192.168.1.145\",\"online\":true},\"opt_in\":{\"audio_hdr\":false,\"audio_surround_mode\":0,\"autoplay_on_signal\":true,\"cloud_ipc\":true,\"hdmi_prefer_50hz\":false,\"hdmi_prefer_high_fps\":true,\"managed_mode\":false,\"opencast\":false,\"preview_channel\":false,\"remote_ducking\":true,\"stats\":false,\"ui_flipped\":false},\"proxy\":{\"mode\":\"system\"},\"settings\":{\"closed_caption\":{},\"control_notifications\":2,\"country_code\":\"US\",\"locale\":\"nl\",\"network_standby\":0,\"system_sound_effects\":false,\"time_format\":1,\"timezone\":\"\",\"wake_on_cast\":1},\"setup\":{\"setup_state\":60,\"ssid_suffix\":\"d\",\"stats\":{\"num_check_connectivity\":0,\"num_connect_wifi\":0,\"num_connected_wifi_not_saved\":0,\"num_initial_eureka_info\":0,\"num_obtain_ip\":0},\"tos_accepted\":true},\"user_eq\":{\"high_shelf\":{\"frequency\":4500.0,\"gain_db\":0.0,\"quality\":0.707},\"low_shelf\":{\"frequency\":150.0,\"gain_db\":0.0,\"quality\":0.707},\"max_peaking_eqs\":0,\"peaking_eqs\":[]},\"version\":8,\"wifi\":{\"bssid\":\"60:38:e0:a1:ae:5b\",\"has_changes\":false,\"ssid\":\"\",\"wpa_configured\":false,\"wpa_id\":10000,\"wpa_state\":10}}";
            json = json.Replace("{0}", DeviceName);
            socket.Send(Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nAccess-Control-Allow-Headers: Content-Type\r\nCache-Control: no-cache\r\nContent-Type: application/json\r\nContent-Length: {json.Length}\r\n\r\n{json}\r\n"));
            socket.Close();
        }

        private void OnReceive(CastMessage message)
        {
            logger.Log($"in [{DateTime.Now.ToLongTimeString()}] [{ipAddress}:8009]: {message.PayloadUtf8}");
            DeviceCommunication.ProcessMessage(DeviceListener, message);
        }

        /// <summary>
        /// Start an action in a new task.
        /// </summary>
        public void StartTask(Action action, CancellationTokenSource cancellationTokenSource = null)
        {
            taskList.Add(action, cancellationTokenSource);
        }

        public void StopListening()
        {
            DeviceListener.StopListening();
            DeviceEurekaInfoListener.StopListening();
        }

        public void SendNewVolume(float level)
        {
            DeviceCommunication.SendNewVolume(level, DeviceListener);
        }

        public void Dispose()
        {
            StopListening();
            GC.SuppressFinalize(this);
        }
    }
}
