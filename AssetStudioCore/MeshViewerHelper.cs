using System.Globalization;
using System.Text;
using AssetStudio;

namespace AssetStudioCore
{
    // Builds a flat JSON geometry payload (positions/normals/uvs/indices) for the
    // three.js-based 3D mesh viewer in AssetStudio.Maui's WebView. Mirrors the same
    // X-flip / winding-order convention used by the OBJ and FBX exporters so the
    // preview matches what gets exported.
    public static class MeshViewerHelper
    {
        public static string BuildGeometryJson(Mesh mesh)
        {
            var sb = new StringBuilder();
            sb.Append("{\"vertices\":[");
            AppendVertices(sb, mesh);
            sb.Append("],\"normals\":[");
            AppendNormals(sb, mesh);
            sb.Append("],\"uvs\":[");
            AppendUVs(sb, mesh);
            sb.Append("],\"indices\":[");
            AppendIndices(sb, mesh);
            sb.Append("]}");
            return sb.ToString();
        }

        private static void AppendVertices(StringBuilder sb, Mesh mesh)
        {
            if (mesh.m_Vertices == null || mesh.m_Vertices.Length == 0)
                return;
            int c = mesh.m_Vertices.Length == mesh.m_VertexCount * 4 ? 4 : 3;
            for (int v = 0; v < mesh.m_VertexCount; v++)
            {
                if (v > 0) sb.Append(',');
                sb.Append(F(-mesh.m_Vertices[v * c])).Append(',')
                  .Append(F(mesh.m_Vertices[v * c + 1])).Append(',')
                  .Append(F(mesh.m_Vertices[v * c + 2]));
            }
        }

        private static void AppendNormals(StringBuilder sb, Mesh mesh)
        {
            if (mesh.m_Normals == null || mesh.m_Normals.Length == 0)
                return;
            int c = mesh.m_Normals.Length == mesh.m_VertexCount * 4 ? 4 : 3;
            for (int v = 0; v < mesh.m_VertexCount; v++)
            {
                if (v > 0) sb.Append(',');
                sb.Append(F(-mesh.m_Normals[v * c])).Append(',')
                  .Append(F(mesh.m_Normals[v * c + 1])).Append(',')
                  .Append(F(mesh.m_Normals[v * c + 2]));
            }
        }

        private static void AppendUVs(StringBuilder sb, Mesh mesh)
        {
            var uv0 = mesh.GetUV(0);
            if (uv0 == null || uv0.Length == 0)
                return;
            int c = uv0.Length == mesh.m_VertexCount * 3 ? 3 : 2;
            for (int v = 0; v < mesh.m_VertexCount; v++)
            {
                if (v > 0) sb.Append(',');
                sb.Append(F(uv0[v * c])).Append(',')
                  .Append(F(uv0[v * c + 1]));
            }
        }

        private static void AppendIndices(StringBuilder sb, Mesh mesh)
        {
            if (mesh.m_Indices == null || mesh.m_Indices.Count == 0)
                return;
            var first = true;
            int triCount = mesh.m_Indices.Count / 3;
            for (int f = 0; f < triCount; f++)
            {
                if (!first) sb.Append(',');
                first = false;
                // reversed winding to compensate for the X mirror above, matching the OBJ/FBX exporters
                sb.Append(mesh.m_Indices[f * 3 + 2]).Append(',')
                  .Append(mesh.m_Indices[f * 3 + 1]).Append(',')
                  .Append(mesh.m_Indices[f * 3]);
            }
        }

        private static string F(float value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
