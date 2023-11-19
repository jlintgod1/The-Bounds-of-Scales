using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using UnityEngine.VFX;

public class GameManager : MonoBehaviour
{
    // Consts for the screen dimensions,
    public static Vector2 CONST_ScreenDimensions = new(384, 216);

    static public GameManager Instance { get; private set; }
    public ScrollingSnake Snake { get; private set; }
    private Vector3 SnakeInitialPosition = new Vector3(-999, -999, -999);
    public PlayerController_Logic Player { get; private set; }
    private Vector3 PlayerInitialPosition = new Vector3(-999, -999, -999);
    public SpriteRenderer LevelBackground;
    public GameplayUIInterface UI;
    public ShopInterface UI_Shop;
    public GameObject UI_MainMenu;
    public Animator ScreenTransition;
    public GameObject VisualEffectTemplate;
    public GameObject QuickstartPortalTemplate;
    public Vector2 QuickstartPortalLocation;
    public GameObject CircleMinigameTemplate;
    public GameObject CircleMinigameCollectibleTemplate;

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
    public int VenomCount 
    {   get
        {
            return SaveManager.saveData.SnakeVenomCount;
        }
        private set
        {
            if (UI != null)
                UI.VenomCounter.text = value.ToString();
            SaveManager.saveData.SnakeVenomCount = value;
        }
    }

    Vector3 TargetCameraPosition = new(0, 0, -10);
    Vector2 TargetCameraScale = new(1, 1); // X=Current, Y=Target
    int SnakeRiseCount;
    public float GlobalDifficulty { get; private set; } // Starts at 0, >1 after 1.5 level loops
    public int GameState { get; private set; } // 0 = Menu, 1 = Playing, 2 = Circle Minigame
    public float CurrentDodgeChance = 0;
    public int EnemiesKilled { get; private set; }

    public DialogueManager.DialogueInfo IntroDialogue;

    public int GetUpgradeCount(string ID)
    {
        int count = 0;
        foreach (var item in SaveManager.saveData.OwnedUpgrades)
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

            SaveManager.saveData.OwnedUpgrades.Add(ID + (upgradeCount > 0 ? upgradeCount.ToString() : ""));
            VenomCount -= upgradeCost;
            return true;
        }

