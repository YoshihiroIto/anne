﻿using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Anne.Model
{
    public class AppConfig
    {
        public List<string> Repositories { get; set; } = new List<string>();

        public int MaxCommitCount { get; set; } = int.MaxValue;

        public static AppConfig LoadFromFile(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<AppConfig>(json);
            }
            catch
            {
                return new AppConfig();
            }
        }

        public static void SaveToFile(string filePath, AppConfig config)
        {
            File.WriteAllText(filePath, JsonConvert.SerializeObject(config, Formatting.Indented));
        }
    }
}