namespace OpenCvMajong;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class Int2DArrayConverter : JsonConverter<int[,]>
{
    public override int[,] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jagged = JsonSerializer.Deserialize<int[][]>(ref reader, options);
        int rows = jagged.Length;
        int cols = jagged[0].Length;
        var result = new int[rows, cols];
        for (int i = 0; i < rows; i++)
        for (int j = 0; j < cols; j++)
            result[i, j] = jagged[i][j];
        return result;
    }

    public override void Write(Utf8JsonWriter writer, int[,] value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        for (int i = 0; i < value.GetLength(0); i++)
        {
            writer.WriteStartArray();
            for (int j = 0; j < value.GetLength(1); j++)
                writer.WriteNumberValue(value[i, j]);
            writer.WriteEndArray();
        }
        writer.WriteEndArray();
    }
}
