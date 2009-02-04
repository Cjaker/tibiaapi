using System;
using Tibia.Packets;
using System.Drawing;

namespace Tibia.Objects
{
    /// <summary>
    /// Creature object.
    /// </summary>
    public class Creature
    {
        protected Client client;
        protected uint address;

        /// <summary>
        /// Create a new creature object with the given client and address.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="address">The address.</param>
        public Creature(Client client, uint address)
        {
            this.client = client;
            this.address = address;
        }

        /// <summary>
        /// Check if a player (creature) is in your party.
        /// </summary>
        /// <returns>True if the player is a member or leader of your party. False otherwise.</returns>
        public bool InParty()
        {
            Constants.Party party = Party;
            return (party != Constants.Party.None
                && party != Constants.Party.Invitee 
                && party != Constants.Party.Inviter);
        }

        /// <summary>
        /// Check if have a path to the creature.
        /// </summary>
        /// <returns></returns>
        public bool IsReachable()
        {
            var tileList = client.Map.GetTiles(delegate(Tile tile) { return true; }, true, false, Addresses.Map.Max_Squares);
            var playerTile = tileList.GetPlayerMapSquare(Client);
            var creatureTile = tileList.GetCreatureMapSquare(Id);

            if (playerTile == null || creatureTile == null)
                return false;

            int xDiff, yDiff, xNew, yNew;
            var Creatures = client.BattleList.GetCreatures(client.ReadInt32(Addresses.Player.Z));
            int playerId = client.ReadInt32(Addresses.Player.Id);

            xDiff = (int)playerTile.MemoryLocation.X - 8;
            yDiff = (int)playerTile.MemoryLocation.Y - 6;

            foreach (Tile tile in tileList)
            {
                xNew = tile.MemoryLocation.X - xDiff;
                yNew = tile.MemoryLocation.Y - yDiff;

                if (xNew > 17)
                    xNew -= 18;
                else if (xNew < 0)
                    xNew += 18;

                if (yNew > 13)
                    yNew -= 14;
                else if (yNew < 0)
                    yNew += 14;


                tile.MemoryLocation = new Location(xNew, yNew, tile.MemoryLocation.Z);

                if (tile.Ground.GetFlag(Addresses.DatItem.Flag.Blocking) ||
                    tile.Ground.GetFlag(Addresses.DatItem.Flag.BlocksPath) ||
                    tile.Items.Find(delegate(Item item)
                    {
                        return item.GetFlag(Addresses.DatItem.Flag.Blocking) ||
                            item.GetFlag(Addresses.DatItem.Flag.BlocksPath);
                    }) != null || tile.Objects.Find(delegate(TileObject obj)
                    {
                        foreach (Creature c in Creatures)
                        {
                            if (c.Id == obj.Data && obj.Data != playerId && obj.Data != Id)
                                return true;
                        }

                        return false;
                    }) != null)
                {
                    client.PathFinder.Grid[tile.MemoryLocation.X, tile.MemoryLocation.Y] = 0;
                }
                else
                {
                    client.PathFinder.Grid[tile.MemoryLocation.X, tile.MemoryLocation.Y] = 1;
                }
            }

            return client.PathFinder.FindPath(new Point(playerTile.MemoryLocation.X, playerTile.MemoryLocation.Y),
                new Point(creatureTile.MemoryLocation.X, creatureTile.MemoryLocation.Y)) != null;
        }



        /// <summary>
        /// Check if the current creature is actually yourself.
        /// </summary>
        /// <returns>True if it's yourself, false otherwise</returns>
        public bool IsSelf()
        {
            return (Id == client.ReadInt32(Addresses.Player.Id));
        }

        public void Approach()
        {
            Player p = client.GetPlayer();
            p.GoTo = Location;
        }

        /// <summary>
        /// Check if the creature is attacking the player.
        /// </summary>
        /// <returns></returns>
        public bool IsAttacking()
        {
            return BlackSquare != 0;
        }

        /// <summary>
        /// Attack the creature.
        /// Sends a packet to the server and sets the red square around the creature.
        /// </summary>
        /// <returns></returns>
        public bool Attack()
        {
            client.WriteInt32(Addresses.Player.Target_ID, Id);
            return Packets.Outgoing.AttackPacket.Send(client, (uint)Id);
        }

        /// <summary>
        /// Look at the creature / player.
        /// Sends a packet to the server with the same effect as
        /// shift-clicking or left-right clicking a creature.
        /// </summary>
        /// <returns></returns>
        public bool Look()
        {
            return Packets.Outgoing.LookAtPacket.Send(client, Location, 0x63, 1);
        }

        /// <summary>
        /// Follow the creature / player.
        /// </summary>
        /// <returns></returns>
        public bool Follow()
        {
            client.WriteInt32(Addresses.Player.GreenSquare, Id);
            return Packets.Outgoing.FollowPacket.Send(client, (uint)Id);
        }

        /// <summary>
        /// Gets the distance between player and creature / player.
        /// </summary>
        /// <returns></returns>
        public int DistanceTo(Location l)
        {
            return Location.DistanceTo(l);
        }

        public uint Address
        {
            get { return address; }
            set { address = value; }
        }

        #region Get/Set Methods

        public Client Client
        {
            get { return client; }
        }

        public int Id
        {
            get { return client.ReadInt32(address + Addresses.Creature.Distance_Id); }
            set { client.WriteInt32(address + Addresses.Creature.Distance_Id, value); }
        }

        public Constants.CreatureType Type
        {
            get { return (Constants.CreatureType)client.ReadByte(address + Addresses.Creature.Distance_Type); }
            set { client.WriteByte(address + Addresses.Creature.Distance_Type, (byte)value); }
        }

