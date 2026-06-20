using AssetStudio;

namespace AssetStudioCore
{
    public static class AudioPreviewHelper
    {
        public static byte[] GetWav(Object asset)
        {
            if (asset is not AudioClip audioClip)
                return null;

            var converter = new AudioClipConverter(audioClip);
            return converter.ConvertToWav();
        }
    }
}
