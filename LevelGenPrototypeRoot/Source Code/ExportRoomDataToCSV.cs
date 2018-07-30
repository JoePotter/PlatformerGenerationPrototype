using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CreativeSpore.SuperTilemapEditor;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Expressions;
using CsvHelper.TypeConversion;

/// <summary>
/// This script is used to export the current tilemap in the editor to a CSV file, so it can later be accessed as a resource and used in level generation as a room template.
/// </summary>
[RequireComponent(typeof(STETilemap))]
public class ExportRoomDataToCSV : MonoBehaviour
{
    #region Variables
    [Header("References")]
    [SerializeField]
    STETilemap tileMap;

    List<string> tileIDs = new List<string>();
    List<TileDataCustom> roomDataList;

    TextWriter textWriter;
    CsvWriter csvWriter;
    TileDataCustom data;


    //---------------------------------------------------------------------------------------------------//
    [Header("Settings")]
    [Tooltip("Custom identifier to be added to the room resource file. I.e., 'boss' for boss rooms, or 'loot' for supply rooms. Can also be used for general tracking/naming purposes.")]
    [SerializeField]
    string roomIdentifier = "";

    RoomDatabaseMaster.RoomType roomType;

    [SerializeField]
    RoomDatabaseMaster.RoomTheme roomTheme;

    [Tooltip("Adds an extra index on the end of the file name in the event of a conflict, instead of asking for user input to solve it.")]
    public bool autoNameConflictResolution = false;

    string directory;
    string filename = "\\roomData";    


    //---------------------------------------------------------------------------------------------------//
    [Header("Connection Types")]
    [Tooltip("Whether the UP connection of this room is open or closed.")]
    [SerializeField]
    public PCG.Connection up;
    [Tooltip("Whether the RIGHT connection of this room is open or closed.")]
    [SerializeField]
    public PCG.Connection right;
    [Tooltip("Whether the DOWN connection of this room is open or closed.")]
    [SerializeField]
    public PCG.Connection down;
    [Tooltip("Whether the LEFT connection of this room is open or closed.")]
    [SerializeField]
    public PCG.Connection left;
    #endregion
      
    public void ExportRoom(bool conflict)
    {
        roomDataList = new List<TileDataCustom>();

        //Create a directory path based on the selected theme and room type, and append any identifiers to the filename
        directory = GetPath() + roomTheme.ToString() + "/" + roomType.ToString();
        Directory.CreateDirectory(directory);
        directory += "/" + filename + "_" + roomTheme.ToString() + "_" + roomType.ToString() + "_" + roomIdentifier;

        //Conflict handling - if automatically handled (shouldn't have got to this point otherwise, but best to be safe), add an index to the filename that is free/unique.
        if (!conflict)
        {
            directory += ".csv";
        }
        else if (conflict && autoNameConflictResolution)
        {
            bool indexFree = false;
            int counter = 1;

            while (!indexFree)
            {
                if (!CheckForExistingRooms(directory + "_" + counter.ToString() + ".csv"))
                {
                    Debug.Log("Room name (" + directory + ") already taken, appending index to distinguish between room identities.");

                    directory += "_" + counter.ToString() + ".csv";
                    indexFree = true;
                }
                else
                {
                    counter++;
                }
            }
        }
        
        //If the file already exists, delete it and overwrite it.
        if (File.Exists(directory))
        {
            System.IO.File.Delete(directory);
            Debug.Log("Deleting existing room file to overwrite");
        }

        //Create and configure the CSV/text writers
        textWriter = File.CreateText(directory);
        csvWriter = new CsvWriter(textWriter);
        csvWriter.Configuration.RegisterClassMap<RoomTileDataMap>();
        csvWriter.WriteHeader<RoomData>();
        csvWriter.NextRecord();

        //For every tile on the tilemap, loop through and create a TileDataCustom object based on all of its data and parameters
        for (int x = tileMap.MinGridX; x < tileMap.GridWidth; x++)
        {
            for (int y = tileMap.MinGridY; y < tileMap.GridHeight; y++)
            {
                uint tileDataRaw = tileMap.GetTileData(x, y);
                Tile tile = tileMap.GetTile(x, y);
                TileData tileData = new TileData(tileDataRaw);

                bool isMutateable = false;
                bool isChunkTemplate = false;
                if (tile != null)
                {
                    isMutateable = tile.paramContainer.GetBoolParam("mutateable");
                    isChunkTemplate = tile.paramContainer.GetBoolParam("isChunkTemplate");
                    if (isChunkTemplate)
                        Debug.Log(new Vector2(x, y));
                }
                data = new TileDataCustom(tileData.tileId, new Vector2(x, y), isMutateable, isChunkTemplate);
                roomDataList.Add(data);
            }
        }

        //Pass data to writing function that actually handles the file writing
        WriteDataToCSV();
    }

    //Writes all of the tile data to CSV format using the RoomTileDataMap class as a template for mapping the data (used both when exporting AND importing)
    private void WriteDataToCSV()
    {
        using (textWriter)
        {
            csvWriter.WriteRecords(roomDataList);
        }

        //Close the writer to remove it from memory
        csvWriter.Dispose();
    }

    /// <summary>
    /// Checks for a naming conflict when exporting a room.
    /// </summary>
    /// <returns></returns>
    public bool RoomNameConflictCheck()
    {
        roomType = RoomDatabaseMaster.Instance.GetRoomType(up, right, down, left);

        directory = GetPath() + roomTheme.ToString() + "/" + roomType.ToString() + "/" + filename + "_" + roomTheme.ToString() + "_" + roomType.ToString() + "_" + roomIdentifier;

        if (!CheckForExistingRooms(directory + ".csv"))
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    private bool CheckForExistingRooms(string path)
    {
        return File.Exists(path);
    }       

    private string GetPath()
    {
#if UNITY_EDITOR
        return Application.dataPath + "/Resources/CSV_FILES/";
#else
        return "";
#endif
    }

}

public class TileDataCustom
{
    public int TileId { get; set; }
    public float TileLocationX { get; set; }
    public float TileLocationY { get; set; }
    public bool Mutateable { get; set; }
    public bool IsChunkTemplate { get; set; }

    public TileDataCustom()
    {
        TileId = 696969;
        TileLocationX = 696969;
        TileLocationY = 696969;
        Mutateable = false;
        IsChunkTemplate = false;
    }

    public TileDataCustom(int tileId, Vector2 tileLocation, bool mutateable, bool isChunkTemplate)
    {
        TileId = tileId;
        TileLocationX = tileLocation.x;
        TileLocationY = tileLocation.y;
        Mutateable = mutateable;
        IsChunkTemplate = isChunkTemplate;
    }
}

public sealed class RoomTileDataMap : ClassMap<TileDataCustom>
{
    public RoomTileDataMap()
    {
        Map(m => m.TileId).Index(0);
        Map(m => m.TileLocationX).Index(1);
        Map(m => m.TileLocationY).Index(2);
        Map(m => m.Mutateable).Index(3);
        Map(m => m.IsChunkTemplate).Index(4);
    }
}
