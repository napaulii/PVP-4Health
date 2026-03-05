using System;

[Serializable]
public class UserChallengeData
{
    public int id;
    public string status;
    public string date;

    public int fk_Challengeid;
    public int fk_Userid;

    // Optional resolved reference
    public ChallengeData challenge;
}