﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tibia.Packets.Incoming
{
    public class CreatureMovePacket : IncomingPacket
    {
        public byte FromStackPosition { get; set; }
        public Objects.Location FromPosition { get; set; }
        public Objects.Location ToPosition { get; set; }

        public CreatureMovePacket(Objects.Client c)
            : base(c)
        {
            Type = IncomingPacketType.CreatureMove;
            Destination = PacketDestination.Client;
        }

        public override bool ParseMessage(NetworkMessage msg, PacketDestination destination, Objects.Location pos)
        {
            if (msg.GetByte() != (byte)IncomingPacketType.CreatureMove)
                return false;

            Destination = destination;
            Type = IncomingPacketType.CreatureMove;
            FromPosition = msg.GetLocation();
            FromStackPosition = msg.GetByte();
            ToPosition = msg.GetLocation();

            return true;
        }

        public override byte[] ToByteArray()
        {
            NetworkMessage msg = new NetworkMessage(0);

            msg.AddByte((byte)Type);
            msg.AddLocation(FromPosition);
            msg.AddByte(FromStackPosition);
            msg.AddLocation(ToPosition);

            return msg.Packet;
        }
    }
}