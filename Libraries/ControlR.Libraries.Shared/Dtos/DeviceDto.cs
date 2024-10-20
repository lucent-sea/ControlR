﻿using ControlR.Libraries.Shared.Dtos.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace ControlR.Libraries.Shared.Dtos;

[MessagePackObject]
public class DeviceDto : DeviceFromAgentDto, IEntityBaseDto
{
  [MsgPackKey]
  [Display(Name = "Alias")]
  public string Alias { get; set; } = string.Empty;

  [MsgPackKey]
  public Guid? DeviceGroupId { get; set; }

  [MsgPackKey]
  [Display(Name = "Id")]
  public Guid Id { get; set; }

  [MsgPackKey]
  public Guid TenantId { get; set; }

  [IgnoreDataMember]
  [JsonIgnore]
  [Display(Name = "Memory Used %")]
  public double UsedMemoryPercent => UsedMemory / TotalMemory;

  [IgnoreDataMember]
  [JsonIgnore]
  [Display(Name = "Storage Used %")]
  public double UsedStoragePercent => UsedStorage / TotalStorage;
}