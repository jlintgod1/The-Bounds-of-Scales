using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{
    // Consts for the screen dimensions,
    public static Vector2 CONST_ScreenDimensions = new(384, 216);

    static public GameManager Instance { get; private set; }
    public ScrollingSnake Snake { get; private set; }
    private Vector3 SnakeInitialPosition = new Vector3(-999, -999, -999);
    public PlayerController_Logic Player { get; private set; }
    private Vector3 PlayerInitialPosition = new Vector3(-999, -999, -999);
    public Camera Camera { get; private set; }
    public GameplayUIInterface UI;
    public ShopInterface UI_Shop;
    public GameObject UI_MainMenu;
    public Animator ScreenTransition;

    public Grid TilemapGrid;
    public TileBase[] DefaultLevelTiles = new Tile[10];
    public LevelTheme CurrentLevelTheme;
    public LevelTheme DefaultLevelTheme;
    public List<GameObject> SpawnedLevelChunks { get; protected set; } = new List<GameObject>();

    [Serializable]
    public struct UpgradeItem
    {
        public string ID;
        public string Name;
        [TextAreaAttribute]
        public string Description;
        public Sprite Icon;

        public int Cost;
        public string[] Prerequisites;
        [Range(0, 3)]
        public int Rarity; // 0=Common, 1=Uncommon, 2=Rare, 3=Legendary

        public int MaxBuyCount; // Used for upgrades that can be bought more than once(HP upgrades, extra lives, etc.)
        [Min(1)]
        public float CostMultiplier; // As more of this upgrade is bought, multiply the cost by Cost Multiplier^(Times Bought)
    }

    public List<UpgradeItem> UpgradeDatabase;
    public List<string> OwnedUpgrades;
    private int _VenomCount;
    public int VenomCount 
    {   get
        {
            return _VenomCount;
        }
        private set
        {
            if (UI != null)
                UI.VenomCounter.text = value.ToString();
            _VenomCount = value;
        }
    }

    Vector3 TargetCameraPosition = new(0, 0, -10);
    Vector3 TargetCameraScale = new(1, 1, 1);
    int SnakeRiseCount;
    public float GlobalDifficulty { get; private set; } // Starts at 0, >1 after 1.5 level loops
    public int GameState { get; private set; } // 0 = Menu, 1 = Playing
    public float CurrentDodgeChance = 0;

    public int GetUpgradeCount(string ID)
    {
        int count = 0;
        foreach (var item in OwnedUpgrades)
        {
            if (item.Contains(ID))
                count++;
        }
        return count;
    }

    public bool BuyUpgrade(string ID)
    {
        if (String.IsNullOrEmpty(ID)) return false;
        int upgradeIndex;
        for (upgradeIndex = 0; upgradeIndex < UpgradeDatabase.Count; upgradeIndex++)
        {
            if (UpgradeDatabase[upgradeIndex].ID != ID) continue;

            int upgradeCount = GetUpgradeCount(ID);
            if (upgradeCount >= UpgradeDatabase[upgradeIndex].MaxBuyCount) return false; // We can't get any more
            int upgradeCost = Mathf.FloorToInt(UpgradeDatabase[upgradeIndex].Cost * Mathf.Pow(UpgradeDatabase[upgradeIndex].CostMultiplier, upgradeCount));
            if (upgradeCost > VenomCount) return false; // No moneys :(

            OwnedUpgrades.Add(ID + (upgradeCount > 0 ? upgradeCount.ToString() : ""));
            VenomCount -= upgradeCost;
            return true;
        }

        return false;
    }

    void ProcessGameplayChunk(GameObject chunk)
    {
        Tilemap chunkTilemap = chunk.GetComponent<Tilemap>();
        if (chunkTilemap == null) return;

        for (int i = 0; i < DefaultLevelTiles.Length; i++)
        {
            if (DefaultLevelTiles[i] == CurrentLevelTheme.tiles[i]) continue;
            chunkTilemap.SwapTile(DefaultLevelTiles[i], CurrentLevelTheme.tiles[i]);
        }
    }

    void GenerateNewLevel(LevelTheme theme)
    {
        foreach (var item in SpawnedLevelChunks)
        {
            Destroy(item);
        }
        SpawnedLevelChunks.Clear();

        CurrentLevelTheme = theme;
        List<GameObject> shuffledChunkList = new List<GameObject>();

        for (int i = 0; i < Mathf.CeilToInt(theme.LevelLength / (float)theme.GameplayChunks.Count); i++)
        {
            foreach (var item in theme.GameplayChunks)
            {
                shuffledChunkList.Insert(UnityEngine.Random.Range(i * (theme.GameplayChunks.Count + 1), shuffledChunkList.Count), item);
            }
            shuffledChunkList.Insert(UnityEngine.Random.Range(0, shuffledChunkList.Count), theme.HardGameplayChunks[UnityEngine.Random.Range(0, theme.HardGameplayChunks.Count)]);
        }

        for (int i = 0; i < theme.LevelLength + 2; i++)
        {
            GameObject chunk;
            if (i == 0)
                chunk = theme.EntranceChunk;
            else if (i == theme.LevelLength + 1)
                chunk = theme.ExitChunk;
            else
                chunk = shuffledChunkList[i];

            chunk = Instantiate(chunk, new Vector3(0, i * 24, TilemapGrid.transform.position.z), Quaternion.identity, TilemapGrid.transform);
            ProcessGameplayChunk(chunk);
            SpawnedLevelChunks.Add(chunk);
        }

        LevelExit[] levelExits = GameObject.FindObjectsOfType<LevelExit>();
        foreach (var item in levelExits)
        {
            item.UpdateLevelExit(theme.ExitLevels[item.ExitIndex]);
        }
    }

    public void SetGameState(int NewState)
    {
        GameState = NewState;

        Time.timeScale = NewState;
    }

    private void ResetGameObjects()
    {
        if (Snake == null)
            Snake = GameObject.FindGameObjectWithTag("Snake").GetComponent<ScrollingSnake>();
        
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController_Logic>();
        
        Camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        //UI_Shop.UpdateShop();

        if (SnakeInitialPosition != new Vector3(-999, -999, -999))
        {
            Snake.transform.position = SnakeInitialPosition;
            Snake.transform.localScale = new Vector3(1, 1, 1);
            Snake.EstimatedTimeToRise = (Snake._spriteRenderer.size.x - 24) / Snake.Speed;
        }
        if (PlayerInitialPosition != new Vector3(-999, -999, -999))
        {
            Player.transform.position = PlayerInitialPosition;
            Player.TakeDamage(gameObject, -99);
        }
        SnakeInitialPosition = Snake.transform.position;
        PlayerInitialPosition = Player.transform.position;
        TargetCameraPosition = new(0, 0, -10);
        Camera.transform.position = TargetCameraPosition;
    }

    public void OnCollectCollectible(MonoBehaviour Collectible)
    {
        if (Collectible as SnakeVenom != null)
        {
            VenomCount += GetUpgradeCount("DoubleVenom") + 1;
        }
    }

    public void OnSnakeRise()
    {
        SnakeRiseCount++;

        if (Snake.CanSnakeRise())
        {
            TargetCameraPosition.y = Mathf.FloorToInt(TargetCameraPosition.y) + 4;
        }
    }

    public void StartGame(bool FromPlayerDeath=false)
    {
        GenerateNewLevel(DefaultLevelTheme);
        SetGameState(0);
        UI_MainMenu.SetActive(false);
        Camera.GetComponent<Animator>().enabled = true;

        if (SaveManager.IsTutorialComplete("Intro"))
        {
            Camera.GetComponent<Animator>().Play("Camera_UpgradeZoomOut");
        }
        else
        {
            Camera.GetComponent<Animator>().Play("Camera_IntroZoomOut");
        }
    }

    public void OnFinishUpgradeShop()
    {
        UI_Shop.gameObject.SetActive(false);
        SetGameState(1);
        Camera.transform.localScale = new(1, 1, 1);
        TargetCameraPosition = new(0, 0, -10);

        Player.UpdateUpgrades();

        SaveManager.saveData.OwnedUpgrades = OwnedUpgrades;
        SaveManager.saveData.SnakeVenomCount = VenomCount;
        SaveManager.SaveSaveFile();
    }

    // Whoops, I died!
    public void OnControllerDeath(Controller controller, GameObject instigator)
    {
        if (controller as PlayerController_Logic != null)
        {
            Time.timeScale = 0.1f;

            TargetCameraPosition = Player.gameObject.transform.position
                + new Vector3(Player.GetComponent<Rigidbody2D>().velocity.x / 16f, Player.GetComponent<Rigidbody2D>().velocity.y / 16f, Camera.transform.position.z);
            Camera.transform.position = TargetCameraPosition;

            TargetCameraScale = new(0.5f, 0.5f, 1);
            Camera.transform.localScale = new(0.5f, 0.5f, 1);

            SaveManager.saveData.SnakeVenomCount = VenomCount;
            SaveManager.SaveSaveFile();

            StartCoroutine(PostPlayerDeath());
        }
        else if (controller.gameObject.CompareTag("Enemy"))
        {
            VenomCount += GetUpgradeCount("EnemySnakeVenom") * (GetUpgradeCount("DoubleVenom") + 1);
        }
    }

    public IEnumerator PostPlayerDeath()
    {
        yield return new WaitForSecondsRealtime(2);
        ScreenTransition.speed = 0.6667f;
        ScreenTransition.SetBool("FadeOut", true);
        yield return new WaitForSecondsRealtime(2);
        ScreenTransition.speed = 1f;
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("MainGame");
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        ResetGameObjects();
        StartGame(true);
        ScreenTransition.SetBool("FadeOut", false);
    }

    public void OnEnterLevelExit(LevelTheme level)
    {
        SetGameState(0);
        ScreenTransition.SetBool("FadeOut", true);
        SaveManager.saveData.SnakeVenomCount = VenomCount;
        SaveManager.SaveSaveFile();
        StartCoroutine(PostLevelExit(level));
    }

    public IEnumerator PostLevelExit(LevelTheme level)
    {
        yield return new WaitForSecondsRealtime(1.5f);
        GenerateNewLevel(level);
        ResetGameObjects();
        ScreenTransition.SetBool("FadeOut", false);
        yield return new WaitForSecondsRealtime(1f);
        SetGameState(1);
    }



    public void OnRecieveAnimationEvent(string Message)
    {
        if (Message == "Camera_PostGameIntro")
        {
            Camera.GetComponent<Animator>().enabled = false;
            SetGameState(1);
            SaveManager.AddTutorial("Intro");
        }
        else if (Message == "Camera_PostUpgradeZoomOut")
        {
            UI_Shop.gameObject.SetActive(true);
            UI_Shop.UpdateShop();

            Camera.GetComponent<Animator>().enabled = false;

            TargetCameraPosition = new(2, 0.25f, Camera.gameObject.transform.position.z);
            Camera.transform.position = TargetCameraPosition;
            Camera.transform.localScale = new(0.33f, 0.33f, 1);
            
        }
    }

    // Awake is called when the script instance is being loaded
    void Start()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        ResetGameObjects();

        SaveData.SaveLoadState saveLoadState = SaveManager.LoadSaveFile();
        if (saveLoadState == SaveData.SaveLoadState.Success) 
        {
            OwnedUpgrades = SaveManager.saveData.OwnedUpgrades;
            VenomCount = SaveManager.saveData.SnakeVenomCount;
        }

        //GenerateNewLevel(CurrentLevelTheme);
        SetGameState(0);
    }

    // Update is called once per frame
    void Update()
    {
        if (GameState == 0) return;
        if (Snake != null && Snake.CanSnakeRise())
        {
            TargetCameraPosition.y += 1 / Snake.EstimatedTimeToRise * Time.deltaTime * 0.375f;
        }
    }

    void FixedUpdate()
    {
        Vector3 CurrentVelocity = new Vector3(0,0,0);
        if (Camera != null) 
        {
            Camera.transform.position = Vector3.SmoothDamp(Camera.transform.position, TargetCameraPosition, ref CurrentVelocity, 0.15f);
        }
    }
}
