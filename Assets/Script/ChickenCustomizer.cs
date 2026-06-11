using UnityEngine;
using System.Collections.Generic;

public class ChickenCustomizer : MonoBehaviour
{
    public static ChickenCustomizer instance;
    public static Color selectedColor = Color.white;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public static void ApplyColorToChicken(GameObject chicken)
    {
        OriginalColorStore store = chicken.GetComponent<OriginalColorStore>();
        if (store == null)
        {
            store = chicken.AddComponent<OriginalColorStore>();
            store.Capture(chicken);
        }

        foreach (var entry in store.entries)
        {
            if (entry.renderer == null) continue;
            Material[] mats = entry.renderer.materials;
            for (int i = 0; i < mats.Length && i < entry.originalColors.Count; i++)
            {
                Color original = entry.originalColors[i];
                Color.RGBToHSV(original, out _, out float s, out float v);

                if (s < 0.3f)
                {
                    Color tinted = selectedColor * original;
                    tinted.a = original.a;

                    if (mats[i].HasProperty("_BaseColor"))
                        mats[i].SetColor("_BaseColor", tinted);
                    else if (mats[i].HasProperty("_Color"))
                        mats[i].SetColor("_Color", tinted);
                }
            }
            entry.renderer.materials = mats;
        }
    }
}

public class OriginalColorStore : MonoBehaviour
{
    [System.Serializable]
    public class RendererEntry
    {
        public Renderer renderer;
        public List<Color> originalColors = new List<Color>();
    }

    public List<RendererEntry> entries = new List<RendererEntry>();

    public void Capture(GameObject obj)
    {
        entries.Clear();
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
        {
            var entry = new RendererEntry { renderer = r };
            foreach (Material m in r.materials)
            {
                Color c = Color.white;
                if (m.HasProperty("_BaseColor"))      c = m.GetColor("_BaseColor");
                else if (m.HasProperty("_Color"))     c = m.GetColor("_Color");
                entry.originalColors.Add(c);
            }
            entries.Add(entry);
        }
    }
}