using AssetStudio;

namespace AssetStudioCore
{
    public class AssetItem
    {
        public Object Asset { get; set; }
        public SerializedFile SourceFile { get; set; }
        public string Container { get; set; } = string.Empty;
        public string TypeString { get; set; }
        public long m_PathID { get; set; }
        public long FullSize { get; set; }
        public ClassIDType Type { get; set; }
        public string Text { get; set; } = string.Empty;
        public string InfoText { get; set; }
        public string UniqueID { get; set; }
        public AssetNode Node { get; set; }

        public AssetItem(Object asset)
        {
            Asset = asset;
            SourceFile = asset.assetsFile;
            Type = asset.type;
            TypeString = Type.ToString();
            m_PathID = asset.m_PathID;
            FullSize = asset.byteSize;
        }
    }
}
