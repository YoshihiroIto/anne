﻿using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

namespace Anne.Model
{
    public class AppConfig
    {
        public List<string> Repositories { get; set; } = new List<string>();

        public static AppConfig LoadFromFile(string filePath)
        {
            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    return new Deserializer().Deserialize<AppConfig>(reader);
                }
            }
            catch
            {
                return new AppConfig();
            }
        }

        public static void SaveToFile(string filePath, AppConfig config)
        {
            using (var writer = new StreamWriter(filePath))
            {
                new Serializer().Serialize(writer, config);
            }
        }
    }
}