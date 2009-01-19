﻿//#define _DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics;
using Tibia.Packets;
using Tibia.Objects;
using System.Windows.Forms;

namespace Tibia.Util
{
    public class Proxy
    {
        static byte[] localHostBytes = new byte[] { 127, 0, 0, 1 };
        static Random randon = new Random();

        private Objects.Client client;

        private LoginServer[] loginServers;
        private uint selectedLoginServer = 0;
        public bool IsOtServer { get; set; }

        private TcpListener tcpServer;
        private Socket socketServer;
        private NetworkStream networkStreamServer;
        private byte[] bufferServer = new byte[2];
        private int readBytesServer;
        private int packetSizeServer;
        private bool writingServer;
        private Queue<NetworkMessage> serverSendQueue = new Queue<NetworkMessage> { };
        private Queue<NetworkMessage> serverReceiveQueue = new Queue<NetworkMessage> { };
        private ushort portServer = 7272;
        private bool isFirstMsg;

        private TcpClient tcpClient;
        private NetworkStream networkStreamClient;
        private byte[] bufferClient = new byte[2];
        private int readBytesClient;
        private int packetSizeClient;
        private bool writingClient;
        private DateTime lastClientWrite = DateTime.UtcNow;
        private Queue<NetworkMessage> clientSendQueue = new Queue<NetworkMessage> { };
        private Queue<NetworkMessage> clientReceiveQueue = new Queue<NetworkMessage> { };

        private bool acceptingConnection;
        private CharList[] charList;

        private Objects.Player player;
        private bool isConnected;


#if _DEBUG
        Form debugFrom;
#endif


        #region "Constructor/Deconstructor"

        public Proxy(Client c)
        {
            client = c;

            loginServers = client.LoginServers;
            client.SetServer("localhost", (short)portServer);

            if (client.RSA == Constants.RSAKey.OpenTibia)
                IsOtServer = true;
            else
            {
                client.RSA = Constants.RSAKey.OpenTibia;
                IsOtServer = false;
            }

            if (client.CharListCount != 0)
            {
                charList = client.CharList;
                client.SetCharListServer(localHostBytes, portServer);
            }

            Start();

            //events
            ReceivedSelfAppearIncomingPacket += new IncomingPacketListener(Proxy_ReceivedSelfAppearIncomingPacket);

            client.UsingProxy = true;


#if _DEBUG
            debugFrom = new Form();
            RichTextBox myRichTextBox = new RichTextBox();
            myRichTextBox.Name = "richTextBox";
            myRichTextBox.Dock = DockStyle.Fill;
            debugFrom.Controls.Add(myRichTextBox);
            debugFrom.Disposed += new EventHandler(debugFrom_Disposed);
            PrintDebug += new ProxyNotification(Proxy_PrintDebug);
            debugFrom.Show();
#endif
        }

#if _DEBUG
        void Proxy_PrintDebug(string message)
        {
            if (debugFrom.Disposing)
                return;

            if (debugFrom.InvokeRequired)
            {
                debugFrom.Invoke(new Action<string>(Proxy_PrintDebug), new object[] { message });
                return;
            }

            RichTextBox myRichTextBox = (RichTextBox)debugFrom.Controls["richTextBox"];
            myRichTextBox.AppendText(message + "\n");
            myRichTextBox.Select(myRichTextBox.TextLength - 1, 0);
            myRichTextBox.ScrollToCaret();
        }

        void debugFrom_Disposed(object sender, EventArgs e)
        {
            PrintDebug -= new ProxyNotification(Proxy_PrintDebug); 
        }
#endif

        ~Proxy()
        {
            if (!client.Process.HasExited)
            {
                client.LoginServers = loginServers;

                if (!IsOtServer)
                   client.RSA = Constants.RSAKey.RealTibia;

                if (client.CharListCount != 0 && client.CharListCount == charList.Length)
                {
                    client.SetCharListServer(charList);
                }
            }

            client.UsingProxy = false;
        }

        #endregion

        #region "Events"

        public delegate void ProxyNotification(string message);
        public event ProxyNotification PrintDebug;

        public delegate void MessageListener(NetworkMessage message);
        public event MessageListener ServerMessageArrived;
        public event MessageListener ClientMessageArrived;

        public delegate bool IncomingPacketListener(Packets.IncomingPacket packet);
        public delegate bool OutgoingPacketListener(Packets.OutgoingPacket packet);


