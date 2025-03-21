using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;


// Класс для загрузки данных из Json-файла
[Serializable]
public class ConfigData
{
    public string WorkingDir;
    public string MapPNGPath;
    public string MapARRAYPath;
    public string InputQArrayPath;
    public string StartPosition;
    public string EndPosition;
    public string[] ColorMap;
}


public class Agent : MonoBehaviour
{
    private FlowMap _flowMap;
    private Dictionary<int, double[]> _Q_dictionary = new();
    private StrengthVector[] _actions;

    private float nextTime = 0f;
    public float interval = 1f; // Интервал в секундах

    private readonly int _obstacleLen = 3;

    // Start is called before the first frame update
    void Start()
    {
        // Загрузка json файла с параметрами
        string path = Path.Combine(Application.streamingAssetsPath, "appsettings.json");
        string jsonText = File.ReadAllText(path);
        ConfigData config = JsonUtility.FromJson<ConfigData>(jsonText);

        Point startPosition = (new Point( int.Parse(config.StartPosition.Trim('(', ')').Split(',')[0]), int.Parse(config.StartPosition.Trim('(', ')').Split(',')[1]) ) );
        Point endPosition = (new Point(int.Parse(config.EndPosition.Trim('(', ')').Split(',')[0]), int.Parse(config.EndPosition.Trim('(', ')').Split(',')[1])));

        transform.position = new Vector2(startPosition.X, startPosition.Y);

        _actions = new StrengthVector[]
        {
            new(5, 0), new(5, 45), new(5, 90), new(5, 135), new(5, 180), new(5, 225), new(5, 270), new(5, 315)
        };


        _Q_dictionary = ParseFile(Path.Combine(config.WorkingDir, config.InputQArrayPath));


        Texture2D texture = Resources.Load<Texture2D>("map");
        ColorMatch[] colorMap = MapReader.GetColorMapFromString(config.ColorMap);

        _flowMap = MapReader.GetArrayFromImage(texture, colorMap);
        _flowMap.StartPoint = startPosition;
        _flowMap.EndPoint = endPosition;

        Debug.Log($"Start position flow: {_flowMap.GetFlow(startPosition)}");
        Debug.Log($"End position flow: {_flowMap.GetFlow(endPosition)}");
    }


    // Update is called once per frame
    void Update()
    {
        if (Time.time >= nextTime)
        {
            nextTime = Time.time + interval;

            Point current_position = new((int)Math.Round(transform.position.x), (int)Math.Round(transform.position.y));
            State state = CoordsToNewState(current_position);
            int stateHash = state.GetHashCode();

            int best_action_index = 0;

            if (_Q_dictionary.ContainsKey(stateHash))
            {
                Debug.Log($"Key found: {state.ToString()}");

                for (int i = 0; i < _Q_dictionary[stateHash].Length; i++)
                {
                    if (_Q_dictionary[stateHash][i] > _Q_dictionary[stateHash][best_action_index])
                        best_action_index = i;
                }
            } else
            {
                Debug.Log($"Key NOT found: {state.ToString()}");
            }


            StrengthVector best_action = _actions[best_action_index];


            int delta_x = (int)Math.Round(Math.Cos((double)best_action.Angle * Math.PI / 180)) * best_action.Strength;
            int delta_y = (int)Math.Round(Math.Sin((double)best_action.Angle * Math.PI / 180)) * best_action.Strength;

            transform.position = new Vector2(transform.position.x + delta_x, transform.position.y + delta_y);
        }
    }




    static Dictionary<int, double[]> ParseFile(string path)
    {
        var result = new Dictionary<int, double[]>();

        foreach (var line in File.ReadLines(path))
        {
            var parts = line.Split(':');
            if (parts.Length != 2) continue;

            if (int.TryParse(parts[0].Trim(), out int key))
            {
                double[] values = parts[1]
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries) // Удаляем пустые элементы
                    .Select(s =>
                    {
                        if (double.TryParse(s.Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double num))
                            return num;
                        return double.NaN; // Если парсинг не удался, вставляем NaN
                    })
                    .Where(d => !double.IsNaN(d)) // Убираем невалидные значения
                    .ToArray();

                result[key] = values;
            }
        }

        return result;
    }

    static Dictionary<int, double[]> ParseFile2(string path)
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


    public State CoordsToNewState(Point coords)
    {
        // Заполнение данных о территории вокруг агента
        Obstacles around = new(_obstacleLen);

        int halfLength = ((around.Len - 1) / 2);

        for (int x = -1 * halfLength; x <= halfLength; x++)
        {
            for (int y = -1 * halfLength; y <= halfLength; y++)
            {
                if (IndexOutOfBounds(new Point(coords.X + x, coords.Y + y)))
                {
                    around.SetValue(new Point(x + halfLength, y + halfLength), ObstaclesEnum.Obstacle);
                }
                else
                {
                    if (_flowMap.GetFlow(coords.X + x, coords.Y + y).Strength == new StrengthVector(-1, -1).Strength && _flowMap.GetFlow(coords.X + x, coords.Y + y).Angle == new StrengthVector(-1, -1).Angle)
                        around.SetValue(new Point(x + halfLength, y + halfLength), ObstaclesEnum.Obstacle);
                }
            }
        }


        // Расстояние и угол до цели
        double distance_x = _flowMap.EndPoint.X - coords.X;
        double distance_y = _flowMap.EndPoint.Y - coords.Y;

        int distance = (int)Math.Round(Math.Sqrt(distance_x * distance_x + distance_y * distance_y));
        double degree1 = Math.Acos(distance_x / distance) * 180 / Math.PI;

        // Округление расстояния и угла до цели

        int degree = (int)Math.Round(degree1 / 10) * 10;

        int lower = distance - (distance % 3);
        int upper = lower + 3;
        distance = (distance - lower < upper - distance) ? lower : upper;


        StrengthVector distanceToEndPosition = new(distance, degree);
        StrengthVector currentFlow = _flowMap.GetFlow(coords);

        State state = new()
        {
            DistanceToEndPosition = distanceToEndPosition,
            CurrentFlow = currentFlow,
            Obstacles = around
        };

        return state;
    }




    private bool IndexOutOfBounds(Point coords)
    {
        bool up = (coords.Y >= _flowMap.LenY);
        bool down = (coords.Y < 0);
        bool left = (coords.X < 0);
        bool right = (coords.X >= _flowMap.LenX);

        return (up || down || left || right);
    }
}
