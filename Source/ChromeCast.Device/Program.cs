using System.Linq;

namespace ChromeCast
{
    class Program
    {
        static void Main(string[] args)
        {
            // Log y/n?
            var log = args.Any(x => x == "-l");

            // Get device name
            var deviceName = "Device";
            var index = args.Select((s, i) => new { i, s }).Where(t => t.s == "-n").Select(t => t.i).ToList().FirstOrDefault();
            if (index >= 0 && args.Length > index + 1 && args[index + 1].First() != '-')
                deviceName = args[index + 1];

            // Get group names
            var groupNames = default(string);
            index = args.Select((s, i) => new { i, s }).Where(t => t.s == "-g").Select(t => t.i).ToList().FirstOrDefault();
            if (index >= 0 && args.Length > index + 1 && args[index + 1].First() != '-')
                groupNames = args[index + 1];

            _ = new Device.Application.Device(log, deviceName, groupNames);
            while (true) { };
        }
    }
}
