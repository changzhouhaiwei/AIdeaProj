package com.unity3d.player;

import android.app.Application;
import android.os.SystemClock;
import android.util.Log;

import com.zzxz.gamesdk.core.attribution.AttrSdk;
import com.zzxz.gamesdk.core.thinkingdata.ThinkingDataSdk;

public class CustomApplication extends Application {
    private static final String TAG = "SortInit";
    private static long sOnCreateEndElapsedRealtimeMs;

    public static long getOnCreateEndElapsedRealtimeMs() {
        return sOnCreateEndElapsedRealtimeMs;
    }

    @Override
    public void onCreate() {
        long tStart = SystemClock.elapsedRealtime();
        super.onCreate();
        long tSdkStart = SystemClock.elapsedRealtime();
        AttrSdk.initialize(this, "REPLACE_WITH_APPSFLYER_DEV_KEY", "", false);
        long tSdkEnd = SystemClock.elapsedRealtime();
        Log.i(TAG, "AttrSdk.initialize: " + (tSdkEnd - tSdkStart) + " ms");
        // 5.7.0 起 ThinkingData 可在 Application 初始化，无需等 Unity Activity。
        // appId / url 换成 Sort 自己的数数项目，不要沿用其他游戏。
        long tTdStart = SystemClock.elapsedRealtime();
        ThinkingDataSdk.init(this, "REPLACE_WITH_THINKINGDATA_APP_ID", "https://api-shushu.sogamecdn.com", false);
        Log.i(TAG, "ThinkingDataSdk.init: " + (SystemClock.elapsedRealtime() - tTdStart) + " ms");
        sOnCreateEndElapsedRealtimeMs = SystemClock.elapsedRealtime();
        Log.i(TAG, "CustomApplication.onCreate 总计: " + (sOnCreateEndElapsedRealtimeMs - tStart) + " ms");
    }
}
