package com.unity3d.player;

import android.app.Activity;
import android.content.pm.PackageManager;
import android.os.Build;

import androidx.core.app.ActivityCompat;
import androidx.core.content.ContextCompat;

public class NotificationPermissionHelper {

    private static final String PERMISSION = "android.permission.POST_NOTIFICATIONS";

    public static void requestIfNeeded(Activity activity) {
        if (activity == null) return;
        if (Build.VERSION.SDK_INT < 33) return;

        if (ContextCompat.checkSelfPermission(activity, PERMISSION) == PackageManager.PERMISSION_GRANTED) {
            return;
        }
        ActivityCompat.requestPermissions(activity, new String[]{PERMISSION}, 0);
    }
}
