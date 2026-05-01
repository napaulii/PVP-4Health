using UnityEngine;
using SupabaseModels;

public class UserChallengeManager : MonoBehaviour
{
    private UserChallengeController _userChallengeController;

    public async void OnGenerateChallengeBtnClick()
    {
        Debug.Log("Trying to create user_challenge");
        var controller = new UserChallengeController();
        //await controller.CreateUserChallengeAsync();
        await controller.GetAllUserChallengesAsync();
        Debug.Log("User_challenge created");
    }
}
