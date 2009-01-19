﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tibia.Packets.Incoming
{
    public class CreatureSkullsPacket : IncomingPacket
    {
        public uint CreatureId { get; set; }
        public byte CreatureSkull { get; set; }

        public CreatureSkullsPacket(Objects.Client c)
            : base(c)
        {
            Type = IncomingPacketType.CreatureSkull;
            Destination = PacketDestination.Client;
        }

        public override bool ParseMessage(NetworkMessage msg, PacketDestination destination, Objects.Location pos)
        {
            if (msg.GetByte() != (byte)IncomingPacketType.CreatureSkull)
                return false;

            Destination = destination;
            Type = IncomingPacketType.CreatureSkull;

            CreatureId = msg.GetUInt32();
            CreatureSkull = msg.GetByte();

            return true;
        }

        public override byte[] ToByteArray()
        {
            NetworkMessage msg = new NetworkMessage(0);

            msg.AddByte((byte)Type);

            msg.AddUInt32(CreatureId);
            msg.AddByte(CreatureSkull);

            return msg.Packet;
        }
    }
}