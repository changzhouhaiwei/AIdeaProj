package com.unity3d.player;

import android.app.Activity;
import android.content.Intent;
import android.content.res.Configuration;
import android.graphics.Color;
import android.os.Bundle;
import android.os.SystemClock;
import android.text.TextUtils;
import android.util.Log;
import android.view.KeyEvent;
import android.view.MotionEvent;
import android.view.View;
import android.view.ViewGroup;
import android.view.Window;
import android.widget.ImageView;

public class UnityPlayerActivity extends Activity implements IUnityPlayerLifecycleEvents
{
    protected UnityPlayer mUnityPlayer;
    private ImageView splashImage;
    private static final String TAG = "UnityPlayerActivity";

    private static long sAfterUnityPlayerCtorElapsedRealtimeMs;
    public static long getAfterUnityPlayerCtorElapsedRealtimeMs() {
        return sAfterUnityPlayerCtorElapsedRealtimeMs;
    }

    protected String updateUnityCommandLineArguments(String cmdLine)
    {
        return cmdLine;
    }

    @Override protected void onCreate(Bundle savedInstanceState)
    {
        requestWindowFeature(Window.FEATURE_NO_TITLE);
        super.onCreate(savedInstanceState);

        checkShortcutIntentDelayed(getIntent());

        String cmdLine = updateUnityCommandLineArguments(getIntent().getStringExtra("unity"));
        getIntent().putExtra("unity", cmdLine);

        long tUnityPlayerStart = SystemClock.elapsedRealtime();
        mUnityPlayer = new UnityPlayer(this, this);
        sAfterUnityPlayerCtorElapsedRealtimeMs = SystemClock.elapsedRealtime();
        setContentView(mUnityPlayer);
        mUnityPlayer.requestFocus();

        Log.i("XSort2Init", "new UnityPlayer: " + (sAfterUnityPlayerCtorElapsedRealtimeMs - tUnityPlayerStart) + " ms");

        showSplashScreen();
    }
    private void showSplashScreen() {
        splashImage = new ImageView(this);
        splashImage.setBackgroundColor(Color.BLACK);
        splashImage.setImageDrawable(getDrawable(R.drawable.sofish_logo_white));
        splashImage.setScaleType(ImageView.ScaleType.CENTER_INSIDE);
        mUnityPlayer.addView(splashImage, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT));
    }

    public void hideSplashScreen() {
        runOnUiThread(new Runnable() {
            @Override
            public void run() {
                mUnityPlayer.removeView(splashImage);
                splashImage.setImageDrawable(null);
            }
        });
    }

    public void OpenServiceCenter(final String appId, final String uid, final String token, final String roleId) {
        runOnUiThread(new Runnable() {
            @Override
            public void run() {
                try {
                    if (TextUtils.isEmpty(appId)) {
                        Log.w(TAG, "OpenServiceCenter failed: appId is empty");
                        return;
                    }

                    String serviceUrl = ServiceUtil.getServiceUrl(UnityPlayerActivity.this, appId, uid, token, roleId);
                    if (TextUtils.isEmpty(serviceUrl)) {
                        Log.w(TAG, "OpenServiceCenter failed: serviceUrl is empty");
                        return;
                    }

                    boolean openResult = ServiceUtil.openBrowser(UnityPlayerActivity.this, serviceUrl);
                    if (!openResult) {
                        Log.w(TAG, "OpenServiceCenter failed: cannot open browser");
                    }
                } catch (Exception e) {
                    Log.e(TAG, "OpenServiceCenter exception", e);
                }
            }
        });
    }

    public void OpenServiceCenter() {
        Log.w(TAG, "OpenServiceCenter() called without params, skip opening service center.");
    }

    @Override public void onUnityPlayerUnloaded() {
        moveTaskToBack(true);
    }

    @Override public void onUnityPlayerQuitted() {
    }

    @Override protected void onNewIntent(Intent intent)
    {
        setIntent(intent);
        checkShortcutIntent(intent);
        mUnityPlayer.newIntent(intent);
    }

    private void checkShortcutIntent(Intent intent) {
        if (intent != null && "toggle_game".equals(intent.getStringExtra("action"))) {
            sendShortcutToUnity();
        }
    }

    private void checkShortcutIntentDelayed(final Intent intent) {
        if (intent != null && "toggle_game".equals(intent.getStringExtra("action"))) {
            new android.os.Handler(android.os.Looper.getMainLooper()).postDelayed(new Runnable() {
                @Override
                public void run() {
                    sendShortcutToUnity();
                }
            }, 2500);
        }
    }

    private void sendShortcutToUnity() {
        runOnUiThread(new Runnable() {
            @Override
            public void run() {
                com.unity3d.player.UnityPlayer.UnitySendMessage("GPSDKMgr", "OnRecieveAndroidMsg", "{\"key\":\"ShortcutToggleGame\",\"content\":\"\",\"uid\":\"0\"}");
            }
        });
    }

    @Override protected void onDestroy ()
    {
        mUnityPlayer.destroy();
        super.onDestroy();
    }

    @Override protected void onStop()
    {
        super.onStop();
        mUnityPlayer.onStop();
    }

    @Override protected void onStart()
    {
        super.onStart();
        mUnityPlayer.onStart();
    }

    @Override protected void onPause()
    {
        super.onPause();
        mUnityPlayer.onPause();
    }

    @Override protected void onResume()
    {
        super.onResume();
        mUnityPlayer.onResume();
    }

    @Override public void onLowMemory()
    {
        super.onLowMemory();
        mUnityPlayer.lowMemory();
    }

    @Override public void onTrimMemory(int level)
    {
        super.onTrimMemory(level);
        if (level == TRIM_MEMORY_RUNNING_CRITICAL)
        {
            mUnityPlayer.lowMemory();
        }
    }

    @Override public void onConfigurationChanged(Configuration newConfig)
    {
        super.onConfigurationChanged(newConfig);
        mUnityPlayer.configurationChanged(newConfig);
    }

    @Override public void onWindowFocusChanged(boolean hasFocus)
    {
        super.onWindowFocusChanged(hasFocus);
        mUnityPlayer.windowFocusChanged(hasFocus);
    }

    @Override public boolean dispatchKeyEvent(KeyEvent event)
    {
        if (event.getAction() == KeyEvent.ACTION_MULTIPLE)
            return mUnityPlayer.injectEvent(event);
        return super.dispatchKeyEvent(event);
    }

    @Override public boolean onKeyUp(int keyCode, KeyEvent event)     { return mUnityPlayer.onKeyUp(keyCode, event); }
    @Override public boolean onKeyDown(int keyCode, KeyEvent event)   { return mUnityPlayer.onKeyDown(keyCode, event); }
    @Override public boolean onTouchEvent(MotionEvent event)          { return mUnityPlayer.onTouchEvent(event); }
    @Override public boolean onGenericMotionEvent(MotionEvent event)  { return mUnityPlayer.onGenericMotionEvent(event); }
}
