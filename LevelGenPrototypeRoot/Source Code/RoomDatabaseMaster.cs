using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CsvHelper;
using CsvHelper.Configuration;
using System.IO;
using System.Linq;

/// <summary>
/// 
/// </summary>
public class RoomDatabaseMaster : MonoBehaviour
{
    //Instance check
    private static RoomDatabaseMaster instance = null;
    private RoomDatabaseMaster() { }
    public static RoomDatabaseMaster Instance
    {
        get
        {
            if (!instance)
            {
                instance = FindObjectOfType(typeof(RoomDatabaseMaster)) as RoomDatabaseMaster;

                if (!instance)
                {
                    Debug.LogError("There needs to be a RoomDatabaseMaster script attached to a gameobject in this scene!");
                }
            }

            return instance;
        }
    }

    /// <summary>
    /// Request a RoomData object of the desired theme, type, and level of difficulty for use in generation
    /// </summary>
    /// <param name="theme">Room Theme (i.e. space, cave, etc)</param>
    /// <param name="type">Room Type (i.e. top open, bottom sealed, etc)</param>
    /// <param name="desiredDifficulty">Room Difficulty Level (currently not implemented)</param>
    /// <returns></returns>
    public RoomData GetRoomData(RoomTheme theme, RoomType type, float desiredDifficulty)
    {
        RoomData roomData = new RoomData();
        List<TextAsset> roomFiles = new List<TextAsset>();

        //Load all rooms of the desired theme from the Unity resources path (works at runtime even when serialised)
        TextAsset[] fileInfoArray = Resources.LoadAll<TextAsset>(GetPath() + theme.ToString());

        //Get a list of all rooms in this theme that match the desired type
        foreach (TextAsset file in fileInfoArray)
        {
            if (file.name.Contains(type.ToString()))
            {
                roomFiles.Add(file);
            }
        }
        
        //Choose a random room from the potential matches
        int randomRoomIndex = Random.Range(0, roomFiles.Count);
        roomData.tileData = ParseTileDataFromCSVFile(roomFiles[randomRoomIndex]);

        //Unload the resources from memory and return the chosen room
        Resources.UnloadUnusedAssets();
        return roomData;
    }

    /// <summary>
    /// Reads through a CSV file and returns a list of TileDataCustom objects to form a tilemap
    /// </summary>
    /// <param name="roomFile">The CSV file to be read</param>
    /// <returns></returns>
    public List<TileDataCustom> ParseTileDataFromCSVFile(TextAsset roomFile)
    {
        List<TileDataCustom> tileDataList = new List<TileDataCustom>();        
        CsvReader csv;

        //Open and configure writer, and convert contents into a list of TileDataCustom objects to be returned
        using (StreamReader textReader = new StreamReader(new MemoryStream(roomFile.bytes)))
        {

            csv = new CsvReader(textReader);
            csv.Configuration.MissingFieldFound = null;
            csv.Configuration.HeaderValidated = null;
            csv.Configuration.RegisterClassMap<RoomTileDataMap>();

            IEnumerable<TileDataCustom> enumerable = csv.GetRecords<TileDataCustom>();
            tileDataList = enumerable.ToList();

            textReader.Close();
        }

        return tileDataList;
    }  

    /// <summary>
    /// Helper function. Returns the necessary room type from the four possible connection types passed in as parameters.
    /// </summary>
    /// <param name="up"></param>
    /// <param name="right"></param>
    /// <param name="down"></param>
    /// <param name="left"></param>
    /// <returns></returns>
    public RoomType GetRoomType(PCG.Connection up, PCG.Connection right, PCG.Connection down, PCG.Connection left)
    {
        string concatenatedRoomType = "U" + up.ToString() + "_R" + right.ToString() + "_D" + down.ToString() + "_L" + left.ToString();
        RoomType roomType = RoomType.UC_RC_DC_LC;

        IEnumerable<RoomType> roomTypesRaw = System.Enum.GetValues(typeof(RoomType)) as IEnumerable<RoomType>;
        List<RoomType> roomTypesList = roomTypesRaw.ToList();

        for (int i = 0; i < roomTypesList.Count; i++)
        {
            if (concatenatedRoomType == roomTypesList[i].ToString())
            {
                roomType = roomTypesList[i];
                break;
            }
        }

        return roomType;
    }

    //Each room type represents which of the four sides is open or closed
    //U R D L represent UP, RIGHT, DOWN, and LEFT
    //O and C represent OPEN and CLOSED
    //I.E. UO_RO_DO_LC means the UP, RIGHT, and DOWN sides are OPEN, and the LEFT side is CLOSED
    public enum RoomType
    {
        UO_RO_DO_LO,
        UC_RO_DO_LO,
        UO_RC_DO_LO,
        UO_RO_DC_LO,
        UO_RO_DO_LC,
        UC_RC_DO_LO,
        UO_RC_DC_LO,
        UO_RO_DC_LC,
        UC_RO_DC_LO,
        UO_RC_DO_LC,
        UC_RO_DO_LC,
        UO_RC_DC_LC,
        UC_RO_DC_LC,
        UC_RC_DO_LC,
        UC_RC_DC_LO,
        UC_RC_DC_LC
    };

    public enum RoomTheme
    {
        Theme1, Theme2, Theme3
    };

    /// <summary>
    /// To be expanded upon the addition of further room generation rules and differing asset path possibilities
    /// </summary>
    /// <returns></returns>
    public string GetPath()
    {
        return "CSV_FILES/";
    }
}

/// <summary>
/// Contains a list of TileDataCustom objects, which in turn contain the tile data associated with all tilemap tiles needed to construct the room and give specific information/data about each tile.
/// </summary>
public class RoomData
{
    public List<TileDataCustom> tileData;
}


