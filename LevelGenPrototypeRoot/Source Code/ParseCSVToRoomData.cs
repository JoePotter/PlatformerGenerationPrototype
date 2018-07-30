using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CsvHelper;
using CsvHelper.Configuration;
using System.IO;
using System.Linq;
using CreativeSpore.SuperTilemapEditor;

/// <summary>
/// This class functions as a way to parse individual room templates from CSV files that have been created by the ExportRoomDataToCSV class.
/// It reads through the CSV file selected by the user in the Unity inspector, and constructs the room template on the referenced tilemap.
/// This can be used to edit existing rooms, or to repeatedly test generation features such as mutable tiles on a specific without having to generate an entire level.
/// </summary>

[RequireComponent(typeof(STETilemap), typeof(LevelGenerator))]
public class ParseCSVToRoomData : MonoBehaviour
{
    //References to the tilemap and level generator components that are active in the scene (set in inspector)
    [Header("References")]
    [SerializeField]
    STETilemap tileMap;
    [SerializeField]
    LevelGenerator levelGen;

    [Header("Settings")]
    //The chance that mutable tiles will spawn
    [SerializeField]
    [Range(0, 1)]
    float mutateableChance = .5f;

    //Reads CSV file data and compiles it into a list of tile data objects that can be used to generate a tilemap
    public void ParseTileMapDataFromCSV(string path)
    {
        //Clears the current tilemap to prevent overlapping tile data
        tileMap.ClearMap();
        
        //Create an empty list of room data objects
        List<TileDataCustom> roomDataList = new List<TileDataCustom>();

        //Create and configure the text/CSV readers and reference the users selected filepath to read from
        TextReader textReader;
        CsvReader csv;
        textReader = File.OpenText(path);
        csv = new CsvReader(textReader);
        csv.Configuration.RegisterClassMap<RoomTileDataMap>();
        csv.Configuration.HeaderValidated = null;

        //Use the CSV reader to read the data from the file and convert it into tile data
        csv.Read();
        IEnumerable<TileDataCustom> enumerable = csv.GetRecords<TileDataCustom>();
        roomDataList = enumerable.ToList();
        textReader.Close();
        
        //Pass the tile data into the tilemap construction function
        ConstructTileMapFromData(roomDataList);
    }

    //Loops through the passed in tile data and sets the tiles on the tilemap to the associated tile ID's
    void ConstructTileMapFromData(List<TileDataCustom> roomDataList)
    {

        for (int i = 0; i < roomDataList.Count; i++)
        {
            //Get the tile location on the tilemap grid by using the data stored in the TileDataCustom objects
            int tileId = roomDataList[i].TileId;
            int x = (int)roomDataList[i].TileLocationX;
            int y = (int)roomDataList[i].TileLocationY;
            
            //If the tile is not an empty tile (id 65535), set the tile on the tilemap to the corresponding ID
            if (roomDataList[i].TileId != 65535)
            {
                tileMap.SetTile(x, y, tileId, 2, eTileFlags.None);
            }
        }

        //Update the mesh once all tile data is set
        tileMap.UpdateMesh();
    }
}