        public string Name
        {
            get { return client.ReadString(address + Addresses.Creature.Distance_Name); }
            set { client.WriteString(address + Addresses.Creature.Distance_Name, value); }
        }

        public int X
        {
            get { return client.ReadInt32(address + Addresses.Creature.Distance_X); }
            set { client.WriteInt32(address + Addresses.Creature.Distance_X, value); }
        }

        public int Y
        {
            get { return client.ReadInt32(address + Addresses.Creature.Distance_Y); }
            set { client.WriteInt32(address + Addresses.Creature.Distance_Y, value); }
        }

        public int Z
        {
            get { return client.ReadInt32(address + Addresses.Creature.Distance_Z); }
            set { client.WriteInt32(address + Addresses.Creature.Distance_Z, value); }
        }

        public Location Location
        {
            get { return new Location(X, Y, Z); }
        }

        public int ScreenOffsetHoriz
        {
            get { return client.ReadInt32(address + Addresses.Creature.Distance_ScreenOffsetHoriz); }
            set { client.WriteInt32(address + Addresses.Creature.Distance_ScreenOffsetHoriz, value); }
        }

        public int ScreenOffsetVert
        {
            get { return client.ReadInt32(address + Addresses.Creature.Distance_ScreenOffsetVert); }
            set { client.WriteInt32(address + Addresses.Creature.Distance_ScreenOffsetVert, value); }
        }

        public bool IsWalking
        {
            get { return Convert.ToBoolean(client.ReadByte(address + Addresses.Creature.Distance_IsWalking)); }
            set { client.WriteByte(address + Addresses.Creature.Distance_IsWalking, Convert.ToByte(value)); }
        }

        public int WalkSpeed
        {
            get { return client.ReadInt32(address + Addresses.Creature.Distance_WalkSpeed); }
            set { client.WriteInt32(address + Addresses.Creature.Distance_WalkSpeed, value); }
        }

        public Constants.Direction Direction
        {
            get { return (Constants.Direction)client.ReadInt32(address + Addresses.Creature.Distance_Direction); }
            set { client.WriteInt32(address + Addresses.Creature.Distance_Direction, (int)value); }
        }

        public bool IsVisible
        {
            get { return Convert.ToBoolean(client.ReadInt32(address + Addresses.Creature.Distance_IsVisible)); }
            set { client.WriteByte(address + Addresses.Creature.Distance_IsVisible, Convert.ToByte(value)); }
        }

        public int Light
        {
            get { return client.ReadInt32(address + Addresses.Creature.Distance_Light); }
            set { client.WriteInt32(address + Addresses.Creature.Distance_Light, value); }
        }

        public int LightColor
        {
            get { return client.ReadInt32(address + Addresses.Creature.Distance_LightColor); }
            set { client.WriteInt32(address + Addresses.Creature.Distance_LightColor, value); }
        }

        public int HPBar
        {
            get { return client.ReadInt32(address + Addresses.Creature.Distance_HPBar); }
            set { client.WriteInt32(address + Addresses.Creature.Distance_HPBar, value); }
        }

        public int BlackSquare
        {
            get { return client.ReadInt32(address + Addresses.Creature.Distance_BlackSquare); }
        }

        public Constants.Skull Skull
        {
            get { return (Constants.Skull)client.ReadInt32(address + Addresses.Creature.Distance_Skull); }
            set { client.WriteInt32(address + Addresses.Creature.Distance_Skull, (int)value); }
        }
        public Constants.Party Party
        {
            get { return (Constants.Party)client.ReadInt32(address + Addresses.Creature.Distance_Party); }
            set { client.WriteInt32(address + Addresses.Creature.Distance_Party, (int)value); }
        }

        public Constants.OutfitType OutfitType
        {
            get { return (Constants.OutfitType)client.ReadInt32(address + Addresses.Creature.Distance_Outfit); }
            set { client.WriteInt32(address + Addresses.Creature.Distance_Outfit, (int)value); }
        }

        public int Color_Head
        {
            get { return client.ReadInt32(address + Addresses.Creature.Distance_Color_Head); }
            set { client.WriteInt32(address + Addresses.Creature.Distance_Color_Head, value); }
        }

        public int Color_Body
        {
            get { return client.ReadInt32(address + Addresses.Creature.Distance_Color_Body); }
            set { client.WriteInt32(address + Addresses.Creature.Distance_Color_Body, value); }
        }

        public int Color_Legs
        {
            get { return client.ReadInt32(address + Addresses.Creature.Distance_Color_Legs); }
            set { client.WriteInt32(address + Addresses.Creature.Distance_Color_Legs, value); }
        }

        public int Color_Feet
        {
            get { return client.ReadInt32(address + Addresses.Creature.Distance_Color_Feet); }
            set { client.WriteInt32(address + Addresses.Creature.Distance_Color_Feet, value); }
        }

        public Constants.OutfitAddon Addon
        {
        	get { return (Constants.OutfitAddon)client.ReadInt32(address + Addresses.Creature.Distance_Addon); }
        	set { client.WriteInt32(address + Addresses.Creature.Distance_Addon, (int)value); }
        }

        public Outfit Outfit
        {
            get
            {
                return new Outfit((ushort)OutfitType, (byte)Color_Head, (byte)Color_Body,
                    (byte)Color_Legs, (byte)Color_Feet, (byte)Addon); 
            }
            set
            {
                OutfitType = (Constants.OutfitType)value.LookType;
                Color_Head = (int)value.Head;
                Color_Body = (int)value.Body;
                Color_Legs = (int)value.Legs;
                Color_Feet = (int)value.Feet;
                Addon = (Constants.OutfitAddon)value.Addons;
            }
        }

        #endregion

        public override string ToString()
        {
            return string.Format("{0}: {1}%", Name, HPBar.ToString());
        }
    }
}
