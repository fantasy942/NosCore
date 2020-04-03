//  __  _  __    __   ___ __  ___ ___  
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

using System.ComponentModel.DataAnnotations;
using NosCore.Data.I18N;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.Data.DataAttributes;
using NosCore.Data.Enumerations.I18N;
using Mapster;

namespace NosCore.Data.StaticEntities
{
	/// <summary>
	/// Represents a DTO class for NosCore.Database.Entities.Quest.
	/// NOTE: This class is generated by GenerateDtos.tt
	/// </summary>
	public class QuestDto : IStaticDto
	{
		[Key]
		public short QuestId { get; set; }

	 	public NosCore.Packets.Enumerations.QuestType QuestType { get; set; }

	 	public byte LevelMin { get; set; }

	 	public byte LevelMax { get; set; }

	 	public int? StartDialogId { get; set; }

	 	public int? EndDialogId { get; set; }

	 	public short? TargetMap { get; set; }

	 	public short? TargetX { get; set; }

	 	public short? TargetY { get; set; }

	 	public short? NextQuestId { get; set; }

	 	public bool IsDaily { get; set; }

	 	public bool AutoFinish { get; set; }

	 	public bool IsSecondary { get; set; }

	 	public int? SpecialData { get; set; }

	 	public short? RequiredQuestId { get; set; }

	 	[I18NFrom(typeof(I18NQuestDto))]
		public I18NString Title { get; set; } = new I18NString();
		[AdaptMember("Title")]
		public string TitleI18NKey { get; set; }

	 	[I18NFrom(typeof(I18NQuestDto))]
		public I18NString Desc { get; set; } = new I18NString();
		[AdaptMember("Desc")]
		public string DescI18NKey { get; set; }

	 }
}