namespace NewLife.Cube.Charts.Models;

/// <summary>K线图元素</summary>
/// <param name="Open">开盘值</param>
/// <param name="Close">收盘值</param>
/// <param name="Lowest">最低值</param>
/// <param name="Highest">最高值</param>
public record CandlestickItem(Double Open, Double Close, Double Lowest, Double Highest);