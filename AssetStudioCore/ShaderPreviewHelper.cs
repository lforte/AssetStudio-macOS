using AssetStudio;

namespace AssetStudioCore
{
    // Unity strips shader source out of built players/bundles - only compiled GPU bytecode
    // (DXBC/SPIR-V/Metal) survives, and decompiling that back into portable, runnable GLSL is
    // not practically feasible. This produces a best-effort representative material (color and
    // a couple of PBR hints) from whatever the shader's own declared property defaults tell us,
    // so different shaders at least look visually distinct in the 3D placeholder preview instead
    // of all rendering identically.
    public static class ShaderPreviewHelper
    {
        public class Descriptor
        {
            public float R = 1, G = 1, B = 1;
            public float Metalness = 0.3f;
            public float Roughness = 0.5f;
        }

        public static Descriptor Build(Shader shader)
        {
            var desc = new Descriptor();
            var name = shader.m_ParsedForm?.m_Name ?? shader.m_Name ?? string.Empty;
            var (hr, hg, hb) = HsvToRgb((uint)name.GetHashCode() % 360 / 360f, 0.55f, 0.9f);
            desc.R = hr;
            desc.G = hg;
            desc.B = hb;

            var props = shader.m_ParsedForm?.m_PropInfo?.m_Props;
            if (props == null)
                return desc;

            foreach (var p in props)
            {
                if (p.m_Type != SerializedPropertyType.Color || p.m_DefValue == null || p.m_DefValue.Length < 3)
                    continue;
                var r = p.m_DefValue[0];
                var g = p.m_DefValue[1];
                var b = p.m_DefValue[2];
                var isWhiteish = r > 0.9f && g > 0.9f && b > 0.9f;
                var isBlackish = r < 0.05f && g < 0.05f && b < 0.05f;
                if (isWhiteish || isBlackish)
                    continue;
                desc.R = r;
                desc.G = g;
                desc.B = b;
                break;
            }

            foreach (var p in props)
            {
                if (p.m_DefValue == null || p.m_DefValue.Length == 0 || string.IsNullOrEmpty(p.m_Name))
                    continue;
                var n = p.m_Name.ToLowerInvariant();
                if (n.Contains("metallic"))
                    desc.Metalness = p.m_DefValue[0];
                else if (n.Contains("smoothness") || n.Contains("glossiness"))
                    desc.Roughness = 1 - p.m_DefValue[0];
                else if (n.Contains("roughness"))
                    desc.Roughness = p.m_DefValue[0];
            }

            return desc;
        }

        private static (float, float, float) HsvToRgb(float h, float s, float v)
        {
            var i = (int)(h * 6);
            var f = h * 6 - i;
            var p = v * (1 - s);
            var q = v * (1 - f * s);
            var t = v * (1 - (1 - f) * s);
            switch (i % 6)
            {
                case 0: return (v, t, p);
                case 1: return (q, v, p);
                case 2: return (p, v, t);
                case 3: return (p, q, v);
                case 4: return (t, p, v);
                default: return (v, p, q);
            }
        }
    }
}
