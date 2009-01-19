﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tibia.Packets.Incoming
{
    public class CreatureLightPacket : IncomingPacket
    {
        public uint CreatureId { get; set; }
        public byte LightColor { get; set; }
        public byte LightLevel { get; set; }

        public CreatureLightPacket(Objects.Client c)
            : base(c)
        {
            Type = IncomingPacketType.CreatureLight;
            Destination = PacketDestination.Client;
        }

        public override bool ParseMessage(NetworkMessage msg, PacketDestination destination)
        {
            int position = msg.Position;

            if (msg.GetByte() != (byte)IncomingPacketType.CreatureLight)
                return false;

            Destination = destination;
            Type = IncomingPacketType.CreatureLight;

            try
            {
                CreatureId = msg.GetUInt32();
                LightLevel = msg.GetByte();
                LightColor = msg.GetByte();
            }
            catch (Exception)
            {
                msg.Position = position;
                return false;
            }

            return true;
        }

        public override byte[] ToByteArray()
        {
            NetworkMessage msg = new NetworkMessage(Client, 0);

            msg.AddByte((byte)Type);

            msg.AddUInt32(CreatureId);
            msg.AddByte(LightLevel);
            msg.AddByte(LightColor);

            return msg.Packet;
        }
    }
}