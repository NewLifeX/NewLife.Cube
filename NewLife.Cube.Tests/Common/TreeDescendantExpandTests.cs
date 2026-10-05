using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Cube;
using Xunit;

namespace NewLife.Cube.Tests.Common;

public class TreeDescendantExpandTests
{
    record Node(Int32 Id, Int32 ParentId);

    static readonly Node Root = new(1, 0);
    static readonly Node A = new(2, 1);
    static readonly Node A1 = new(3, 2);
    static readonly Node B = new(4, 1);

    static readonly Node[] All = [Root, A, A1, B];

    static IList<Node> Expand(IList<Node> hits, Func<Node, Boolean> canView = null, Int32 maxRows = 100)
    {
        var index = All.GroupBy(e => e.ParentId).ToDictionary(g => g.Key, g => g.ToList());
        return TreeDescendantExpand.Expand(
            hits,
            n => n.Id,
            ids =>
            {
                var acc = new List<Node>();
                foreach (var id in ids)
                {
                    if (id is Int32 i && index.TryGetValue(i, out var kids)) acc.AddRange(kids);
                }
                return acc;
            },
            canView,
            maxRows);
    }

    [Fact(DisplayName = "树扩子孙_命中A含A1不含旁支B")]
    public void Expand_HitA_IncludesA1_ExcludesB()
    {
        var list = Expand([A]);
        Assert.Equal([2, 3], list.Select(e => e.Id).ToArray());
    }

    [Fact(DisplayName = "树扩子孙_命中根含全树")]
    public void Expand_HitRoot_IncludesAll()
    {
        var list = Expand([Root]);
        Assert.Equal([1, 2, 4, 3], list.Select(e => e.Id).ToArray());
    }

    [Fact(DisplayName = "树扩子孙_环不无限循环")]
    public void Expand_Cycle_DoesNotHang()
    {
        var loopA = new Node(10, 11);
        var loopB = new Node(11, 10);
        var nodes = new[] { loopA, loopB };
        var list = TreeDescendantExpand.Expand(
            new List<Node> { loopA },
            n => n.Id,
            ids => nodes.Where(n => ids.Cast<Int32>().Contains(n.ParentId)),
            maxRows: 50);
        Assert.Equal(2, list.Count);
        Assert.Contains(list, e => e.Id == 10);
        Assert.Contains(list, e => e.Id == 11);
    }

    [Fact(DisplayName = "树扩子孙_行权剔除子孙")]
    public void Expand_CanView_DropsHiddenChild()
    {
        var list = Expand([A], n => n.Id != 3);
        Assert.Equal([2], list.Select(e => e.Id).ToArray());
    }

    [Fact(DisplayName = "树扩子孙_超过封顶截断")]
    public void Expand_TruncatesAtMaxRows()
    {
        var truncated = false;
        var index = All.GroupBy(e => e.ParentId).ToDictionary(g => g.Key, g => g.ToList());
        var list = TreeDescendantExpand.Expand(
            new List<Node> { Root },
            n => n.Id,
            ids =>
            {
                var acc = new List<Node>();
                foreach (var id in ids)
                {
                    if (id is Int32 i && index.TryGetValue(i, out var kids)) acc.AddRange(kids);
                }
                return acc;
            },
            maxRows: 2,
            onTruncated: (_, _) => truncated = true);
        Assert.Equal(2, list.Count);
        Assert.True(truncated);
    }

    [Fact(DisplayName = "树表请求门控_仅tree扩")]
    public void IsTreeViewRequest_OnlyTree()
    {
        Assert.True(TreeDescendantExpand.IsTreeViewRequest("tree"));
        Assert.True(TreeDescendantExpand.IsTreeViewRequest("TREE"));
        Assert.False(TreeDescendantExpand.IsTreeViewRequest("table"));
        Assert.False(TreeDescendantExpand.IsTreeViewRequest(""));
        Assert.False(TreeDescendantExpand.IsTreeViewRequest((String)null));
    }

    [Fact(DisplayName = "封顶规范_0回落默认_过大夹取")]
    public void ResolveMaxRows_Clamp()
    {
        Assert.Equal(100_000, TreeDescendantExpand.ResolveMaxRows(0));
        Assert.Equal(100_000, TreeDescendantExpand.ResolveMaxRows(-1));
        Assert.Equal(50, TreeDescendantExpand.ResolveMaxRows(50));
        Assert.Equal(1_000_000, TreeDescendantExpand.ResolveMaxRows(9_999_999));
    }
}
