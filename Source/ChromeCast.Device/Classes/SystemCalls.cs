using System.Diagnostics;

namespace ChromeCast.Classes
{
    public class SystemCalls
    {
        public static void StartPlaying(string url)
        {
            StopPlaying();

            using var PlayerProcess = new Process();
            PlayerProcess.StartInfo.FileName = "cvlc";
            PlayerProcess.StartInfo.Arguments = $"--play-and-exit {url}";
            PlayerProcess.Start();
        }

        public static void StopPlaying()
        {
            using var KillProcess = new Process();
            KillProcess.StartInfo.FileName = "pkill";
            KillProcess.StartInfo.Arguments = "vlc";
            KillProcess.Start();
            KillProcess.WaitForExit();
        }

        public static float GetVolume()
        {
            using var process = new Process();
            process.StartInfo.FileName = "amixer";
            process.StartInfo.Arguments = @"get Master playback";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            var reader = process.StandardOutput;
            var output = reader.ReadToEnd();
            if (int.TryParse(output.Substring(output.IndexOf("["), 4).Replace("%", "").Replace("[", "").Replace("]", "").Replace(" ", ""), out int levelInt))
            {
                return levelInt / 100.0f;
            }
            else
            {
                return 1.0f;
            }
        }

        public static void SetVolume(float level)
        {
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

        private static void ToggleMute()
        {
            using var process = new Process();
            process.StartInfo.FileName = "amixer";
            process.StartInfo.Arguments = $"-q sset Master toggle";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            process.WaitForExit(1000);
        }

        public static string SystemGuid()
        {
#if DEBUG
            return "debugging-40634b2a14f64d5caa028822412cc3cb";
#else
            try
            {
                using var process = new Process();
                process.StartInfo.FileName = "blkid";
                process.StartInfo.Arguments = "-s UUID -o value";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();
                var reader = process.StandardOutput;
                var output = reader.ReadToEnd();
                var guids = output.Split('\n').Where(x => !string.IsNullOrEmpty(x)).OrderByDescending(y => y.Length).ToArray();
                if (guids.Any())
                {
                    return guids.First().Trim();
                }
            }
            catch (Exception)
            {
            }

            return Guid.NewGuid().ToString();
#endif
        }
    }
}
