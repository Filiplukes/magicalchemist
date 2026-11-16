using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Rozšíří quad UI obrázku o průhledný okraj, aby se rozmazání z UI/Poison mohlo
/// "rozlít" ven z obdélníku sprite a nebylo useknuté do čtverce.
/// UV se protáhnou mimo 0–1; shader vše mimo 0–1 bere jako průhledné.
/// Přidává ho PoisonEffect.ApplyTo – ručně není potřeba.
/// </summary>
[RequireComponent(typeof(Graphic))]
[DisallowMultipleComponent]
public class PoisonUIPadding : BaseMeshEffect
{
    [Tooltip("Okraj na každé straně jako podíl velikosti obrázku. Musí pokrýt maximální rozmazání.")]
    [Range(0f, 0.5f)]
    public float padding = 0.15f;

    static readonly List<UIVertex> verts = new List<UIVertex>();

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || padding <= 0f || vh.currentVertCount == 0) return;

        verts.Clear();
        vh.GetUIVertexStream(verts);

        // Hranice pozic a UV celého meshe.
        Vector2 pMin = new Vector2(float.MaxValue, float.MaxValue), pMax = -pMin;
        Vector2 uMin = pMin, uMax = pMax;
        for (int i = 0; i < verts.Count; i++)
        {
            Vector2 p = verts[i].position;
            Vector2 u = verts[i].uv0;
            pMin = Vector2.Min(pMin, p); pMax = Vector2.Max(pMax, p);
            uMin = Vector2.Min(uMin, u); uMax = Vector2.Max(uMax, u);
        }

        Vector2 pSize = pMax - pMin;
        Vector2 uSize = uMax - uMin;
        if (pSize.x <= 0f || pSize.y <= 0f || uSize.x <= 0f || uSize.y <= 0f) return;

        Vector2 pCenter = (pMin + pMax) * 0.5f;
        Vector2 uCenter = (uMin + uMax) * 0.5f;
        float scale = 1f + padding * 2f;

        // Roztáhne mesh i UV kolem středu stejným poměrem – obrázek zůstane na místě a ve stejné velikosti.
        for (int i = 0; i < verts.Count; i++)
        {
            UIVertex v = verts[i];
            Vector2 p = v.position;
            Vector2 u = v.uv0;
            p = pCenter + (p - pCenter) * scale;
            u = uCenter + (u - uCenter) * scale;
            v.position = new Vector3(p.x, p.y, v.position.z);
            v.uv0 = new Vector4(u.x, u.y, 0f, 0f);
            verts[i] = v;
        }

        vh.Clear();
        vh.AddUIVertexTriangleStream(verts);
    }
}
