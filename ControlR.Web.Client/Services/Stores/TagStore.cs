﻿using ControlR.Web.Client.ViewModels;

namespace ControlR.Web.Client.Services.Stores;

public interface ITagStore : IStoreBase<TagResponseDto>
{}

public class TagStore(IControlrApi controlrApi, ISnackbar snackbar, ILogger<TagStore> logger)
  : StoreBase<TagResponseDto>(controlrApi, snackbar, logger), ITagStore
{
  protected override async Task RefreshImpl()
  {
    var getResult = await ControlrApi.GetAllTags(includeLinkedIds: true);
    if (!getResult.IsSuccess)
    {
      Snackbar.Add(getResult.Reason, Severity.Error);
      return;
    }

    foreach (var tag in getResult.Value)
    {
      Cache.AddOrUpdate(tag.Id, tag, (_, _) => tag);
    }
  }
}