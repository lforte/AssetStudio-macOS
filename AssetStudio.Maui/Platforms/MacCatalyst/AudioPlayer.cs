using AVFoundation;
using Foundation;

namespace AssetStudio.Maui;

public static class AudioPlayer
{
    private static AVAudioPlayer player;

    public static void Play(byte[] wavData)
    {
        Stop();
        var data = NSData.FromArray(wavData);
        player = new AVAudioPlayer(data, "wav", out var error);
        if (error != null)
            throw new InvalidOperationException(error.LocalizedDescription);
        player.Play();
    }

    public static void Stop()
    {
        player?.Stop();
        player?.Dispose();
        player = null;
    }

    public static bool IsPlaying => player?.Playing ?? false;
}
