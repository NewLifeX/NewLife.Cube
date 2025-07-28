namespace NewLife.Cube.Charts.Models;

/// <summary>箱线图元素</summary>
/// <param name="Min">最小值</param>
/// <param name="Q1">下四分位数</param>
/// <param name="Median">中位数</param>
/// <param name="Q3">上四分位数</param>
/// <param name="Max">最大值</param>
public record BoxplotItem(Double Min, Double Q1, Double Median, Double Q3, Double Max);