        //incoming
        public event IncomingPacketListener ReceivedAnimatedTextIncomingPacket;
        public event IncomingPacketListener ReceivedItemTextWindowIncomingPacket;
        public event IncomingPacketListener ReceivedCreatureSpeakIncomingPacket;
        public event IncomingPacketListener ReceivedOpenChannelIncomingPacket;
        public event IncomingPacketListener ReceivedChannelListIncomingPacket;
        public event IncomingPacketListener ReceivedTextMessageIncomingPacket;
        public event IncomingPacketListener ReceivedPlayerCancelWalkIncomingPacket;
        public event IncomingPacketListener ReceivedTileAddThingIncomingPacket;
        public event IncomingPacketListener ReceivedTileTransformThingIncomingPacket;
        public event IncomingPacketListener ReceivedTileRemoveThingIncomingPacket;
        public event IncomingPacketListener ReceivedCreatureOutfitIncomingPacket;
        public event IncomingPacketListener ReceivedCreatureLightIncomingPacket;
        public event IncomingPacketListener ReceivedCreatureHealthIncomingPacket;
        public event IncomingPacketListener ReceivedCreatureSpeedIncomingPacket;
        public event IncomingPacketListener ReceivedCreatureSquareIncomingPacket;
        public event IncomingPacketListener ReceivedCreatureMoveIncomingPacket;
        public event IncomingPacketListener ReceivedCloseContainerIncomingPacket;
        public event IncomingPacketListener ReceivedContainerAddItemIncomingPacket;
        public event IncomingPacketListener ReceivedContainerRemoveItemIncomingPacket;
        public event IncomingPacketListener ReceivedContainerUpdateItemIncomingPacket;
        public event IncomingPacketListener ReceivedOpenContainerIncomingPacket;
        public event IncomingPacketListener ReceivedWorldLightIncomingPacket;
        public event IncomingPacketListener ReceivedDistanceShotIncomingPacket;
        public event IncomingPacketListener ReceivedMapDescriptionIncomingPacket;
        public event IncomingPacketListener ReceivedMoveNorthIncomingPacket;
        public event IncomingPacketListener ReceivedMoveSouthIncomingPacket;
        public event IncomingPacketListener ReceivedMoveEastIncomingPacket;
        public event IncomingPacketListener ReceivedMoveWestIncomingPacket;
        public event IncomingPacketListener ReceivedSelfAppearIncomingPacket;
        public event IncomingPacketListener ReceivedMagicEffectIncomingPacket;
        public event IncomingPacketListener ReceivedFloorChangeDownIncomingPacket;
        public event IncomingPacketListener ReceivedFloorChangeUpIncomingPacket;
        public event IncomingPacketListener ReceivedPlayerStatsIncomingPacket;
        public event IncomingPacketListener ReceivedCreatureSkullsIncomingPacket;
        public event IncomingPacketListener ReceivedWaitingListIncomingPacket;
        public event IncomingPacketListener ReceivedPingIncomingPacket;
        public event IncomingPacketListener ReceivedDeathIncomingPacket;
        public event IncomingPacketListener ReceivedCanReportBugsIncomingPacket;
        public event IncomingPacketListener ReceivedUpdateTileIncomingPacket;
        public event IncomingPacketListener ReceivedFYIMessageIncomingPacket;
        public event IncomingPacketListener ReceivedInventorySetSlotIncomingPacket;
        public event IncomingPacketListener ReceivedInventoryResetSlotIncomingPacket;
        public event IncomingPacketListener ReceivedSafeTradeRequestAckIncomingPacket;
        public event IncomingPacketListener ReceivedSafeTradeRequestNoAckIncomingPacket;
        public event IncomingPacketListener ReceivedSafeTradeCloseIncomingPacket;
        public event IncomingPacketListener ReceivedPlayerSkillsIncomingPacket;
        public event IncomingPacketListener ReceivedPlayerIconsIncomingPacket;
        public event IncomingPacketListener ReceivedOpenPrivatePlayerChatIncomingPacket;
        public event IncomingPacketListener ReceivedCreatePrivateChannelIncomingPacket;
        public event IncomingPacketListener ReceivedClosePrivateChannelIncomingPacket;
        public event IncomingPacketListener ReceivedVipLogoutIncomingPacket;
        public event IncomingPacketListener ReceivedVipLoginIncomingPacket;
        public event IncomingPacketListener ReceivedVipStateIncomingPacket;
        public event IncomingPacketListener ReceivedShopSaleItemListIncomingPacket;
        public event IncomingPacketListener ReceivedOpenShopWindowIncomingPacket;
        public event IncomingPacketListener ReceivedCloseShopWindowIncomingPacket;
        public event IncomingPacketListener ReceivedOutfitWindowIncomingPacket;
        public event IncomingPacketListener ReceivedRuleViolationsChannelIncomingPacket;
        public event IncomingPacketListener ReceivedRemoveReportIncomingPacket;
        public event IncomingPacketListener ReceivedRuleViolationCancelIncomingPacket;
        public event IncomingPacketListener ReceivedRuleViolationLockIncomingPacket;
        public event IncomingPacketListener ReceivedCancelTargetIncomingPacket;

        //outgoing
        public event OutgoingPacketListener ReceivedCloseChannelOutgoingPacket;
        public event OutgoingPacketListener ReceivedOpenChannelOutgoingPacket;
        public event OutgoingPacketListener ReceivedSayOutgoingPacket;
        public event OutgoingPacketListener ReceivedAttackOutgoingPacket;
        public event OutgoingPacketListener ReceivedFollowOutgoingPacket;
        public event OutgoingPacketListener ReceivedLookAtOutgoingPacket;
        public event OutgoingPacketListener ReceivedUseItemOutgoingPacket;
        public event OutgoingPacketListener ReceivedUseItemExOutgoingPacket;
        public event OutgoingPacketListener ReceivedThrowOutgoingPacket;
        public event OutgoingPacketListener ReceivedCancelMoveOutgoingPacket;
        public event OutgoingPacketListener ReceivedBattleWindowOutgoingPacket;
        public event OutgoingPacketListener ReceivedLogoutOutgoingPacket;
        public event OutgoingPacketListener ReceivedCloseContainerOutgoingPacket;
        public event OutgoingPacketListener ReceivedUpArrowContainerOutgoingPacket;

        private bool Proxy_ReceivedSelfAppearIncomingPacket(IncomingPacket packet)
        {
            isConnected = true;
            return true;
        }

        #endregion

        #region "Properties"

        public Objects.Client Client
        {
            get { return client; }
        }

        public bool Connected
        {
            get { return isConnected; }
        }

        #endregion

  
        public void SendToClient(NetworkMessage msg)
        {
            serverSendQueue.Enqueue(msg);
            ProcessServerSendQueue();
        }

        public void SendToServer(NetworkMessage msg)
        {
            clientSendQueue.Enqueue(msg);
            ProcessClientSendQueue();
        }

        private void Close()
        {

#if _DEBUG
            WRITE_DEBUG("Close Function.");
#endif

            if (tcpClient != null)
                tcpClient.Close();

            if (tcpServer != null)
                tcpServer.Stop();

            if (socketServer != null)
                socketServer.Close();

            acceptingConnection = false;
        }

        private void Restart()
        {
#if _DEBUG
            WRITE_DEBUG("Restart Function.");
#endif

            lock ("acceptingConnection")
            {
                if (acceptingConnection)
                    return;

                isConnected = false;

                Close();
                Start();
            }
        }

        #region "Server"

        public void Start()
        {
#if _DEBUG
            WRITE_DEBUG("Start Function");
#endif

            if (acceptingConnection)
                return;

            acceptingConnection = true;

            serverReceiveQueue.Clear();
            serverSendQueue.Clear();
            clientReceiveQueue.Clear();
            clientSendQueue.Clear();

            tcpServer = new TcpListener(System.Net.IPAddress.Any, portServer);
            tcpServer.Start();
            tcpServer.BeginAcceptSocket((AsyncCallback)SocketAcepted, null);
        }

        private void SocketAcepted(IAsyncResult ar)
        {
#if _DEBUG
            WRITE_DEBUG("OnSocketAcepted Function.");
#endif

            socketServer = tcpServer.EndAcceptSocket(ar);

            if (socketServer.Connected)
                networkStreamServer = new NetworkStream(socketServer);

            acceptingConnection = false;

            isFirstMsg = true;
            networkStreamServer.BeginRead(bufferServer, 0, 2, (AsyncCallback)ServerReadPacket, null);
        }

