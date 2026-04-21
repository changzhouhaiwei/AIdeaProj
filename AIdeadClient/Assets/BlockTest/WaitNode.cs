using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class WaitNode : MonoBehaviour
{
    [SerializeField] ShapeNode shapePrefab;
    [SerializeField] RectTransform[] slots = new RectTransform[3];

    readonly List<ShapeNode> _active = new List<ShapeNode>();

    void Start()
    {
        if (_active.Count == 0)
            BeginRound();
    }

    public void BeginRound()
    {
        DealThree();
    }

    public void DealThree()
    {
        foreach (var s in _active)
        {
            if (s) Destroy(s.gameObject);
        }

        _active.Clear();
        EnsureSlots();

        if (shapePrefab == null || slots == null || slots.Length < 3)
            return;

        var gm = BlockGameManager.Instance;
        if (gm != null && gm.IsGameOver) return;

        for (int i = 0; i < 3; i++)
        {
            if (slots[i] == null) continue;
            var slot = slots[i];
            var kind = ShapeDefine.RandomKind();
            var rot = ShapeDefine.RandomRotation();
            var node = Instantiate(shapePrefab, slot);
            var rt = node.transform as RectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            node.Init(kind, rot, RandomBlockColor(), slot, this);
            _active.Add(node);
        }

        if (!AnyCurrentCanPlace() && gm != null) gm.EndGame();
    }

    void EnsureSlots()
    {
        if (slots != null && slots.Length >= 3 &&
            slots[0] != null && slots[1] != null && slots[2] != null)
            return;

        var list = new List<RectTransform>(3);
        var self = transform as RectTransform;
        for (int i = 0; i < transform.childCount; i++)
        {
            var rt = transform.GetChild(i) as RectTransform;
            if (rt == null || rt == self) continue;
            list.Add(rt);
            if (list.Count >= 3) break;
        }

        if (list.Count >= 3)
            slots = new[] { list[0], list[1], list[2] };
    }

    static Color RandomBlockColor()
    {
        return Color.HSVToRGB(Random.value, 0.55f, 0.96f);
    }

    public void NotifyPlaced(ShapeNode node)
    {
        _active.Remove(node);
        if (node) Destroy(node.gameObject);

        var gm = BlockGameManager.Instance;
        if (gm != null && gm.IsGameOver) return;

        if (_active.Count == 0)
        {
            DealThree();
            return;
        }

        if (!AnyCurrentCanPlace() && gm != null) gm.EndGame();
    }

    bool AnyCurrentCanPlace()
    {
        var b = BlockGameManager.Instance != null ? BlockGameManager.Instance.Board : null;
        if (b == null) return true;

        foreach (var sn in _active)
        {
            if (sn == null) continue;
            if (b.HasPlacementFor(sn.Kind, sn.Rot)) return true;
        }

        return false;
    }
}
