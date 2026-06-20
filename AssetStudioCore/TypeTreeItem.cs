using System.Text;
using AssetStudio;

namespace AssetStudioCore
{
    public class TypeTreeItem
    {
        private readonly TypeTree m_Type;
        public int TypeID { get; }
        public string Text { get; }

        public TypeTreeItem(int typeID, TypeTree m_Type)
        {
            TypeID = typeID;
            this.m_Type = m_Type;
            Text = m_Type.m_Nodes[0].m_Type + " " + m_Type.m_Nodes[0].m_Name;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var i in m_Type.m_Nodes)
            {
                sb.AppendFormat("{0}{1} {2} {3} {4}\r\n", new string('\t', i.m_Level), i.m_Type, i.m_Name, i.m_ByteSize, (i.m_MetaFlag & 0x4000) != 0);
            }
            return sb.ToString();
        }
    }
}