        private void ServerReadPacket(IAsyncResult ar)
        {
            if (acceptingConnection)
                return;

            try
            {
                readBytesServer = networkStreamServer.EndRead(ar);
            }
            catch (Exception) 
            {
                return;
            }

            if (readBytesServer == 0)
            {
                Restart();
                return;
            }

            packetSizeServer = (int)BitConverter.ToUInt16(bufferServer, 0) + 2;
            NetworkMessage msg = new NetworkMessage(packetSizeServer);
            Array.Copy(bufferServer, msg.GetBuffer(), 2);

            while (readBytesServer < packetSizeServer)
            {
                if (networkStreamServer.CanRead)
                    readBytesServer += networkStreamServer.Read(msg.GetBuffer(), readBytesServer, packetSizeServer - readBytesServer);
                else
                {
                    Restart();
                    return;
                }
            }

            if (ClientMessageArrived != null)
                ClientMessageArrived.BeginInvoke(new NetworkMessage(msg.Packet), null, null);

            if (isFirstMsg)
            {
                isFirstMsg = false;
                ServerParseFirstMsg(msg);
            }
            else
            {
                serverReceiveQueue.Enqueue(msg);
                ProcessServerReceiveQueue();

                if (networkStreamServer.CanRead)
                    networkStreamServer.BeginRead(bufferServer, 0, 2, (AsyncCallback)ServerReadPacket, null);
                else
                    Restart();
            }
        }

        private void ServerParseFirstMsg(NetworkMessage msg)
        {
#if _DEBUG
            WRITE_DEBUG("ServerParseFirstMsg Function.");
#endif

            msg.Position = 6;

            byte protocolId = msg.GetByte();
            uint[] key = new uint[4];

            int pos;

            switch (protocolId)
            {
                case 0x01: //login server

                    ushort osVersion = msg.GetUInt16();
                    ushort clientVersion = msg.GetUInt16();

                    msg.GetUInt32();
                    msg.GetUInt32();
                    msg.GetUInt32();

                    pos = msg.Position;

                    msg.RsaOTDecrypt();

                    if (msg.GetByte() != 0)
                    {
                        //TODO: ...
                    }

                    key[0] = msg.GetUInt32();
                    key[1] = msg.GetUInt32();
                    key[2] = msg.GetUInt32();
                    key[3] = msg.GetUInt32();

                    NetworkMessage.XTEAKey = key;

                    if (clientVersion != 840)
                    {
                        DisconnectClient(0x0A, "This proxy requires client 8.40");
                        return;
                    }

                    try
                    {
                        tcpClient = new TcpClient(loginServers[selectedLoginServer].Server, loginServers[selectedLoginServer].Port);
                        networkStreamClient = tcpClient.GetStream();
                    }
                    catch (Exception)
                    {
                        DisconnectClient(0x0A, "Connection time out.");
                        return;
                    }


                    if (IsOtServer)
                        msg.RsaOTEncrypt(pos);
                    else
                        msg.RsaCipEncrypt(pos);

                    msg.InsertAdler32();
                    msg.InsertPacketHeader();

                    networkStreamClient.BeginWrite(msg.Packet, 0, msg.Length, null, null);
                    networkStreamClient.BeginRead(bufferClient, 0, 2, (AsyncCallback)CharListReceived, null);

                    break;

                case 0x0A: // world server

                    msg.GetUInt16(); //os
                    msg.GetUInt16(); //version

                    pos = msg.Position;

                    msg.RsaOTDecrypt();
                    msg.GetByte();

                    key[0] = msg.GetUInt32();
                    key[1] = msg.GetUInt32();
                    key[2] = msg.GetUInt32();
                    key[3] = msg.GetUInt32();

                    NetworkMessage.XTEAKey = key;

                    msg.GetByte();
                    msg.GetString();
                    string name = msg.GetString();

                    int selectedChar = GetSelectedChar(name);

                    if (selectedChar >= 0)
                    {
                        try
                        {
                            tcpClient = new TcpClient(BitConverter.GetBytes(charList[selectedChar].WorldIP).ToIPString(), charList[selectedChar].WorldPort);
                            networkStreamClient = tcpClient.GetStream();
                        }
                        catch (Exception)
                        {
                            DisconnectClient(0x14, "Connection timeout.");
                            return;
                        }

                        if (IsOtServer)
                            msg.RsaOTEncrypt(pos);
                        else
                            msg.RsaCipEncrypt(pos);

                        msg.InsertAdler32();
                        msg.InsertPacketHeader();

                        networkStreamClient.Write(msg.Packet, 0, msg.Length);

                        networkStreamClient.BeginRead(bufferClient, 0, 2, (AsyncCallback)ClientReadPacket, null);
                        networkStreamServer.BeginRead(bufferServer, 0, 2, (AsyncCallback)ServerReadPacket, null);

                        return;

                    }
                    else
                    {
                        DisconnectClient(0x14, "Unknow character, please relogin..");
                        return;
                    }

                default:
                    {
                        Restart();
                        return;
                    }
            }
        }

        private void CharListReceived(IAsyncResult ar)
        {
#if _DEBUG
            WRITE_DEBUG("OnCharListReceived Function.");
#endif

            readBytesClient = networkStreamClient.EndRead(ar);

            if (readBytesClient == 2)
            {
                packetSizeClient = (int)BitConverter.ToUInt16(bufferClient, 0) + 2;
                NetworkMessage msg = new NetworkMessage(packetSizeClient);
                Array.Copy(bufferClient, msg.GetBuffer(), 2);

                while (readBytesClient < packetSizeClient)
                {
                    if (networkStreamClient.CanRead)
                        readBytesClient += networkStreamClient.Read(msg.GetBuffer(), readBytesClient, packetSizeClient - readBytesClient);
                    else
                        Restart();
                }

                if (ServerMessageArrived != null)
                    ServerMessageArrived.BeginInvoke(new NetworkMessage(msg.Packet), null, null);

                msg.PrepareToRead();
                msg.GetUInt16(); //packet size..

                while (msg.Position < msg.Length)
                {
                    byte cmd = msg.GetByte();

                    switch (cmd)
                    {
                        case 0x0A: //Error message
                            {
                                msg.GetString();
                                break;
                            }
                        case 0x0B: //For your information
                            {
                                msg.GetString();
                                break;
                            }
                        case 0x14: //MOTD
                            {
                                msg.GetString();
                                break;
                            }
                        case 0x1E: //Patching exe/dat/spr messages
                        case 0x1F:
                        case 0x20:
                            {
                                DisconnectClient(0x0A, "A new client are avalible, please download it first!");
                                return;
                            }
                        case 0x28: //Select other login server
                            {
                                selectedLoginServer = (uint)randon.Next(0, loginServers.Length - 1);
                                break;
                            }
                        case 0x64: //character list
                            {
                                int nChar = (int)msg.GetByte();
                                charList = new CharList[nChar];

                                for (int i = 0; i < nChar; i++)
                                {
                                    charList[i].CharName = msg.GetString();
                                    charList[i].WorldName = msg.GetString();
                                    charList[i].WorldIP = msg.PeekUInt32();
                                    msg.AddBytes(localHostBytes);
                                    charList[i].WorldPort = msg.PeekUInt16();
                                    msg.AddUInt16(portServer);
                                }

                                //ushort premmy = msg.GetUInt16();
                                //send this data to client

                                msg.PrepareToSend();

                                if (networkStreamServer.CanWrite)
                                    networkStreamServer.Write(msg.Packet, 0, msg.Length);

                                Restart();
                                return;
                            }
                        default:
                            break;
                    }
                }

                msg.PrepareToSend();
                networkStreamServer.Write(msg.Packet, 0, msg.Length);

                Restart();
                return;

            }
            else
                Restart();

        }