        return false;
    }

    void ProcessGameplayChunk(GameObject chunk)
    {
        Tilemap chunkTilemap = chunk.GetComponent<Tilemap>();
        if (chunkTilemap == null) return;

        for (int i = 0; i < DefaultLevelTheme.tiles.Length; i++)
        {
            if (DefaultLevelTheme.tiles[i] == CurrentLevelTheme.tiles[i]) continue;
            chunkTilemap.SwapTile(DefaultLevelTheme.tiles[i], CurrentLevelTheme.tiles[i]);
        }
        TilemapRenderer tilemapRenderer = chunk.GetComponent<TilemapRenderer>();
        if (tilemapRenderer != null)
            tilemapRenderer.material = CurrentLevelTheme.TileMaterial;
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
            if (item.ExitIndex < 0) continue;
            item.UpdateLevelExit(theme.ExitLevels[item.ExitIndex]);
        }

        if (GetUpgradeCount("CircleMinigame") > 0)
        {
            GameObject[] minigameSpawnPoints = GameObject.FindGameObjectsWithTag("MinigameSpawnPoint");
            if (minigameSpawnPoints.Length > 0)
                Instantiate(CircleMinigameCollectibleTemplate, minigameSpawnPoints[UnityEngine.Random.Range(0, minigameSpawnPoints.Length)].transform.position, Quaternion.identity);
        }

        LevelBackground.material = theme.BackgroundMaterial;
    }

    public VisualEffect SpawnParticleSystem(VisualEffectAsset template, Vector3 position, Transform parent = null)
    {
        GameObject newObject = Instantiate(VisualEffectTemplate, position, Quaternion.identity, parent);
        VisualEffect visualEffect = newObject.GetComponent<VisualEffect>();

        if (visualEffect != null)
        {
            visualEffect.gameObject.name = template.name;
            visualEffect.visualEffectAsset = template;
            visualEffect.Play();
            return visualEffect;
        }

        Debug.LogWarning("SpawnParticleSystem: GameObject for holding Visual Effects doesn't contain a VisualEffect component!");
        return null;
    }

    public void SetGameState(int NewState)
    {
        GameState = NewState;

        Time.timeScale = NewState == 2 ? 0.005f : NewState;
    }

    private void ResetGameObjects()
    {
        if (Snake == null)
            Snake = GameObject.FindGameObjectWithTag("Snake").GetComponent<ScrollingSnake>();
        
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController_Logic>();
        
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
        Camera.main.transform.position = TargetCameraPosition;
        TargetCameraScale = new(1,1);
        Camera.main.GetComponent<PixelPerfectCamera>().assetsPPU = 16;
        LevelBackground.transform.position = Camera.main.transform.position + new Vector3(0, 0, 20);
    }

    IEnumerator CircleMinigameCoroutine(Vector3 position)
    {
        for (float i = 1; i >= 0; i-=0.01f) 
        {
            Time.timeScale = i;
            yield return new WaitForSecondsRealtime(0.0075f);
        }
        SetGameState(2);

        CircleMinigame minigame = Instantiate(CircleMinigameTemplate, position, Quaternion.identity).GetComponent<CircleMinigame>();
        if (minigame != null) 
        {
            yield return new WaitForSecondsRealtime(0.5f);
            Vector3 OldTargetPosition = TargetCameraPosition;
            TargetCameraPosition = new(position.x, position.y, TargetCameraPosition.z);
            TargetCameraScale = new(TargetCameraScale.x, 0.75f);
            UI.Timer.animator.SetBool("Active", true);

            yield return new WaitForSecondsRealtime(0.5f);
            SetGameState(0);

            float timeLimit = (GetUpgradeCount("BetterBounceRings") > 0 ? 10 : 7);
            for (float i = timeLimit; i >= 0; i -= 0.01f)
            {
                UI.Timer.value = i / timeLimit;
                yield return new WaitForSecondsRealtime(0.01f);
            }

            SetGameState(2);
            minigame.OnFinishGame();
            Player.InitiateFreeRise(Mathf.Min(4 * minigame.CurrentLevel, CurrentLevelTheme.LevelLength * 24 - Player.transform.position.y - 4), 1);
            TargetCameraPosition = OldTargetPosition;
            TargetCameraScale = new(TargetCameraScale.x, 1f);
            UI.Timer.animator.SetBool("Active", false);
            yield return new WaitForSecondsRealtime(1f);
            SetGameState(1);
            while (Player.InFreeRise)
            {
                TargetCameraPosition = new(TargetCameraPosition.x, Player.transform.position.y, TargetCameraPosition.z);
                Camera.main.transform.position = TargetCameraPosition;
                yield return null;
            }
            TargetCameraPosition = new(TargetCameraPosition.x, Mathf.Floor(TargetCameraPosition.y / 24) * 24, TargetCameraPosition.z);
            Snake.transform.position = TargetCameraPosition + new Vector3(-12, -4.25f, 10.5f);
        }
    }

    public void OnCollectCollectible(MonoBehaviour Collectible)
    {
        if (Collectible is CircleMinigameCollectible)
        {
            StartCoroutine(CircleMinigameCoroutine(Collectible.transform.position));
        }
        else if (Collectible is SnakeVenom)
        {
            if (Collectible.GetComponent<SnakeVenom>().GivesHealth)
                Player.TakeDamage(gameObject, -1);
            
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
        Camera.main.GetComponent<Animator>().enabled = true;

        Player.UpdateUpgrades();

        if (SaveManager.IsTutorialComplete("Intro"))
        {
            Camera.main.GetComponent<Animator>().Play("Camera_UpgradeZoomOut");
        }
        else
        {
            Camera.main.GetComponent<Animator>().Play("Camera_IntroZoomOut");
        }
    }

    public void OnFinishUpgradeShop()
    {
        UI_Shop.gameObject.SetActive(false);
        SetGameState(1);
        TargetCameraScale = new(TargetCameraScale.x, 1);
        TargetCameraPosition = new(0, 0, -10);

        foreach (var item in UI.PanelAnimators)
        {
            item.SetBool("Active", true);
        }

        Player.UpdateUpgrades();
        Snake.UpdatePanelTier();

        if (GetUpgradeCount("LevelHeadstart") > 0)
        {
            GameObject portal = Instantiate(QuickstartPortalTemplate, QuickstartPortalLocation, Quaternion.identity);
            portal.GetComponent<LevelExit>().UpdateLevelExit(DefaultLevelTheme.ExitLevels[UnityEngine.Random.Range(2, DefaultLevelTheme.ExitLevels.Count)]);
            SpawnedLevelChunks.Add(portal);
        }

        SaveManager.SaveSaveFile();
    }

    // Whoops, I died!
    public void OnControllerDeath(Controller controller, GameObject instigator)
    {
        if (controller as PlayerController_Logic != null)
        {
            Time.timeScale = 0.25f;

            TargetCameraPosition = Player.gameObject.transform.position
                + new Vector3(Player.GetComponent<Rigidbody2D>().velocity.x / 16f, Player.GetComponent<Rigidbody2D>().velocity.y / 16f, Camera.main.transform.position.z);
            Camera.main.transform.position = TargetCameraPosition;

            TargetCameraScale = new(0.8f, 0.75f);
            Camera.main.GetComponent<PixelPerfectCamera>().assetsPPU = Mathf.FloorToInt(16 / TargetCameraScale.x);

            foreach (var item in UI.PanelAnimators)
            {
                item.SetBool("Active", false);
            }

            SaveManager.SaveSaveFile();

            StartCoroutine(PostPlayerDeath());
        }
        else if (controller.gameObject.CompareTag("Enemy"))
        {
            EnemiesKilled++;
            int venomUpgrade = GetUpgradeCount("EnemySnakeVenom");
            if (venomUpgrade > 0 && EnemiesKilled % (4 - venomUpgrade) == 0)
                VenomCount ++;
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
        SaveManager.SaveSaveFile();
        StartCoroutine(PostLevelExit(level));
    }

    public IEnumerator PostLevelExit(LevelTheme level)
    {
        yield return new WaitForSecondsRealtime(1f);
        GlobalDifficulty += 0.1f + (Player.Health >= Player.MaxHealth ? 0.033f : 0);
        GenerateNewLevel(level);
        ResetGameObjects();
        yield return new WaitForSecondsRealtime(0.5f);
        ScreenTransition.SetBool("FadeOut", false);
        yield return new WaitForSecondsRealtime(1f);
        SetGameState(1);
    }

    public void OnRecieveAnimationEvent(string Message)
    {
        if (Message == "Camera_PostGameIntro")
        {
            Camera.main.GetComponent<Animator>().enabled = false;
            SetGameState(1);
            SaveManager.AddTutorial("Intro");

            foreach (var item in UI.PanelAnimators)
            {
                item.SetBool("Active", true);
            }
        }
        else if (Message == "Camera_PostUpgradeZoomOut")
        {
            UI_Shop.gameObject.SetActive(true);
            UI_Shop.UpdateShop();

            Camera.main.GetComponent<Animator>().enabled = false;

            TargetCameraPosition = new(2, 0.25f, Camera.main.gameObject.transform.position.z);
            Camera.main.transform.position = TargetCameraPosition;
            TargetCameraScale = new(0.333f, 0.333f);
            Camera.main.GetComponent<PixelPerfectCamera>().assetsPPU = Mathf.FloorToInt(16 / TargetCameraScale.x);

            UI.PanelAnimators[1].SetBool("Active", true);
        }
        else if (Message == "Camera_ActivateDialogue")
        {
            if (SaveManager.IsTutorialComplete("Intro"))
            {

            }
            else
            {
                DialogueManager.Instance.RunDialogue(IntroDialogue);
            }
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
        if (UI != null)
            UI.VenomCounter.text = VenomCount.ToString();

        //GenerateNewLevel(CurrentLevelTheme);
        SetGameState(0);
    }

    // Update is called once per frame
    void Update()
    {
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        if (GameState != 1) return;
        if (Snake != null && Snake.CanSnakeRise())
        {
            TargetCameraPosition.y += 1 / Snake.EstimatedTimeToRise * Time.deltaTime * 0.375f;
        }
    }

    void UpdateCamera()
    {
        float CurrentVelocityF = 0;
        Vector3 CurrentVelocity = new Vector3(0, 0, 0);
        if (Camera.main != null)
        {
            Camera.main.transform.position = Vector3.SmoothDamp(Camera.main.transform.position, TargetCameraPosition, ref CurrentVelocity, 0.15f, Mathf.Infinity, Time.fixedUnscaledDeltaTime);
            LevelBackground.transform.position = Camera.main.transform.position + new Vector3(0, 0, 20);

            TargetCameraScale = new(Mathf.SmoothDamp(TargetCameraScale.x, TargetCameraScale.y, ref CurrentVelocityF, 0.125f, Mathf.Infinity, Time.fixedUnscaledDeltaTime), TargetCameraScale.y);
            Camera.main.GetComponent<PixelPerfectCamera>().assetsPPU = Mathf.FloorToInt(16 / TargetCameraScale.x);
        }
    }
    void FixedUpdate()
    {
        UpdateCamera();
    }
}
