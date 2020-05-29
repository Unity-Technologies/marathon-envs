
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScoreHistogramData
{
    int _columnCount;
    int _historyDepth;

    public List<List<double>> Items;


    public ScoreHistogramData (int columnCount, int historyDepth)
    {
        _columnCount = columnCount;
        _historyDepth = historyDepth;
        ReCreateScoreHistogramData();
    }

    public void ReCreateScoreHistogramData ()
    {
        Items = new List<List<double>>();
        for (int i = 0; i < _columnCount; i++)
        {
            // var item = Enumerable.Range(0,_historyDepth).Select(x=>default(T)).ToList();
            var item = new List<double>();
            Items.Add(item);
        }
    }
    public void SetItem(int column, double value)
    {
        Items[column].Add(value);
        if (Items[column].Count > _historyDepth)
            Items[column].RemoveAt(0);
    }
    public double GetAverage(int column)
    {
        double average = Items[column].Average();
        return average;
    }
    public List<double> GetAverages()
    {
        List<double> averages = Items.Select(x=>x.Count > 0 ? x.Average() : 0).ToList();
        return averages;
    }
}
