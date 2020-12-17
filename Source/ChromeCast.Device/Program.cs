using System.Linq;

namespace ChromeCast
{
    class Program
    {
        static void Main(string[] args)
        {
            var log = args.Any(x => x == "-l");
            var deviceName = "Device";
            var index = args.Select((s, i) => new { i, s }).Where(t => t.s == "-n").Select(t => t.i).ToList().FirstOrDefault();
            if (index > 0 && args.Length >= index + 1)
                deviceName = args[index + 1];

            _ = new Device.Application.Device(log, deviceName);
            while (true) { };
        }
    }
}
