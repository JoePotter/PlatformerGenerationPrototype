using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CreativeSpore.SuperTilemapEditor;

public class LevelGenerator : MonoBehaviour
{
    #region LevelGen Variables
    [Header("References")]

    [SerializeField]
    BackgroundGenerator backgroundTilemap;
    STETilemap tileMap;
    PCG.DrunkManWalking drunkManWalking;
    RoomData room;
    public Coroutine generationCoroutine;

    //---------------------------------------------------------------------------------------------------//
    [Header("Generation Settings")]

    [Tooltip("The desired theme for the level to be generated from.")]
    public RoomDatabaseMaster.RoomTheme theme;

    [Tooltip("The tilemap brushID used to paint the tiles. Ensure the correct brush for the theme is selected, or the results may be incorrect.")]
    [SerializeField]
    int brushID;

    public static float tileSize = 2.5f;

    [Tooltip("The chance that mutable tiles will spawn/be changed on generation.")]
    [SerializeField]
    [Range(0, 1)]
    float mutateableChance = .5f;

    [Tooltip("The chance that chunk tiles/templates will spawn on generation.")]
    [SerializeField]
    [Range(0, 1)]
    float chunkTemplateChance = .33f;

    public List<ChunkTemplate> theme1_ChunkTemplates = new List<ChunkTemplate>();

    //---------------------------------------------------------------------------------------------------//
    [Header("Clutter Pass Settings:")]

    [SerializeField]
    bool spawnClutterInEditor = false;

    [SerializeField]
    [Range(0, 1)]
    float groundClutterChance = .33f;

    [SerializeField]
    [Range(0, 1)]
    float ceilingClutterChance = .33f;

    [SerializeField]
    [Range(0, 1)]
    float wallClutterChance = .33f;

    List<Vector2> clutteredTilePositions;

    //---------------------------------------------------------------------------------------------------//
    [Header("Clutter Collection Objects")]
    [SerializeField]
    ClutterCollectionTheme Clutter_Theme1;

    [SerializeField]
    ClutterCollectionTheme Clutter_Theme2;

    [SerializeField]
    ClutterCollectionTheme Clutter_Theme3;

    //Trackers
    bool roomGenerationComplete = false;
    #endregion

    #region Base Level Generation
    //Assign references attached to this object and generate the level upon initialisation
    private void Start()
    {
        drunkManWalking = GetComponent<PCG.DrunkManWalking>();
        tileMap = GetComponent<STETilemap>();
        tileSize = tileMap.CellSize.x;

        GenerateLevel();
    }

    /// <summary>
    /// Begin the level generation coroutine.
    /// </summary>
    public void GenerateLevel()
    {
        generationCoroutine = StartCoroutine(GenerateLevelEnumerator());
    }

    public IEnumerator GenerateLevelEnumerator()
    {
        //Clear and re-initialise all lists, dictionaries, and current tilemaps in order to generate a fresh level
        clutteredTilePositions = new List<Vector2>();
        DestructibleTerrainHandler.ClearAllClutterFromMap();
        DestructibleTerrainHandler.associatedClutterDictionary = new Dictionary<Vector2, GameObject>();

        tileMap.ClearMap();

        //Call for a new room layout from the DMW algorithm
        drunkManWalking.GenerateLevel();

        //Loop through each room from the DMW layout and request a room of that type from the room database master.
        foreach (KeyValuePair<Vector2, PCG.Room> entry in drunkManWalking.RoomDictionary)
        {
            //Creates a room that we return from the master class. Requires a theme, difficulty and type (open and closed points).
            room = RoomDatabaseMaster.Instance.GetRoomData(theme,
                RoomDatabaseMaster.Instance.GetRoomType(entry.Value.U, entry.Value.R, entry.Value.D, entry.Value.L), 0f);

            //With this room returned, call the construction function with the tile data supplied to create the room on the tilemap itself
            ConstructTileMapFromData(room.tileData, entry.Key);

            //If this generation is happening at runtime, update the mesh after each loop and wait until the end of the current frame before looping again
            //This is mainly to split up generation into a 'loading' process instead of just stalling until it is all completed
            if (Application.isPlaying)
            {
                tileMap.UpdateMesh();
                yield return new WaitForEndOfFrame();
            }
        }

        //Generate the background tilemap based off the main one
        backgroundTilemap.GenerateBackgroundTilemap(tileMap);

        //Begin the clutter pass to spawn contextual props on the generated level
#if UNITY_EDITOR
        if (Application.isPlaying || spawnClutterInEditor)
            ClutterPass(theme);
#else
        ClutterPass(theme);
#endif

        //Update the mesh one final time now that all generation is complete
        tileMap.UpdateMesh();
        roomGenerationComplete = true;
        yield return new WaitForSeconds(0);
    }

