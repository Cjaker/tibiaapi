﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tibia.Packets.Incoming
{
    public class VipLogoutPacket : IncomingPacket
    {

        public uint PlayerId { get; set; }

        public VipLogoutPacket(Objects.Client c)
            : base(c)
        {
            Type = IncomingPacketType.VipLogout;
            Destination = PacketDestination.Client;
        }

        public override bool ParseMessage(NetworkMessage msg, PacketDestination destination)
        {
            int position = msg.Position;

            if (msg.GetByte() != (byte)IncomingPacketType.VipLogout)
                return false;

            Destination = destination;
            Type = IncomingPacketType.VipLogout;

            try
            {
                PlayerId = msg.GetUInt32();
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

            msg.AddUInt32(PlayerId);

            return msg.Packet;
        }
    }
}