using AssetStudio;

namespace AssetStudioCore
{
    public class ExportSettings
    {
        public bool displayAll = false;
        public bool enablePreview = true;
        public bool displayInfo = true;
        public bool openAfterExport = true;
        public int assetGroupOption = 0;
        public bool convertTexture = true;
        public bool convertAudio = true;
        public ImageFormat convertType = ImageFormat.Png;
        public bool eulerFilter = true;
        public decimal filterPrecision = 0.25m;
        public bool exportAllNodes = true;
        public bool exportSkins = true;
        public bool exportAnimations = true;
        public decimal boneSize = 10;
        public int fbxVersion = 3;
        public int fbxFormat = 0;
        public decimal scaleFactor = 1;
        public bool exportBlendShape = true;
        public bool castToBone = false;
        public bool restoreExtensionName = true;
        public bool exportAllUvsAsDiffuseMaps = false;
    }
}