    /// <summary>
    /// Takes in room tile data and sets all of the corresponding tiles on the tilemaps to the correct values. Also deals with mutateable tile logic.
    /// </summary>
    /// <param name="roomDataList">A list of the rooms tile data to draw tiles from.</param>
    /// <param name="offset">The offset of the current room on the tilemap</param>
    void ConstructTileMapFromData(List<TileDataCustom> roomDataList, Vector2 offset)
    {
        int xOffset = (int)offset.x * 32;
        int yOffset = (int)offset.y * 16;
        List<TileDataCustom> mutateableInCurrentRoom = new List<TileDataCustom>();

        for (int i = 0; i < roomDataList.Count; i++)
        {
            int tileId = roomDataList[i].TileId;
            int x = (int)roomDataList[i].TileLocationX + xOffset;
            int y = (int)roomDataList[i].TileLocationY + yOffset;

            //If tile not empty
            if (tileId != 65535)
            {
                //If current tile data is not a mutateable tile, spawn it regularly
                //If not, add it to the mutatabeable list to be dealt with later
                if (!roomDataList[i].Mutateable)
                {
                    tileMap.SetTile(x, y, tileId, brushID, eTileFlags.None);
                }
                else
                {
                    mutateableInCurrentRoom.Add(roomDataList[i]);
                }
            }
        }

        //For every mutateable tile found in this room, check to see whether to spawn it or not
        for (int i = 0; i < mutateableInCurrentRoom.Count; i++)
        {
            //If tile is mutateable, roll a dice and see whether to set it or not [TEMP]
            //If not mutateable, just set it immediately
            if (mutateableInCurrentRoom[i].Mutateable)
            {
                int tileId = mutateableInCurrentRoom[i].TileId;
                int x = (int)mutateableInCurrentRoom[i].TileLocationX + xOffset;
                int y = (int)mutateableInCurrentRoom[i].TileLocationY + yOffset;

                //If random int is under the chance threshold, spawn the mutateable tile on the tilemap
                int rand = Random.Range(0, 11);
                if (rand < (int)(mutateableChance * 10))
                {
                    tileMap.SetTile(x, y, tileId, brushID, eTileFlags.None);
                }
            }
        }
    }
    #endregion

