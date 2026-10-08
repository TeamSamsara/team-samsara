// File: /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/Fakes/FakeMemberImageService.cs
// Version: 1.1.0
// Latest commit: fix/profile-atomic-update
// Author: Gerrah
//
// Purpose: A member image service that hands out ids, tracks what is stored, and can be made to refuse or fail.

using TeamSamsara.Shared.Assets;

namespace TeamSamsara.Modules.Identity.Tests.Fakes;

public class FakeMemberImageService : IMemberImageService
{
    #region Fields

    private readonly List<string> _storedAssetIds = new();
    private readonly List<string> _deletedAssetIds = new();
    private int _counter;

    #endregion

    #region Properties

    // What the next StoreAsync call reports; anything but Success stores nothing
    public MemberImageStatus NextStatus { get; set; } = MemberImageStatus.Success;

    // Makes DeleteAsync throw, as a failing storage backend would
    public bool FailDeletes { get; set; }

    // Runs at the start of every StoreAsync call, to simulate something else changing while an upload is in flight
    public Func<Task>? BeforeStore { get; set; }

    // The asset ids that are stored right now
    public IReadOnlyList<string> StoredAssetIds => _storedAssetIds;

    // Every asset id that was deleted successfully
    public IReadOnlyList<string> DeletedAssetIds => _deletedAssetIds;

    #endregion

    #region Public Methods

    public async Task<MemberImageResult> StoreAsync(MemberImageKind kind, Stream content)
    {
        if (BeforeStore is not null)
        {
            await BeforeStore();
        }

        if (NextStatus != MemberImageStatus.Success)
        {
            return new MemberImageResult(NextStatus, null);
        }

        var assetId = $"asset-{++_counter}";
        _storedAssetIds.Add(assetId);

        return new MemberImageResult(MemberImageStatus.Success, assetId);
    }

    public Task DeleteAsync(string assetId)
    {
        if (FailDeletes)
        {
            throw new InvalidOperationException("Delete failed.");
        }

        _storedAssetIds.Remove(assetId);
        _deletedAssetIds.Add(assetId);

        return Task.CompletedTask;
    }

    #endregion
}