        private void DisconnectClient(byte cmd, string message)
        {
#if _DEBUG
            WRITE_DEBUG("DisconnectClient Function.");
#endif

            NetworkMessage msg = new NetworkMessage();
            msg.AddByte(cmd);
            msg.AddString(message);

            msg.InsetLogicalPacketHeader();
            msg.PrepareToSend();

            networkStreamServer.Write(msg.Packet, 0, msg.Length);

            Restart();
        }

        private void ProcessServerReceiveQueue()
        {
            while (serverReceiveQueue.Count > 0)
            {
                NetworkMessage msg = serverReceiveQueue.Dequeue();
                NetworkMessage output = new NetworkMessage();
                bool haveContent = false;

                msg.PrepareToRead();
                msg.GetUInt16(); //logical packet size

                Objects.Location pos = /*GetPlayerPosition()*/ Location.GetInvalid();

                while (msg.Position < msg.Length)
                {
                    OutgoingPacket packet = ParseServerPacket(msg, pos);

                    if (packet == null)
                    {
#if _DEBUG
                        WRITE_DEBUG("Unknow outgoing packet.. skping the rest! type: " + msg.PeekByte());
#endif

                        //skip the rest...
                        haveContent = true;
                        output.AddBytes(msg.GetBytes(msg.Length - msg.Position));
                        break;
                    }
                    else
                    {
                        if (packet.Forward)
                        {
                            haveContent = true;
                            output.AddBytes(packet.ToByteArray());
                        }
                    }

                }

                if (haveContent)
                {
                    output.InsetLogicalPacketHeader();
                    output.PrepareToSend();
                    clientSendQueue.Enqueue(output);
                    ProcessClientSendQueue();
                }
            }

        }

