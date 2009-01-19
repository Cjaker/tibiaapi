﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tibia.Packets.Incoming
{
    public class MagicEffectPacket : IncomingPacket
    {
        public Objects.Location Position { get; set; }
        public byte Effect { get; set; }

        public MagicEffectPacket(Objects.Client c)
            : base(c)
        {
            Type = IncomingPacketType.MagicEffect;
            Destination = PacketDestination.Client;
        }

        public override bool ParseMessage(NetworkMessage msg, PacketDestination destination, Objects.Location pos)
        {
            if (msg.GetByte() != (byte)IncomingPacketType.MagicEffect)
                return false;

            Destination = destination;
            Type = IncomingPacketType.MagicEffect;

            Position = msg.GetLocation();
            Effect = msg.GetByte();

            return true;
        }

        public override byte[] ToByteArray()
        {
            NetworkMessage msg = new NetworkMessage(0);

            msg.AddByte((byte)Type);

            msg.AddLocation(Position);
            msg.AddByte(Effect);

            return msg.Packet;
        }
    }
}