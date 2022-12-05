using ChromeCast.Classes;
using System.Threading;

namespace ChromeCast.Device.Application
{
    public class VolumeHook
    {
        public float Level { get; set; }

        public void Start(Device Device)
        {
            Level = SystemCalls.GetVolume();
            while (true)
            {
                var newLevel = SystemCalls.GetVolume();
                if (newLevel != Level)
                {
                    Level = newLevel;
                    Device.SendNewVolume();
                }
                Thread.Sleep(1000);
            }
        }
    }
}