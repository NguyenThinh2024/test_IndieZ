using UnityEngine;

public static class GameDataHelper
{
    public static string PlayType = "home"; //play game từ đâu: home | setting_restart | lose_restart | win_next
    public static int PlayDuration; //tổng số thời gian chơi level mili second
    public static int LevelProgress; //tỷ lệ đã hoàn thành của level: 0-100%
    public static int BoosterCount; //tổng số booster dùng trong level
    public static int ReviveUsed; //tổng số lượt revive level
    public static string LoseBy; //thua bởi nguyên nhân gì: out_of_slot | restart
    public static string Result; //kết quả của màn chơi: win | lose | exit
}
