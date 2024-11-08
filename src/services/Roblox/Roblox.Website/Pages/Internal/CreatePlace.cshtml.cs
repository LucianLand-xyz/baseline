using Microsoft.AspNetCore.Mvc.RazorPages;
using Roblox.Dto.Users;
using Roblox.Logging;
using Roblox.Models.Assets;
using Roblox.Models.Trades;
using Roblox.Services;
using Roblox.Services.App.FeatureFlags;
using Type = Roblox.Models.Assets.Type;

namespace Roblox.Website.Pages.Internal;

public class CreatePlace : RobloxPageModel
{
    public string? errorMessage { get; set; }
    public string? successUrl { get; set; }
    
    public void OnGet()
    {
        
    }

    public enum PlaceCreationFailureReason
    {
        Ok = 1,
        AccountTooNew,
        TooManyPlaces,
        NoApplication,
        TooInactive,
        LatestPlaceCreatedTooRecently,
        NotEnoughVisitsForNewPlace,
    }

    private string GetRedisKeyForRejection(long userId)
    {
        return "app_rejected_recently_for_place:v1.2:" + userId;
    }

    private async Task<bool> WasRejectedRecently(long userId)
    {
        var result = await Roblox.Services.Cache.distributed.StringGetAsync(GetRedisKeyForRejection(userId));
        if (result != null)
            return true;
        return false;
    }

    public async Task<bool> IsActiveEnoughForPlace(long userId)
    {
        var log = Writer.CreateWithId(LogGroup.AbuseDetection);
        log.Info("start IsActiveEnoughForPlace with userId={0}", userId);
        
        var latestPlay = await services.games.GetOldestPlay(userId);
        if (latestPlay == null || latestPlay.createdAt > DateTime.UtcNow.Subtract(TimeSpan.FromDays(5)))
        {
            log.Info("user has not played any games");
            return false;
        }

        var gamesPlayed = (await services.games.GetRecentGamePlays(userId, TimeSpan.FromDays(7))).ToList();
        long timePlayedMinutes = 0;
        foreach (var item in gamesPlayed)
        {
            var ended = item.endedAt ?? item.createdAt.Add(TimeSpan.FromMinutes(1));
            var minutesDiff = (long)Math.Truncate(Math.Min((ended - item.createdAt).TotalMinutes, 10));
            log.Info("user played {0} for {1}m", item.placeId, minutesDiff);
            timePlayedMinutes += minutesDiff;
        }
        log.Info("user play time is {0}m", timePlayedMinutes);
#if false
        // avatar should have some paid items...
        var avatar = await services.assets.MultiGetInfoById(await services.avatar.GetWornAssets(userId));
        var hasLimitedOrForSale = avatar.FirstOrDefault(c =>
        {
            var restrict = c.itemRestrictions.ToArray();
            if (restrict.Contains("Limited") || restrict.Contains("LimitedUnique"))
                return true;
            if (c.price != 0)
                return true;

            return false;
        }) != null;
        
        // or, user should have some trades
        var tradeHistory = await services.trades.GetTradesOfType(userId, TradeType.Completed, 100, 0);
        var hasTradeHistory = tradeHistory.Any();
        
        // or they could own a group.
        // user should be in at least 1 group as well
        var groups = (await services.groups.GetAllRolesForUser(userId)).ToArray();
        if (!groups.Any())
        {
            log.Info("user is not in any groups");
            return false;
        }
        
        var doesOwnGroup = groups.FirstOrDefault(c => c.rank == 255) != null;

        if (!hasLimitedOrForSale && !hasTradeHistory && !doesOwnGroup)
        {
            log.Info("user is not wearing any limited or for-sale items, has no trade history, and does not own a group");
            return false;
        }

        var postCount = await services.forums.GetPostCount(userId);
        if (postCount < 2)
        {
            log.Info("user post count is less than 2");
            return false;
        }
#endif
        Writer.Info(LogGroup.AbuseDetection, "User passed IsActiveEnoughForPlace: {0}", userId);

        return true; // OK
    }
    
