using UnityEngine;
using System.Collections;
using System.Text;

public static class TransformExtensions
{
    #region Transform Init
    public static void InitParent(this Transform transform, Transform parent)
    {
        if (null == transform) return;
        transform.SetParent(parent);
    }

    public static void InitParent(this Component component, Transform parent)
    {
        if (null == component) return;
        InitParent(component.transform, parent);
    }

    public static void InitParent(this GameObject go, Transform parent)
    {
        if (null == go) return;
        InitParent(go.transform, parent);
    }


    public static void InitLocalTransform(this Transform transform)
    {
		if (null == transform) return;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    public static void InitLocalTransform(this Component component)
    {
        if (null == component) return;
        InitLocalTransform(component.transform);
    }

    public static void InitLocalTransform(this GameObject go)
    {
        if (null == go) return;
        InitLocalTransform(go.transform);
    }


	public static void InitParentAndLocalTransform(this Transform transform, Transform parent)
	{
		if (null == transform) return;
		transform.SetParent(parent);
		transform.InitLocalTransform();
	}

    public static void InitParentAndLocalTransform(this Component component, Transform parent)
    {
        if (null == component) return;
        InitParentAndLocalTransform(component.transform, parent);
    }

    public static void InitParentAndLocalTransform(this GameObject go, Transform parent)
    {
        if (null == go) return;
        InitParentAndLocalTransform(go.transform, parent);
    }


	public static string FullName(this Transform transform)
	{
		if (null == transform) return string.Empty;

        var builder = StringUtility.SingleBuilder();
        builder.Append(transform.name);
		Transform trans = transform.parent;
		while (null != trans)
		{
            builder.Insert(0, trans.name + "/");
			trans = trans.parent;
		}
        return builder.ToString();
	}

	public static string FullName(this GameObject go)
	{
		if (null == go) return string.Empty;
		return go.transform.FullName();
	}

    public static string FullName(this Component component)
    {
        if (null == component) return string.Empty;
        return component.transform.FullName();
    }

    #endregion



    #region Transform Position

    public static Vector3 GetWorldPos(this Transform transform)
    {
		if (null == transform) return Vector3.zero;
        return transform.position;
    }

    public static Vector3 GetLocalPos(this Transform transform)
    {
		if (null == transform) return Vector3.zero;
        return transform.localPosition;
    }

    public static void SetWorldPos(this Transform transform, Vector3 pos)
    {
		if (null == transform) return;
        transform.position = pos;
    }

    public static void SetWorldPos(this Transform transform, float x, float y, float z)
    {
        if (null == transform) return;
        transform.position = new Vector3(x, y, z);
    }

    public static void SetWorldPosX(this Transform transform, float x)
    {
		if (null == transform) return;
        Vector3 tempPos = transform.position;
        tempPos.x = x;
        transform.position = tempPos;
    }

    public static void SetWorldPosY(this Transform transform, float y)
    {
		if (null == transform) return;
        Vector3 tempPos = transform.position;
        tempPos.y = y;
        transform.position = tempPos;
    }

    public static void SetWorldPosZ(this Transform transform, float z)
    {
		if (null == transform) return;
        Vector3 tempPos = transform.position;
        tempPos.z = z;
        transform.position = tempPos;
    }

    public static void SetLocalPos(this Transform transform, Vector3 pos)
    {
		if (null == transform) return;
        transform.localPosition = pos;
    }

    public static void SetLocalPos(this Transform transform, float x, float y, float z)
    {
        if (null == transform) return;
        transform.localPosition = new Vector3(x, y, z);
    }

    public static void SetLocalPosX(this Transform transform, float x)
    {
		if (null == transform) return;
        Vector3 tempPos = transform.localPosition;
        tempPos.x = x;
        transform.localPosition = tempPos;
    }

    public static void SetLocalPosY(this Transform transform, float y)
    {
		if (null == transform) return;
        Vector3 tempPos = transform.localPosition;
        tempPos.y = y;
        transform.localPosition = tempPos;
    }

    public static void SetLocalPosZ(this Transform transform, float z)
    {
		if (null == transform) return;
        Vector3 tempPos = transform.localPosition;
        tempPos.z = z;
        transform.localPosition = tempPos;
    }

    public static void AddWorldPos(this Transform transform, Vector3 pos)
    {
		if (null == transform) return;
        transform.position += pos;
    }

    public static void AddWorldPos(this Transform transform, float x, float y, float z)
    {
        if (null == transform) return;
        transform.position += new Vector3(x, y, z);
    }

    public static void AddWorldPosX(this Transform transform, float x)
    {
		if (null == transform) return;
        Vector3 tempPos = transform.position;
        tempPos.x += x;
        transform.position = tempPos;
    }

    public static void AddWorldPosY(this Transform transform, float y)
    {
		if (null == transform) return;
        Vector3 tempPos = transform.position;
        tempPos.y += y;
        transform.position = tempPos;
    }

    public static void AddWorldPosZ(this Transform transform, float z)
    {
		if (null == transform) return;
        Vector3 tempPos = transform.position;
        tempPos.z += z;
        transform.position = tempPos;
    }

    public static void AddLocalPos(this Transform transform, Vector3 pos)
    {
		if (null == transform) return;
        transform.localPosition += pos;
    }

    public static void AddLocalPos(this Transform transform, float x, float y, float z)
    {
        if (null == transform) return;
        transform.localPosition += new Vector3(x, y, z);
    }

    public static void AddLocalPosX(this Transform transform, float x)
    {
		if (null == transform) return;
        Vector3 tempPos = transform.localPosition;
        tempPos.x += x;
        transform.localPosition = tempPos;
    }

    public static void AddLocalPosY(this Transform transform, float y)
    {
		if (null == transform) return;
        Vector3 tempPos = transform.localPosition;
        tempPos.y += y;
        transform.localPosition = tempPos;
    }

    public static void AddLocalPosZ(this Transform transform, float z)
    {
		if (null == transform) return;
        Vector3 tempPos = transform.localPosition;
        tempPos.z += z;
        transform.localPosition = tempPos;
    }

    #endregion



    #region GameObject Position

    public static Vector3 GetWorldPos(this GameObject go)
    {
		if (null == go) return Vector3.zero;
        return go.transform.GetWorldPos();
    }

    public static Vector3 GetLocalPos(this GameObject go)
    {
		if (null == go) return Vector3.zero;
        return go.transform.GetLocalPos();
    }

    public static void SetWorldPos(this GameObject go, Vector3 pos)
    {
		if (null == go) return;
        go.transform.SetWorldPos(pos);
    }

    public static void SetWorldPos(this GameObject go, float x, float y, float z)
    {
        if (null == go) return;
        go.transform.SetWorldPos(x, y, z);
    }

    public static void SetWorldPosX(this GameObject go, float x)
    {
		if (null == go) return;
        go.transform.SetWorldPosX(x);
    }

    public static void SetWorldPosY(this GameObject go, float y)
    {
		if (null == go) return;
        go.transform.SetWorldPosY(y);
    }

    public static void SetWorldPosZ(this GameObject go, float z)
    {
		if (null == go) return;
        go.transform.SetWorldPosZ(z);
    }

    public static void SetLocalPos(this GameObject go, Vector3 pos)
    {
		if (null == go) return;
        go.transform.SetLocalPos(pos);
    }

    public static void SetLocalPos(this GameObject go, float x, float y, float z)
    {
        if (null == go) return;
        go.transform.SetLocalPos(x, y, z);
    }

    public static void SetLocalPosX(this GameObject go, float x)
    {
		if (null == go) return;
        go.transform.SetLocalPosX(x);
    }

    public static void SetLocalPosY(this GameObject go, float y)
    {
		if (null == go) return;
        go.transform.SetLocalPosY(y);
    }

    public static void SetLocalPosZ(this GameObject go, float z)
    {
		if (null == go) return;
        go.transform.SetLocalPosZ(z);
    }

    public static void AddWorldPos(this GameObject go, Vector3 pos)
    {
		if (null == go) return;
        go.transform.AddWorldPos(pos);
    }

    public static void AddWorldPos(this GameObject go, float x, float y, float z)
    {
        if (null == go) return;
        go.transform.AddWorldPos(x, y, z);
    }

    public static void AddWorldPosX(this GameObject go, float x)
    {
		if (null == go) return;
        go.transform.AddWorldPosX(x);
    }

    public static void AddWorldPosY(this GameObject go, float y)
    {
		if (null == go) return;
        go.transform.AddWorldPosY(y);
    }

    public static void AddWorldPosZ(this GameObject go, float z)
    {
		if (null == go) return;
        go.transform.AddWorldPosZ(z);
    }

    public static void AddLocalPos(this GameObject go, Vector3 pos)
    {
		if (null == go) return;
        go.transform.AddLocalPos(pos);
    }

    public static void AddLocalPos(this GameObject go, float x, float y, float z)
    {
        if (null == go) return;
        go.transform.AddLocalPos(x, y, z);
    }

    public static void AddLocalPosX(this GameObject go, float x)
    {
		if (null == go) return;
        go.transform.AddLocalPosX(x);
    }

    public static void AddLocalPosY(this GameObject go, float y)
    {
		if (null == go) return;
        go.transform.AddLocalPosY(y);
    }

    public static void AddLocalPosZ(this GameObject go, float z)
    {
		if (null == go) return;
        go.transform.AddLocalPosZ(z);
    }

    #endregion



    #region Component Position

    public static Vector3 GetWorldPos(this Component go)
    {
        if (null == go) return Vector3.zero;
        return go.transform.GetWorldPos();
    }

    public static Vector3 GetLocalPos(this Component go)
    {
        if (null == go) return Vector3.zero;
        return go.transform.GetLocalPos();
    }

    public static void SetWorldPos(this Component go, Vector3 pos)
    {
        if (null == go) return;
        go.transform.SetWorldPos(pos);
    }

    public static void SetWorldPos(this Component go, float x, float y, float z)
    {
        if (null == go) return;
        go.transform.SetWorldPos(x, y, z);
    }

    public static void SetWorldPosX(this Component go, float x)
    {
        if (null == go) return;
        go.transform.SetWorldPosX(x);
    }

    public static void SetWorldPosY(this Component go, float y)
    {
        if (null == go) return;
        go.transform.SetWorldPosY(y);
    }

    public static void SetWorldPosZ(this Component go, float z)
    {
        if (null == go) return;
        go.transform.SetWorldPosZ(z);
    }

    public static void SetLocalPos(this Component go, Vector3 pos)
    {
        if (null == go) return;
        go.transform.SetLocalPos(pos);
    }

    public static void SetLocalPos(this Component go, float x, float y, float z)
    {
        if (null == go) return;
        go.transform.SetLocalPos(x, y, z);
    }

    public static void SetLocalPosX(this Component go, float x)
    {
        if (null == go) return;
        go.transform.SetLocalPosX(x);
    }

    public static void SetLocalPosY(this Component go, float y)
    {
        if (null == go) return;
        go.transform.SetLocalPosY(y);
    }

    public static void SetLocalPosZ(this Component go, float z)
    {
        if (null == go) return;
        go.transform.SetLocalPosZ(z);
    }

    public static void AddWorldPos(this Component go, Vector3 pos)
    {
        if (null == go) return;
        go.transform.AddWorldPos(pos);
    }

    public static void AddWorldPos(this Component go, float x, float y, float z)
    {
        if (null == go) return;
        go.transform.AddWorldPos(x, y, z);
    }

    public static void AddWorldPosX(this Component go, float x)
    {
        if (null == go) return;
        go.transform.AddWorldPosX(x);
    }

    public static void AddWorldPosY(this Component go, float y)
    {
        if (null == go) return;
        go.transform.AddWorldPosY(y);
    }

    public static void AddWorldPosZ(this Component go, float z)
    {
        if (null == go) return;
        go.transform.AddWorldPosZ(z);
    }

    public static void AddLocalPos(this Component go, Vector3 pos)
    {
        if (null == go) return;
        go.transform.AddLocalPos(pos);
    }

    public static void AddLocalPos(this Component go, float x, float y, float z)
    {
        if (null == go) return;
        go.transform.AddLocalPos(x, y, z);
    }

    public static void AddLocalPosX(this Component go, float x)
    {
        if (null == go) return;
        go.transform.AddLocalPosX(x);
    }

    public static void AddLocalPosY(this Component go, float y)
    {
        if (null == go) return;
        go.transform.AddLocalPosY(y);
    }

    public static void AddLocalPosZ(this Component go, float z)
    {
        if (null == go) return;
        go.transform.AddLocalPosZ(z);
    }

    #endregion



    #region Transform EulerAngles

    public static void SetWorldEulerX(this Transform transform, float x)
    {
		if (null == transform) return;
        Vector3 tempEuler = transform.eulerAngles;
        tempEuler.x = x;
        transform.eulerAngles = tempEuler;
    }

    public static void SetWorldEulerY(this Transform transform, float y)
    {
		if (null == transform) return;
        Vector3 tempEuler = transform.eulerAngles;
        tempEuler.y = y;
        transform.eulerAngles = tempEuler;
    }

    public static void SetWorldEulerZ(this Transform transform, float z)
    {
		if (null == transform) return;
        Vector3 tempEuler = transform.eulerAngles;
        tempEuler.z = z;
        transform.eulerAngles = tempEuler;
    }

    public static void SetLocalEulerX(this Transform transform, float x)
    {
		if (null == transform) return;
        Vector3 tempEuler = transform.localEulerAngles;
        tempEuler.x = x;
        transform.localEulerAngles = tempEuler;
    }

    public static void SetLocalEulerY(this Transform transform, float y)
    {
		if (null == transform) return;
        Vector3 tempEuler = transform.localEulerAngles;
        tempEuler.y = y;
        transform.localEulerAngles = tempEuler;
    }

    public static void SetLocalEulerZ(this Transform transform, float z)
    {
		if (null == transform) return;
        Vector3 tempEuler = transform.localEulerAngles;
        tempEuler.z = z;
        transform.localEulerAngles = tempEuler;
    }

    public static void AddWorldEuler(this Transform transform, Vector3 euler)
    {
		if (null == transform) return;
        transform.eulerAngles += euler;
    }

    public static void AddWorldEuler(this Transform transform, float x, float y, float z)
    {
        if (null == transform) return;
        transform.eulerAngles += new Vector3(x, y, z);
    }

    public static void AddWorldEulerX(this Transform transform, float x)
    {
		if (null == transform) return;
        Vector3 tempEuler = transform.eulerAngles;
        tempEuler.x += x;
        transform.eulerAngles = tempEuler;
    }

    public static void AddWorldEulerY(this Transform transform, float y)
    {
		if (null == transform) return;
        Vector3 tempEuler = transform.eulerAngles;
        tempEuler.y += y;
        transform.eulerAngles = tempEuler;
    }

    public static void AddWorldEulerZ(this Transform transform, float z)
    {
		if (null == transform) return;
        Vector3 tempEuler = transform.eulerAngles;
        tempEuler.z += z;
        transform.eulerAngles = tempEuler;
    }

    public static void AddLocalEuler(this Transform transform, Vector3 euler)
    {
		if (null == transform) return;
        transform.localEulerAngles += euler;
    }

    public static void AddLocalEuler(this Transform transform, float x, float y, float z)
    {
        if (null == transform) return;
        transform.localEulerAngles += new Vector3(x, y, z);
    }

    public static void AddLocalEulerX(this Transform transform, float x)
    {
		if (null == transform) return;
        Vector3 tempEuler = transform.localEulerAngles;
        tempEuler.x += x;
        transform.localEulerAngles = tempEuler;
    }

    public static void AddLocalEulerY(this Transform transform, float y)
    {
		if (null == transform) return;
        Vector3 tempEuler = transform.localEulerAngles;
        tempEuler.y += y;
        transform.localEulerAngles = tempEuler;
    }

    public static void AddLocalEulerZ(this Transform transform, float z)
    {
		if (null == transform) return;
        Vector3 tempEuler = transform.localEulerAngles;
        tempEuler.z += z;
        transform.localEulerAngles = tempEuler;
    }
    
#endregion



    #region GameObject EulerAngles

    public static void SetWorldEulerX(this GameObject go, float x)
    {
		if (null == go) return;
        go.transform.SetWorldEulerX(x);
    }

    public static void SetWorldEulerY(this GameObject go, float y)
    {
		if (null == go) return;
		go.transform.SetWorldEulerY(y);
    }

    public static void SetWorldEulerZ(this GameObject go, float z)
    {
		if (null == go) return;
        go.transform.SetWorldEulerZ(z);
    }

    public static void SetLocalEulerX(this GameObject go, float x)
    {
		if (null == go) return;
        go.transform.SetLocalEulerX(x);
    }

    public static void SetLocalEulerY(this GameObject go, float y)
    {
		if (null == go) return;
        go.transform.SetLocalEulerY(y);
    }

    public static void SetLocalEulerZ(this GameObject go, float z)
    {
		if (null == go) return;
        go.transform.SetLocalEulerZ(z);
    }

    public static void AddWorldEuler(this GameObject go, Vector3 euler)
    {
		if (null == go) return;
		go.transform.AddWorldEuler(euler);
    }

    public static void AddWorldEuler(this GameObject go, float x, float y, float z)
    {
        if (null == go) return;
        go.transform.AddWorldEuler(x, y, z);
    }

    public static void AddWorldEulerX(this GameObject go, float x)
    {
		if (null == go) return;
		go.transform.AddWorldEulerX(x);
    }

    public static void AddWorldEulerY(this GameObject go, float y)
    {
		if (null == go) return;
		go.transform.AddWorldEulerY(y);
    }

    public static void AddWorldEulerZ(this GameObject go, float z)
    {
		if (null == go) return;
		go.transform.AddWorldEulerZ(z);
    }

    public static void AddLocalEuler(this GameObject go, Vector3 euler)
    {
		if (null == go) return;
        go.transform.AddLocalEuler(euler);
    }

    public static void AddLocalEuler(this GameObject go, float x, float y, float z)
    {
        if (null == go) return;
        go.transform.AddLocalEuler(x, y, z);
    }

    public static void AddLocalEulerX(this GameObject go, float x)
    {
		if (null == go) return;
        go.transform.AddLocalEulerX(x);
    }

    public static void AddLocalEulerY(this GameObject go, float y)
    {
		if (null == go) return;
        go.transform.AddLocalEulerY(y);
    }

    public static void AddLocalEulerZ(this GameObject go, float z)
    {
		if (null == go) return;
        go.transform.AddLocalEulerZ(z);
    }

#endregion



    #region Component Euler

    public static void SetWorldEulerX(this Component go, float x)
    {
        if (null == go) return;
        go.transform.SetWorldEulerX(x);
    }

    public static void SetWorldEulerY(this Component go, float y)
    {
        if (null == go) return;
        go.transform.SetWorldEulerY(y);
    }

    public static void SetWorldEulerZ(this Component go, float z)
    {
        if (null == go) return;
        go.transform.SetWorldEulerZ(z);
    }

    public static void SetLocalEulerX(this Component go, float x)
    {
        if (null == go) return;
        go.transform.SetLocalEulerX(x);
    }

    public static void SetLocalEulerY(this Component go, float y)
    {
        if (null == go) return;
        go.transform.SetLocalEulerY(y);
    }

    public static void SetLocalEulerZ(this Component go, float z)
    {
        if (null == go) return;
        go.transform.SetLocalEulerZ(z);
    }

    public static void AddWorldEuler(this Component go, Vector3 euler)
    {
        if (null == go) return;
        go.transform.AddWorldEuler(euler);
    }

    public static void AddWorldEuler(this Component go, float x, float y, float z)
    {
        if (null == go) return;
        go.transform.AddWorldEuler(x, y, z);
    }

    public static void AddWorldEulerX(this Component go, float x)
    {
        if (null == go) return;
        go.transform.AddWorldEulerX(x);
    }

    public static void AddWorldEulerY(this Component go, float y)
    {
        if (null == go) return;
        go.transform.AddWorldEulerY(y);
    }

    public static void AddWorldEulerZ(this Component go, float z)
    {
        if (null == go) return;
        go.transform.AddWorldEulerZ(z);
    }

    public static void AddLocalEuler(this Component go, Vector3 euler)
    {
        if (null == go) return;
        go.transform.AddLocalEuler(euler);
    }

    public static void AddLocalEuler(this Component go, float x, float y, float z)
    {
        if (null == go) return;
        go.transform.AddLocalEuler(x, y, z);
    }

    public static void AddLocalEulerX(this Component go, float x)
    {
        if (null == go) return;
        go.transform.AddLocalEulerX(x);
    }

    public static void AddLocalEulerY(this Component go, float y)
    {
        if (null == go) return;
        go.transform.AddLocalEulerY(y);
    }

    public static void AddLocalEulerZ(this Component go, float z)
    {
        if (null == go) return;
        go.transform.AddLocalEulerZ(z);
    }

    #endregion



    #region Transform Scale

    public static Vector3 GetWorldScale(this Transform transform)
    {
		if (null == transform) return Vector3.one;

		Matrix4x4 localToWorldMatrix = transform.localToWorldMatrix;
		return new Vector3(localToWorldMatrix.m00, localToWorldMatrix.m11, localToWorldMatrix.m22);
    }

    public static float GetWorldScaleX(this Transform transform)
    {
		if (null == transform) return 1.0f;
		return transform.localToWorldMatrix.m00;
    }

    public static float GetWorldScaleY(this Transform transform)
    {
		if (null == transform) return 1.0f;
		return transform.localToWorldMatrix.m11;
    }

    public static float GetWorldScaleZ(this Transform transform)
    {
		if (null == transform) return 1.0f;
		return transform.localToWorldMatrix.m22;
    }

    public static Vector3 GetLocalScale(this Transform transform)
    {
		if (null == transform) return Vector3.one;
        return transform.localScale;
    }

    public static float GetLocalScaleX(this Transform transform)
    {
		if (null == transform) return 1.0f;
        return transform.localScale.x;
    }

    public static float GetLocalScaleY(this Transform transform)
    {
		if (null == transform) return 1.0f;
        return transform.localScale.y;
    }

    public static float GetLocalScaleZ(this Transform transform)
    {
		if (null == transform) return 1.0f;
        return transform.localScale.z;
    }

    public static void SetWorldScale(this Transform transform, Vector3 scale)
    {
		if (null == transform) return;
		Matrix4x4 worldToLocalMatrix = transform.worldToLocalMatrix;
		transform.localScale = new Vector3(worldToLocalMatrix.m00 * scale.x, worldToLocalMatrix.m11 * scale.y, worldToLocalMatrix.m22 * scale.z);
    }

    public static void SetWorldScale(this Transform transform, float x, float y, float z)
    {
        if (null == transform) return;
        Matrix4x4 worldToLocalMatrix = transform.worldToLocalMatrix;
        transform.localScale = new Vector3(worldToLocalMatrix.m00 * x, worldToLocalMatrix.m11 * y, worldToLocalMatrix.m22 * z);
    }

    public static void SetWorldScale(this Transform transform, float scale)
    {
		if (null == transform) return;
        transform.SetWorldScale(Vector3.one * scale);
    }

    public static void SetWorldScaleX(this Transform transform, float scaleX)
    {
		if (null == transform) return;
		Vector3 worldScale = transform.GetWorldScale();
		worldScale.x = scaleX;
		transform.SetWorldScale(worldScale);
    }

    public static void SetWorldScaleY(this Transform transform, float scaleY)
    {
		if (null == transform) return;
		Vector3 worldScale = transform.GetWorldScale();
		worldScale.y = scaleY;
		transform.SetWorldScale(worldScale);
    }

    public static void SetWorldScaleZ(this Transform transform, float scaleZ)
    {
		if (null == transform) return;
		Vector3 worldScale = transform.GetWorldScale();
		worldScale.z = scaleZ;
		transform.SetWorldScale(worldScale);
    }

    public static void SetLocalScale(this Transform transform, Vector3 scale)
    {
		if (null == transform) return;
        transform.localScale = scale;
    }

    public static void SetLocalScale(this Transform transform, float x, float y, float z)
    {
        if (null == transform) return;
        transform.localScale = new Vector3(x, y, z);
    }

    public static void SetLocalScale(this Transform transform, float scale)
    {
		if (null == transform) return;
        transform.localScale = Vector3.one * scale;
    }

    public static void SetLocalScaleX(this Transform transform, float x)
    {
		if (null == transform) return;
		Vector3 localScale = transform.localScale;
		localScale.x = x;
		transform.localScale = localScale;
    }

    public static void SetLocalScaleY(this Transform transform, float y)
    {
		if (null == transform) return;
        Vector3 tempScale = transform.localScale;
        tempScale.y = y;
        transform.localScale = tempScale;
    }

    public static void SetLocalScaleZ(this Transform transform, float z)
    {
		if (null == transform) return;
        Vector3 tempScale = transform.localScale;
        tempScale.z = z;
        transform.localScale = tempScale;
    }

    public static void AddWorldScale(this Transform transform, Vector3 scale)
    {
		if (null == transform) return;
        Vector3 mScale = transform.GetWorldScale();
        mScale += scale;
        transform.SetWorldScale(mScale);
    }

    public static void AddWorldScale(this Transform transform, float x, float y, float z)
    {
        if (null == transform) return;
        Vector3 mScale = transform.GetWorldScale();
        mScale += new Vector3(x, y, z);
        transform.SetWorldScale(mScale);
    }

    public static void AddWorldScale(this Transform transform, float scale)
    {
		if (null == transform) return;
        transform.AddWorldScale(Vector3.one * scale);
    }

    public static void AddWorldScaleX(this Transform transform, float scaleX)
    {
		if (null == transform) return;
        float mScaleX = transform.GetWorldScaleX();
        mScaleX += scaleX;
        transform.SetWorldScaleX(mScaleX);
    }

    public static void AddWorldScaleY(this Transform transform, float scaleY)
    {
		if (null == transform) return;
        float mScaleY = transform.GetWorldScaleY();
        mScaleY += scaleY;
        transform.SetWorldScaleY(mScaleY);
    }

    public static void AddWorldScaleZ(this Transform transform, float scaleZ)
    {
		if (null == transform) return;
        float mScaleZ = transform.GetWorldScaleZ();
        mScaleZ += scaleZ;
        transform.SetWorldScaleZ(mScaleZ);
    }

    public static void AddLocalScale(this Transform transform, Vector3 scale)
    {
		if (null == transform) return;
        transform.localScale += scale;
    }

    public static void AddLocalScale(this Transform transform, float x, float y, float z)
    {
        if (null == transform) return;
        transform.localScale += new Vector3(x, y, z);
    }

    public static void AddLocalScale(this Transform transform, float scale)
    {
		if (null == transform) return;
        transform.localScale += Vector3.one * scale;
    }

    public static void AddLocalScaleX(this Transform transform, float x)
    {
		if (null == transform) return;
        Vector3 tempScale = transform.localScale;
        tempScale.x += x;
        transform.localScale = tempScale;
    }

    public static void AddLocalScaleY(this Transform transform, float y)
    {
		if (null == transform) return;
        Vector3 tempScale = transform.localScale;
        tempScale.y += y;
        transform.localScale = tempScale;
    }

    public static void AddLocalScaleZ(this Transform transform, float z)
    {
		if (null == transform) return;
        Vector3 tempScale = transform.localScale;
        tempScale.z += z;
        transform.localScale = tempScale;
    }

    public static void MulWorldScale(this Transform transform, Vector3 scale)
    {
		if (null == transform) return;
        Vector3 mScale = transform.GetWorldScale();
        mScale.x *= scale.x;
        mScale.y *= scale.y;
        mScale.z *= scale.z;
        transform.SetWorldScale(mScale);
    }


    public static void MulWorldScale(this Transform transform, float x, float y, float z)
    {
        if (null == transform) return;
        Vector3 mScale = transform.GetWorldScale();
        mScale.x *= x;
        mScale.y *= y;
        mScale.z *= z;
        transform.SetWorldScale(mScale);
    }

    public static void MulWorldScale(this Transform transform, float scale)
    {
		if (null == transform) return;
        transform.MulWorldScale(Vector3.one * scale);
    }

    public static void MulWorldScaleX(this Transform transform, float scaleX)
    {
		if (null == transform) return;
        float mScaleX = transform.GetWorldScaleX();
        mScaleX *= scaleX;
        transform.SetWorldScale(mScaleX);
    }

    public static void MulWorldScaleY(this Transform transform, float scaleY)
    {
		if (null == transform) return;
        float mScaleY = transform.GetWorldScaleY();
        mScaleY *= scaleY;
        transform.SetWorldScaleY(mScaleY);
    }

    public static void MulWorldScaleZ(this Transform transform, float scaleZ)
    {
		if (null == transform) return;
        float mScaleZ = transform.GetWorldScaleZ();
        mScaleZ *= scaleZ;
        transform.SetWorldScaleZ(mScaleZ);
    }

    public static void MulLocalScale(this Transform transform, Vector3 scale)
    {
		if (null == transform) return;
        Vector3 mLocalScale = transform.localScale;
        mLocalScale.x *= scale.x;
        mLocalScale.y *= scale.y;
        mLocalScale.z *= scale.z;
        transform.localScale = mLocalScale;
    }

    public static void MulLocalScale(this Transform transform, float x, float y, float z)
    {
        if (null == transform) return;
        Vector3 mLocalScale = transform.localScale;
        mLocalScale.x *= x;
        mLocalScale.y *= y;
        mLocalScale.z *= z;
        transform.localScale = mLocalScale;
    }

    public static void MulLocalScale(this Transform transform, float scale)
    {
		if (null == transform) return;
        transform.MulLocalScale(Vector3.one * scale);
    }

    public static void MulLocalScaleX(this Transform transform, float scaleX)
    {
		if (null == transform) return;
        Vector3 mLocalScale = transform.localScale;
        mLocalScale.x *= scaleX;
        transform.localScale = mLocalScale;
    }

    public static void MulLocalScaleY(this Transform transform, float scaleY)
    {
		if (null == transform) return;
        Vector3 mLocalScale = transform.localScale;
        mLocalScale.y *= scaleY;
        transform.localScale = mLocalScale;
    }

    public static void MulLocalScaleZ(this Transform transform, float scaleZ)
    {
		if (null == transform) return;
        Vector3 mLocalScale = transform.localScale;
        mLocalScale.z *= scaleZ;
        transform.localScale = mLocalScale;
    }

    #endregion



    #region GameObject Scale

    public static Vector3 GetWorldScale(this GameObject go)
    {
		if (null == go) return Vector3.one;
        return go.transform.GetWorldScale();
    }

    public static float GetWorldScaleX(this GameObject go)
    {
		if (null == go) return 1.0f;
        return go.transform.GetWorldScaleX();
    }

    public static float GetWorldScaleY(this GameObject go)
    {
		if (null == go) return 1.0f;
        return go.transform.GetWorldScaleY();
    }

    public static float GetWorldScaleZ(this GameObject go)
    {
		if (null == go) return 1.0f;
        return go.transform.GetWorldScaleZ();
    }

    public static Vector3 GetLocalScale(this GameObject go)
    {
		if (null == go) return Vector3.one;
        return go.transform.GetLocalScale();
    }

    public static float GetLocalScaleX(this GameObject go)
    {
		if (null == go) return 1.0f;
        return go.transform.GetLocalScaleX();
    }

    public static float GetLocalScaleY(this GameObject go)
    {
		if (null == go) return 1.0f;
        return go.transform.GetLocalScaleY();
    }

    public static float GetLocalScaleZ(this GameObject go)
    {
		if (null == go) return 1.0f;
        return go.transform.GetLocalScaleZ();
    }

    public static void SetWorldScale(this GameObject go, Vector3 scale)
    {
		if (null == go) return;
        go.transform.SetWorldScale(scale);
    }

    public static void SetWorldScale(this GameObject go, float x, float y, float z)
    {
        if (null == go) return;
        go.transform.SetWorldScale(x, y, z);
    }

    public static void SetWorldScale(this GameObject go, float scale)
    {
		if (null == go) return;
        go.transform.SetWorldScale(scale);
    }

    public static void SetWorldScaleX(this GameObject go, float x)
    {
		if (null == go) return;
        go.transform.SetWorldScaleX(x);
    }

    public static void SetWorldScaleY(this GameObject go, float y)
    {
		if (null == go) return;
        go.transform.SetWorldScaleY(y);
    }

    public static void SetWorldScaleZ(this GameObject go, float z)
    {
		if (null == go) return;
        go.transform.SetWorldScaleZ(z);
    }

    public static void SetLocalScale(this GameObject go, Vector3 scale)
    {
		if (null == go) return;
        go.transform.SetLocalScale(scale);
    }

    public static void SetLocalScale(this GameObject go, float x, float y, float z)
    {
        if (null == go) return;
        go.transform.SetLocalScale(x, y, z);
    }

    public static void SetLocalScale(this GameObject go, float scale)
    {
		if (null == go) return;
        go.transform.SetLocalScale(scale);
    }

    public static void SetLocalScaleX(this GameObject go, float x)
    {
		if (null == go) return;
        go.transform.SetLocalScaleX(x);
    }

    public static void SetLocalScaleY(this GameObject go, float y)
    {
		if (null == go) return;
        go.transform.SetLocalScaleY(y);
    }

    public static void SetLocalScaleZ(this GameObject go, float z)
    {
		if (null == go) return;
        go.transform.SetLocalScaleZ(z);
    }

    public static void AddWorldScale(this GameObject go, Vector3 scale)
    {
		if (null == go) return;
        go.transform.AddWorldScale(scale);
    }

    public static void AddWorldScale(this GameObject go, float x, float y, float z)
    {
        if (null == go) return;
        go.transform.AddWorldScale(x, y, z);
    }

    public static void AddWorldScale(this GameObject go, float scale)
    {
		if (null == go) return;
        go.transform.AddWorldScale(scale);
    }

    public static void AddWorldScaleX(this GameObject go, float x)
    {
		if (null == go) return;
        go.transform.AddWorldScaleX(x);
    }

    public static void AddWorldScaleY(this GameObject go, float y)
    {
		if (null == go) return;
        go.transform.AddWorldScaleY(y);
    }

    public static void AddWorldScaleZ(this GameObject go, float z)
    {
		if (null == go) return;
        go.transform.AddWorldScaleZ(z);
    }

    public static void AddLocalScale(this GameObject go, Vector3 scale)
    {
		if (null == go) return;
        go.transform.AddLocalScale(scale);
    }

    public static void AddLocalScale(this GameObject go, float x, float y, float z)
    {
        if (null == go) return;
        go.transform.AddLocalScale(x, y, z);
    }

    public static void AddLocalScale(this GameObject go, float scale)
    {
		if (null == go) return;
        go.transform.AddLocalScale(scale);
    }

    public static void AddLocalScaleX(this GameObject go, float x)
    {
		if (null == go) return;
        go.transform.AddLocalScaleX(x);
    }

    public static void AddLocalScaleY(this GameObject go, float y)
    {
		if (null == go) return;
        go.transform.AddLocalScaleY(y);
    }

    public static void AddLocalScaleZ(this GameObject go, float z)
    {
		if (null == go) return;
        go.transform.AddLocalScaleZ(z);
    }

    public static void MulWorldScale(this GameObject go, Vector3 scale)
    {
		if (null == go) return;
        go.transform.MulWorldScale(scale);
    }

    public static void MulWorldScale(this GameObject go, float x, float y, float z)
    {
        if (null == go) return;
        go.transform.MulWorldScale(x, y, z);
    }

    public static void MulWorldScale(this GameObject go, float scale)
    {
		if (null == go) return;
        go.transform.MulWorldScale(scale);
    }

    public static void MulWorldScaleX(this GameObject go, float x)
    {
		if (null == go) return;
        go.transform.MulWorldScaleX(x);
    }

    public static void MulWorldScaleY(this GameObject go, float y)
    {
		if (null == go) return;
        go.transform.MulWorldScaleY(y);
    }

    public static void MulWorldScaleZ(this GameObject go, float z)
    {
		if (null == go) return;
        go.transform.MulWorldScaleZ(z);
    }

    public static void MulLocalScale(this GameObject go, Vector3 scale)
    {
		if (null == go) return;
        go.transform.MulLocalScale(scale);
    }

    public static void MulLocalScale(this GameObject go, float x, float y, float z)
    {
        if (null == go) return;
        go.transform.MulLocalScale(x, y, z);
    }

    public static void MulLocalScale(this GameObject go, float scale)
    {
		if (null == go) return;
        go.transform.MulLocalScale(scale);
    }

    public static void MulLocalScaleX(this GameObject go, float x)
    {
		if (null == go) return;
        go.transform.MulLocalScaleX(x);
    }

    public static void MulLocalScaleY(this GameObject go, float y)
    {
		if (null == go) return;
        go.transform.MulLocalScaleY(y);
    }

    public static void MulLocalScaleZ(this GameObject go, float z)
    {
		if (null == go) return;
        go.transform.MulLocalScaleZ(z);
    }

    #endregion



    #region GameObject Scale

    public static Vector3 GetWorldScale(this Component go)
    {
        if (null == go) return Vector3.one;
        return go.transform.GetWorldScale();
    }

    public static float GetWorldScaleX(this Component go)
    {
        if (null == go) return 1.0f;
        return go.transform.GetWorldScaleX();
    }

    public static float GetWorldScaleY(this Component go)
    {
        if (null == go) return 1.0f;
        return go.transform.GetWorldScaleY();
    }

    public static float GetWorldScaleZ(this Component go)
    {
        if (null == go) return 1.0f;
        return go.transform.GetWorldScaleZ();
    }

    public static Vector3 GetLocalScale(this Component go)
    {
        if (null == go) return Vector3.one;
        return go.transform.GetLocalScale();
    }

    public static float GetLocalScaleX(this Component go)
    {
        if (null == go) return 1.0f;
        return go.transform.GetLocalScaleX();
    }

    public static float GetLocalScaleY(this Component go)
    {
        if (null == go) return 1.0f;
        return go.transform.GetLocalScaleY();
    }

    public static float GetLocalScaleZ(this Component go)
    {
        if (null == go) return 1.0f;
        return go.transform.GetLocalScaleZ();
    }

    public static void SetWorldScale(this Component go, Vector3 scale)
    {
        if (null == go) return;
        go.transform.SetWorldScale(scale);
    }

    public static void SetWorldScale(this Component go, float x, float y, float z)
    {
        if (null == go) return;
        go.transform.SetWorldScale(x, y, z);
    }

    public static void SetWorldScale(this Component go, float scale)
    {
        if (null == go) return;
        go.transform.SetWorldScale(scale);
    }

    public static void SetWorldScaleX(this Component go, float x)
    {
        if (null == go) return;
        go.transform.SetWorldScaleX(x);
    }

    public static void SetWorldScaleY(this Component go, float y)
    {
        if (null == go) return;
        go.transform.SetWorldScaleY(y);
    }

    public static void SetWorldScaleZ(this Component go, float z)
    {
        if (null == go) return;
        go.transform.SetWorldScaleZ(z);
    }

    public static void SetLocalScale(this Component go, Vector3 scale)
    {
        if (null == go) return;
        go.transform.SetLocalScale(scale);
    }

    public static void SetLocalScale(this Component go, float x, float y, float z)
    {
        if (null == go) return;
        go.transform.SetLocalScale(x, y, z);
    }

    public static void SetLocalScale(this Component go, float scale)
    {
        if (null == go) return;
        go.transform.SetLocalScale(scale);
    }

    public static void SetLocalScaleX(this Component go, float x)
    {
        if (null == go) return;
        go.transform.SetLocalScaleX(x);
    }

    public static void SetLocalScaleY(this Component go, float y)
    {
        if (null == go) return;
        go.transform.SetLocalScaleY(y);
    }

    public static void SetLocalScaleZ(this Component go, float z)
    {
        if (null == go) return;
        go.transform.SetLocalScaleZ(z);
    }

    public static void AddWorldScale(this Component go, Vector3 scale)
    {
        if (null == go) return;
        go.transform.AddWorldScale(scale);
    }

    public static void AddWorldScale(this Component go, float x, float y, float z)
    {
        if (null == go) return;
        go.transform.AddWorldScale(x, y, z);
    }

    public static void AddWorldScale(this Component go, float scale)
    {
        if (null == go) return;
        go.transform.AddWorldScale(scale);
    }

    public static void AddWorldScaleX(this Component go, float x)
    {
        if (null == go) return;
        go.transform.AddWorldScaleX(x);
    }

    public static void AddWorldScaleY(this Component go, float y)
    {
        if (null == go) return;
        go.transform.AddWorldScaleY(y);
    }

    public static void AddWorldScaleZ(this Component go, float z)
    {
        if (null == go) return;
        go.transform.AddWorldScaleZ(z);
    }

    public static void AddLocalScale(this Component go, Vector3 scale)
    {
        if (null == go) return;
        go.transform.AddLocalScale(scale);
    }

    public static void AddLocalScale(this Component go, float x, float y, float z)
    {
        if (null == go) return;
        go.transform.AddLocalScale(x, y, z);
    }

    public static void AddLocalScale(this Component go, float scale)
    {
        if (null == go) return;
        go.transform.AddLocalScale(scale);
    }

    public static void AddLocalScaleX(this Component go, float x)
    {
        if (null == go) return;
        go.transform.AddLocalScaleX(x);
    }

    public static void AddLocalScaleY(this Component go, float y)
    {
        if (null == go) return;
        go.transform.AddLocalScaleY(y);
    }

    public static void AddLocalScaleZ(this Component go, float z)
    {
        if (null == go) return;
        go.transform.AddLocalScaleZ(z);
    }

    public static void MulWorldScale(this Component go, Vector3 scale)
    {
        if (null == go) return;
        go.transform.MulWorldScale(scale);
    }

    public static void MulWorldScale(this Component go, float x, float y, float z)
    {
        if (null == go) return;
        go.transform.MulWorldScale(x, y, z);
    }

    public static void MulWorldScale(this Component go, float scale)
    {
        if (null == go) return;
        go.transform.MulWorldScale(scale);
    }

    public static void MulWorldScaleX(this Component go, float x)
    {
        if (null == go) return;
        go.transform.MulWorldScaleX(x);
    }

    public static void MulWorldScaleY(this Component go, float y)
    {
        if (null == go) return;
        go.transform.MulWorldScaleY(y);
    }

    public static void MulWorldScaleZ(this Component go, float z)
    {
        if (null == go) return;
        go.transform.MulWorldScaleZ(z);
    }

    public static void MulLocalScale(this Component go, Vector3 scale)
    {
        if (null == go) return;
        go.transform.MulLocalScale(scale);
    }

    public static void MulLocalScale(this Component go, float x, float y, float z)
    {
        if (null == go) return;
        go.transform.MulLocalScale(x, y, z);
    }

    public static void MulLocalScale(this Component go, float scale)
    {
        if (null == go) return;
        go.transform.MulLocalScale(scale);
    }

    public static void MulLocalScaleX(this Component go, float x)
    {
        if (null == go) return;
        go.transform.MulLocalScaleX(x);
    }

    public static void MulLocalScaleY(this Component go, float y)
    {
        if (null == go) return;
        go.transform.MulLocalScaleY(y);
    }

    public static void MulLocalScaleZ(this Component go, float z)
    {
        if (null == go) return;
        go.transform.MulLocalScaleZ(z);
    }

    #endregion
}
