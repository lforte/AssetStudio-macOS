using System.Collections.Generic;
using AssetStudio;

namespace AssetStudioCore
{
    // Wraps a standalone Mesh asset (no GameObject/Renderer/Material data) into the
    // IImported shape ModelExporter.ExportFbx expects, so a bare Mesh can be exported
    // as geometry-only FBX (no skin, no animation, no material).
    public class MeshFbxConverter : IImported
    {
        public ImportedFrame RootFrame { get; }
        public List<ImportedMesh> MeshList { get; } = new List<ImportedMesh>();
        public List<ImportedMaterial> MaterialList { get; } = new List<ImportedMaterial>();
        public List<ImportedTexture> TextureList { get; } = new List<ImportedTexture>();
        public List<ImportedKeyframedAnimation> AnimationList { get; } = new List<ImportedKeyframedAnimation>();
        public List<ImportedMorph> MorphList { get; } = new List<ImportedMorph>();

        public MeshFbxConverter(Mesh mesh)
        {
            RootFrame = new ImportedFrame
            {
                Name = mesh.m_Name,
                LocalPosition = Vector3.Zero,
                LocalRotation = Vector3.Zero,
                LocalScale = Vector3.One
            };

            var iMesh = new ImportedMesh
            {
                Path = mesh.m_Name,
                SubmeshList = new List<ImportedSubmesh>(),
                hasNormal = mesh.m_Normals?.Length > 0,
                hasUV = new bool[8],
                hasTangent = mesh.m_Tangents != null && mesh.m_Tangents.Length == mesh.m_VertexCount * 4,
                hasColor = mesh.m_Colors?.Length > 0
            };
            for (int uv = 0; uv < 8; uv++)
            {
                iMesh.hasUV[uv] = mesh.GetUV(uv)?.Length > 0;
            }

            int firstFace = 0;
            for (int i = 0; i < mesh.m_SubMeshes.Length; i++)
            {
                var submesh = mesh.m_SubMeshes[i];
                int numFaces = (int)submesh.indexCount / 3;
                var iSubmesh = new ImportedSubmesh
                {
                    Material = string.Empty,
                    BaseVertex = (int)submesh.firstVertex,
                    FaceList = new List<ImportedFace>(numFaces)
                };

                var end = firstFace + numFaces;
                for (int f = firstFace; f < end; f++)
                {
                    var face = new ImportedFace
                    {
                        VertexIndices = new[]
                        {
                            (int)(mesh.m_Indices[f * 3 + 2] - submesh.firstVertex),
                            (int)(mesh.m_Indices[f * 3 + 1] - submesh.firstVertex),
                            (int)(mesh.m_Indices[f * 3] - submesh.firstVertex)
                        }
                    };
                    iSubmesh.FaceList.Add(face);
                }
                firstFace = end;
                iMesh.SubmeshList.Add(iSubmesh);
            }

            iMesh.VertexList = new List<ImportedVertex>((int)mesh.m_VertexCount);
            for (var j = 0; j < mesh.m_VertexCount; j++)
            {
                var iVertex = new ImportedVertex();

                int c = mesh.m_Vertices.Length == mesh.m_VertexCount * 4 ? 4 : 3;
                iVertex.Vertex = new Vector3(-mesh.m_Vertices[j * c], mesh.m_Vertices[j * c + 1], mesh.m_Vertices[j * c + 2]);

                if (iMesh.hasNormal)
                {
                    var nc = mesh.m_Normals.Length == mesh.m_VertexCount * 4 ? 4 : 3;
                    iVertex.Normal = new Vector3(-mesh.m_Normals[j * nc], mesh.m_Normals[j * nc + 1], mesh.m_Normals[j * nc + 2]);
                }

                iVertex.UV = new float[8][];
                for (int uv = 0; uv < 8; uv++)
                {
                    if (!iMesh.hasUV[uv])
                        continue;
                    var m_UV = mesh.GetUV(uv);
                    var uvc = m_UV.Length == mesh.m_VertexCount * 3 ? 3 : 2;
                    iVertex.UV[uv] = uvc == 3
                        ? new[] { m_UV[j * uvc], m_UV[j * uvc + 1], m_UV[j * uvc + 2] }
                        : new[] { m_UV[j * uvc], m_UV[j * uvc + 1] };
                }

                if (iMesh.hasColor)
                {
                    var cc = mesh.m_Colors.Length == mesh.m_VertexCount * 3 ? 3 : 4;
                    iVertex.Color = cc == 3
                        ? new Color(mesh.m_Colors[j * cc], mesh.m_Colors[j * cc + 1], mesh.m_Colors[j * cc + 2], 1)
                        : new Color(mesh.m_Colors[j * cc], mesh.m_Colors[j * cc + 1], mesh.m_Colors[j * cc + 2], mesh.m_Colors[j * cc + 3]);
                }

                iMesh.VertexList.Add(iVertex);
            }

            MeshList.Add(iMesh);
        }
    }
}
