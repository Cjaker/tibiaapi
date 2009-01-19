using System;
using System.Collections.Generic;

namespace Tibia.Objects
{
    /// <summary>
    /// Represents a square on the map.
    /// </summary>
    public class MapSquare
    {
        private Client client;
        public uint Address;
        public uint SquareNumber;
        public Location MemoryLocation;
        public int ObjectCount;
        public Tile Tile = new Tile();

        public List<Item> Items
        {
            get
            {
                List<Item> items = new List<Item>();

                foreach (MapObject mo in Objects)
                {
                    Item item = new Item(client, (uint)mo.Id, (byte)mo.Data, "", new ItemLocation(Tile.Location, (byte)mo.StackOrder), true); 
                    items.Add(item);
                }

                return items;
            }
        }

        public List<MapObject> Objects
        {
            get
            {
                List<MapObject> objects = new List<MapObject>();

                uint pointer = Address + Addresses.Map.Distance_Square_Objects;

                for (int i = 0; i < ObjectCount; i++)
                {
                    // Go to the next object
                    pointer += Addresses.Map.Step_Square_Object;

                    objects.Add(new MapObject(
                        client.ReadInt32(pointer + 
                            Addresses.Map.Distance_Object_Id),
                        client.ReadInt32(pointer + 
                            Addresses.Map.Distance_Object_Data),
                        client.ReadInt32(pointer + 
                            Addresses.Map.Distance_Object_Data_Ex),
                        i + 1));
                }

                return objects;
            }
        }

        public MapSquare(Client client, uint address, uint squareNumber, Location location)
            : this(client, address, squareNumber)
        {
            Tile.Location = location;
        }

        public MapSquare(Client client, uint address, uint squareNumber)
        {
            this.client = client;
            this.SquareNumber = squareNumber;
            this.Address = address;
            this.MemoryLocation = Map.ConvertSquareNumberToMemoryLocation(squareNumber);

            ObjectCount = client.ReadInt32(address + Addresses.Map.Distance_Square_ObjectCount) - 1; // -1 for Tile

            // Get the tile data (first object)
            Tile.Id = Convert.ToUInt32(client.ReadInt32(address + 
                Addresses.Map.Distance_Square_Objects + 
                Addresses.Map.Distance_Object_Id));
        }

        public void ReplaceTile(uint newId)
        {
            client.WriteInt32(Address + 
                Addresses.Map.Distance_Square_Objects + 
                Addresses.Map.Distance_Object_Id, (int)newId);
        }

        public void ReplaceObject(MapObject oldObject, MapObject newObject)
        {
            uint pointer = (uint)(Address +
                (Addresses.Map.Distance_Square_Objects +
                Addresses.Map.Step_Square_Object * oldObject.StackOrder));
            client.WriteInt32(pointer + 
                Addresses.Map.Distance_Object_Id,
                newObject.Id);
            client.WriteInt32(pointer +
                Addresses.Map.Distance_Object_Data,
                newObject.Data);
            client.WriteInt32(pointer +
                Addresses.Map.Distance_Object_Data_Ex,
                newObject.DataEx);
        }
    }

    /// <summary>
    /// Represents an object on a MapSquare
    /// </summary>
    public class MapObject
    {
        public int StackOrder { get; set; }
        public int Id { get; set; }
        public int Data { get; set; }
        public int DataEx { get; set; }

        public MapObject(int id, int data, int dataEx)
            : this(id, data, dataEx, 0) { }


        public MapObject(int id, int data, int dataEx, int stackOrder)
        {
            StackOrder = stackOrder;
            Id = id;
            Data = data;
            DataEx = dataEx;
        }
    }
}
