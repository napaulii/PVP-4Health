using System;

[Serializable]
public class ChallengeData
{
    public int id;
    public string type;
    public string description;
    public int reward;

    public int fk_Categoryid;

    // Optional: resolved reference
    public CategoryData category;
}