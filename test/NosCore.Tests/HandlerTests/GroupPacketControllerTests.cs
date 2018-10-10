﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using log4net;
using log4net.Config;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Configuration;
using NosCore.Controllers;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.Networking;
using NosCore.Core.Serializing;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.Database;
using NosCore.DAL;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Group;
using NosCore.Shared.Enumerations.Map;
using NosCore.Shared.I18N;
using Character = NosCore.GameObject.Character;

namespace NosCore.Tests.HandlerTests
{
    [TestClass]
    public class GroupPacketControllerTests
    {
        private const string ConfigurationPath = "../../../configuration";
        //private readonly List<ClientSession> _sessions = new List<ClientSession>();
        private readonly List<AccountDTO> _accounts = new List<AccountDTO>();
        private readonly List<CharacterDTO> _characters = new List<CharacterDTO>();
        private readonly List<GroupPacketController> _handlers = new List<GroupPacketController>();

        [TestInitialize]
        public void Setup()
        {
            PacketFactory.Initialize<NoS0575Packet>();
            var builder = new ConfigurationBuilder();
            var contextBuilder = new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString());
            DataAccessHelper.Instance.InitializeForTest(contextBuilder.Options);
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo(ConfigurationPath + "/log4net.config"));
            Logger.InitializeLogger(LogManager.GetLogger(typeof(GroupPacketControllerTests)));
            var map = new MapDTO { MapId = 1 };
            DAOFactory.MapDAO.InsertOrUpdate(ref map);

