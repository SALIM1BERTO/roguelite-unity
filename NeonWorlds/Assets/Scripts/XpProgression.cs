using UnityEngine;

public static class XpProgression
{
    public static int BaseReward(float seconds)
    {
        // A smooth reward curve prevents the old 50 -> 800 XP jumps from
        // overwhelming level costs late in a run.
        int minute = Mathf.FloorToInt(Mathf.Max(0f, seconds) / 60f);
        return Mathf.Min(60, 24 + minute * 3);
    }
    public static int Reward(float seconds,float enemyMultiplier)
    {return Mathf.Max(1,Mathf.RoundToInt(BaseReward(Mathf.Max(0,seconds))*Mathf.Clamp(enemyMultiplier,.1f,10f)));}
    public static int RequiredForLevel(int level)
    {
        double n=System.Math.Max(0d,(double)level-1);double late=System.Math.Max(0,n-4);
        return (int)System.Math.Min(int.MaxValue,150+75*n+18*late*late);
    }
}
