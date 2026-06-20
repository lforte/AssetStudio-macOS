using System.Collections.Generic;
using AssetStudio;

namespace AssetStudioCore
{
    public class AssetNode
    {
        public string Text { get; set; }
        public GameObject GameObject { get; set; }
        public bool Checked { get; set; }
        public List<AssetNode> Nodes { get; set; } = new List<AssetNode>();

        public AssetNode(string text)
        {
            Text = text;
        }

        public AssetNode(GameObject gameObject)
        {
            GameObject = gameObject;
            Text = gameObject.m_Name;
        }
    }
}
