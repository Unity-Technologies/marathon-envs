using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAgents;
using UnityEngine;

public class Scorer : MonoBehaviour
{
    public int TotalEpisodesToScore = 100;
    public int EpisodesScored;

    public float AverageScore;
    public float StdDiv;
    public string ScoreInfo;
    public List<float> scores;
    public List<string> scoreInfos;
    [TextArea]
    public string ScoreReport;
    // Start is called before the first frame update
    void Start()
    {
        scores = new List<float>(TotalEpisodesToScore);
        scoreInfos = new List<string>(TotalEpisodesToScore);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ReportScore(float score, string scoreInfo)
    {
        if (EpisodesScored >= TotalEpisodesToScore)
            return;
        scores.Add(score);
        scoreInfos.Add(scoreInfo);
        ScoreInfo = scoreInfo;
        EpisodesScored = scores.Count;
        AverageScore = scores.Average();
        var sum = scores.Sum(d => (d - AverageScore) * (d - AverageScore));
        StdDiv = Mathf.Sqrt(sum / EpisodesScored);
        string name = string.Empty;
        var spawnEnv = FindObjectOfType<SpawnableEnv>();
        if (spawnEnv != null)
            name = $"{spawnEnv.gameObject.name} ";
        ScoreReport = $"{name}AveScore:{AverageScore}, StdDiv:{StdDiv} over {EpisodesScored} episodes using {scoreInfo}";
    }
}
