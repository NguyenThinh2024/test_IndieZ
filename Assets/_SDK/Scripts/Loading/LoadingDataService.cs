using System.Threading.Tasks;
using Nexzap.Base.Data;
using Nexzap.Template;

namespace Nexzap.Base
{
    public static class LoadingDataService
    {
        public static async Task LoadAsync()
        {
            await LoadUserProfileAsync();
            EnsureFirstTimeData();
            await Task.Yield();
        }

        private static Task<UserData> LoadUserProfileAsync()
        {
            var completionSource = new TaskCompletionSource<UserData>();
            UserProfileController.Instance.LoadUserProfile(data => completionSource.TrySetResult(data));
            return completionSource.Task;
        }

        private static void EnsureFirstTimeData()
        {
            bool firstTime = UserProfileController.Instance.GetParam<bool>("firstTime");
            if (firstTime)
            {
                return;
            }

            UserProfileController.Instance.SetParam("firstTime", true);
            UserProfileController.Instance.SetParam("currentLevel", 1);
            UserProfileController.Instance.SetParam("timeToAddLife", ConfigController.Instance.GameConfig.refillLifeTime);
            UserProfileController.Instance.SetParam("coin", ConfigController.Instance.GameConfig.startCoin);
            UserProfileController.Instance.SetParam("life", ConfigController.Instance.GameConfig.maxLife);
            UserProfileController.Instance.SetParam("adsTicket", 0);

            UserProfileController.Instance.SetParam("booster_1", 3);
            UserProfileController.Instance.SetParam("booster_2", 3);
            UserProfileController.Instance.SetParam("booster_3", 3);

            UserProfileController.Instance.SetParam("booster_1_tutorial", false);
            UserProfileController.Instance.SetParam("booster_2_tutorial", false);
            UserProfileController.Instance.SetParam("booster_3_tutorial", false);

            UserProfileController.Instance.SetParam("winStreak", 0);
            UserProfileController.Instance.SetParam("openedWinStreakCount", 0);
        }
    }
}
