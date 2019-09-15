﻿using ChickenAPI.Packets.ClientPackets.Bazaar;
using ChickenAPI.Packets.ServerPackets.Bazaar;
using ChickenAPI.Packets.ServerPackets.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.HttpClients.BazaarHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.PacketHandlers.Bazaar;
using NosCore.Tests.Helpers;
using Serilog;
using System;
using System.Linq;

namespace NosCore.Tests.BazaarTests
{
    [TestClass]
    public class CScalcPacketHandlerTest
    {
        private CScalcPacketHandler _cScalcPacketHandler;
        private ClientSession _session;
        private Mock<IBazaarHttpClient> _bazaarHttpClient;
        private Mock<IItemProvider> _itemProvider;
        private Mock<IGenericDao<IItemInstanceDto>> _itemInstanceDao;
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        [TestInitialize]
        public void Setup()
        {
            TestHelpers.Reset();
            Broadcaster.Reset();
            _session = TestHelpers.Instance.GenerateSession();
            _bazaarHttpClient = new Mock<IBazaarHttpClient>();
            _itemProvider = new Mock<IItemProvider>();
            _itemInstanceDao = new Mock<IGenericDao<IItemInstanceDto>>();
            _cScalcPacketHandler = new CScalcPacketHandler(TestHelpers.Instance.WorldConfiguration, _bazaarHttpClient.Object, _itemProvider.Object, _logger, _itemInstanceDao.Object);

            _bazaarHttpClient.Setup(b => b.GetBazaarLink(0)).Returns(
                new BazaarLink
                {
                    SellerName = _session.Character.Name,
                    BazaarItem = new BazaarItemDto { Price = 50, Amount = 1 },
                    ItemInstance = new ItemInstanceDto { ItemVNum = 1012, Amount = 0 }
                });
            _bazaarHttpClient.Setup(b => b.GetBazaarLink(2)).Returns(
                new BazaarLink
                {
                    SellerName = "test",
                    BazaarItem = new BazaarItemDto { Price = 60, Amount = 1 },
                    ItemInstance = new ItemInstanceDto { ItemVNum = 1012, Amount = 0 }
                });
            _bazaarHttpClient.Setup(b => b.GetBazaarLink(1)).Returns((BazaarLink)null);
            _bazaarHttpClient.Setup(b => b.Remove(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<string>())).Returns(true);
            _itemProvider.Setup(s => s.Convert(It.IsAny<IItemInstanceDto>())).Returns(new ItemInstance { Amount = 0, ItemVNum = 1012, Item = new Item() });
        }

        [TestMethod]
        public void RetrieveWhenInExchangeOrTrade()
        {
            _session.Character.InExchangeOrTrade = true;
            _cScalcPacketHandler.Execute(new CScalcPacket
            {
                BazaarId = 1,
                Price = 50,
                Amount = 1,
                VNum = 1012,
            }, _session);
            Assert.IsNull(_session.LastPackets.FirstOrDefault());
        }

        [TestMethod]
        public void RetrieveWhenNoItem()
        {
            _cScalcPacketHandler.Execute(new CScalcPacket
            {
                BazaarId = 1,
                Price = 50,
                Amount = 1,
                VNum = 1012,
            }, _session);
            var lastpacket = (RCScalcPacket)_session.LastPackets.FirstOrDefault(s => s is RCScalcPacket);
            Assert.AreEqual(0, lastpacket.Price);
        }

        [TestMethod]
        public void RetrieveWhenNotYourItem()
        {
            _cScalcPacketHandler.Execute(new CScalcPacket
            {
                BazaarId = 2,
                Price = 50,
                Amount = 1,
                VNum = 1012,
            }, _session);
            var lastpacket = (RCScalcPacket)_session.LastPackets.FirstOrDefault(s => s is RCScalcPacket);
            Assert.AreEqual(0, lastpacket.Price);
        }

        [TestMethod]
        public void RetrieveWhenNotEnoughPlace()
        {
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            _session.Character.Inventory.AddItemToPocket(new InventoryItemInstance
            { Id = guid2, ItemInstanceId = guid2, Slot = 0, Type = NoscorePocketType.Main, ItemInstance = new ItemInstance { ItemVNum = 1012, Amount = 999, Id = guid2 } });
            _session.Character.Inventory.AddItemToPocket(new InventoryItemInstance
            { Id = guid1, ItemInstanceId = guid1, Slot = 1, Type = NoscorePocketType.Main, ItemInstance = new ItemInstance { ItemVNum = 1012, Amount = 999, Id = guid1 } });
            _cScalcPacketHandler.Execute(new CScalcPacket
            {
                BazaarId = 0,
                Price = 50,
                Amount = 1,
                VNum = 1012,
            }, _session);
            var lastpacket = (RCScalcPacket)_session.LastPackets.FirstOrDefault(s => s is RCScalcPacket);
            Assert.AreEqual(50, lastpacket.Price);
        }

        [TestMethod]
        public void RetrieveWhenMaxGold()
        {
            _session.Character.Gold = TestHelpers.Instance.WorldConfiguration.MaxGoldAmount;
            _cScalcPacketHandler.Execute(new CScalcPacket
            {
                BazaarId = 0,
                Price = 50,
                Amount = 1,
                VNum = 1012,
            }, _session);
            var lastpacket = (MsgPacket)_session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.AreEqual(Language.Instance.GetMessageFromKey(LanguageKey.MAX_GOLD,
                          _session.Account.Language), lastpacket.Message);
        }


        [TestMethod]
        public void Retrieve()
        {
            _cScalcPacketHandler.Execute(new CScalcPacket
            {
                BazaarId = 0,
                Price = 50,
                Amount = 1,
                VNum = 1012,
            }, _session);
            var lastpacket = (RCScalcPacket)_session.LastPackets.FirstOrDefault(s => s is RCScalcPacket);
            Assert.AreEqual(50, lastpacket.Total);
        }
    }
}