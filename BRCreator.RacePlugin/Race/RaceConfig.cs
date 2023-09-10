using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace BRCreator.RacePlugin.Race
{
    public class RaceConfig
    {
        public int Stage { get; set; } = -1;
        public IEnumerable<SerDesVector3> MapPins { get; set; } = new List<SerDesVector3>();

        public SerDesVector3 StartPosition { get; set; } = new SerDesVector3(0, 0, 0);

        public static RaceConfig ToRaceConfig(string filePath)
        {
            using var fileStream = new FileStream(filePath, FileMode.Open);
            using var sr = new StreamReader(fileStream, Encoding.UTF8);
            using var reader = new StreamReader(fileStream);

            return JsonSerializer.Deserialize<RaceConfig>(reader.ReadToEnd());
        }
    }
}
