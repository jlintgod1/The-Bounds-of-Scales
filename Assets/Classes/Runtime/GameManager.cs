using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Audio;
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
    public LocalAudioManager MusicManager { get; private set; }
    public ScrollingSnake Snake { get; private set; }
    public GameObject SnakeTemplate;
    private Vector3 SnakeInitialPosition = new Vector3(-999, -999, -999);
    public PlayerController_Logic Player { get; private set; }
    public GameObject PlayerTemplate;
    private Vector3 PlayerInitialPosition = new Vector3(-999, -999, -999);
    public SpriteRenderer LevelBackground;
    public GameplayUIInterface UI;
    public ShopInterface UI_Shop;
    public MainMenu UI_MainMenu;
    public List<Material> FontMaterials;
    public List<GameObject> HeadstartPortalTemplate;
    public Vector2 HeadstartPortalLocation;
    public GameObject CircleMinigameTemplate;
    public GameObject CircleMinigameCollectibleTemplate;

    public Grid TilemapGrid;
    public TileBase[] DefaultLevelTiles = new Tile[10];
    public LevelTheme CurrentLevelTheme;
    [Tooltip("Element 0 should be the default level theme, and the last element should be the Core.")]
    public List<LevelTheme> AllLevelThemes;
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
    public int LevelsPlayed { get; private set; }
    private int HeadstartCounter = -1;
    private bool PlayedALevel = false;

    public DialogueManager.DialogueInfo IntroDialogue;
    public DialogueManager.DialogueInfo UpgradeTutorialDialogue;
    public List<DialogueManager.DialogueInfo> StartSessionDialogue;
    public List<DialogueManager.DialogueInfo> PostPlayerDeathDialogue;
    public DialogueManager.DialogueInfo BuyFirePanelDialogue;
    public DialogueManager.DialogueInfo BuyCoreKeyDialogue;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    public static extern void InitializeJavascript();
