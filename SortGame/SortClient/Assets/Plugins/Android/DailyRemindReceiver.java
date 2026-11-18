package com.unity3d.player;

import android.app.AlarmManager;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.graphics.Bitmap;
import android.graphics.Canvas;
import android.graphics.drawable.BitmapDrawable;
import android.graphics.drawable.Drawable;
import android.os.Build;
import android.util.Log;

import androidx.core.app.NotificationCompat;
import androidx.core.app.NotificationManagerCompat;

import java.util.Calendar;

public class DailyRemindReceiver extends BroadcastReceiver {

    private static final String TAG = "DailyRemindReceiver";
    private static final int REQUEST_CODE_ALARM = 1001;
    private static final int HOUR = 20;
    private static final int MINUTE = 0;

    @Override
    public void onReceive(Context context, Intent intent) {
        if (Intent.ACTION_BOOT_COMPLETED.equals(intent.getAction())) {
            scheduleDailyAlarm(context);
            return;
        }

        showNotification(context);
        scheduleDailyAlarm(context);
    }

    private void showNotification(Context context) {
        String channelId = getStringResource(context, "notify_channel_id", "daily_remind");
        String channelName = getStringResource(context, "notify_channel_name", "Remind");
        String content = getStringResource(context, "notify_desc_1", "Come back and enjoy a game!");
        String title = getStringResource(context, "SystemPush_Title1", "Puzzle time!");

        createNotificationChannel(context, channelId, channelName);

        Intent openApp = new Intent(context, UnityPlayerActivity.class);
        openApp.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TOP);
        PendingIntent pendingIntent = PendingIntent.getActivity(context, 0, openApp,
                PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE);

        NotificationCompat.Builder builder = new NotificationCompat.Builder(context, channelId)
                .setSmallIcon(android.R.drawable.ic_dialog_info)
                .setContentTitle(title)
                .setContentText(content)
                .setContentIntent(pendingIntent)
                .setAutoCancel(true)
                .setPriority(NotificationCompat.PRIORITY_DEFAULT);

        Bitmap largeIcon = getAppIconBitmap(context);
        if (largeIcon != null) {
            builder.setLargeIcon(largeIcon);
        }

        try {
            NotificationManagerCompat.from(context).notify(REQUEST_CODE_ALARM, builder.build());
        } catch (SecurityException e) {
            Log.w(TAG, "No notification permission: " + e.getMessage());
        }
    }

    private void createNotificationChannel(Context context, String channelId, String channelName) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            NotificationChannel channel = new NotificationChannel(channelId, channelName,
                    NotificationManager.IMPORTANCE_DEFAULT);
            NotificationManager nm = context.getSystemService(NotificationManager.class);
            if (nm != null) {
                nm.createNotificationChannel(channel);
            }
        }
    }

    private String getStringResource(Context context, String name, String fallback) {
        int id = context.getResources().getIdentifier(name, "string", context.getPackageName());
        return id != 0 ? context.getString(id) : fallback;
    }

    private Bitmap getAppIconBitmap(Context context) {
        try {
            Drawable drawable = context.getPackageManager().getApplicationIcon(context.getApplicationInfo());
            if (drawable instanceof BitmapDrawable) {
                Bitmap bitmap = ((BitmapDrawable) drawable).getBitmap();
                int size = (int) (48 * context.getResources().getDisplayMetrics().density);
                return Bitmap.createScaledBitmap(bitmap, size, size, true);
            }
            int size = (int) (48 * context.getResources().getDisplayMetrics().density);
            Bitmap bitmap = Bitmap.createBitmap(size, size, Bitmap.Config.ARGB_8888);
            Canvas canvas = new Canvas(bitmap);
            drawable.setBounds(0, 0, canvas.getWidth(), canvas.getHeight());
            drawable.draw(canvas);
            return bitmap;
        } catch (Exception e) {
            Log.w(TAG, "Failed to get app icon: " + e.getMessage());
            return null;
        }
    }

    public static void scheduleDailyAlarm(Context context) {
        AlarmManager alarmManager = (AlarmManager) context.getSystemService(Context.ALARM_SERVICE);
        if (alarmManager == null) return;

        Intent intent = new Intent(context, DailyRemindReceiver.class);
        PendingIntent pendingIntent = PendingIntent.getBroadcast(context, REQUEST_CODE_ALARM, intent,
                PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE);

        Calendar calendar = Calendar.getInstance();
        calendar.set(Calendar.HOUR_OF_DAY, HOUR);
        calendar.set(Calendar.MINUTE, MINUTE);
        calendar.set(Calendar.SECOND, 0);
        calendar.set(Calendar.MILLISECOND, 0);

        if (calendar.getTimeInMillis() <= System.currentTimeMillis()) {
            calendar.add(Calendar.DAY_OF_YEAR, 1);
        }

        long triggerAt = calendar.getTimeInMillis();

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S && !alarmManager.canScheduleExactAlarms()) {
            alarmManager.setAndAllowWhileIdle(AlarmManager.RTC_WAKEUP, triggerAt, pendingIntent);
        } else if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
            alarmManager.setExactAndAllowWhileIdle(AlarmManager.RTC_WAKEUP, triggerAt, pendingIntent);
        } else {
            alarmManager.setExact(AlarmManager.RTC_WAKEUP, triggerAt, pendingIntent);
        }
        Log.d(TAG, "Scheduled daily alarm at local " + HOUR + ":" + String.format("%02d", MINUTE));
    }
}
