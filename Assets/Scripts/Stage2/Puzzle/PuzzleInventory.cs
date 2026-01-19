using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[System.Serializable]
public class PuzzlePiece
{
    public int pieceID;
    public string pieceName;
    public Sprite pieceImage;
    public bool isCollected;
    
    public PuzzlePiece(int id, string name, Sprite image)
    {
        pieceID = id;
        pieceName = name;
        pieceImage = image;
        isCollected = false;
    }
}

[System.Serializable]
public class PuzzleSlot
{
    public int slotID;
    public GameObject slotObject;
    public RectTransform rectTransform;
    public Image slotImage;
    public PuzzlePiece placedPiece;
    public bool isCorrectPosition;
}

public class PuzzleInventory : MonoBehaviour
{
    public static PuzzleInventory Instance { get; private set; }
    
    [Header("UI Settings")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject container;
    [SerializeField] private Transform piecesContainer;
    [SerializeField] private GameObject pieceUIPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private Text progressText;
    
    [Header("Puzzle Settings")]
    [Range(1, 20)]
    [SerializeField] private int totalPieces = 5;
    [SerializeField] private Sprite defaultPieceImage;
    
    [Header("Puzzle Grid Settings")]
    [SerializeField] private Transform puzzleGridContainer;
    [SerializeField] private GameObject puzzleSlotPrefab;
    [SerializeField] private int gridColumns = 3;
    [SerializeField] private float gridSpacing = 10f;
    [SerializeField] private float slotSize = 200f; // Ukuran slot untuk menampung foto besar
    
    [Header("Draggable Piece Settings")]
    [SerializeField] private float pieceWidth = 1024f; // Lebar foto
    [SerializeField] private float pieceHeight = 768f; // Tinggi foto
    [SerializeField] private bool randomizeInitialPosition = true;
    [SerializeField] private Vector2 randomAreaOffset = new Vector2(-300f, 0f); // Offset area acak dari center container
    [SerializeField] private Vector2 randomAreaSize = new Vector2(500f, 700f); // Ukuran area untuk acak piece
    
    [Header("Complete Puzzle Settings")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private Text victoryText;
    [SerializeField] private AudioClip puzzleCompleteSFX;
    [SerializeField] private AudioClip piecePlaceSFX;
    [SerializeField] private AudioClip pieceWrongSFX;
    [SerializeField] private float sfxVolume = 0.7f;
    
    private List<PuzzlePiece> puzzlePieces = new List<PuzzlePiece>();
    private List<PuzzleSlot> puzzleSlots = new List<PuzzleSlot>();
    private List<GameObject> draggablePieces = new List<GameObject>();
    private bool isInventoryOpen = true;
    private bool isPuzzleSolved = false;
    
    private void Awake()
    {
        
        if (Instance == null)
        {
            Instance = this;
            // JANGAN gunakan DontDestroyOnLoad di sini
            Debug.Log("PuzzleInventory Instance dibuat");
        }
        else
        {
            Debug.LogWarning("PuzzleInventory sudah ada, menghapus duplicate...");
            Destroy(gameObject);
            return;
        }
        
        inventoryPanel.SetActive(false);
        InitializeInventory();
    }
    
    private void AutoFindReferences()
    {
        Debug.Log("=== AUTO-FINDING REFERENCES ===");
        
        // Coba cari inventoryPanel jika null
        if (inventoryPanel == null)
        {
            GameObject foundPanel = GameObject.Find("InventoryPanel");
            if (foundPanel != null)
            {
                inventoryPanel = foundPanel;
                Debug.Log("InventoryPanel ditemukan otomatis!");
            }
        }
        
        if (container == null)
        {
            GameObject foundContainer = GameObject.Find("MainContainer");
            if (foundContainer != null)
            {
                container = foundContainer;
                Debug.Log("MainContainer ditemukan otomatis!");
            }
        }
        
        if (piecesContainer == null)
        {
            GameObject foundPieces = GameObject.Find("PiecesContainer");
            if (foundPieces != null)
            {
                piecesContainer = foundPieces.transform;
                Debug.Log("PiecesContainer ditemukan otomatis!");
            }
        }
        
        if (puzzleGridContainer == null)
        {
            GameObject foundGrid = GameObject.Find("PuzzleGridContainer");
            if (foundGrid != null)
            {
                puzzleGridContainer = foundGrid.transform;
                Debug.Log("PuzzleGridContainer ditemukan otomatis!");
            }
        }
        
        if (progressText == null)
        {
            GameObject foundText = GameObject.Find("ProgressText");
            if (foundText != null)
            {
                progressText = foundText.GetComponent<Text>();
                Debug.Log("ProgressText ditemukan otomatis!");
            }
        }
        
        if (closeButton == null)
        {
            GameObject foundButton = GameObject.Find("CloseButton");
            if (foundButton != null)
            {
                closeButton = foundButton.GetComponent<Button>();
                Debug.Log("CloseButton ditemukan otomatis!");
            }
        }
        
        if (victoryPanel == null)
        {
            GameObject foundVictory = GameObject.Find("VictoryPanel");
            if (foundVictory != null)
            {
                victoryPanel = foundVictory;
                Debug.Log("VictoryPanel ditemukan otomatis!");
            }
        }
        
        if (victoryText == null)
        {
            GameObject foundVText = GameObject.Find("VictoryText");
            if (foundVText != null)
            {
                victoryText = foundVText.GetComponent<Text>();
                Debug.Log("VictoryText ditemukan otomatis!");
            }
        }
    }
    
    private void Start()
    {
        Debug.Log("=== PUZZLE INVENTORY START ===");
        
        // PAKSA CARI ULANG reference di Start untuk memastikan
        FindReferencesInScene();
        
        // Debug semua reference
        Debug.Log($"Inventory Panel: {(inventoryPanel != null ? inventoryPanel.name : "NULL")}");
        Debug.Log($"Container: {(container != null ? container.name : "NULL")}");
        Debug.Log($"Pieces Container: {(piecesContainer != null ? piecesContainer.name : "NULL")}");
        Debug.Log($"Piece UI Prefab: {(pieceUIPrefab != null ? pieceUIPrefab.name : "NULL")}");
        Debug.Log($"Puzzle Grid Container: {(puzzleGridContainer != null ? puzzleGridContainer.name : "NULL")}");
        Debug.Log($"Puzzle Slot Prefab: {(puzzleSlotPrefab != null ? puzzleSlotPrefab.name : "NULL")}");
        
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
            Debug.Log("Inventory Panel dinonaktifkan di Start");
        }
        else
        {
            Debug.LogError("INVENTORY PANEL TIDAK TER-ASSIGN!");
        }
        
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseInventory);
        }
        
