using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PCG
{
    public class DrunkManWalking : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        GameObject roomRepresentation;
        GameObject holder;
        public Dictionary<Vector2, Room> RoomDictionary { get; private set; }

        [Header("Settings")]

        [SerializeField]
        bool drawDebug;        
        
        [SerializeField]
        Vector2 widthRange;
        [SerializeField]
        Vector2 heightRange;

        [SerializeField]
        int randomizePasses = 2;

        int[,] roomGrid;
        bool exitFound;

        int width;
        int height;
        int xLowerBound;
        int yLowerBound;

        private void Start()
        {
            GenerateLevel();
        }

        //Creates a 'path' through an int grid of 'rooms' by choosing random directions of movement across the grid.
        //This is a sort of interpretation of the simple 'drunk man walking' algorithm
        public void GenerateLevel()
        {
            //Initialise variables
            exitFound = false;
            width = Random.Range((int)widthRange.x, (int)widthRange.y);
            height = Random.Range((int)heightRange.x, (int)heightRange.y);

            roomGrid = new int[width, height];

            int x = 0;
            int y = Random.Range(0, height);

            int index = 1;
            roomGrid[0, y] = index;
            index++;

            //While no exit to the main path of the level is found, continue progressing along the path until it is
            while (!exitFound)
            {
                int dir = Random.Range(0, 3);
                switch (dir)
                {
                    case 0:
                        if (y > 0 && roomGrid[x, y - 1] == 0)
                        {
                            y -= 1;
                            break;
                        }
                        goto case 1;
                    case 1:
                        //If the X of the current room is at the far right edge of the map and the path direction is also going right, an exit has been found
                        if (x == width - 1)
                        {
                            exitFound = true;
                            break;
                        }
                        if (x < width - 1 && roomGrid[x + 1, y] == 0)
                        {
                            x += 1;
                            break;
                        }
                        goto case 2;
                    case 2:
                        if (y < height - 1 && roomGrid[x, y + 1] == 0)
                        {
                            y += 1;
                            break;
                        }
                        goto case 0;
                }
                roomGrid[x, y] = index;
                index++;
            }

            LayoutRooms();
        }

        //Once the path is complete, the rest of the rooms need to have their connections to neighbouring rooms handled
        //These are initially chosen at random, but are later overwritten when room must match their connections to their neighbours
        void LayoutRooms()
        {
            RoomDictionary = new Dictionary<Vector2, Room>();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    //Create a new room to populate
                    Room current = new Room
                    {
                        //Handle edges and connections to other rooms on the path after this so that it overwrites anything important
                        //TODO: we need to add some more complicated logic here when we know more about how we want the extra generation to work
                        //Like for example we can add limits e.g. so  we don't get 5 loot rooms or 10 minibosses
                        U = PickRandomConnection(),
                        R = PickRandomConnection(),
                        D = PickRandomConnection(),
                        L = PickRandomConnection(),
                        location = new Vector2(x, y),
                        onPath = false
                    };

                    //Check to see if any edges are the bounds of the map
                    if (x == 0) { current.L = Connection.C; }
                    if (y == 0) { current.D = Connection.C; }
                    if (x == width - 1) { current.R = Connection.C; }
                    if (y == height - 1) { current.U = Connection.C; }

                    //Check if the room is on the 'path'
                    if (roomGrid[x, y] > 0)
                    {
                        //On the path - check if any of the connecting rooms are also on the path and then make the connection open
                        //Also check if room is on the bounds of the map to avoid null references
                        if (y > 0 && roomGrid[x, y - 1] > 0) { current.D = Connection.O; }
                        if (y < height - 1 && roomGrid[x, y + 1] > 0) { current.U = Connection.O; }
                        if (x > 0 && roomGrid[x - 1, y] > 0) { current.L = Connection.O; }
                        if (x < width - 1 && roomGrid[x + 1, y] > 0) { current.R = Connection.O; }
                        current.onPath = true;
                    }
                    RoomDictionary.Add(current.location, current);
                }
            }
            MatchRoomConnections();
        }

        //Matches the current rooms connections with the neighbouring rooms
        void MatchRoomConnections()
        {
            for (int x = xLowerBound; x < width; x++)
            {
                for (int y = yLowerBound; y < height; y++)
                {
                    Room current;
                    if (RoomDictionary.TryGetValue(new Vector2(x, y), out current))
                    {
                        Room temp;
                        if (RoomDictionary.TryGetValue(new Vector2(x, y + 1), out temp))
                        {
                            current.U = temp.D;
                        }
                        if (RoomDictionary.TryGetValue(new Vector2(x - 1, y), out temp))
                        {
                            current.L = temp.R;
                        }
                        RoomDictionary[new Vector2(x, y)] = current;
                    }
                }
            }
        }

        Connection PickRandomConnection()
        {
            int rand = Random.Range(0, 2);
            switch (rand)
            {
                case 0:
                    return Connection.O;
                case 1:
                    return Connection.C;
            }
            return Connection.C;
        }
    }

    public enum Connection
    {
        O,
        C
    }

    public struct Room
    {
        public Connection U;
        public Connection R;
        public Connection D;
        public Connection L;

        public bool onPath;

        public Vector2 location;
    }
}