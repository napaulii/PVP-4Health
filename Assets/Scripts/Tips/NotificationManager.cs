// NotificationManager.cs
// Attach to an empty GameObject named "NotificationManager" in your main scene.
// This persists across scenes (DontDestroyOnLoad).

using System;
using UnityEngine;
using Unity.Notifications.Android;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager instance;

    private const string CHANNEL_ID = "tip_channel";

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        RegisterChannel();
    }

    private void RegisterChannel()
    {
        var channel = new AndroidNotificationChannel()
        {
            Id = CHANNEL_ID,
            Name = "Tips Channel",
            Importance = Importance.Default,
            Description = "Daily tips notification channel.",
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);
    }

    /// <summary>
    /// Schedules a notification to fire after a delay.
    /// Called automatically by TipController on app start.
    /// </summary>
    public void ScheduleNotification(string title, string body, int secondsFromNow)
    {
        AndroidNotificationCenter.CancelAllScheduledNotifications();

        var notification = new AndroidNotification()
        {
            Title = title,
            Text = body,
            FireTime = DateTime.Now.AddSeconds(secondsFromNow),
            SmallIcon = "icon_small",
            LargeIcon = "icon_large"
        };

        AndroidNotificationCenter.SendNotification(notification, CHANNEL_ID);
        Debug.Log($"[NotificationManager] Notification scheduled in {secondsFromNow}s: {body}");
    }

    public void CancelAll()
    {
        AndroidNotificationCenter.CancelAllScheduledNotifications();
    }
}