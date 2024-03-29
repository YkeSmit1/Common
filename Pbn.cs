﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Common
{
    [PublicAPI]
    public class Pbn
    {
        public List<BoardDto> Boards { get; set; } = [];

        public void Load(string filePath)
        {
            Boards.Clear();
            var sb = new StringBuilder();
            using var fileStream = File.OpenRead(filePath);
            using var streamReader = new StreamReader(fileStream);
            while (streamReader.ReadLine() is { } line)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    if (!string.IsNullOrWhiteSpace(sb.ToString()))
                        Boards.Add(BoardDto.FromString(sb.ToString()));
                    sb.Clear();
                }
                else if (line[0] != '%')
                    sb.AppendLine(line);
            }
            if (!string.IsNullOrWhiteSpace(sb.ToString()))
                Boards.Add(BoardDto.FromString(sb.ToString()));
        }
        public void Save(string filePath)
        {
            File.Delete(filePath);
            using var fileStream = File.OpenWrite(filePath);
            using var streamWriter = new StreamWriter(fileStream);
            foreach (var board in Boards)
            {
                streamWriter.Write(board.ToString());
                streamWriter.Write(Environment.NewLine);
            }
        }

        public async Task LoadAsync(string filePath)
        {
            Boards.Clear();
            var sb = new StringBuilder();
            await using var fileStream = File.OpenRead(filePath);
            using var streamReader = new StreamReader(fileStream);
            while (await streamReader.ReadLineAsync() is { } line)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    if (!string.IsNullOrWhiteSpace(sb.ToString()))
                        Boards.Add(BoardDto.FromString(sb.ToString()));
                    sb.Clear();
                }
                else if (line[0] != '%')
                    sb.AppendLine(line);
            }
            if (!string.IsNullOrWhiteSpace(sb.ToString()))
                Boards.Add(BoardDto.FromString(sb.ToString()));
        }
        public async Task SaveAsync(string filePath)
        {
            File.Delete(filePath);
            await using var fileStream = File.OpenWrite(filePath);
            await using var streamWriter = new StreamWriter(fileStream);
            foreach (var board in Boards)
            {
                await streamWriter.WriteAsync(board.ToString());
                await streamWriter.WriteAsync(Environment.NewLine);
            }
        }

    }
}
