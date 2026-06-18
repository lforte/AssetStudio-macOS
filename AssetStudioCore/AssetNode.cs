using System.Collections.Generic;
using AssetStudio;

namespace AssetStudioCore
{
    public class AssetNode
    {
        public string Text;
        public GameObject GameObject;
        public bool Checked;
        public List<AssetNode> Nodes = new List<AssetNode>();

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
