using System;
using static YouDooSDKConstants;
/// <summary>
/// 这个demo中测试的数据统计。
/// </summary>

[System.Serializable]
public class  DataStatisticsUserData
{
    public string username;
    public int user_level;
    public bool is_vip;
    public int vip_level;

    public long  update_time;
    public string  user_id;
}

[System.Serializable]
public class  DataStatisticsEnterGameData : DataStatisticsBase
{
    public string game_type;
    public int player_number;
    public string level_id;
    public string difficulty;
    public string song_id;
    public string course_id;
    public string track_id;
    public string prop_id;
    public string skin_id;
    public string role_id;
    public string player_id;
    public long start_timestamp;
}

[System.Serializable]
public class  DataStatisticsEndGameData : DataStatisticsBase
{
    public string game_type;
    public int player_number;
    public string level_id;
    public string difficulty;
    public string song_id;
    public string course_id;
    public string track_id;
    public string prop_id;
    public string skin_id;
    public string role_id;
    public string player_id;
    public int score;
    public float hit_rate;
    public int hit_count;
    public int combo;
    public int remain_revival;
    public int remain_lives;
    public string special_mechanism_id;
    public string result;
    public string grade;
    public float calories;
    public string death_location;
    public long end_timestamp;
    public int duration;
    public string fish_id;
    public string location_id;
    public float fish_size;
    public string bait_id;
    public string image_list;
}
