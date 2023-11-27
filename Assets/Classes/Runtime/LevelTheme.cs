using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "LevelTheme", menuName = "ScriptableObjects/LevelTheme", order = 1)]
public class LevelTheme : ScriptableObject
{
    public string LevelName;
    public Material BackgroundMaterial;
    public Sound Music;

    [Tooltip("Potential sections that we want the level to be made of.")]
    public List<GameObject> GameplayChunks;
    [Tooltip("Potential difficult sections that we want to have at least 1 of.")]
    public List<GameObject> HardGameplayChunks;
    [Tooltip("Enemies that should be spawned by theme dependent DynamicEnemySpawners. Important for reusing previous gameplay chunks.")]
    public List<GameObject> ThemeEnemies;
    [Tooltip("Where the player starts the level.")]
    public GameObject EntranceChunk;
    [Tooltip("Where the play exits to the next level.")]
    public GameObject ExitChunk;
    [Tooltip("How long the level should be in chunks, excluding the entrance and exit chunks.")]
    public int LevelLength;
    [Tooltip("Level choices in the exit chunk.")]
    public List<LevelTheme> ExitLevels;

    [Tooltip("0=Rule Tile, 1-9=Static tiles for the sides")]
    public TileBase[] tiles = new TileBase[10];
    [Tooltip("Material used for the tiles.")]
    public Material TileMaterial;
    public Sprite FallingPlatformSprite;
}
