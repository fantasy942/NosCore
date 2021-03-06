﻿//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Data.Dto;
using NosCore.GameObject.ComponentEntities.Interfaces;

namespace NosCore.GameObject.Providers.MinilandProvider
{
    public class Miniland : MinilandDto
    {
        public Guid MapInstanceId { get; set; }

        public ICharacterEntity? CharacterEntity { get; set; }
        public int CurrentMinigame { get; set; }

        public MlInfoBrPacket GenerateMlinfobr()
        {
            return new MlInfoBrPacket
            {
                Unknown1 = 3800,
                Name = CharacterEntity?.Name,
                MinilandMessage = MinilandMessage,
                DailyVisitCount = DailyVisitCount,
                Unknown2 = 0,
                VisitCount = VisitCount
            };
        }

        public MlinfoPacket GenerateMlinfo()
        {
            return new MlinfoPacket
            {
                Unknown1 = 3800,
                DailyVisitCount = DailyVisitCount,
                VisitCount = VisitCount,
                Unknown2 = 0,
                Unknown3 = 0,
                MinilandPoint = MinilandPoint,
                MinilandState = State,
                MinilandWelcomeMessage = MinilandMessage,
                WelcomeMusicInfo = WelcomeMusicInfo
            };
        }
    }
}