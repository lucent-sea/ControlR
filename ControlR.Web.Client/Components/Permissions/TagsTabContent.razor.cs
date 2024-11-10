﻿using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using ControlR.Web.Client.Services.Stores;
using ControlR.Web.Client.ViewModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace ControlR.Web.Client.Components.Permissions;

public partial class TagsTabContent : ComponentBase, IDisposable
{
  private ImmutableArray<IDisposable>? _changeHandlers;
  private string? _newTagName;
  private TagViewModel? _selectedTag;

  [Inject]
  public required IControlrApi ControlrApi { get; init; }

  [Inject]
  public required IDeviceStore DeviceStore { get; init; }

  [Inject]
  public required IDialogService DialogService { get; init; }

  [Inject]
  public required ILogger<TagsTabContent> Logger { get; init; }

  [Inject]
  public required ISnackbar Snackbar { get; init; }

  [Inject]
  public required ITagStore TagStore { get; init; }

  [Inject]
  public required IUserStore UserStore { get; init; }

  private IOrderedEnumerable<TagViewModel> SortedTags => TagStore.Items.OrderBy(x => x.Name);

  public void Dispose()
  {
    _changeHandlers?.DisposeAll();
    GC.SuppressFinalize(this);
  }

  protected override async Task OnInitializedAsync()
  {
    await base.OnInitializedAsync();
    _changeHandlers =
    [
      TagStore.RegisterChangeHandler(this, async () => await InvokeAsync(StateHasChanged))
    ];
  }

  private async Task CreateTag()
  {
    if (string.IsNullOrWhiteSpace(_newTagName))
    {
      Snackbar.Add("Tag name is required", Severity.Error);
      return;
    }

    if (!IsNewTagNameValid())
    {
      return;
    }

    var createResult = await ControlrApi.CreateTag(_newTagName, TagType.Permission);
    if (!createResult.IsSuccess)
    {
      Snackbar.Add(createResult.Reason, Severity.Error);
      return;
    }

    Snackbar.Add("Tag created", Severity.Success);
    await TagStore.AddOrUpdate(new TagViewModel(createResult.Value));
    _newTagName = null;
  }

  private async Task DeleteSelectedTag()
  {
    if (_selectedTag is null)
    {
      return;
    }

    var result =
      await DialogService.ShowMessageBox("Confirm Deletion", "Are you sure you want to delete this tag?", "Yes", "No");
    if (!result.HasValue || !result.Value)
    {
      return;
    }

    var deleteResult = await ControlrApi.DeleteTag(_selectedTag.Id);
    if (!deleteResult.IsSuccess)
    {
      Snackbar.Add(deleteResult.Reason, Severity.Error);
      return;
    }

    await TagStore.Remove(_selectedTag);
    Snackbar.Add("Tag deleted", Severity.Success);
  }

  private async Task HandleNewTagKeyDown(KeyboardEventArgs args)
  {
    if (args.Key == "Enter")
    {
      await CreateTag();
    }
  }

  [MemberNotNullWhen(true, nameof(_newTagName))]
  private bool IsNewTagNameValid()
  {
    return ValidateNewTagName(_newTagName) == null;
  }

  private async Task SetUserTag(bool isToggled, TagViewModel tag, Guid userId)
  {
    try
    {
      // TODO: Create API endpoint.
      await Task.Yield();
      if (isToggled)
      {
        tag.UserIds.Add(userId);
      }
      else
      {
        tag.UserIds.Remove(userId);
      }

      await TagStore.InvokeItemsChanged();

      Snackbar.Add(isToggled
        ? "Tag added"
        : "Tag removed", Severity.Success);
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Error while setting tag.");
      Snackbar.Add("An error occurred while setting tag", Severity.Error);
    }
  }

  private string? ValidateNewTagName(string? tagName)
  {
    if (string.IsNullOrWhiteSpace(tagName))
    {
      return null;
    }

    if (tagName.Length > 100)
    {
      return "Tag name must be 100 characters or less.";
    }

    if (SortedTags.Any(x => x.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase)))
    {
      return "Tag name already exists.";
    }

    return Validators.TagNameValidator().IsMatch(tagName)
      ? "Tag name can only contain lowercase letters, numbers, underscores, and hyphens."
      : null;
  }
}