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

namespace NosCore.Data.Dto
{
	/// <summary>
	/// Represents a DTO class for NosCore.Database.Entities.Mail.
	/// NOTE: This class is generated by GenerateDtos.tt
	/// </summary>
	public class MailDto : IDto
	{
		public short? Hat { get; set; }

	 	public short? Armor { get; set; }

	 	public short? MainWeapon { get; set; }

	 	public short? SecondaryWeapon { get; set; }

	 	public short? Mask { get; set; }

	 	public short? Fairy { get; set; }

	 	public short? CostumeSuit { get; set; }

	 	public short? CostumeHat { get; set; }

	 	public short? WeaponSkin { get; set; }

	 	public short? WingSkin { get; set; }

	 	public System.DateTime Date { get; set; }

	 	public bool IsOpened { get; set; }

	 	public bool IsSenderCopy { get; set; }

	 	public System.Guid? ItemInstanceId { get; set; }

	 	[Key]
		public long MailId { get; set; }

	 	public string Message { get; set; }

	 	public long ReceiverId { get; set; }

	 	public long? SenderId { get; set; }

	 	public NosCore.Packets.Enumerations.CharacterClassType? SenderCharacterClass { get; set; }

	 	public NosCore.Packets.Enumerations.GenderType? SenderGender { get; set; }

	 	public NosCore.Packets.Enumerations.HairColorType? SenderHairColor { get; set; }

	 	public NosCore.Packets.Enumerations.HairStyleType? SenderHairStyle { get; set; }

	 	public short? SenderMorphId { get; set; }

	 	public string Title { get; set; }

	 }
}