        private OutgoingPacket ParseServerPacket(NetworkMessage msg, Location pos)
        {
            OutgoingPacket packet;
            OutgoingPacketType type = (OutgoingPacketType)msg.PeekByte();

            switch (type)
            {
                case OutgoingPacketType.ChannelClose:
                    {
                        packet = new Packets.Outgoing.ChannelClosePacket(client);

                        if (packet.ParseMessage(msg, PacketDestination.Server, pos))
                        {
                            if (ReceivedCloseChannelOutgoingPacket != null)
                                packet.Forward = ReceivedCloseChannelOutgoingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case OutgoingPacketType.ChannelOpen:
                    {
                        packet = new Packets.Outgoing.ChannelOpenPacket(client);

                        if (packet.ParseMessage(msg, PacketDestination.Server, pos))
                        {
                            if (ReceivedOpenChannelOutgoingPacket != null)
                                packet.Forward = ReceivedOpenChannelOutgoingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case OutgoingPacketType.PlayerSpeech:
                    {
                        packet = new Packets.Outgoing.PlayerSpeechPacket(client);

                        if (packet.ParseMessage(msg, PacketDestination.Server, pos))
                        {
                            if (ReceivedSayOutgoingPacket != null)
                                packet.Forward = ReceivedSayOutgoingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case OutgoingPacketType.Attack:
                    {
                        packet = new Packets.Outgoing.AttackPacket(client);

                        if (packet.ParseMessage(msg, PacketDestination.Server, pos))
                        {
                            if (ReceivedAttackOutgoingPacket != null)
                                packet.Forward = ReceivedAttackOutgoingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case OutgoingPacketType.Follow:
                    {
                        packet = new Packets.Outgoing.FollowPacket(client);

                        if (packet.ParseMessage(msg, PacketDestination.Server, pos))
                        {
                            if (ReceivedFollowOutgoingPacket != null)
                                packet.Forward = ReceivedFollowOutgoingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case OutgoingPacketType.LookAt:
                    {
                        packet = new Packets.Outgoing.LookAtPacket(client);

                        if (packet.ParseMessage(msg, PacketDestination.Server, pos))
                        {
                            if (ReceivedLookAtOutgoingPacket != null)
                                packet.Forward = ReceivedLookAtOutgoingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case OutgoingPacketType.ItemUse:
                    {
                        packet = new Packets.Outgoing.ItemUsePacket(client);

                        if (packet.ParseMessage(msg, PacketDestination.Server, pos))
                        {
                            if (ReceivedUseItemOutgoingPacket != null)
                                packet.Forward = ReceivedUseItemOutgoingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case OutgoingPacketType.ItemUseOn:
                    {
                        packet = new Packets.Outgoing.ItemUseOnPacket(client);

                        if (packet.ParseMessage(msg, PacketDestination.Server, pos))
                        {
                            if (ReceivedUseItemExOutgoingPacket != null)
                                packet.Forward = ReceivedUseItemExOutgoingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case OutgoingPacketType.ItemMove:
                    {
                        packet = new Packets.Outgoing.ItemMovePacket(client);

                        if (packet.ParseMessage(msg, PacketDestination.Server, pos))
                        {
                            if (ReceivedThrowOutgoingPacket != null)
                                packet.Forward = ReceivedThrowOutgoingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case OutgoingPacketType.CancelMove:
                    {
                        packet = new Packets.Outgoing.CancelMovePacket(client);

                        if (packet.ParseMessage(msg, PacketDestination.Server, pos))
                        {
                            if (ReceivedCancelMoveOutgoingPacket != null)
                                packet.Forward = ReceivedCancelMoveOutgoingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case OutgoingPacketType.ItemUseBattlelist:
                    {
                        packet = new Packets.Outgoing.ItemUseBattlelistPacket(client);

                        if (packet.ParseMessage(msg, PacketDestination.Server, pos))
                        {
                            if (ReceivedBattleWindowOutgoingPacket != null)
                                packet.Forward = ReceivedBattleWindowOutgoingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case OutgoingPacketType.Logout:
                    {
                        packet = new Packets.Outgoing.LogoutPacket(client);

                        if (packet.ParseMessage(msg, PacketDestination.Server, pos))
                        {
                            if (ReceivedLogoutOutgoingPacket != null)
                                packet.Forward = ReceivedLogoutOutgoingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case OutgoingPacketType.ContainerClose:
                    {
                        packet = new Packets.Outgoing.ContainerClosePacket(client);

                        if (packet.ParseMessage(msg, PacketDestination.Server, pos))
                        {
                            if (ReceivedCloseContainerOutgoingPacket != null)
                                packet.Forward = ReceivedCloseContainerOutgoingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case OutgoingPacketType.ContainerOpenParent:
                    {
                        packet = new Packets.Outgoing.ContainerOpenParentPacket(client);

                        if (packet.ParseMessage(msg, PacketDestination.Server, pos))
                        {
                            if (ReceivedUpArrowContainerOutgoingPacket != null)
                                packet.Forward = ReceivedUpArrowContainerOutgoingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                default:
                    break;
            }

            return null;
        }

        private void ProcessServerSendQueue()
        {

            if (writingServer)
                return;

            if (serverSendQueue.Count > 0)
            {
                NetworkMessage msg = serverSendQueue.Dequeue();
                ServerWrite(msg.Packet);
            }
        }

        private void ServerWrite(byte[] buffer)
        {

            if (!writingServer)
            {
                writingServer = true;

                if (networkStreamServer.CanWrite)
                    networkStreamServer.BeginWrite(buffer, 0, buffer.Length, (AsyncCallback)ServerWriteDone, null);
                else
                {
                    //TODO: Handle the error.
                }
            }
        }

        private void ServerWriteDone(IAsyncResult ar)
        {
            networkStreamServer.EndWrite(ar);
            writingServer = false;

            if (serverSendQueue.Count > 0)
                ProcessServerSendQueue();
        }

        #endregion

        #region "Client"

        private void ClientReadPacket(IAsyncResult ar)
        {
            if (acceptingConnection)
                return;

            readBytesClient = networkStreamClient.EndRead(ar);

            if (readBytesClient == 0)
            {
                Restart();
                return;
            }

            packetSizeClient = (int)BitConverter.ToUInt16(bufferClient, 0) + 2;
            NetworkMessage msg = new NetworkMessage(packetSizeClient);
            Array.Copy(bufferClient, msg.GetBuffer(), 2);

            while (readBytesClient < packetSizeClient)
            {
                if (networkStreamClient.CanRead)
                    readBytesClient += networkStreamClient.Read(msg.GetBuffer(), readBytesClient, packetSizeClient - readBytesClient);
                else
                    Restart();
            }

            if (ServerMessageArrived != null)
                ServerMessageArrived.BeginInvoke(new NetworkMessage(msg.Packet), null, null);

            clientReceiveQueue.Enqueue(msg);
            ProcessClientReceiveQueue();

            if (networkStreamClient.CanRead)
                networkStreamClient.BeginRead(bufferClient, 0, 2, (AsyncCallback)ClientReadPacket, null);
            else
                Restart();

        }

        private void ProcessClientReceiveQueue()
        {
            while (clientReceiveQueue.Count > 0)
            {
                NetworkMessage msg = clientReceiveQueue.Dequeue();
                NetworkMessage output = new NetworkMessage();
                bool haveContent = false;

                msg.PrepareToRead();
                msg.GetUInt16(); //logical packet size

                Objects.Location pos = GetPlayerPosition();

                while (msg.Position < msg.Length)
                {
                    IncomingPacket packet = ParseClientPacket(msg, pos);

                    if (packet == null)
                    {
                        WRITE_DEBUG("Unknow incoming packet.. skping the rest! type: " + msg.PeekByte());

                        //skip the rest...
                        haveContent = true;
                        output.AddBytes(msg.GetBytes(msg.Length - msg.Position));
                        break;
                    }
                    else
                    {
                        if (packet.Forward)
                        {
                            haveContent = true;
                            output.AddBytes(packet.ToByteArray());
                        }
                    }

                }

                if (haveContent)
                {
                    output.InsetLogicalPacketHeader();
                    output.PrepareToSend();
                    serverSendQueue.Enqueue(output);
                    ProcessServerSendQueue();
                }
            }
        }

        private void ProcessClientSendQueue()
        {
            if (writingClient)
                return;

            if (clientSendQueue.Count > 0)
            {
                NetworkMessage msg = clientSendQueue.Dequeue();

                if (msg != null)
                    ClientWrite(msg.Packet);
            }
        }

        private void ClientWrite(byte[] buffer)
        {
            if (!writingClient)
            {
                writingClient = true;

                if (lastClientWrite.AddMilliseconds(125) > DateTime.UtcNow)
                    System.Threading.Thread.Sleep(125);

                if (networkStreamClient.CanWrite)
                    networkStreamClient.BeginWrite(buffer, 0, buffer.Length, (AsyncCallback)ClientWriteDone, null);
            }
        }

        private void ClientWriteDone(IAsyncResult ar)
        {
            networkStreamClient.EndWrite(ar);
            writingClient = false;

            if (clientSendQueue.Count > 0)
                ProcessClientSendQueue();
        }

        private IncomingPacket ParseClientPacket(NetworkMessage msg, Objects.Location pos)
        {
            IncomingPacket packet;
            IncomingPacketType type = (IncomingPacketType)msg.PeekByte();

            switch (type)
            {
                case IncomingPacketType.AnimatedText:
                {
#if _DEBUG
                    WRITE_DEBUG("ANIMATED_TEXT");
#endif
                    packet = new Packets.Incoming.AnimatedTextPacket(Client);
                    if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                    {
                        if (ReceivedAnimatedTextIncomingPacket != null)
                            packet.Forward = ReceivedAnimatedTextIncomingPacket.Invoke(packet);

                        return packet;
                    }
                    break;
                }
                case IncomingPacketType.ContainerClose:
                {
#if _DEBUG
                    WRITE_DEBUG("CLOSE_CONTAINER");
#endif
                    packet = new Packets.Incoming.CloseContainerPacket(Client);

                    if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                    {
                        if (ReceivedCloseContainerIncomingPacket != null)
                            packet.Forward = ReceivedCloseContainerIncomingPacket.Invoke(packet);

                        return packet;
                    }
                    break;
                }
                case IncomingPacketType.CreatureSpeak:
                {
#if _DEBUG
                    WRITE_DEBUG("CREATURE_SPEAK");
#endif
                    packet = new Packets.Incoming.CreatureSpeakPacket(Client);

                    if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                    {
                        if (ReceivedCreatureSpeakIncomingPacket != null)
                            packet.Forward = ReceivedCreatureSpeakIncomingPacket.Invoke(packet);

                        return packet;
                    }
                    break;
                }
                case IncomingPacketType.ChannelOpen:
                {
#if _DEBUG
                    WRITE_DEBUG("OPEN_CHANNEL");
#endif
                    packet = new Packets.Incoming.OpenChannelPacket(Client);

                    if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                    {
                        if (ReceivedOpenChannelIncomingPacket != null)
                            packet.Forward = ReceivedOpenChannelIncomingPacket.Invoke(packet);

                        return packet;
                    }
                    break;
                }
                case IncomingPacketType.PlayerCancelWalk:
                    {
#if _DEBUG
                        WRITE_DEBUG("PLAYER_CANCEL_WALK");
#endif
                        packet = new Packets.Incoming.PlayerCancelWalkPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedPlayerCancelWalkIncomingPacket != null)
                                packet.Forward = ReceivedPlayerCancelWalkIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.ChannelList:
                    {
#if _DEBUG
                        WRITE_DEBUG("CHANNEL_LIST");
#endif
                        packet = new Packets.Incoming.ChannelListPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedChannelListIncomingPacket != null)
                                packet.Forward = ReceivedChannelListIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.CreatureMove:
                    {
#if _DEBUG
                        WRITE_DEBUG("CREATURE_MOVE");
#endif
                        packet = new Packets.Incoming.CreatureMovePacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedCreatureMoveIncomingPacket != null)
                                packet.Forward = ReceivedCreatureMoveIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.StatusMessage:
                    {
#if _DEBUG
                        WRITE_DEBUG("TEXT_MESSAGE");
#endif
                        packet = new Packets.Incoming.TextMessagePacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedTextMessageIncomingPacket != null)
                                packet.Forward = ReceivedTextMessageIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.TileAddThing:
                    {
#if _DEBUG
                        WRITE_DEBUG("TILE_ADD_THING");
#endif
                        packet = new Packets.Incoming.TileAddThingPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedTileAddThingIncomingPacket != null)
                                packet.Forward = ReceivedTileAddThingIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.CreatureOutfit:
                    {
#if _DEBUG
                        WRITE_DEBUG("CREATURE_OUTFIT");
#endif
                        packet = new Packets.Incoming.CreatureOutfitPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedCreatureOutfitIncomingPacket != null)
                                packet.Forward = ReceivedCreatureOutfitIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.CreatureLight:
                    {
#if _DEBUG
                        WRITE_DEBUG("CREATURE_LIGHT");
#endif
                        packet = new Packets.Incoming.CreatureLightPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedCreatureLightIncomingPacket != null)
                                packet.Forward = ReceivedCreatureLightIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.CreatureHealth:
                    {
#if _DEBUG
                        WRITE_DEBUG("CREATURE_HEALTH");
#endif
                        packet = new Packets.Incoming.CreatureHealthPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedCreatureHealthIncomingPacket != null)
                                packet.Forward = ReceivedCreatureHealthIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.CreatureSpeed:
                    {
#if _DEBUG
                        WRITE_DEBUG("CREATURE_SPEED");
#endif
                        packet = new Packets.Incoming.CreatureSpeedPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedCreatureSpeedIncomingPacket != null)
                                packet.Forward = ReceivedCreatureSpeedIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.CreatureSquare:
                    {
#if _DEBUG
                        WRITE_DEBUG("CREATURE_SQUARE");
#endif
                        packet = new Packets.Incoming.CreatureSquarePacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedCreatureSquareIncomingPacket != null)
                                packet.Forward = ReceivedCreatureSquareIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.TileTransformThing:
                    {
#if _DEBUG
                        WRITE_DEBUG("TILE_TRANSFORM_THING");
#endif
                        packet = new Packets.Incoming.TileTransformThingPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedTileTransformThingIncomingPacket != null)
                                packet.Forward = ReceivedTileTransformThingIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.TileRemoveThing:
                    {
#if _DEBUG
                        WRITE_DEBUG("TILE_REMOVE_THING");
#endif
                        packet = new Packets.Incoming.TileRemoveThingPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedTileRemoveThingIncomingPacket != null)
                                packet.Forward = ReceivedTileRemoveThingIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.ContainerAddItem:
                    {
#if _DEBUG
                        WRITE_DEBUG("CONTAINER_ADD_ITEM");
#endif
                        packet = new Packets.Incoming.ContainerAddItemPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedContainerAddItemIncomingPacket != null)
                                packet.Forward = ReceivedContainerAddItemIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.ContainerRemoveItem:
                    {
#if _DEBUG
                        WRITE_DEBUG("CONTAINER_REMOVE_ITEM");
#endif
                        packet = new Packets.Incoming.ContainerRemoveItemPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedContainerRemoveItemIncomingPacket != null)
                                packet.Forward = ReceivedContainerRemoveItemIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.ContainerUpdateItem:
                    {
#if _DEBUG
                        WRITE_DEBUG("CONTAINER_UPDATE_ITEM");
#endif
                        packet = new Packets.Incoming.ContainerUpdateItemPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedContainerUpdateItemIncomingPacket != null)
                                packet.Forward = ReceivedContainerUpdateItemIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.ContainerOpen:
                    {
#if _DEBUG
                        WRITE_DEBUG("OPEN_CONTAINER");
#endif
                        packet = new Packets.Incoming.OpenContainerPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedOpenContainerIncomingPacket != null)
                                packet.Forward = ReceivedOpenContainerIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.ItemTextWindow:
                    {
#if _DEBUG
                        WRITE_DEBUG("ITEM_TEXT_WINDOW");
#endif
                        packet = new Packets.Incoming.ItemTextWindowPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedItemTextWindowIncomingPacket != null)
                                packet.Forward = ReceivedItemTextWindowIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.WorldLight:
                    {
#if _DEBUG
                        WRITE_DEBUG("WORLD_LIGHT");
#endif
                        packet = new Packets.Incoming.WorldLightPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedWorldLightIncomingPacket != null)
                                packet.Forward = ReceivedWorldLightIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.Projectile:
                    {
#if _DEBUG
                        WRITE_DEBUG("DISTANCE_SHOT");
#endif
                        packet = new Packets.Incoming.DistanceShotPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedDistanceShotIncomingPacket != null)
                                packet.Forward = ReceivedDistanceShotIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }

                case IncomingPacketType.MapDescription:
                    {
#if _DEBUG
                        WRITE_DEBUG("MAP_DESCRIPTION");
#endif
                        packet = new Packets.Incoming.MapDescriptionPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedMapDescriptionIncomingPacket != null)
                                packet.Forward = ReceivedMapDescriptionIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.MoveNorth:
                    {
#if _DEBUG
                        WRITE_DEBUG("MOVE_NORTH");
#endif
                        packet = new Packets.Incoming.MoveNorthPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedMoveNorthIncomingPacket != null)
                                packet.Forward = ReceivedMoveNorthIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.MoveSouth:
                    {
#if _DEBUG
                        WRITE_DEBUG("MOVE_SOUTH");
#endif
                        packet = new Packets.Incoming.MoveSouthPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedMoveSouthIncomingPacket != null)
                                packet.Forward = ReceivedMoveSouthIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.MoveEast:
                    {
#if _DEBUG
                        WRITE_DEBUG("MOVE_EAST");
#endif
                        packet = new Packets.Incoming.MoveEastPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedMoveEastIncomingPacket != null)
                                packet.Forward = ReceivedMoveEastIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.MoveWest:
                    {
#if _DEBUG
                        WRITE_DEBUG("MOVE_WEST");
#endif
                        packet = new Packets.Incoming.MoveWestPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedMoveWestIncomingPacket != null)
                                packet.Forward = ReceivedMoveWestIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.SelfAppear:
                    {
#if _DEBUG
                        WRITE_DEBUG("SELF_APPEAR");
#endif
                        packet = new Packets.Incoming.SelfAppearPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedSelfAppearIncomingPacket != null)
                                packet.Forward = ReceivedSelfAppearIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.MagicEffect:
                    {
#if _DEBUG
                        WRITE_DEBUG("MAGIC_EFFECT");
#endif
                        packet = new Packets.Incoming.MagicEffectPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedMagicEffectIncomingPacket != null)
                                packet.Forward = ReceivedMagicEffectIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.FloorChangeDown:
                    {
#if _DEBUG
                        WRITE_DEBUG("FLOOR_CHANGE_DOWN");
#endif
                        packet = new Packets.Incoming.FloorChangeDownPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedFloorChangeDownIncomingPacket != null)
                                packet.Forward = ReceivedFloorChangeDownIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.FloorChangeUp:
                    {
#if _DEBUG
                        WRITE_DEBUG("FLOOR_CHANGE_UP");
#endif
                        packet = new Packets.Incoming.FloorChangeUpPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedFloorChangeUpIncomingPacket != null)
                                packet.Forward = ReceivedFloorChangeUpIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.PlayerStatusUpdate:
                    {
#if _DEBUG
                        WRITE_DEBUG("PLAYER_STATS");
#endif
                        packet = new Packets.Incoming.PlayerStatsPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedPlayerStatsIncomingPacket != null)
                                packet.Forward = ReceivedPlayerStatsIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.CreatureSkull:
                    {
#if _DEBUG
                        WRITE_DEBUG("CREATURE_SKULLS");
#endif
                        packet = new Packets.Incoming.CreatureSkullsPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedCreatureSkullsIncomingPacket != null)
                                packet.Forward = ReceivedCreatureSkullsIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.WaitingList:
                    {
#if _DEBUG
                        WRITE_DEBUG("WAITING_LIST");
#endif
                        packet = new Packets.Incoming.WaitingListPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedWaitingListIncomingPacket != null)
                                packet.Forward = ReceivedWaitingListIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.Ping:
                    {
#if _DEBUG
                        WRITE_DEBUG("PING");
#endif
                        packet = new Packets.Incoming.PingPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedPingIncomingPacket != null)
                                packet.Forward = ReceivedPingIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.Death:
                    {
#if _DEBUG
                        WRITE_DEBUG("DEATH");
#endif
                        packet = new Packets.Incoming.DeathPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedDeathIncomingPacket != null)
                                packet.Forward = ReceivedDeathIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.CanReportBugs:
                    {
#if _DEBUG
                        WRITE_DEBUG("DEATH");
#endif
                        packet = new Packets.Incoming.CanReportBugsPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedCanReportBugsIncomingPacket != null)
                                packet.Forward = ReceivedCanReportBugsIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.UpdateTile:
                    {
#if _DEBUG
                        WRITE_DEBUG("UPDATE_TILE");
#endif
                        packet = new Packets.Incoming.UpdateTilePacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedUpdateTileIncomingPacket != null)
                                packet.Forward = ReceivedUpdateTileIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.FyiMessage:
                    {
#if _DEBUG
                        WRITE_DEBUG("FYI_MESSAGE");
#endif
                        packet = new Packets.Incoming.FYIMessagePacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedFYIMessageIncomingPacket != null)
                                packet.Forward = ReceivedFYIMessageIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.InventorySetSlot:
                    {
#if _DEBUG
                        WRITE_DEBUG("INVENTORY_SET_SLOT");
#endif
                        packet = new Packets.Incoming.InventorySetSlotPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedInventorySetSlotIncomingPacket != null)
                                packet.Forward = ReceivedInventorySetSlotIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.InventoryResetSlot:
                    {
#if _DEBUG
                        WRITE_DEBUG("INVENTORY_RESET_SLOT");
#endif
                        packet = new Packets.Incoming.InventoryResetSlotPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedInventoryResetSlotIncomingPacket != null)
                                packet.Forward = ReceivedInventoryResetSlotIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.SafeTradeRequestAck:
                    {
#if _DEBUG
                        WRITE_DEBUG("SAFE_TRADE_REQUEST_ACK");
#endif
                        packet = new Packets.Incoming.SafeTradeRequestAckPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedSafeTradeRequestAckIncomingPacket != null)
                                packet.Forward = ReceivedSafeTradeRequestAckIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.SafeTradeRequestNoAck:
                    {
#if _DEBUG
                        WRITE_DEBUG("SAFE_TRADE_REQUEST_NO_ACK");
#endif
                        packet = new Packets.Incoming.SafeTradeRequestNoAckPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedSafeTradeRequestNoAckIncomingPacket != null)
                                packet.Forward = ReceivedSafeTradeRequestNoAckIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.SafeTradeClose:
                    {
#if _DEBUG
                        WRITE_DEBUG("SAFE_TRADE_CLOSE");
#endif
                        packet = new Packets.Incoming.SafeTradeClosePacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedSafeTradeCloseIncomingPacket != null)
                                packet.Forward = ReceivedSafeTradeCloseIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.PlayerSkillsUpdate:
                    {
#if _DEBUG
                        WRITE_DEBUG("PLAYER_SKILLS");
#endif
                        packet = new Packets.Incoming.PlayerSkillsPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedPlayerSkillsIncomingPacket != null)
                                packet.Forward = ReceivedPlayerSkillsIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.PlayerFlagUpdate:
                    {
#if _DEBUG
                        WRITE_DEBUG("PLAYER_ICONS");
#endif
                        packet = new Packets.Incoming.PlayerIconsPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedPlayerIconsIncomingPacket != null)
                                packet.Forward = ReceivedPlayerIconsIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.ChannelOpenPrivate:
                    {
#if _DEBUG
                        WRITE_DEBUG("OPEN_PRIVATE_PLAYER_CHAT");
#endif
                        packet = new Packets.Incoming.OpenPrivatePlayerChatPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedOpenPrivatePlayerChatIncomingPacket != null)
                                packet.Forward = ReceivedOpenPrivatePlayerChatIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.PrivateChannelCreate:
                    {
#if _DEBUG
                        WRITE_DEBUG("CREATE_PRIVATE_CHANNEL");
#endif
                        packet = new Packets.Incoming.CreatePrivateChannelPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedCreatePrivateChannelIncomingPacket != null)
                                packet.Forward = ReceivedCreatePrivateChannelIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.PrivateChannelClose:
                    {
#if _DEBUG
                        WRITE_DEBUG("CLOSE_PRIVATE_CHANNEL");
#endif
                        packet = new Packets.Incoming.ClosePrivateChannelPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedClosePrivateChannelIncomingPacket != null)
                                packet.Forward = ReceivedClosePrivateChannelIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.VipState:
                    {
#if _DEBUG
                        WRITE_DEBUG("VIP_STATE");
#endif
                        packet = new Packets.Incoming.VipStatePacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedVipStateIncomingPacket != null)
                                packet.Forward = ReceivedVipStateIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.VipLogin:
                    {
#if _DEBUG
                        WRITE_DEBUG("VIP_LOGIN");
#endif
                        packet = new Packets.Incoming.VipLoginPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedVipLoginIncomingPacket != null)
                                packet.Forward = ReceivedVipLoginIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.VipLogout:
                    {
#if _DEBUG
                        WRITE_DEBUG("VIP_LOGOUT");
#endif
                        packet = new Packets.Incoming.VipLogoutPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedVipLogoutIncomingPacket != null)
                                packet.Forward = ReceivedVipLogoutIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.ShopSaleGoldCount:
                    {
#if _DEBUG
                        WRITE_DEBUG("SHOP_SALE_ITEM_LIST");
#endif
                        packet = new Packets.Incoming.ShopSaleItemListPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedShopSaleItemListIncomingPacket != null)
                                packet.Forward = ReceivedShopSaleItemListIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.ShopWindowOpen:
                    {
#if _DEBUG
                        WRITE_DEBUG("OPEN_SHOP_WINDOW");
#endif
                        packet = new Packets.Incoming.OpenShopWindowPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedOpenShopWindowIncomingPacket != null)
                                packet.Forward = ReceivedOpenShopWindowIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.ShopWindowClose:
                    {
#if _DEBUG
                        WRITE_DEBUG("CLOSE_SHOP_WINDOW");
#endif
                        packet = new Packets.Incoming.CloseShopWindowPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedCloseShopWindowIncomingPacket != null)
                                packet.Forward = ReceivedCloseShopWindowIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.OutfitWindow:
                    {
#if _DEBUG
                        WRITE_DEBUG("OUTFIT_WINDOW");
#endif
                        packet = new Packets.Incoming.OutfitWindowPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedOutfitWindowIncomingPacket != null)
                                packet.Forward = ReceivedOutfitWindowIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.RuleViolationOpen:
                    {
#if _DEBUG
                        WRITE_DEBUG("RULE_VIOLATIONS_CHANNEL");
#endif
                        packet = new Packets.Incoming.RuleViolationsChannelPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedRuleViolationsChannelIncomingPacket != null)
                                packet.Forward = ReceivedRuleViolationsChannelIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.RemoveReport:
                    {
#if _DEBUG
                        WRITE_DEBUG("REMOVE_REPORT");
#endif
                        packet = new Packets.Incoming.RemoveReportPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedRemoveReportIncomingPacket != null)
                                packet.Forward = ReceivedRemoveReportIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.RuleViolationCancel:
                    {
#if _DEBUG
                        WRITE_DEBUG("RULE_VIOLATION_CANCEL");
#endif
                        packet = new Packets.Incoming.RuleViolationCancelPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedRuleViolationCancelIncomingPacket != null)
                                packet.Forward = ReceivedRuleViolationCancelIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.RuleViolationLock:
                    {
#if _DEBUG
                        WRITE_DEBUG("RULE_VIOLATION_LOCK");
#endif
                        packet = new Packets.Incoming.RuleViolationLockPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedRuleViolationLockIncomingPacket != null)
                                packet.Forward = ReceivedRuleViolationLockIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                case IncomingPacketType.CancelTarget:
                    {
#if _DEBUG
                        WRITE_DEBUG("CANCEL_TARGET");
#endif
                        packet = new Packets.Incoming.CancelTargetPacket(Client);

                        if (packet.ParseMessage(msg, PacketDestination.Client, pos))
                        {
                            if (ReceivedCancelTargetIncomingPacket != null)
                                packet.Forward = ReceivedCancelTargetIncomingPacket.Invoke(packet);

                            return packet;
                        }
                        break;
                    }
                default:
                    break;
            }

            return null;
        }

        #endregion

        #region "Debug"

        private void WRITE_DEBUG(string message)
        {
            if (PrintDebug != null)
                PrintDebug.BeginInvoke(message, null, null);
        }

        #endregion

        #region "Other Functions"

        private int GetSelectedChar(string name)
        {
            for (int i = 0; i < charList.Length; i++)
            {
                if (charList[i].CharName == name)
                    return i;
            }

            return -1;
        }

        public Objects.Player GetPlayer()
        {
            try
            {
                if (player == null)
                    player = client.GetPlayer();
            }
            catch (Exception) { }

            return player;
        }

        public Objects.Location GetPlayerPosition()
        {
            Location pos = Location.GetInvalid();

            try
            {
                pos = GetPlayer().Location;
            }
            catch (Exception) { }

            return pos;
        }

        #endregion

    }
}
