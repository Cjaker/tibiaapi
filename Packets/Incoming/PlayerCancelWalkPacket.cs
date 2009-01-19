﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tibia.Packets.Incoming
{
    public class PlayerCancelWalkPacket : IncomingPacket
    {
        public byte Direction { get; set; }

        public PlayerCancelWalkPacket(Objects.Client c)
            : base(c)
        {
            Type = IncomingPacketType.PlayerCancelWalk;
            Destination = PacketDestination.Client;
        }

        public override bool ParseMessage(NetworkMessage msg, PacketDestination destination, Objects.Location pos)
        {
            if (msg.GetByte() != (byte)IncomingPacketType.PlayerCancelWalk)
                return false;

            Destination = destination;
            Type = IncomingPacketType.PlayerCancelWalk;
            Direction = msg.GetByte();

            return true;
        }

        public override byte[] ToByteArray()
        {
            NetworkMessage msg = new NetworkMessage(0);

            msg.AddByte((byte)Type);
            msg.AddByte(Direction);

            return msg.Packet;
        }
    }
}