    #region Clutter Pass
    /// <summary>
    /// Spawns contextual props/clutter around the map based off the surrounding tiles (i.e. stalagtites on the ceiling, grass on the floor, etc).
    /// Chances can be weighted in the inspector in order to propogate more or less of certain prop types (more ceiling, less ground props, etc).
    /// </summary>
    /// <param name="theme">The theme of the props to be spawned.</param>
    void ClutterPass(RoomDatabaseMaster.RoomTheme theme)
    {
        Debug.Log("Starting clutter pass");

        ClutterCollectionTheme currentClutterList = null;

        //Set the corresponding room clutter from the passed in theme
        if (theme == RoomDatabaseMaster.RoomTheme.Theme1)
        {
            currentClutterList = Clutter_Theme1;
        }
        else if(theme == RoomDatabaseMaster.RoomTheme.Theme2)
        {
            currentClutterList = Clutter_Theme2;
        }
        else if (theme == RoomDatabaseMaster.RoomTheme.Theme3)
        {
            currentClutterList = Clutter_Theme3;
        }

        //Loops through every tile on the tilemap and gets the tile data and the status of the other tiles surrounding it
        //When props are spawned, they are also associated or 'linked' with the tile they are spawned on (grass linked to tile belowe, stalagtites to the tile above)
        //These links are used so that when the associated tile could possibly be destroyed by the player's actions, the associated prop is destroyed too and not left floating.
        for (float x = 0; x < tileMap.GridWidth * tileMap.CellSize.x; x += tileMap.CellSize.x)
        {
            for (float y = 0; y < tileMap.GridHeight * tileMap.CellSize.y; y += tileMap.CellSize.y)
            {
                uint tileDataRaw = tileMap.GetTileData(new Vector2(x, y));
                TileData tileDataProcessed = new TileData(tileDataRaw);

                //If the current tile is empty space, a prop CAN be spawned there (cannot spawn props on an occupied space)
                if (tileDataProcessed.tileId == 65535)
                {
                    //Assign the surrounding tiles statuses to an array on int values (0 is free, 1 is taken, etc)
                    int[] surroundingTiles = SurroundingTileStatus(new Vector2(x, y));
                    bool clutterSet = false;

                    //The following statements check the values of the surrounding tiles returns, and request the type of clutter based on the results of these checks
                    //Prioritises ground clutter > ceiling > walls for aesthetic reasons

                    //Check if ground clutter can be spawned
                    if (surroundingTiles[0] == 0 && surroundingTiles[2] == 1 && !clutterSet && !DestructibleTerrainHandler.associatedClutterDictionary.ContainsKey(new Vector2(x, y - tileMap.CellSize.y)))
                    {
                        float rand = Random.Range(0f, 1f);
                        if (rand <= groundClutterChance)
                        {
                            GameObject clutterObj = Instantiate(RequestClutter(currentClutterList.groundClutter), new Vector3(x, y, 1), Quaternion.identity);
                            DestructibleTerrainHandler.associatedClutterDictionary.Add(new Vector2(x, y - tileMap.CellSize.y), clutterObj);
                            clutteredTilePositions.Add(new Vector2(x, y));
                            clutterSet = true;
                        }
                    }
                    //If not, can ceiling clutter be spawned
                    else if (surroundingTiles[0] == 1 && surroundingTiles[2] == 0 && !clutterSet && !DestructibleTerrainHandler.associatedClutterDictionary.ContainsKey(new Vector2(x, y + tileMap.CellSize.y)))
                    {
                        float rand = Random.Range(0f, 1f);
                        if (rand <= ceilingClutterChance)
                        {
                            GameObject clutterObj = Instantiate(RequestClutter(currentClutterList.ceilingClutter), new Vector3(x, y, 1), Quaternion.identity);
                            DestructibleTerrainHandler.associatedClutterDictionary.Add(new Vector2(x, y + tileMap.CellSize.y), clutterObj);
                            clutteredTilePositions.Add(new Vector2(x, y));
                            clutterSet = true;
                        }
                    }
                    //If not, left wall clutter?
                    else if (surroundingTiles[1] == 0 && surroundingTiles[3] == 1 && !clutterSet && !DestructibleTerrainHandler.associatedClutterDictionary.ContainsKey(new Vector2(x - tileMap.CellSize.x, y)))
                    {
                        float rand = Random.Range(0f, 1f);
                        if (rand <= wallClutterChance)
                        {
                            GameObject clutterObj = Instantiate(RequestClutter(currentClutterList.leftWallClutter), new Vector3(x, y, 1), Quaternion.identity);
                            DestructibleTerrainHandler.associatedClutterDictionary.Add(new Vector2(x - tileMap.CellSize.x, y), clutterObj);
                            clutteredTilePositions.Add(new Vector2(x, y));
                            clutterSet = true;
                        }
                    }
                    //If not, right wall clutter?
                    else if (surroundingTiles[3] == 0 && surroundingTiles[1] == 1 && !clutterSet && !DestructibleTerrainHandler.associatedClutterDictionary.ContainsKey(new Vector2(x + tileMap.CellSize.x, y)))
                    {
                        float rand = Random.Range(0f, 1f);
                        if (rand <= wallClutterChance)
                        {
                            GameObject clutterObj = Instantiate(RequestClutter(currentClutterList.rightWallClutter), new Vector3(x, y, 1), Quaternion.identity);
                            DestructibleTerrainHandler.associatedClutterDictionary.Add(new Vector2(x + tileMap.CellSize.x, y), clutterObj);
                            clutteredTilePositions.Add(new Vector2(x, y));
                            clutterSet = true;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Returns a clutter object of the specific type and theme from the clutter collection based off a weighted chance (allows different rarities of props).
    /// </summary>
    /// <param name="clutterCollection"></param>
    /// <returns></returns>
    GameObject RequestClutter(ClutterCollectionSO clutterCollection)
    {
        int randomIndex = GetRandomWeightedIndex(clutterCollection.associatedChances);
        return clutterCollection.clutter[randomIndex];
    }

    /// <summary>
    /// Gets the status of the tiles around the tile passed in and returns an array of 4 ints. 0 = tile is free, 1 = tile is occupied, -1 = tile is taken by existing clutter.
    /// </summary>
    /// <param name="tilePos">Position of the current tile.</param>
    /// <returns></returns>
    int[] SurroundingTileStatus(Vector2 tilePos)
    {
        int[] surroundingTiles = new int[4] { 0, 0, 0, 0 };

        //Check the surrounding tile is within the tilemap bounds (if not, regard it as a free space)
        //UP
        if (tilePos.y < tileMap.GridHeight * tileMap.CellSize.y)
        {
            //Check if the surrounding tile already contains clutter
            //If not, check if the tile is an empty tile or a terrain tile
            if (!clutteredTilePositions.Contains(new Vector2(tilePos.x, tilePos.y + tileMap.CellSize.y)))
            {
                uint tileDataRaw = tileMap.GetTileData(new Vector2(tilePos.x, tilePos.y + tileMap.CellSize.y));
                TileData tileDataProcessed = new TileData(tileDataRaw);

                //if tileID of chosen tile is not 65535, it is not an empty tile, and should not spawn clutter
                if (tileDataProcessed.tileId != 65535)
                    surroundingTiles[0] = 1;
            }
            else
            {
                surroundingTiles[0] = -1;
            }
        }
        //RIGHT
        if (tilePos.x < tileMap.GridWidth * tileMap.CellSize.x)
        {
            if (!clutteredTilePositions.Contains(new Vector2(tilePos.x + tileMap.CellSize.x, tilePos.y)))
            {
                uint tileDataRaw = tileMap.GetTileData(new Vector2(tilePos.x + tileMap.CellSize.x, tilePos.y));
                TileData tileDataProcessed = new TileData(tileDataRaw);

                //if tileID of chosen tile is not 65535, it is not an empty tile, and should not spawn clutter
                if (tileDataProcessed.tileId != 65535)
                    surroundingTiles[1] = 1;
            }
            else
            {
                surroundingTiles[1] = -1;
            }
        }
        //DOWN
        if (tilePos.y > 0)
        {
            if (!clutteredTilePositions.Contains(new Vector2(tilePos.x, tilePos.y - tileMap.CellSize.y)))
            {
                uint tileDataRaw = tileMap.GetTileData(new Vector2(tilePos.x, tilePos.y - tileMap.CellSize.y));
                TileData tileDataProcessed = new TileData(tileDataRaw);

                //if tileID of chosen tile is not 65535, it is not an empty tile, and should not spawn clutter
                if (tileDataProcessed.tileId != 65535)
                    surroundingTiles[2] = 1;
            }
            else
            {
                surroundingTiles[2] = -1;
            }
        }
        //LEFT
        if (tilePos.x > 0)
        {
            if (!clutteredTilePositions.Contains(new Vector2(tilePos.x - tileMap.CellSize.x, tilePos.y)))
            {
                uint tileDataRaw = tileMap.GetTileData(new Vector2(tilePos.x - tileMap.CellSize.x, tilePos.y));
                TileData tileDataProcessed = new TileData(tileDataRaw);

                //if tileID of chosen tile is not 65535, it is not an empty tile, and should not spawn clutter
                if (tileDataProcessed.tileId != 65535)
                    surroundingTiles[3] = 1;
            }
            else
            {
                surroundingTiles[3] = -1;
            }
        }

        return surroundingTiles;
    }

    /// <summary>
    /// Helper function. Returns a 'random' index based off a list of weighted chances between 0 and 1.
    /// </summary>
    /// <param name="weights">A list of weighted chance floats (must be between 0-1).</param>
    /// <returns></returns>
    public int GetRandomWeightedIndex(float[] weights)
    {
        if (weights == null || weights.Length == 0) return -1;

        float w = 0;
        float t = 0;
        int i;
        for (i = 0; i < weights.Length; i++)
        {
            w = weights[i];
            if (float.IsPositiveInfinity(w)) return i;
            else if (w >= 0f && !float.IsNaN(w)) t += weights[i];
        }

        float r = Random.value;
        float s = 0f;

        for (i = 0; i < weights.Length; i++)
        {
            w = weights[i];
            if (float.IsNaN(w) || w <= 0f) continue;

            s += w / t;
            if (s >= r) return i;
        }

        return -1;
    }
    #endregion

    #region Debug
    //Debug features for regenerating the level at runtime for testing and 'respawning' the player
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GenerateLevel();
        }

        if (roomGenerationComplete)
        {
            GameObject.FindGameObjectWithTag("Player").transform.position = FindAvailablePlayerLocation();
            roomGenerationComplete = false;
        }
    }

    Vector2 FindAvailablePlayerLocation()
    {
        bool locationFound = false;
        Vector2 playerLocation = Vector2.zero;
        while (!locationFound)
        {
            int x = Random.Range(0, tileMap.GridWidth);
            int y = Random.Range(0, tileMap.GridHeight);

            uint raw = tileMap.GetTileData(x, y);
            TileData processed = new TileData(raw);

            //if chosen tile is empty, valid player location found
            if (processed.tileId == 65535)
            {
                playerLocation = new Vector2(x * tileMap.CellSize.x, y * tileMap.CellSize.y);
                locationFound = true;
            }
        }

        return playerLocation;
    }
    #endregion
}
