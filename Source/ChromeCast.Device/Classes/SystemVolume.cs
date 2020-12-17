using System.Diagnostics;

namespace ChromeCast.Classes
{
    public static class SystemVolume
    {
        public static float Get()
        {
            //amixer get PCM playback | sed -n '/.*\[\([0-9]*\)%].*/s//\1/p'
            using var process = new Process();
            process.StartInfo.FileName = "amixer";
            process.StartInfo.Arguments = @"get Master playback";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            var reader = process.StandardOutput;
            var output = reader.ReadToEnd();
            if (int.TryParse(output.Substring(output.IndexOf("["), 3).Replace("%", "").Replace("[", "").Replace("]", ""), out int levelInt))
            {
                return levelInt / 100.0f;
            }
            else
            {
                return 1.0f;
            }
        }

        public static void Set(float level)
        {
            //amixer sset Master 65536
            using var process = new Process();
            process.StartInfo.FileName = "amixer";
            process.StartInfo.Arguments = $"set Master {level*65536}";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            process.WaitForExit(1000);
        }

        public static void SetMute(bool muted)
        {
            var systemMuted = IsMuted();
            if (systemMuted != muted)
            {
                ToggleMute();
            }
        }

        public static bool IsMuted()
        {
            using var process = new Process();
            process.StartInfo.FileName = "amixer";
            process.StartInfo.Arguments = "get Master playback";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            var reader = process.StandardOutput;
            var output = reader.ReadToEnd();

            return output.Contains("[off]");
        }

        public static void ToggleMute()
        {
            //amixer -q sset Master toggle
            using var process = new Process();
            process.StartInfo.FileName = "amixer";
            process.StartInfo.Arguments = $"-q sset Master toggle";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            process.WaitForExit(1000);
        }
    }
}
