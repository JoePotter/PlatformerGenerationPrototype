using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CreativeSpore.SuperTilemapEditor;

/// <summary>
/// This script is responsible for generating the background tilemap (behind the main world) after the generation of the rest of the level is complete.
/// Can either create an organic looking perlin noise background randomly, or create a background using the tile data from the main tilemap.
/// If using the main tilemap, basically changes any tile behind a foreground tile to a rubbly/damaged tile.
/// This means that when a player destroys a foreground tile with a rocket, it looks like the background has responded to the destruction without the need for actual regeneration of tiles.
/// </summary>
public class BackgroundGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    STETilemap thisTilemap;


    [Header("Settings")]
    [SerializeField]
    int brushId;

    [SerializeField]
    float perlinThreshold;

    [SerializeField]
    float perlinScale = 20f;

    [SerializeField]
    public BackgroundType backgroundType;

    public enum BackgroundType
    {
        perlin, fromMainMap
    }


    private void Awake()
    {
        if (thisTilemap == null)
        {
            thisTilemap = GetComponent<STETilemap>();
        }
    }

    public void GenerateBackgroundTilemap(STETilemap existingTilemap)
    {
        //Clear the current background tilemap
        thisTilemap.ClearMap();

        //If background type chosen is perlin, run the perlin background generation functionality
        if (backgroundType == BackgroundType.perlin)
        {
            //random seed for variation
            float rand = Random.Range(0f, 999999f);

            //Loop through and assign tiles based on whether their perlin value at the given tile (with the seed/scale taken into account) is above a set threshold
            //If its above the threshold, set it as a tile, if not, leave it blank
            //This leads to organic and natural looking curves of tiles and a nice looking randomised background
            for (int x = 0; x < existingTilemap.GridWidth; x++)
            {
                for (int y = 0; y < existingTilemap.GridHeight; y++)
                {
                    float xCoord = rand + (float)x / (float)existingTilemap.GridWidth * perlinScale;
                    float yCoord = rand + (float)y / (float)existingTilemap.GridHeight * perlinScale;

                    float p = Mathf.PerlinNoise(xCoord, yCoord);

                    if (p > perlinThreshold)
                    {
                        thisTilemap.SetTile(x, y, 0, brushId, eTileFlags.None);
                    }
                }
            }
        }
        //If the background mode is set from the main map, set any tile behind a foreground tile to a rubbly/destroyed tile, and the rest to regular background tiles
        else if (backgroundType == BackgroundType.fromMainMap)
        {
            for (int x = 0; x < existingTilemap.GridWidth; x++)
            {
                for (int y = 0; y < existingTilemap.GridHeight; y++)
                {
                    uint raw = existingTilemap.GetTileData(x, y);
                    TileData processed = new TileData(raw);

                    if (processed.tileId != 65535)
                    {
                        thisTilemap.SetTile(x, y, 0, brushId, eTileFlags.None);
                    }
                    else
                    {
                        thisTilemap.SetTile(x, y, 0, brushId, eTileFlags.None);
                    }
                }
            }
        }

        //Update the background mesh after all generation is complete
        thisTilemap.UpdateMesh();
    }
}
