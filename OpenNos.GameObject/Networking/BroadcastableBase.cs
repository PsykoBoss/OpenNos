﻿/*
 * This file is part of the OpenNos Emulator Project. See AUTHORS file for Copyright information
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

using OpenNos.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenNos.GameObject
{
    public abstract class BroadcastableBase
    {
        #region Members

        /// <summary>
        /// List of all connected clients.
        /// </summary>
        private readonly ThreadSafeSortedList<long, ClientSession> _sessions;

        private bool _disposed;

        #endregion

        #region Instantiation

        public BroadcastableBase()
        {
            LastUnregister = DateTime.Now.AddMinutes(-1);
            _sessions = new ThreadSafeSortedList<long, ClientSession>();
        }

        #endregion

        #region Properties

        public DateTime LastUnregister { get; set; }

        public int SessionCount
        {
            get
            {
                return _sessions.Count;
            }
        }

        public IEnumerable<ClientSession> Sessions
        {
            get
            {
                return _sessions.GetAllItems().Where(s => s.HasSelectedCharacter && !s.IsDisposing && s.IsConnected);
            }
        }

        #endregion

        #region Methods

        public void Broadcast(string packet)
        {
            Broadcast(null, packet);
        }

        public void Broadcast(string packet, int xRangeCoordinate, int yRangeCoordinate)
        {
            Broadcast(new BroadcastPacket(null, packet, ReceiverType.AllInRange, xCoordinate: xRangeCoordinate, yCoordinate: yRangeCoordinate));
        }

        public void Broadcast(string[] packets)
        {
            Broadcast(null, packets);
        }

        public void Broadcast(string[] packets, int xRangeCoordinate, int yRangeCoordinate)
        {
            Broadcast(packets.Select(p => new BroadcastPacket(null, p, ReceiverType.AllInRange, xCoordinate: xRangeCoordinate, yCoordinate: yRangeCoordinate)));
        }

        public void Broadcast(PacketDefinition packet)
        {
            Broadcast(null, packet);
        }

        public void Broadcast(PacketDefinition packet, int xRangeCoordinate, int yRangeCoordinate)
        {
            Broadcast(new BroadcastPacket(null, PacketFactory.Serialize(packet), ReceiverType.AllInRange, xCoordinate: xRangeCoordinate, yCoordinate: yRangeCoordinate));
        }

        public void Broadcast(ClientSession client, PacketDefinition packet, ReceiverType receiver = ReceiverType.All, string characterName = "", long characterId = -1)
        {
            Broadcast(client, PacketFactory.Serialize(packet), receiver, characterName, characterId);
        }

        public void Broadcast(ClientSession client, string[] packets, ReceiverType receiver = ReceiverType.All, string characterName = "", long characterId = -1)
        {
            try
            {
                foreach (string packet in packets)
                {
                    SpreadBroadcastpacket(new BroadcastPacket(client, packet, receiver, characterName, characterId));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public void Broadcast(IEnumerable<BroadcastPacket> packets)
        {
            try
            {
                SpreadBroadcasts(packets);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public void Broadcast(BroadcastPacket packet)
        {
            try
            {
                SpreadBroadcastpacket(packet);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public void Broadcast(ClientSession client, string content, ReceiverType receiver = ReceiverType.All, string characterName = "", long characterId = -1)
        {
            try
            {
                SpreadBroadcastpacket(new BroadcastPacket(client, content, receiver, characterName, characterId));
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public virtual void Dispose()
        {
            if (!_disposed)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
                _disposed = true;
            }
        }

        public ClientSession GetSessionByCharacterId(long characterId)
        {
            if (_sessions.ContainsKey(characterId))
            {
                return _sessions[characterId];
            }

            return null;
        }

        public virtual void RegisterSession(ClientSession session)
        {
            if (!session.HasSelectedCharacter)
            {
                return;
            }

            if (session != null)
            {
                // Create a ChatClient and store it in a collection
                _sessions[session.Character.CharacterId] = session;
            }
        }

        public void SpreadBroadcastpacket(BroadcastPacket sentPacket)
        {
            if (sentPacket != null && !String.IsNullOrEmpty(sentPacket.Packet))
            {
                switch (sentPacket.Receiver)
                {
                    case ReceiverType.All: // send packet to everyone
                        foreach (ClientSession session in Sessions)
                        {
                            session.SendPacket(sentPacket.Packet);
                        }
                        break;

                    case ReceiverType.AllExceptMe: // send to everyone except the sender
                        foreach (ClientSession session in Sessions.Where(s => s.SessionId != sentPacket.Sender.SessionId))
                        {
                            session.SendPacket(sentPacket.Packet);
                        }
                        break;

                    case ReceiverType.AllInRange: // send to everyone which is in a range of 50x50
                        if (sentPacket.XCoordinate != 0 && sentPacket.YCoordinate != 0)
                        {
                            foreach (ClientSession session in Sessions.Where(s => s.Character.IsInRange(sentPacket.XCoordinate, sentPacket.YCoordinate)))
                            {
                                session.SendPacket(sentPacket.Packet, 1);
                            }
                        }
                        break;

                    case ReceiverType.OnlySomeone:
                        {
                            if (sentPacket.SomeonesCharacterId > 0 || !String.IsNullOrEmpty(sentPacket.SomeonesCharacterName))
                            {
                                ClientSession targetSession = Sessions.SingleOrDefault(s => s.Character.CharacterId == sentPacket.SomeonesCharacterId || s.Character.Name == sentPacket.SomeonesCharacterName);

                                if (targetSession != null)
                                {
                                    targetSession.SendPacket(sentPacket.Packet);
                                }
                            }

                            break;
                        }
                    case ReceiverType.AllNoEmoBlocked:
                        foreach (ClientSession session in Sessions.Where(s => !s.Character.EmoticonsBlocked))
                        {
                            session.SendPacket(sentPacket.Packet);
                        }
                        break;

                    case ReceiverType.AllNoHeroBlocked:
                        foreach (ClientSession session in Sessions.Where(s => !s.Character.HeroChatBlocked))
                        {
                            session.SendPacket(sentPacket.Packet);
                        }
                        break;

                    case ReceiverType.Group:
                        foreach (ClientSession session in Sessions.Where(s => s.Character != null && s.Character.Group != null
                                 && sentPacket.Sender != null && sentPacket.Sender.Character != null && sentPacket.Sender.Character.Group != null
                                 && s.Character.Group.GroupId == sentPacket.Sender.Character.Group.GroupId))
                        {
                            session.SendPacket(sentPacket.Packet);
                        }
                        break;
                }
            }
        }

        public void SpreadBroadcasts(IEnumerable<BroadcastPacket> sentpackets)
        {
            foreach (BroadcastPacket broadcastPacket in sentpackets)
            {
                SpreadBroadcastpacket(broadcastPacket);
            }
        }

        public virtual void UnregisterSession(long characterId)
        {
            // Get client from client list, if not in list do not continue
            var session = _sessions[characterId];
            if (session == null)
            {
                return;
            }

            // Remove client from online clients list
            _sessions.Remove(characterId);

            LastUnregister = DateTime.Now;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _sessions.Dispose();
            }
        }

        #endregion
    }
}