using System.Diagnostics;

namespace ChromeCast.Device.Application
{
    public class Player
    {
        public PlayerState PlayerState { get; set; }

        public void Play(string url)
        {
            using (var PlayerProcess = new Process())
            {
                PlayerProcess.StartInfo.FileName = "cvlc";
                PlayerProcess.StartInfo.Arguments = url;
                PlayerProcess.Start();
            }
            PlayerState = PlayerState.Buffering;
        }

        public void Stop()
        {
            using (var KillProcess = new Process())
            {
                KillProcess.StartInfo.FileName = "pkill";
                KillProcess.StartInfo.Arguments = "vlc";
                KillProcess.Start();
            }
            PlayerState = PlayerState.Idle;
        }
    }

    public enum PlayerState
    {
        Idle,
        Buffering,
        Playing
    }
}