    public async Task<PlaceCreationFailureReason> CanCreatePlace(long userId)
    {
        var log = Writer.CreateWithId(LogGroup.AbuseDetection);
        log.Info("start CanCreatePlace with userId={0}",userId);
        var userInfo = await services.users.GetUserById(userId);

        var createdPlaces = (await services.assets.GetCreations(CreatorType.User, userId, Type.Place, 0, 100)).ToArray();
        if (createdPlaces.Length != 0)
        {
            if (createdPlaces.Length > 5)
            {
                log.Info("account has too many places {0}", createdPlaces.Length);
                return PlaceCreationFailureReason.TooManyPlaces;
            }
            
            var placeDetails = (await services.games.MultiGetPlaceDetails(createdPlaces
                    .Select(c => c.assetId)))
                .ToArray();
            
            if (placeDetails.Length != createdPlaces.Length)
            {
                // uhhh
                log.Info("placeDetails len and createdPlaces len do not match: {0} vs {1}", placeDetails.Length, createdPlaces.Length);
                if (placeDetails.Length == 0)
                    throw new Exception("Place details len is zero while createdPlaces len is not zero");
            }

            
            var isAnyPlaceCreatedLessThanADayAgo =
                placeDetails.FirstOrDefault(v => v.created > DateTime.UtcNow.Subtract(TimeSpan.FromDays(1))) != null;
            

            if (isAnyPlaceCreatedLessThanADayAgo)
            {
                log.Info("account place was created less than a day ago");
                return PlaceCreationFailureReason.LatestPlaceCreatedTooRecently;
            }

            long totalPlaceVisits = 0;
            if (placeDetails.Length != 0)
            {
                long placeVisitsRequiredForNewPlace = 100 * placeDetails.Length;
                var universeDetails =
                    await services.games.MultiGetUniverseInfo(placeDetails.Select(c => c.universeId));
                foreach (var item in universeDetails)
                {
                    totalPlaceVisits += item.visits;
                }


            }
        }

        
        return PlaceCreationFailureReason.Ok;
    }

    private string GetMessage(PlaceCreationFailureReason reason)
    {
        return reason switch
        {
            PlaceCreationFailureReason.AccountTooNew =>
                "Your account is too new. Try again when your account is at least 7 days old.",
            PlaceCreationFailureReason.TooManyPlaces => "Your account already has the maximum amount of places on it.",
            PlaceCreationFailureReason.NoApplication => "You cannot create a place if you did not join through the application system.",
            PlaceCreationFailureReason.TooInactive => "Your account is too inactive to create a place. Staff cannot comment on the exact reason, so please do not ask. Try playing around some more, posting on places like the forums, joining groups, buying items, then try again in a few days.",
            PlaceCreationFailureReason.LatestPlaceCreatedTooRecently => "Latest place was created too recently. Try again in a day.",
            PlaceCreationFailureReason.NotEnoughVisitsForNewPlace => "You do not have enough visits to create a new place. Try again in a few days.",
            _ => "Unknown reason. Code = " + reason.ToString(),
        };
    }

    public async Task OnPost()
    {
        if (userSession == null)
        {
            errorMessage = "Not logged in.";
            return;
        }

        if (!FeatureFlags.IsEnabled(FeatureFlag.CreatePlaceSelfService))
        {
            errorMessage = "Place creation is disabled globally at this time. Try again later.";
            return;
        }

        await using var createGameLock =
            await Roblox.Services.Cache.redLock.CreateLockAsync("CreatePlaceSelfServiceV1:UserId:" + userSession.userId,
                TimeSpan.FromSeconds(10));
        
        if (!createGameLock.IsAcquired)
        {
            Writer.Info(LogGroup.AbuseDetection, "CreatePlace OnPost could not acquire createGameLock");
            errorMessage = "Too many attempts. Try again in a few seconds.";
            return;
        }

        var createStatus = await CanCreatePlace(userSession.userId);
        if (createStatus != PlaceCreationFailureReason.Ok)
        {
            errorMessage = GetMessage(createStatus);
            return;
        }
        Writer.Info(LogGroup.AbuseDetection, "CreatePlace OnPost userId={0} can create a place, creating it", userSession.userId);
        // create one!
        var asset = await services.assets.CreatePlace(userSession.userId, CreatorType.User, userSession.userId);
        // create universe too
        await services.games.CreateUniverse(asset.placeId);
        // give url
        successUrl = "https://moonic.wtf/internal/place-update?id=" + asset.placeId;
    }
}