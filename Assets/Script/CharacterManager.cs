using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterManager : MonoBehaviour
{
    public static CharacterManager instance;

    [System.Serializable]
    public class CharacterData
    {
        public string     characterName;
        public GameObject prefab;
        public Sprite     thumbnail;     
        public Color      defaultColor = Color.white;
    }

    [Header("Daftar Karakter (tambah bebas)")]
    public CharacterData[] characters;

    // State
    public static int   selectedIndex = 0;
    public static Color selectedColor = Color.white;

    [Header("UI Carousel")]
    public Button          btnPrev;           // tombol ←
    public Button          btnNext;           // tombol →
    public TextMeshProUGUI characterNameText; // nama karakter di tengah
    public Image           thumbnailImage;    // opsional, gambar karakter

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (btnPrev != null) btnPrev.onClick.AddListener(PrevCharacter);
        if (btnNext != null) btnNext.onClick.AddListener(NextCharacter);
        RefreshUI();
    }

    public void PrevCharacter()
    {
        selectedIndex = (selectedIndex - 1 + characters.Length) % characters.Length;
        selectedColor = characters[selectedIndex].defaultColor;
        RefreshUI();
        NotifyPreview();
    }

    public void NextCharacter()
    {
        selectedIndex = (selectedIndex + 1) % characters.Length;
        selectedColor = characters[selectedIndex].defaultColor;
        RefreshUI();
        NotifyPreview();
    }

    void RefreshUI()
    {
        if (characters.Length == 0) return;

        if (characterNameText != null)
            characterNameText.text = characters[selectedIndex].characterName;

        if (thumbnailImage != null)
        {
            bool hasThumbnail = characters[selectedIndex].thumbnail != null;
            thumbnailImage.gameObject.SetActive(hasThumbnail);
            if (hasThumbnail) thumbnailImage.sprite = characters[selectedIndex].thumbnail;
        }
    }

    // Kasih tau ChickenCustomizePanel buat update preview model
    void NotifyPreview()
    {
        ChickenCustomizePanel panel = FindObjectOfType<ChickenCustomizePanel>();
        if (panel != null) panel.UpdatePreviewModel();
    }

    public static GameObject GetSelectedPrefab()
    {
        if (instance == null || instance.characters.Length == 0) return null;
        return instance.characters[selectedIndex].prefab;
    }

    public static void ApplyCharacterInScene()
    {
        ChickenController oldChicken = null;
        foreach (var chicken in FindObjectsOfType<ChickenController>())
        {
            if (Vector3.Distance(chicken.transform.position, new Vector3(500f, 0f, 500f)) > 10f)
            {
                oldChicken = chicken;
                break;
            }
        }
        if (oldChicken == null || instance == null) return;

        GameObject newPrefab = GetSelectedPrefab();
        if (newPrefab == null) return;

        // Simpan info posisi & parent
        Transform  parent   = oldChicken.transform.parent;
        Vector3    pos      = oldChicken.transform.position;
        Quaternion rot      = oldChicken.transform.rotation;

        // Simpan starting transform asli dari oldChicken jika sudah diset, jika belum gunakan posisi saat ini
        Vector3    origStartPos = oldChicken.IsStartTransformSet() ? oldChicken.GetStartPosition() : pos;
        Quaternion origStartRot = oldChicken.IsStartTransformSet() ? oldChicken.GetStartRotation() : rot;

        // Nonaktifkan dan hapus yang lama agar tidak ada update logic tersisa di frame ini
        oldChicken.enabled = false;
        oldChicken.gameObject.SetActive(false);
        Destroy(oldChicken.gameObject);

        // Spawn yang baru di tempat yang sama
        GameObject newChicken = Instantiate(newPrefab, pos, rot, parent);

        // Copy starting transform asli ke new chicken agar reset posisi bekerja dengan benar
        ChickenController newController = newChicken.GetComponent<ChickenController>();
        if (newController != null)
        {
            newController.SetStartTransform(origStartPos, origStartRot);
        }

        // Apply warna
        ChickenCustomizer.selectedColor = selectedColor;
        ChickenCustomizer.ApplyColorToChicken(newChicken);
    }
}