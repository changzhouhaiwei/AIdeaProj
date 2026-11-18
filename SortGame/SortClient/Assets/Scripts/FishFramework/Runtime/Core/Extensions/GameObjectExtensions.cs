using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public static class GameObjectExtensions
{
    public static bool IsNull(this UnityEngine.Object o)
    {
        return o == null;
    }

    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        if (null == go) return default(T);

        T component = go.GetComponent<T>();
        if (null == component)
			component = go.AddComponent<T>();
        return component;
    }

    public static Component GetOrAddComponent(this GameObject go, Type type)
    {
        if (null == go) return null;

        Component component = go.GetComponent(type);
        if (null == component)
            component = go.AddComponent(type);
        return component;
    }

    public static T GetOrAddComponent<T>(this Component component) where T : Component
    {
        if (null == component) return default(T);

        return component.gameObject.GetOrAddComponent<T>();
    }

    public static Component GetOrAddComponent(this Component component, Type type)
    {
        if (null == component) return null;

        return component.gameObject.GetOrAddComponent(type);
    }

    public static T FindChildComponent<T>(this GameObject go, string name) where T : Component
    {
        if (null == go) return default(T);

        Transform child = go.transform.Find(name);
        if (null == child) return default(T);

        return child.GetComponent<T>();
    }

    public static Component FindChildComponent(this GameObject go, string name, Type type)
    {
        if (null == go) return null;

        Transform child = go.transform.Find(name);
        if (null == child) return null;

        return child.GetComponent(type);
    }

    public static T FindChildComponent<T>(this Component go, string name) where T : Component
    {
        if (null == go) return default(T);

        Transform child = go.transform.Find(name);
        if (null == child) return default(T);

        return child.GetComponent<T>();
    }

    public static Component FindChildComponent(this Component go, string name, Type type)
    {
        if (null == go) return null;

        Transform child = go.transform.Find(name);
        if (null == child) return null;

        return child.GetComponent(type);
    }

    public static T FindChildComponent<T>(this Transform go, string name) where T : Component
    {
        if (null == go) return default(T);

        Transform child = go.Find(name);
        if (null == child) return default(T);

        return child.GetComponent<T>();
    }

    public static Component FindChildComponent(this Transform go, string name, Type type)
    {
        if (null == go) return null;

        Transform child = go.Find(name);
        if (null == child) return null;

        return child.GetComponent(type);
    }

    public static void SetActiveEx(this GameObject obj, bool value)
	{
		if (null == obj) return;
		obj.SetActive(value);
	}

	public static void SetParentActiveEx(this GameObject obj, bool value)
	{
		if (null == obj) return;
		Transform parent = obj.transform.parent;
		if (null == parent) return;
		if (null == parent.gameObject) return;

		parent.gameObject.SetActive(value);
	}

    public static void SetActiveEx(this Component component, bool value)
    {
        if (null == component) return;
        if (null == component.gameObject) return;
        component.gameObject.SetActive(value);
    }

	public static void SetParentActiveEx(this Component component, bool value)
	{
		if (null == component) return;
		Transform parent = component.transform.parent;
		if (null == parent) return;
		if (null == parent.gameObject) return;

		parent.gameObject.SetActive(value);
	}

    public static void DestroyGameObject(this GameObject obj)
    {
        if (null == obj) return;
        UnityEngine.Object.Destroy(obj);
    }

    public static void DestroyGameObject(this Component component)
    {
        if (null == component) return;
        DestroyGameObject(component.gameObject);
    }

    public static void SetEnableEx(this Behaviour obj, bool value)
	{
		if (null == obj) return;
		obj.enabled = value;
    }
}
