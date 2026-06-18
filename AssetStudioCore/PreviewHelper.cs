using AssetStudio;

namespace AssetStudioCore
{
    public static class PreviewHelper
    {
        public static byte[] GetPreviewPng(AssetItem item)
        {
            switch (item.Asset)
            {
                case Texture2D m_Texture2D:
                    using (var image = m_Texture2D.ConvertToImage(true))
                    {
                        if (image == null)
                            return null;
                        using (var stream = image.ConvertToStream(ImageFormat.Png))
                        {
                            return stream.ToArray();
                        }
                    }
                case Sprite m_Sprite:
                    using (var image = m_Sprite.GetImage())
                    {
                        if (image == null)
                            return null;
                        using (var stream = image.ConvertToStream(ImageFormat.Png))
                        {
                            return stream.ToArray();
                        }
                    }
                default:
                    return null;
            }
        }
    }
}
