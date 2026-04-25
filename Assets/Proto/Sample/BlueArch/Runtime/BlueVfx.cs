using UnityEngine;

namespace Proto.Sample.BlueArch
{
    public static class BlueVfx
    {
        public static void Muzzle(Vector3 position, Vector3 direction)
        {
            Vector3 dir = direction.sqrMagnitude > 1e-4f ? direction.normalized : Vector3.forward;
            GameObject go = CreatePrim(PrimitiveType.Sphere, "BlueVfx_Muzzle",
                position + dir * 0.4f, 0.35f, new Color(1f, 0.85f, 0.3f));
            go.AddComponent<BlueVfxFade>().Init(0.12f);
        }

        public static void Hit(Vector3 position)
        {
            GameObject go = CreatePrim(PrimitiveType.Sphere, "BlueVfx_Hit",
                position, 0.45f, Color.white);
            go.AddComponent<BlueVfxFade>().Init(0.18f);
        }

        public static void SkillRing(Vector3 center, float radius)
        {
            GameObject go = new GameObject("BlueVfx_SkillRing");
            go.transform.position = center;

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = 64;
            lr.widthMultiplier = 0.18f;
            lr.material = NewUnlitMaterial(new Color(0.3f, 0.85f, 1f, 1f));
            Color ringColor = new Color(0.3f, 0.85f, 1f, 1f);
            lr.startColor = ringColor;
            lr.endColor = ringColor;

            go.AddComponent<BlueVfxRing>().Init(radius, 0.45f);
        }

        private static GameObject CreatePrim(PrimitiveType type, string name, Vector3 pos, float scale, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;

            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = NewUnlitMaterial(color);
            return go;
        }

        private static Material NewUnlitMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            Material mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;
            return mat;
        }
    }
}
