using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class Agent : MonoBehaviour
{
    private readonly string _pathToDictioanry = "C://IT//QDictionary.txt";
    private readonly string _pathToMap = "C://IT//map.png";

    private FlowMap _flowMap;
    private Dictionary<int, double[]> _Q_dictionary = new();
    private StrengthVector[] _actions;


    private float nextTime = 0f;
    public float interval = 1f; // Интервал в секундах


    // Start is called before the first frame update
    void Start()
    {
        transform.position = new Vector2(0, 0);

        _actions = new StrengthVector[]
        {
            new(5, 0), new(5, 45), new(5, 90), new(5, 135), new(5, 180), new(5, 225), new(5, 270), new(5, 315)
        };

        _Q_dictionary = ParseFile(_pathToDictioanry);


        Texture2D texture = Resources.Load<Texture2D>("map");

        _flowMap = MapReader.GetArrayFromImage();
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time >= nextTime)
        {
            nextTime = Time.time + interval;

            Point current_position = new Point((int)Math.Round(transform.position.x), (int)Math.Round(transform.position.y));
            int best_action_index = -1;

            for (int i = 0; i < _Q_dictionary[current_position].Length; i++)
                if (_Q_dictionary[current_position][i] == 1.0)
                    best_action_index = i;

            (int, int) best_action = _actions[best_action_index];


            int delta_x = (int)Math.Round(Math.Cos((double)best_action.Item2 * Math.PI / 180)) * best_action.Item1;
            int delta_y = (int)Math.Round(Math.Sin((double)best_action.Item2 * Math.PI / 180)) * best_action.Item1;

            transform.position = new Vector2(transform.position.x + delta_x, transform.position.y + delta_y);
        }
    }


    static Dictionary<int, double[]> ParseFile(string path)
    {
        var result = new Dictionary<int, double[]>();

        foreach (var line in File.ReadLines(path))
        {
            var parts = line.Split(": ");
            if (parts.Length != 2) continue;

            if (int.TryParse(parts[0], out int key))
            {
                double[] values = parts[1]
                    .Split(' ')
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => double.Parse(s.Replace(',', '.')))
                    .ToArray();

                result[key] = values;
            }
        }

        return result;
    }
}