            ServerManager.Instance.Sessions = new ConcurrentDictionary<long, ClientSession>();
            ServerManager.Instance.Groups = new ConcurrentDictionary<long, Group>();
            // So we don't have to modify this for raid tests
            for (byte i = 0; i < (byte)GroupType.GiantTeam; i++)
            {
                ServerManager.Instance.Sessions.TryAdd(i, new ClientSession(null, new List<PacketController>() { new GroupPacketController() }, null));
                var session = ServerManager.Instance.Sessions.Values.ElementAt(i);

                _accounts.Add(new AccountDTO { Name = $"AccountTest{i}", Password = EncryptionHelper.Sha512("test") });
                var acc = _accounts.ElementAt(i);
                DAOFactory.AccountDAO.InsertOrUpdate(ref acc);

                _characters.Add(new CharacterDTO
                {
                    Name = $"TestExistingCharacter{i}",
                    Slot = 1,
                    AccountId = acc.AccountId,
                    MapId = 1,
                    State = CharacterState.Active
                });

                var chara = _characters.ElementAt(i);

                DAOFactory.CharacterDAO.InsertOrUpdate(ref chara);
                session.InitializeAccount(acc);
                _handlers.Add(new GroupPacketController());
                var handler = _handlers.ElementAt(i);
                handler.RegisterSession(session);

                session.SetCharacter(chara.Adapt<Character>());
                session.Character.MapInstance = new MapInstance(new Map(), Guid.NewGuid(), true, MapInstanceType.BaseMapInstance, null);
            }
        }

        [TestMethod]
        public void Test_Accept_Group_Join()
        {
            ServerManager.Instance.Sessions.Values.ElementAt(1).Character.GroupRequestCharacterIds.Add(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.CharacterId);

            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = ServerManager.Instance.Sessions.Values.ElementAt(1).Character.CharacterId
            };

            _handlers.ElementAt(0).ManageGroup(pjoinPacket);
            Assert.IsTrue(!ServerManager.Instance.Sessions.Values.ElementAt(0).Character.Group.IsEmpty && !ServerManager.Instance.Sessions.Values.ElementAt(1).Character.Group.IsEmpty && ServerManager.Instance.Sessions.Values.ElementAt(0).Character.Group.GroupId == ServerManager.Instance.Sessions.Values.ElementAt(1).Character.Group.GroupId);
        }

        [TestMethod]
        public void Test_Join_Full_Group()
        {
            PjoinPacket pjoinPacket;

            for (var i = 0; i < 3; i++)
            {
                ServerManager.Instance.Sessions.Values.ElementAt(i).Character.GroupRequestCharacterIds.Add(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.CharacterId);

                pjoinPacket = new PjoinPacket
                {
                    RequestType = GroupRequestType.Accepted,
                    CharacterId = ServerManager.Instance.Sessions.Values.ElementAt(i).Character.CharacterId
                };

                _handlers.ElementAt(0).ManageGroup(pjoinPacket);
            }

            Assert.IsTrue(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.Group.IsGroupFull && ServerManager.Instance.Sessions.Values.ElementAt(1).Character.Group.IsGroupFull && ServerManager.Instance.Sessions.Values.ElementAt(2).Character.Group.IsGroupFull);

            ServerManager.Instance.Sessions.Values.ElementAt(3).Character.GroupRequestCharacterIds.Add(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.CharacterId);

            pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = ServerManager.Instance.Sessions.Values.ElementAt(3).Character.CharacterId
            };

            _handlers.ElementAt(0).ManageGroup(pjoinPacket);
            Assert.IsTrue(ServerManager.Instance.Sessions.Values.ElementAt(3).Character.Group.IsEmpty);
        }

        [TestMethod]
        public void Test_Leave_Group()
        {
            ServerManager.Instance.Sessions.Values.ElementAt(1).Character.GroupRequestCharacterIds.Add(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.CharacterId);

            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = ServerManager.Instance.Sessions.Values.ElementAt(1).Character.CharacterId
            };

            _handlers.ElementAt(0).ManageGroup(pjoinPacket);

            ServerManager.Instance.Sessions.Values.ElementAt(2).Character.GroupRequestCharacterIds.Add(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.CharacterId);

            pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = ServerManager.Instance.Sessions.Values.ElementAt(2).Character.CharacterId
            };

            _handlers.ElementAt(0).ManageGroup(pjoinPacket);

            Assert.IsTrue(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.Group.IsGroupFull && ServerManager.Instance.Sessions.Values.ElementAt(1).Character.Group.IsGroupFull && ServerManager.Instance.Sessions.Values.ElementAt(2).Character.Group.IsGroupFull);

            _handlers.ElementAt(1).LeaveGroup(new PleavePacket());

            Assert.IsTrue(ServerManager.Instance.Sessions.Values.ElementAt(1).Character.Group.IsEmpty);
        }

        [TestMethod]
        public void Test_Leader_Change()
        {
            ServerManager.Instance.Sessions.Values.ElementAt(1).Character.GroupRequestCharacterIds.Add(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.CharacterId);

            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = ServerManager.Instance.Sessions.Values.ElementAt(1).Character.CharacterId
            };

            _handlers.ElementAt(0).ManageGroup(pjoinPacket);

            ServerManager.Instance.Sessions.Values.ElementAt(2).Character.GroupRequestCharacterIds.Add(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.CharacterId);

            pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = ServerManager.Instance.Sessions.Values.ElementAt(2).Character.CharacterId
            };

            _handlers.ElementAt(0).ManageGroup(pjoinPacket);

            Assert.IsTrue(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.Group.IsGroupFull && ServerManager.Instance.Sessions.Values.ElementAt(1).Character.Group.IsGroupFull && ServerManager.Instance.Sessions.Values.ElementAt(2).Character.Group.IsGroupFull && ServerManager.Instance.Sessions.Values.ElementAt(0).Character.Group.IsGroupLeader(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.CharacterId));

            _handlers.ElementAt(0).LeaveGroup(new PleavePacket());

            Assert.IsTrue(ServerManager.Instance.Sessions.Values.ElementAt(1).Character.Group.IsGroupLeader(ServerManager.Instance.Sessions.Values.ElementAt(1).Character.CharacterId));
        }

        [TestMethod]
        public void Test_Leaveing_Two_Person_Group()
        {
            ServerManager.Instance.Sessions.Values.ElementAt(1).Character.GroupRequestCharacterIds.Add(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.CharacterId);

            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = ServerManager.Instance.Sessions.Values.ElementAt(1).Character.CharacterId
            };

            _handlers.ElementAt(0).ManageGroup(pjoinPacket);
            Assert.IsTrue(_sessions.ElementAt(0).Character.Group != null && _sessions.ElementAt(1).Character.Group != null && _sessions.ElementAt(0).Character.Group.GroupId == _sessions.ElementAt(1).Character.Group.GroupId);

            ServerManager.Instance.Sessions.Values.ElementAt(2).Character.GroupRequestCharacterIds.Add(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.CharacterId);

            pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = ServerManager.Instance.Sessions.Values.ElementAt(2).Character.CharacterId
            };

            _handlers.ElementAt(0).ManageGroup(pjoinPacket);

            Assert.IsTrue(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.Group.IsGroupFull && ServerManager.Instance.Sessions.Values.ElementAt(1).Character.Group.IsGroupFull && ServerManager.Instance.Sessions.Values.ElementAt(2).Character.Group.IsGroupFull && ServerManager.Instance.Sessions.Values.ElementAt(0).Character.Group.IsGroupLeader(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.CharacterId));

            _handlers.ElementAt(1).LeaveGroup(new PleavePacket());

            Assert.IsTrue(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.Group.IsGroupLeader(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.CharacterId));
        }

        [TestMethod]
        public void Test_Rejected_Group_Join()
        {
            ServerManager.Instance.Sessions.Values.ElementAt(1).Character.GroupRequestCharacterIds.Add(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.CharacterId);

            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Declined,
                CharacterId = ServerManager.Instance.Sessions.Values.ElementAt(1).Character.CharacterId
            };

            _handlers.ElementAt(0).ManageGroup(pjoinPacket);
            Assert.IsTrue(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.Group.IsEmpty && ServerManager.Instance.Sessions.Values.ElementAt(1).Character.Group.IsEmpty);
        }

        [TestMethod]
        public void Test_Leaving_Two_Person_Group()
        {
            ServerManager.Instance.Sessions.Values.ElementAt(1).Character.GroupRequestCharacterIds.Add(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.CharacterId);

            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = ServerManager.Instance.Sessions.Values.ElementAt(1).Character.CharacterId
            };

            _handlers.ElementAt(0).ManageGroup(pjoinPacket);
            Assert.IsTrue(!ServerManager.Instance.Sessions.Values.ElementAt(0).Character.Group.IsEmpty && !ServerManager.Instance.Sessions.Values.ElementAt(1).Character.Group.IsEmpty && ServerManager.Instance.Sessions.Values.ElementAt(0).Character.Group.GroupId == ServerManager.Instance.Sessions.Values.ElementAt(1).Character.Group.GroupId);

            _handlers.ElementAt(0).LeaveGroup(new PleavePacket());

            Assert.IsTrue(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.Group.IsEmpty && ServerManager.Instance.Sessions.Values.ElementAt(1).Character.Group.IsEmpty);
        }
    }
}