#endif

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

            if (ID == "FirePanels")
                DialogueManager.Instance.RunDialogue(BuyFirePanelDialogue);
            else if (ID == "CoreKey")
                DialogueManager.Instance.RunDialogue(BuyCoreKeyDialogue);

            return true;
        }

        return false;
    }

    void ProcessGameplayChunk(GameObject chunk)
    {
        Tilemap chunkTilemap = chunk.GetComponent<Tilemap>();
        if (chunkTilemap == null) return;

        for (int i = 0; i < AllLevelThemes[0].tiles.Length; i++)
        {
            if (AllLevelThemes[0].tiles[i] == CurrentLevelTheme.tiles[i]) continue;
            chunkTilemap.SwapTile(AllLevelThemes[0].tiles[i], CurrentLevelTheme.tiles[i]);
        }
        TilemapRenderer tilemapRenderer = chunk.GetComponent<TilemapRenderer>();
        if (tilemapRenderer != null)
            tilemapRenderer.material = CurrentLevelTheme.TileMaterial;

        int DifficultyImportance = 0;
        foreach (var item in FindObjectsOfType<DynamicEnemySpawner>())
        {
            if (!item.gameObject.transform.IsChildOf(chunk.transform)) continue;
            item.DifficultyImportance = DifficultyImportance;
            DifficultyImportance++;
        }

        GameObject[] spikeTilemapGameObjects = GameObject.FindGameObjectsWithTag("Spikes");
        foreach (var item in spikeTilemapGameObjects)
        {
            if (item.GetComponent<Tilemap>() != null)
                item.SetActive(GetUpgradeCount("FirePanels") > 0 && UnityEngine.Random.value <= GlobalDifficulty / 1.5f);
        }
    }

    void GenerateNewLevel(LevelTheme theme)
    {
        foreach (var item in SpawnedLevelChunks)
        {
            Destroy(item);
        }
        SpawnedLevelChunks.Clear();

        UnityEngine.Random.InitState((int)DateTime.Now.Ticks + VenomCount + EnemiesKilled);

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

        LevelExit[] levelExits = FindObjectsOfType<LevelExit>();
        foreach (var item in levelExits)
        {
            if (item == null || item.ExitIndex < 0 || item.ExitIndex >= theme.ExitLevels.Count ) continue;
            LevelTheme chosenLevelTheme = theme.ExitLevels[item.ExitIndex];
            if (chosenLevelTheme == AllLevelThemes[AllLevelThemes.Count - 1] && GetUpgradeCount("CoreKey") <= 3)
                chosenLevelTheme = AllLevelThemes[UnityEngine.Random.Range(0, AllLevelThemes.Count - 1)];
            item.UpdateLevelExit(chosenLevelTheme);
        }

        FallingPlatform[] fallingPlatforms = FindObjectsOfType<FallingPlatform>();
        foreach (var item in fallingPlatforms)
        {
            item.GetComponent<SpriteRenderer>().sprite = theme.FallingPlatformSprite;
        }

        if (GetUpgradeCount("CircleMinigame") > 0 && LevelsPlayed % 2 == 0)
        {
            GameObject[] minigameSpawnPoints = GameObject.FindGameObjectsWithTag("MinigameSpawnPoint");
            if (minigameSpawnPoints.Length > 0)
            {
                GameObject minigameCollectible = Instantiate(CircleMinigameCollectibleTemplate, minigameSpawnPoints[UnityEngine.Random.Range(0, minigameSpawnPoints.Length)].transform.position, Quaternion.identity);
                SpawnedLevelChunks.Add(minigameCollectible);
            }
        }

        LevelBackground.material = theme.BackgroundMaterial;

        LevelsPlayed++;
    }

    public VisualEffect SpawnParticleSystem(VisualEffectAsset template, Vector3 position, Transform parent = null)
    {
        GameObject newObject = new(template.name + "_Particles");
        newObject.transform.position = position;
        newObject.transform.parent = parent;
        VisualEffect visualEffect = newObject.AddComponent<VisualEffect>();

        if (visualEffect != null)
        {
            visualEffect.visualEffectAsset = template;
            visualEffect.Play();
            return visualEffect;
        }

        Debug.LogWarning("SpawnParticleSystem: Visual Effect component failed to be added!");
        return null;
    }

    // Copied and modified from AudioSource.PlayClipAtPoint()
    public static void PlaySoundAtPoint(Sound sound, Vector3 position, float volumeMultiplier=1f, float pitchMultiplier=1f, bool Manual=false)
    {
        GameObject gameObject = new GameObject(sound.name + "OneShot");
        gameObject.transform.position = position;

        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor || sound.oggClip == null)
            audioSource.clip = sound.clip;
        else
            audioSource.clip = sound.oggClip;
        audioSource.volume = sound.volume * volumeMultiplier;
        audioSource.pitch = sound.pitch * pitchMultiplier;
        //audioSource.loop = sound.loop;
        audioSource.spatialBlend = sound.spatialAmount;
        audioSource.outputAudioMixerGroup = sound.audioMixerGroup;

        if (!Manual)
        {
            audioSource.Play();
            Destroy(gameObject, sound.clip.length * ((Time.timeScale < 0.01f) ? 0.01f : Time.timeScale));
        }
    }

    public void SetGameState(int NewState)
    {
        GameState = NewState;

        Time.timeScale = NewState == 2 ? 0.005f : NewState;
    }

    private void ResetGameObjects(bool Respawn = false)
    {
        if (Respawn)
        {
            Destroy(Snake.gameObject);
            Destroy(Player.gameObject);

            GameObject newSnake = Instantiate(SnakeTemplate, SnakeInitialPosition, Quaternion.identity);
            Snake = newSnake.GetComponent<ScrollingSnake>();
            GameObject newPlayer = Instantiate(PlayerTemplate, PlayerInitialPosition, Quaternion.identity);
            Player = newPlayer.GetComponent<PlayerController_Logic>();
        }
        else
        {
            if (Snake == null)
                Snake = GameObject.FindGameObjectWithTag("Snake").GetComponent<ScrollingSnake>();

            Player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController_Logic>();
        }

        //UI_Shop.UpdateShop();

        foreach (var item in GameObject.FindGameObjectsWithTag("Snake"))
        {
            if (item.GetComponent<ScrollingSnake>() != null && item.GetComponent<ScrollingSnake>() != Snake)
                Destroy(item);
        }

        if (SnakeInitialPosition != new Vector3(-999, -999, -999))
        {
            Snake.transform.position = SnakeInitialPosition;
            Snake.transform.localScale = new Vector3(1, 1, 1);
            Snake.EstimatedTimeToRise = (Snake.GetComponent<SpriteRenderer>().size.x - 24) / Snake.Speed;
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

        for (float i = 1; i >= 0; i-=Time.unscaledDeltaTime * 2) 
        {
            Time.timeScale = i;
            MusicManager.ActiveSounds[0].audioSource.pitch = Mathf.Lerp(0.5f, MusicManager.ActiveSounds[0].sound.pitch, i);
            yield return null;
        }
        SetGameState(0);

        CircleMinigame minigame = Instantiate(CircleMinigameTemplate, position, Quaternion.identity).GetComponent<CircleMinigame>();
        if (minigame != null)
        {
            Vector3 OldTargetPosition = TargetCameraPosition;
            TargetCameraPosition = new(position.x, position.y, TargetCameraPosition.z);
            TargetCameraScale = new(0.75f, 0.75f);
            Camera.main.transform.position = TargetCameraPosition;
            Camera.main.GetComponent<PixelPerfectCamera>().assetsPPU = Mathf.FloorToInt(16 / TargetCameraScale.x);
            yield return new WaitForSecondsRealtime(0.5f);

            UI.Timer.animator.SetBool("Active", true);

            yield return new WaitForSecondsRealtime(0.5f);
            SetGameState(0);

            float timeLimit = (GetUpgradeCount("BetterBounceRings") > 0 ? 8 : 5);
            for (float i = timeLimit; i >= 0; i -= 0.01f)
            {
                UI.Timer.value = (i / timeLimit) * 96;
                yield return new WaitForSecondsRealtime(0.01f);
            }

            minigame.OnFinishGame();
            TargetCameraPosition = OldTargetPosition;
            TargetCameraScale = new(TargetCameraScale.x, 1f);
            Camera.main.transform.position = TargetCameraPosition;
            Camera.main.GetComponent<PixelPerfectCamera>().assetsPPU = Mathf.FloorToInt(16 / TargetCameraScale.x);
            UI.Timer.animator.SetBool("Active", false);
            yield return new WaitForSecondsRealtime(1.5f);
            MusicManager.ActiveSounds[0].audioSource.pitch = MusicManager.ActiveSounds[0].sound.pitch;
            SetGameState(1);
            Player.InitiateFreeRise(Mathf.Min(3 * minigame.CurrentLevel, CurrentLevelTheme.LevelLength * 24 - Player.transform.position.y - 4), 1);
            while (Player.InFreeRise)
            {
                TargetCameraPosition = new(TargetCameraPosition.x, Player.transform.position.y, TargetCameraPosition.z);
                Camera.main.transform.position = TargetCameraPosition;
                yield return null;
            }
            TargetCameraPosition = new(TargetCameraPosition.x, Mathf.Floor(TargetCameraPosition.y / 4) * 4, TargetCameraPosition.z);
            Snake.transform.position = TargetCameraPosition + new Vector3(-12 * Snake.transform.localScale.x, -4.25f, 10.5f);
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
            UI.LifestealEnemiesRemaining.transform.parent.gameObject.SetActive(false);

            MusicManager.FadeSound(MusicManager.ActiveSounds[0], 0.001f, 0);

            SaveManager.SaveSaveFile();

            StartCoroutine(PostPlayerDeath());
        }
        else if (controller.gameObject.CompareTag("Enemy"))
        {
            EnemiesKilled++;
            int venomUpgrade = GetUpgradeCount("EnemySnakeVenom");
            if (venomUpgrade > 0 && EnemiesKilled % (4 - venomUpgrade) == 0)
            {
                VenomCount += GetUpgradeCount("DoubleVenom") + 1;
            }

            int lifestealUpgrade = GetUpgradeCount("Lifesteal");
            if (lifestealUpgrade > 0)
            {
                if (EnemiesKilled % (22 - lifestealUpgrade * 2) == 0)
                {
                    int extraHealth = (UnityEngine.Random.value <= 0.05f * (lifestealUpgrade - 1)) ? 1 : 0;
                    Player.TakeDamage(gameObject, -1 - extraHealth);
                }
                UI.LifestealEnemiesRemaining.text = ((22 - lifestealUpgrade * 2) - EnemiesKilled % (22 - lifestealUpgrade * 2)).ToString();
            }
        }
    }

    public IEnumerator PostPlayerDeath()
    {
        yield return new WaitForSecondsRealtime(2);
        UI.TriggerCircleFade(false, 1.5f, Color.black, Camera.main.WorldToViewportPoint(Player.transform.position));
        yield return new WaitForSecondsRealtime(2);
        
        StartGame(true);
        UI.TriggerCircleFade(true, 0.5f, Color.black, new(0.5f, 0.5f));
    }

    public void StartGame(bool FromPlayerDeath=false)
    {
        ResetGameObjects(FromPlayerDeath);
        GlobalDifficulty = 0;
        GenerateNewLevel(AllLevelThemes[0]);
        SetGameState(0);
        UI_MainMenu.gameObject.SetActive(false);
        Camera.main.GetComponent<Animator>().enabled = true;

        Player.GetComponent<Animator>().updateMode = AnimatorUpdateMode.UnscaledTime;
        Player.NoAnimation = true;
        Player.SetExpression(PlayerController_Logic.PlayerExpression.Sleeping, 9999);

        UI.LifestealEnemiesRemaining.transform.parent.gameObject.SetActive(false);

        if (SaveManager.IsTutorialComplete("Intro"))
        {
            Camera.main.GetComponent<Animator>().Play("Camera_UpgradeZoomOut");
            if (FromPlayerDeath)
                Camera.main.GetComponent<Animator>().SetBool("ForceUpgradeZoomOut", true);
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
        MusicManager.CrossFadeSound(MusicManager.ActiveSounds[0], AllLevelThemes[0].Music, 0.5f);

        foreach (var item in UI.PanelAnimators)
        {
            item.SetBool("Active", true);
        }

        Player.UpdateUpgrades();
        Snake.UpdatePanelTier();

        Player.animator.updateMode = AnimatorUpdateMode.Normal;
        Player.NoAnimation = false;

        UI.LifestealEnemiesRemaining.transform.parent.gameObject.SetActive(GetUpgradeCount("Lifesteal") > 0);

        if (HeadstartCounter < 0)
            HeadstartCounter = UnityEngine.Random.Range(0, 5);
        if (PlayedALevel)
            HeadstartCounter += 1 + EnemiesKilled % 2;

        if (GetUpgradeCount("LevelHeadstart") > 0)
        {
            GameObject portal = Instantiate(HeadstartPortalTemplate[GetUpgradeCount("BetterLevel_Headstart")], HeadstartPortalLocation, Quaternion.identity);

            if (GetUpgradeCount("BetterLevel_Headstart") > 0)
            {
                portal.GetComponent<LevelExit>().UpdateLevelExit(AllLevelThemes[1 + (HeadstartCounter % (AllLevelThemes.Count - 2))]);
                portal.GetComponent<LevelExit>().AdditionalDifficulty = 0.2f;
            }
            else
            {
                portal.GetComponent<LevelExit>().UpdateLevelExit(AllLevelThemes[0].ExitLevels[HeadstartCounter % AllLevelThemes[0].ExitLevels.Count]);
                portal.GetComponent<LevelExit>().AdditionalDifficulty = 0.1f;
            }

            SpawnedLevelChunks.Add(portal);
        }

        PlayedALevel = false;

        SaveManager.SaveSaveFile();
    }

    public void OnEnterLevelExit(LevelExit level)
    {
        SetGameState(0);
        UI.TriggerCircleFade(false, 1f, Color.white, Camera.main.WorldToViewportPoint(Player.transform.position));
        MusicManager.CrossFadeSound(MusicManager.ActiveSounds[0], level.LevelTheme.Music, 1);
        SaveManager.AddTutorial(level.LevelTheme.name);
        SaveManager.SaveSaveFile();
        StartCoroutine(PostLevelExit(level));
    }

    public IEnumerator PostLevelExit(LevelExit level)
    {
        yield return new WaitForSecondsRealtime(1.01f);
        // +3.3% difficulty if the player took no damage, +2.5% difficulty if the core is available
        GlobalDifficulty += 0.125f + (Player.Health >= Player.MaxHealth ? 0.033f : 0) + (GetUpgradeCount("CoreKey") * 0.025f) + level.AdditionalDifficulty;
        PlayedALevel = true;
        GenerateNewLevel(level.LevelTheme);
        ResetGameObjects();
        yield return new WaitForSecondsRealtime(0.5f);
        UI.TriggerCircleFade(true, 1f, Color.white, Camera.main.WorldToViewportPoint(Player.transform.position));
        yield return new WaitForSecondsRealtime(0.5f);
        SetGameState(1);
    }

    public void OnRecieveAnimationEvent(string Message)
    {
        switch(Message)
        {
            case "Camera_PostGameIntro":
                Camera.main.GetComponent<Animator>().enabled = false;
                SetGameState(1);

                Player.animator.updateMode = AnimatorUpdateMode.Normal;
                Player.NoAnimation = false;

                SaveManager.AddTutorial("Intro");

                foreach (var item in UI.PanelAnimators)
                {
                    item.SetBool("Active", true);
                }
                break;

            case "Camera_PostUpgradeZoomOut":
                UI_Shop.gameObject.SetActive(true);
                UI_Shop.UpdateShop();

                Camera.main.GetComponent<Animator>().SetBool("ForceUpgradeZoomOut", false);
                Camera.main.GetComponent<Animator>().enabled = false;

                TargetCameraPosition = new(2, 0.25f, Camera.main.gameObject.transform.position.z);
                Camera.main.transform.position = TargetCameraPosition;
                TargetCameraScale = new(0.333f, 0.333f);
                Camera.main.GetComponent<PixelPerfectCamera>().assetsPPU = Mathf.FloorToInt(16 / TargetCameraScale.x);

                UI.PanelAnimators[1].SetBool("Active", true);
                break;

            case "Camera_ActivateDialogue":
                if (SaveManager.IsTutorialComplete("Intro"))
                {
                    if (!SaveManager.IsTutorialComplete("Upgrades")) // Upgrade shop tutorial
                    {
                        DialogueManager.Instance.RunDialogue(UpgradeTutorialDialogue);
                        SaveManager.AddTutorial("Upgrades");
                    }
                    else if (HeadstartCounter < 0) // First time playing in this session
                    {
                        DialogueManager.Instance.RunDialogue(StartSessionDialogue[UnityEngine.Random.Range(0, StartSessionDialogue.Count)]);
                    }
                    else // After player death
                    {
                        DialogueManager.Instance.RunDialogue(PostPlayerDeathDialogue[UnityEngine.Random.Range(0, PostPlayerDeathDialogue.Count)]);
                    }
                    MusicManager.PlaySound(UI_Shop.ShopMusic);
                }
                else
                {
                    DialogueManager.Instance.RunDialogue(IntroDialogue);
                    MusicManager.PlaySound(AllLevelThemes[0].Music);
                }
                
                break;

            case "PlayerExpression_Normal":
                Player.SetExpression(PlayerController_Logic.PlayerExpression.Normal, 1);
                break;

            case "PlayerExpression_Surprised":
                Player.SetExpression(PlayerController_Logic.PlayerExpression.Surprised, 9999);
                break;

            default:
                break;
            
        }
    }

    public void OnApplicationQuit()
    {
        SaveManager.saveData.TimeSpent += Mathf.FloorToInt(Time.unscaledTime);
        VenomCount++; // Free venom for quitting the game :)
        SaveManager.SaveSaveFile();
    }

    // Awake is called when the script instance is being loaded
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        MusicManager = gameObject.AddComponent<LocalAudioManager>();

        ResetGameObjects();

        SaveData.SaveLoadState saveLoadState = SaveManager.LoadSaveFile();
        if (UI != null)
            UI.VenomCounter.text = VenomCount.ToString();

#if UNITY_WEBGL && !UNITY_EDITOR
        UnityEngine.Cursor.SetCursor(null, new(0, 0), CursorMode.Auto);
        InitializeJavascript();
#endif
        //UnityEngine.Cursor.SetCursor()
        //GenerateNewLevel(CurrentLevelTheme);
        SetGameState(0);
    }

    // Update is called once per frame
    void Update()
    {
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        foreach (var item in FontMaterials)
        {
            item.SetFloat("_RealTime", Time.unscaledTime);

        }

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
            Camera.main.transform.position = Vector3.SmoothDamp(Camera.main.transform.position, TargetCameraPosition, ref CurrentVelocity, 0.15f);
            LevelBackground.transform.position = Camera.main.transform.position + new Vector3(0, 0, 20);

            TargetCameraScale = new(Mathf.SmoothDamp(TargetCameraScale.x, TargetCameraScale.y, ref CurrentVelocityF, 0.125f), TargetCameraScale.y);
            Camera.main.GetComponent<PixelPerfectCamera>().assetsPPU = Mathf.FloorToInt(16 / TargetCameraScale.x);
        }
    }
    void FixedUpdate()
    {
        UpdateCamera();
    }
}
