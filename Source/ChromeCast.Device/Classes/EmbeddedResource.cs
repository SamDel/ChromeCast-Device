using System.Reflection;

namespace ChromeCast.Device.Classes
{
    public static class EmbeddedResource
    {
        public static byte[] GetResource(string ResourceName)
        {
            var asm = Assembly.GetEntryAssembly();
            //var names = asm.GetManifestResourceNames();
            var stream = asm.GetManifestResourceStream(ResourceName);
            var data = new byte[stream.Length];
            stream.Read(data, 0, (int)stream.Length);
            return data;
        }
    }
}
