﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tibia.Objects
{
    public class ContextMenu
    {
        Client client;

        public ContextMenu(Client client)
        {
            this.client = client;
        }

        /// <summary>
        /// Call this function only if you know what you are doing
        /// </summary>
        public void AddInternalEvents()
        {
            client.Pipe.OnReceive += new Tibia.Util.Pipe.PipeListener(Pipe_OnReceive);
        }

        private void Pipe_OnReceive(Tibia.Packets.NetworkMessage msg)
        {
            if (msg.GetUInt16() == 5 && msg.GetByte() == (byte)Packets.PipePacketType.OnClickContextMenu)
            {
                //raise the event
                if (Click != null)
                    Click.BeginInvoke((int)msg.GetUInt32(), null, null);
            }
        }

        public bool AddContextMenu(int eventId, string text, Constants.ContextMenuType type, bool hasSeparator)
        {
            if (client.Pipe == null)
            {
                client.InitializePipe();
                client.PipeIsReady.WaitOne();
            }

            if (eventId < 0 || eventId > 2000 || text == string.Empty)
                return false;

            return Packets.Pipes.AddContextMenuPacket.Send(client, eventId, text, type, hasSeparator);
        }

        public bool RemoveContextMenu(int eventId, string text, Constants.ContextMenuType type, bool hasSeparator)
        {
            if (client.Pipe == null)
            {
                client.InitializePipe();
                client.PipeIsReady.WaitOne();
            }

            if (eventId < 0 || eventId > 2000 || text == string.Empty)
                return false;

            return Packets.Pipes.RemoveContextMenuPacket.Send(client, eventId, text, type, hasSeparator);
        }

        public void RemoveAll()
        {
            if (client.Pipe == null)
            {
                client.InitializePipe();
                client.PipeIsReady.WaitOne();
            }

            Packets.Pipes.RemoveAllContextMenusPacket.Send(client);
        }


        /// <summary>
        /// A generic function prototype for context menu events.
        /// </summary>
        public delegate void ContextMenuEvent(int eventId);

        /// <summary>
        /// Called when the context menu is clicked.
        /// </summary>
        public event ContextMenuEvent Click;
    }
}
