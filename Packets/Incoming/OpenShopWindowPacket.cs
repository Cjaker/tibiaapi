﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tibia.Packets.Incoming
{
    public class OpenShopWindowPacket : IncomingPacket
    {

        public List<ShopInfo> ShopList { get; set; }

        public OpenShopWindowPacket(Objects.Client c)
            : base(c)
        {
            Type = IncomingPacketType.ShopWindowOpen;
            Destination = PacketDestination.Client;
        }

        public override bool ParseMessage(NetworkMessage msg, PacketDestination destination, Objects.Location pos)
        {
            if (msg.GetByte() != (byte)IncomingPacketType.ShopWindowOpen)
                return false;

            Destination = destination;
            Type = IncomingPacketType.ShopWindowOpen;

            byte cap = msg.GetByte();
            ShopList = new List<ShopInfo> { };

            for (int i = 0; i < cap; i++)
            {
                ShopInfo item = new ShopInfo();

                item.ItemId = msg.GetUInt16();
                item.SubType = msg.GetByte();
                item.ItemName = msg.GetString();
                item.Weight = msg.GetUInt32();
                item.BuyPrice = msg.GetUInt32();
                item.SellPrice = msg.GetUInt32();
                ShopList.Add(item);
            }

            return true;
        }

        public override byte[] ToByteArray()
        {
            NetworkMessage msg = new NetworkMessage(0);

            msg.AddByte((byte)Type);

            msg.AddByte((byte)ShopList.Count);

            foreach (ShopInfo i in ShopList)
            {
                msg.AddUInt16(i.ItemId);
                msg.AddByte(i.SubType);
                msg.AddString(i.ItemName);
                msg.AddUInt32(i.Weight);
                msg.AddUInt32(i.BuyPrice);
                msg.AddUInt32(i.SellPrice);
            }

            return msg.Packet;
        }
    }
}