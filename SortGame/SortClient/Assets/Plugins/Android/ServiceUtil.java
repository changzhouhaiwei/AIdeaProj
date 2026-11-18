package com.unity3d.player;

import android.annotation.SuppressLint;
import android.content.Context;
import android.content.Intent;
import android.content.res.Configuration;
import android.os.Build;
import android.provider.Settings;
import android.text.TextUtils;
import android.util.Base64;
import android.net.Uri;

import org.json.JSONObject;

import java.nio.charset.StandardCharsets;
import java.util.Locale;
import java.util.Map;
import java.util.TreeMap;

public final class ServiceUtil {

    private static final String URL = "https://customer-h5.sofishgame.com/index.html?encodeStr=";
    private static final String COMMON_BASE_URL = "https://fl.sogamecdn.com/domain/domainUrltest.json";

    private ServiceUtil() {
    }

    public static boolean openBrowser(Context context, String url) {
        if (context == null || TextUtils.isEmpty(url)) {
            return false;
        }
        try {
            Uri uri = Uri.parse(url);
            Intent intent = new Intent(Intent.ACTION_VIEW, uri);
            intent.addCategory(Intent.CATEGORY_BROWSABLE);
            intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
            context.startActivity(intent);
            return true;
        } catch (Exception e) {
            e.printStackTrace();
        }
        return false;
    }

    public static String getServiceUrl(Context context, String appId, String uid, String token, String roleId) {
        if (context == null || TextUtils.isEmpty(appId)) {
            return null;
        }
        Map<String, String> params = new TreeMap<>();
        params.put("appId", appId);
        params.put("uId", uid == null ? "" : uid);
        params.put("token", token == null ? "" : token);
        params.put("roleID", roleId == null ? "" : roleId);
        params.put("imei", getAndroidId(context));
        params.put("appLang", getCurrentLanguage(context));
        params.put("commonBaseUrl", COMMON_BASE_URL);

        String json = new JSONObject(params).toString();
        String encodeStr = Base64.encodeToString(json.getBytes(StandardCharsets.UTF_8), Base64.NO_WRAP);
        return URL + encodeStr;
    }

    @SuppressLint("HardwareIds")
    public static String getAndroidId(Context context) {
        try {
            return Settings.Secure.getString(context.getContentResolver(), Settings.Secure.ANDROID_ID);
        } catch (Exception e) {
            e.printStackTrace();
        }
        return "";
    }

    public static String getCurrentLanguage(Context context) {
        Configuration configuration = context.getResources().getConfiguration();
        Locale locale = Build.VERSION.SDK_INT >= Build.VERSION_CODES.N ? configuration.getLocales().get(0) : configuration.locale;
        String language = locale.getLanguage();
        String country = locale.getCountry();
        if (TextUtils.isEmpty(country)) {
            return language;
        }
        return language + "-" + country;
    }
}
