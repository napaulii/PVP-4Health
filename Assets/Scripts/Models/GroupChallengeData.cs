using System;

[Serializable]
public class GroupChallengeData
{
    public int id;
    public string status;
    public string date;

    public int fk_Challengeid;
    public int fk_Groupid;

    // Optional resolved reference
    public ChallengeData challenge;
}