        if (puzzleGridContainer != null && puzzleSlotPrefab != null)
        {
            SetupPuzzleGrid();
        }
        else
        {
            Debug.LogWarning("Puzzle Grid Container atau Slot Prefab belum di-assign!");
        }
        
        UpdateUI();
        
        Debug.Log("=== PUZZLE INVENTORY START SELESAI ===");
    }
    
    // Method baru untuk cari reference di scene
    private void FindReferencesInScene()
    {
        Debug.Log("=== MENCARI REFERENCES DI SCENE ===");
        
        if (inventoryPanel == null)
        {
            inventoryPanel = GameObject.Find("InventoryPanel");
            if (inventoryPanel != null)
                Debug.Log("✓ InventoryPanel ditemukan!");
            else
                Debug.LogError("✗ InventoryPanel TIDAK ditemukan!");
        }
        
        if (container == null)
        {
            container = GameObject.Find("MainContainer");
            if (container != null)
                Debug.Log("✓ MainContainer ditemukan!");
        }
        
        if (piecesContainer == null)
        {
            GameObject found = GameObject.Find("PiecesContainer");
            if (found != null)
            {
                piecesContainer = found.transform;
                Debug.Log("✓ PiecesContainer ditemukan!");
            }
        }
        
        if (puzzleGridContainer == null)
        {
            GameObject found = GameObject.Find("PuzzleGridContainer");
            if (found != null)
            {
                puzzleGridContainer = found.transform;
                Debug.Log("✓ PuzzleGridContainer ditemukan!");
            }
        }
        
        if (progressText == null)
        {
            GameObject found = GameObject.Find("ProgressText");
            if (found != null)
            {
                progressText = found.GetComponent<Text>();
                Debug.Log("✓ ProgressText ditemukan!");
            }
        }
        
        if (closeButton == null)
        {
            GameObject found = GameObject.Find("CloseButton");
            if (found != null)
            {
                closeButton = found.GetComponent<Button>();
                Debug.Log("✓ CloseButton ditemukan!");
            }
        }
        
        if (victoryPanel == null)
        {
            victoryPanel = GameObject.Find("VictoryPanel");
            if (victoryPanel != null)
                Debug.Log("✓ VictoryPanel ditemukan!");
        }
        
        if (victoryText == null)
        {
            GameObject found = GameObject.Find("VictoryText");
            if (found != null)
            {
                victoryText = found.GetComponent<Text>();
                Debug.Log("✓ VictoryText ditemukan!");
            }
        }
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }
        
        if (Input.GetKeyDown(KeyCode.R) && isInventoryOpen)
        {
            ResetPuzzlePositions();
        }
    }
    
    private void InitializeInventory()
    {
        puzzlePieces.Clear();
        
        for (int i = 0; i < totalPieces; i++)
        {
            puzzlePieces.Add(new PuzzlePiece(i, $"Puzzle Piece {i + 1}", defaultPieceImage));
        }
        
        Debug.Log($"Inventory diinisialisasi dengan {totalPieces} slot");
    }
    
    private void SetupPuzzleGrid()
    {
        foreach (Transform child in puzzleGridContainer)
        {
            Destroy(child.gameObject);
        }
        puzzleSlots.Clear();
        
        int gridRows = Mathf.CeilToInt((float)totalPieces / gridColumns);
        
        for (int i = 0; i < totalPieces; i++)
        {
            GameObject slotObj = Instantiate(puzzleSlotPrefab, puzzleGridContainer);
            RectTransform slotRect = slotObj.GetComponent<RectTransform>();
            
            slotRect.sizeDelta = new Vector2(slotSize, slotSize);
            
            int row = i / gridColumns;
            int col = i % gridColumns;
            
            float posX = col * (slotSize + gridSpacing);
            float posY = -row * (slotSize + gridSpacing);
            
            slotRect.anchoredPosition = new Vector2(posX, posY);
            
            Image slotImage = slotObj.GetComponent<Image>();
            if (slotImage == null)
            {
                slotImage = slotObj.AddComponent<Image>();
            }
            slotImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            
            PuzzleSlot slot = new PuzzleSlot
            {
                slotID = i,
                slotObject = slotObj,
                rectTransform = slotRect,
                slotImage = slotImage,
                placedPiece = null,
                isCorrectPosition = false
            };
            
            puzzleSlots.Add(slot);
            
            SetupDropZone(slotObj, i);
            
            Text slotText = slotObj.GetComponentInChildren<Text>();
            if (slotText != null)
            {
                slotText.text = (i + 1).ToString();
            }
        }
        
        Debug.Log($"Puzzle grid dibuat: {gridRows}x{gridColumns} ({totalPieces} slot)");
    }
    
    private void SetupDropZone(GameObject slotObj, int slotID)
    {
        PuzzleSlotDrop dropComponent = slotObj.GetComponent<PuzzleSlotDrop>();
        if (dropComponent == null)
        {
            dropComponent = slotObj.AddComponent<PuzzleSlotDrop>();
        }
        dropComponent.Initialize(slotID, this);
    }
    
    public void AddPiece(int pieceID, string pieceName, Sprite pieceImage)
    {
        if (pieceID < 0 || pieceID >= puzzlePieces.Count)
        {
            Debug.LogError($"Piece ID {pieceID} tidak valid! (Max: {puzzlePieces.Count - 1})");
            return;
        }
        
        PuzzlePiece piece = puzzlePieces[pieceID];
        
        if (piece.isCollected)
        {
            Debug.LogWarning($"Piece {pieceID} sudah dikumpulkan sebelumnya!");
            return;
        }
        
        piece.pieceName = pieceName;
        piece.pieceImage = pieceImage ?? defaultPieceImage;
        piece.isCollected = true;
        
        Debug.Log($"Piece ditambahkan: {pieceName} (ID: {pieceID})");
        
        UpdateUI();
        
        // TIDAK AUTO-SELESAI, hanya notifikasi semua piece terkumpul
        if (IsPuzzleComplete())
        {
            OnAllPiecesCollected(); // Method baru untuk notifikasi
        }
    }
    
    // Method baru: Notifikasi semua piece sudah dikumpulkan (tapi belum disusun)
    private void OnAllPiecesCollected()
    {
        Debug.Log("=== SEMUA PIECE TELAH DIKUMPULKAN! ===");
        Debug.Log("Buka Inventory (tekan I) dan susun puzzle untuk menyelesaikan!");
        
        // Play SFX collection complete (berbeda dengan puzzle solved)
        PlaySFX(puzzleCompleteSFX);
        
        // Bisa tambahkan notifikasi UI di sini
        // Misalnya: tampilkan text "Semua foto terkumpul! Buka inventory untuk menyusun puzzle"
    }
    
    public bool HasPiece(int pieceID)
    {
        if (pieceID < 0 || pieceID >= puzzlePieces.Count)
            return false;
            
        return puzzlePieces[pieceID].isCollected;
    }
    
    public int GetCollectedCount()
    {
        int count = 0;
        foreach (PuzzlePiece piece in puzzlePieces)
        {
            if (piece.isCollected) count++;
        }
        return count;
    }
    
    public bool IsPuzzleComplete()
    {
        return GetCollectedCount() >= totalPieces;
    }
    
    public bool IsPuzzleSolved()
    {
        return isPuzzleSolved;
    }
    
    private void CreateDraggablePiece(PuzzlePiece piece)
    {
        if (pieceUIPrefab == null || piecesContainer == null) return;
        
        GameObject pieceUI = Instantiate(pieceUIPrefab, piecesContainer);
        
        // Setup Image
        Image image = pieceUI.GetComponent<Image>();
        if (image == null)
        {
            image = pieceUI.AddComponent<Image>();
        }
        image.sprite = piece.pieceImage;
        image.color = Color.white;
        
        // Setup Text (opsional, bisa di-hide untuk puzzle foto)
        Text text = pieceUI.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.gameObject.SetActive(false); // Sembunyikan text untuk puzzle foto
        }
        
        // Setup Outline untuk visual highlight
        Outline outline = pieceUI.GetComponent<Outline>();
        if (outline == null)
        {
            outline = pieceUI.AddComponent<Outline>();
        }
        outline.effectColor = new Color(1f, 1f, 1f, 0.5f); // Putih semi-transparan
        outline.effectDistance = new Vector2(4, 4);
        
        // SET UKURAN FOTO: 1024x768
        RectTransform rect = pieceUI.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(pieceWidth, pieceHeight);
        }
        
        SetupDraggableComponent(pieceUI, piece.pieceID);
        
        draggablePieces.Add(pieceUI);
    }
    
    private void SetupDraggableComponent(GameObject pieceUI, int pieceID)
    {
        DraggablePuzzlePiece draggable = pieceUI.GetComponent<DraggablePuzzlePiece>();
        if (draggable == null)
        {
            draggable = pieceUI.AddComponent<DraggablePuzzlePiece>();
        }
        draggable.Initialize(pieceID, this);
        
        RectTransform rect = pieceUI.GetComponent<RectTransform>();
        if (rect != null && piecesContainer != null)
        {
            if (randomizeInitialPosition)
            {
                // POSISI ACAK DI AREA KIRI (antara puzzle grid)
                // Area acak dibatasi agar tidak keluar canvas
                
                float randomX = Random.Range(
                    randomAreaOffset.x - randomAreaSize.x / 2,
                    randomAreaOffset.x + randomAreaSize.x / 2
                );
                
                float randomY = Random.Range(
                    randomAreaOffset.y - randomAreaSize.y / 2,
                    randomAreaOffset.y + randomAreaSize.y / 2
                );
                
                // Clamp agar tidak keluar dari PiecesContainer
                RectTransform containerRect = piecesContainer.GetComponent<RectTransform>();
                if (containerRect != null)
                {
                    float maxX = containerRect.rect.width / 2 - pieceWidth / 2;
                    float maxY = containerRect.rect.height / 2 - pieceHeight / 2;
                    
                    randomX = Mathf.Clamp(randomX, -maxX, maxX);
                    randomY = Mathf.Clamp(randomY, -maxY, maxY);
                }
                
                rect.anchoredPosition = new Vector2(randomX, randomY);
                
                // TIDAK ADA ROTASI (sudah dinonaktifkan sesuai permintaan)
                rect.localRotation = Quaternion.identity;
                
                Debug.Log($"Piece {pieceID} spawn di posisi: ({randomX:F0}, {randomY:F0})");
            }
            else
            {
                // Posisi default grid-like
                int row = pieceID / 2;
                int col = pieceID % 2;
                
                float posX = col * (pieceWidth + 20) - pieceWidth / 2;
                float posY = -row * (pieceHeight + 20);
                
                rect.anchoredPosition = new Vector2(posX, posY);
            }
        }
    }
    
    public void OnPieceDragged(int pieceID, GameObject pieceObj)
    {
        foreach (PuzzleSlot slot in puzzleSlots)
        {
            if (slot.slotID == pieceID && slot.slotImage != null)
            {
                slot.slotImage.color = new Color(0.5f, 0.8f, 0.5f, 0.8f);
            }
            else if (slot.slotImage != null)
            {
                slot.slotImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            }
        }
    }
    
    public void OnPieceDragEnd(int pieceID, GameObject pieceObj)
    {
        foreach (PuzzleSlot slot in puzzleSlots)
        {
            if (slot.slotImage != null)
            {
                slot.slotImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            }
        }
    }
    
    public void OnPieceDroppedOnSlot(int slotID, int pieceID, GameObject pieceObj)
    {
        if (slotID < 0 || slotID >= puzzleSlots.Count) return;
        
        PuzzleSlot slot = puzzleSlots[slotID];
        
        if (slot.placedPiece != null)
        {
            Debug.Log("Slot sudah terisi, memindahkan piece lama...");
            ResetPiecePosition(slot.placedPiece.pieceID);
        }
        
        if (pieceID == slotID)
        {
            slot.placedPiece = puzzlePieces[pieceID];
            slot.isCorrectPosition = true;
            
            RectTransform pieceRect = pieceObj.GetComponent<RectTransform>();
            pieceRect.SetParent(slot.rectTransform);
            pieceRect.anchoredPosition = Vector2.zero;
            pieceRect.localScale = Vector3.one;
            
            DraggablePuzzlePiece draggable = pieceObj.GetComponent<DraggablePuzzlePiece>();
            if (draggable != null)
            {
                draggable.SetLocked(true);
            }
            
            if (slot.slotImage != null)
            {
                slot.slotImage.color = new Color(0.2f, 0.8f, 0.2f, 0.5f);
            }
            
            PlaySFX(piecePlaceSFX);
            
            Debug.Log($"Piece {pieceID} ditempatkan di slot {slotID} (BENAR)");
            
            CheckPuzzleCompletion();
        }
        else
        {
            slot.placedPiece = puzzlePieces[pieceID];
            slot.isCorrectPosition = false;
            
            RectTransform pieceRect = pieceObj.GetComponent<RectTransform>();
            pieceRect.SetParent(slot.rectTransform);
            pieceRect.anchoredPosition = Vector2.zero;
            pieceRect.localScale = Vector3.one;
            
            if (slot.slotImage != null)
            {
                slot.slotImage.color = new Color(0.8f, 0.2f, 0.2f, 0.5f);
            }
            
            PlaySFX(pieceWrongSFX);
            
            Debug.Log($"Piece {pieceID} ditempatkan di slot {slotID} (SALAH)");
        }
    }
    
    private void ResetPiecePosition(int pieceID)
    {
        foreach (GameObject pieceObj in draggablePieces)
        {
            DraggablePuzzlePiece draggable = pieceObj.GetComponent<DraggablePuzzlePiece>();
            if (draggable != null && draggable.GetPieceID() == pieceID)
            {
                pieceObj.transform.SetParent(piecesContainer);
                
                RectTransform rect = pieceObj.GetComponent<RectTransform>();
                if (rect != null && piecesContainer != null)
                {
                    if (randomizeInitialPosition)
                    {
                        // POSISI ACAK BARU di area kiri
                        float randomX = Random.Range(
                            randomAreaOffset.x - randomAreaSize.x / 2,
                            randomAreaOffset.x + randomAreaSize.x / 2
                        );
                        
                        float randomY = Random.Range(
                            randomAreaOffset.y - randomAreaSize.y / 2,
                            randomAreaOffset.y + randomAreaSize.y / 2
                        );
                        
                        // Clamp agar tidak keluar
                        RectTransform containerRect = piecesContainer.GetComponent<RectTransform>();
                        if (containerRect != null)
                        {
                            float maxX = containerRect.rect.width / 2 - pieceWidth / 2;
                            float maxY = containerRect.rect.height / 2 - pieceHeight / 2;
                            
                            randomX = Mathf.Clamp(randomX, -maxX, maxX);
                            randomY = Mathf.Clamp(randomY, -maxY, maxY);
                        }
                        
                        rect.anchoredPosition = new Vector2(randomX, randomY);
                        rect.localRotation = Quaternion.identity; // Tidak ada rotasi
                    }
                    else
                    {
                        int row = pieceID / 2;
                        int col = pieceID % 2;
                        
                        float posX = col * (pieceWidth + 20) - pieceWidth / 2;
                        float posY = -row * (pieceHeight + 20);
                        
                        rect.anchoredPosition = new Vector2(posX, posY);
                    }
                }
                
                draggable.SetLocked(false);
                break;
            }
        }
    }
    
    private void CheckPuzzleCompletion()
    {
        // Cek apakah SEMUA slot sudah terisi dengan BENAR
        bool allCorrect = true;
        int correctCount = 0;
        
        foreach (PuzzleSlot slot in puzzleSlots)
        {
            if (!slot.isCorrectPosition || slot.placedPiece == null)
            {
                allCorrect = false;
            }
            else
            {
                correctCount++;
            }
        }
        
        Debug.Log($"Progress: {correctCount}/{puzzleSlots.Count} pieces benar");
        
        // Update progress text
        if (progressText != null)
        {
            progressText.text = $"{correctCount}/{puzzleSlots.Count} Pieces Correct";
            
            if (correctCount == puzzleSlots.Count)
            {
                progressText.color = Color.green;
            }
            else
            {
                progressText.color = Color.white;
            }
        }
        
        // PUZZLE SELESAI hanya jika SEMUA piece di tempat yang BENAR
        if (allCorrect && !isPuzzleSolved)
        {
            isPuzzleSolved = true;
            OnPuzzleSolved();
        }
    }
    
    private void OnPuzzleSolved()
    {
        Debug.Log("=== 🎉 PUZZLE BERHASIL DISELESAIKAN! 🎉 ===");
        
        PlaySFX(puzzleCompleteSFX);
        
        // Tampilkan victory panel
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }
        
        if (victoryText != null)
        {
            victoryText.text = "SELAMAT!\nANDA MENANG!\n\nPuzzle Berhasil Diselesaikan!";
        }
        
        // Lock semua piece agar tidak bisa digerakkan lagi
        foreach (GameObject pieceObj in draggablePieces)
        {
            DraggablePuzzlePiece draggable = pieceObj.GetComponent<DraggablePuzzlePiece>();
            if (draggable != null)
            {
                draggable.SetLocked(true);
            }
        }
        
        // Bisa trigger event lain di sini, misalnya buka pintu
        // GameManager.Instance.OnPuzzleSolved();
    }
    
    private void ResetPuzzlePositions()
    {
        Debug.Log("Reset posisi puzzle...");
        
        foreach (PuzzleSlot slot in puzzleSlots)
        {
            slot.placedPiece = null;
            slot.isCorrectPosition = false;
            if (slot.slotImage != null)
            {
                slot.slotImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            }
        }
        
        isPuzzleSolved = false;
        
        foreach (GameObject pieceObj in draggablePieces)
        {
            Destroy(pieceObj);
        }
        draggablePieces.Clear();
        
        // Recreate pieces dengan posisi acak baru
        foreach (PuzzlePiece piece in puzzlePieces)
        {
            if (piece.isCollected)
            {
                CreateDraggablePiece(piece);
            }
        }
        
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }
        
        Debug.Log("Puzzle direset dengan posisi acak baru!");
    }
    
    private void UpdateUI()
    {
        if (piecesContainer == null || pieceUIPrefab == null) return;
        
        foreach (GameObject pieceObj in draggablePieces)
        {
            Destroy(pieceObj);
        }
        draggablePieces.Clear();
        
        foreach (PuzzlePiece piece in puzzlePieces)
        {
            if (piece.isCollected)
            {
                CreateDraggablePiece(piece);
            }
        }
        
        if (progressText != null)
        {
            int collectedCount = GetCollectedCount();
            progressText.text = $"{collectedCount}/{totalPieces} Pieces";
            
            if (IsPuzzleComplete())
            {
                progressText.color = Color.green;
                progressText.text += " - LENGKAP!";
            }
            else
            {
                progressText.color = Color.white;
            }
        }
    }
    
    private void ToggleInventory()
    {
        if (inventoryPanel == null) return;
        
        isInventoryOpen = !isInventoryOpen;
        inventoryPanel.SetActive(isInventoryOpen);
        
        PlayerMovement.IsMovementBlocked = isInventoryOpen;
        
        if (isInventoryOpen)
        {
            UpdateUI();
        }
    }
    
    public void OpenInventory()
    {
        Debug.Log("Membuka inventory puzzle...");
        
        if (inventoryPanel == null)
        {
            Debug.LogError("INVENTORY PANEL NULL! Pastikan sudah di-assign di Inspector!");
            return;
        }
        
        Debug.Log("=== INVENTORY PUZZLE DIBUKA ===");
        inventoryPanel.SetActive(true);
        isInventoryOpen = true;
        PlayerMovement.IsMovementBlocked = true;
        UpdateUI();
        
        Debug.Log($"Inventory Panel aktif: {inventoryPanel.activeSelf}");
    }
    
    public void CloseInventory()
    {
        if (inventoryPanel == null) return;
        
        inventoryPanel.SetActive(false);
        isInventoryOpen = false;
        PlayerMovement.IsMovementBlocked = false;
    }
    
    private void OnPuzzleComplete()
    {
        Debug.Log("=== SEMUA PIECE TELAH DIKUMPULKAN! ===");
        // Jangan play SFX victory di sini, karena puzzle belum diselesaikan
        // Hanya notifikasi bahwa semua piece sudah terkumpul
    }
    
    private void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        
        GameObject audioObject = new GameObject("TempAudio");
        AudioSource audioSource = audioObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = sfxVolume;
        audioSource.spatialBlend = 0f;
        audioSource.Play();
        Destroy(audioObject, clip.length + 0.1f);
    }
}